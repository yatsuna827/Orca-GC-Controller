using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArduinoAPI;
using ORCA.Core;

namespace ORCA.Runtime.Macro
{
    public class MacroScript
    {
        class Context : IMacroContext
        {
            public void StartTimer()
                => _waitTimer.Restart();
            public void StartTimer(int label)
            {
                _parent.frameTimers[label].Restart();
                if (_parent._hitIndex < 0) _parent._hitIndex = 0;
            }

            public bool Wait(int duration, in CancellationToken token, bool withRestart = true)
                => _waitTimer.CancelableWait(duration, token, withRestart);
            public bool Wait(int label, int duration, in CancellationToken token, bool withRestart = true)
                => _parent.frameTimers[label].CancelableWait(duration, token, withRestart);

            public void GetNextHitIndex() => _parent._hitIndex++;

            private readonly IReadOnlyDictionary<string, int> _arguments;
            public int? GetArgument(string name)
                => _arguments.TryGetValue(name, out var value) ? value : null as int?;

            private readonly Dictionary<string, int> _intContext = new Dictionary<string, int>();
            public int? GetIntContext(string key) => _intContext.ContainsKey(key) ? _intContext[key] : null as int?;
            public void SetIntContext(string key, int value) => _intContext[key] = value;

            private readonly Dictionary<string, string> _stringContext = new Dictionary<string, string>();
            public string GetStringContext(string key) => _stringContext.ContainsKey(key) ? _stringContext[key] : null;
            public void SetStringContext(string key, string value) => _stringContext[key] = value;

            private readonly Dictionary<string, object> _objectContext = new Dictionary<string, object>();
            public object GetObjectContext(string key) => _objectContext.ContainsKey(key) ? _objectContext[key] : null;
            public void SetObjectContext(string key, object value) => _objectContext[key] = value;

            private readonly MacroScript _parent;
            private readonly Stopwatch _waitTimer = new Stopwatch();
            public Context(MacroScript parent, IReadOnlyDictionary<string, int> arguments)
            {
                _parent = parent;
                _arguments = arguments;
            }
        }

        public static Dictionary<string, IMacroCommandParser<MacroCommand>> GetDefaultParsers() => new Dictionary<string, IMacroCommandParser<MacroCommand>>()
        {
            { "Press", new PressCommandParser() },
            { "Wait",  new WaitCommandParser() },
            { "Start", new StartCommandParser() },
            { "Hit",   new HitCommandParser() },
        };

        public static MacroScript Compile(string[] macroLines, Dictionary<string, IMacroCommandParser<MacroCommand>> parsers)
        {
            var context = new ParserContext();
            var commands = new List<(int Line, MacroCommand Command)>();
            for (int i = 0; i < macroLines.Length; i++)
            {
                context.CurrentLine = i;

                var line = macroLines[i];
                if (line.Length == 0) continue; // 空行.
                if (line[0] == '#') continue; // コメント行.

                var args = line.Replace(", ", ",").Split();
                var commandName = args[0];
                if (!parsers.ContainsKey(commandName))
                    throw new Exception($"[{i + 1}行目] コマンド名が不正です: {commandName}");

                // 先頭を除去する.
                args = args.Skip(1).ToArray();

                var command = parsers[commandName].Parse(args, context, out var error);
                if (command is null)
                    throw new Exception(error);

                commands.Add((i, command));
            }

            if (commands.Count == 0) throw new Exception("有効なコマンドがありませんでした");

            var macro = new MacroScript(commands, context.GetHitPlan(), context.GetParameters());

            return macro;
        }

        private MacroScript(
            IEnumerable<(int Line, MacroCommand Command)> commands,
            (int, MacroArg, MacroArg)[] hitPlan,
            MacroParameter[] parameters)
        {
            this._commands = commands.Select((_, i) => (i, _.Line, _.Command));
            this._hitPlan = hitPlan;
            this.Parameters = parameters;
        }

        public IReadOnlyList<MacroParameter> Parameters { get; }

        private readonly IEnumerable<(int Index, int Line, MacroCommand Command)> _commands;
        private readonly Stopwatch[] frameTimers = Enumerable.Range(0, 10).Select(_ => new Stopwatch()).ToArray();

        // 現在実行中のコマンド
        public int CurrentCommandIndex { get; private set; } = -1;

        // 現在実行中の行, 空行やコメント行があるのでCurrentCommandIndexとズレる
        public int CurrentLine { get; private set; } = -1;

        // 現在のループ回数
        public int CurrentLoopIndex { get; private set; } = -1;

        // 次のHitまでの残り時間
        // Startされる前や全Hit消化後はnullが返る
        public int? GetRemainingFrame()
        {
            var hitPlan = _resolvedHitPlan;
            if (hitPlan is null || _hitIndex < 0 || _hitIndex >= hitPlan.Length) return null;

            var (label, frame) = hitPlan[_hitIndex];
            var remain = frame - (int)(frameTimers[label].ElapsedMilliseconds * 59.7275 / 1000);

            return remain;
        }
        private int _hitIndex = -1;
        private readonly (int Label, MacroArg Frame, MacroArg Correct)[] _hitPlan;

        private (int Label, int Frame)[] _resolvedHitPlan;

        private static string Format(IEnumerable<string> names)
            => string.Join(", ", names.Select(_ => $"{{{_}}}"));

        private Context CreateContext(IReadOnlyDictionary<string, int> arguments)
        {
            var resolved = new Dictionary<string, int>();
            var missing = new List<string>();
            var negative = new List<string>();
            foreach (var parameter in Parameters)
            {
                int value;
                if (arguments != null && arguments.TryGetValue(parameter.Name, out var passed))
                    value = passed;
                else if (parameter.DefaultValue.HasValue)
                    value = parameter.DefaultValue.Value;
                else
                {
                    missing.Add(parameter.Name);
                    continue;
                }

                if (!parameter.AllowsNegative && value < 0)
                {
                    negative.Add(parameter.Name);
                    continue;
                }

                resolved[parameter.Name] = value;
            }

            var declared = new HashSet<string>(Parameters.Select(_ => _.Name));
            var unknown = arguments is null
                ? new List<string>()
                : arguments.Keys.Where(_ => !declared.Contains(_)).ToList();

            var errors = new List<string>();
            if (missing.Count > 0) errors.Add($"パラメータ{Format(missing)}に値が指定されていません");
            if (negative.Count > 0) errors.Add($"パラメータ{Format(negative)}は負でない数値である必要があります");
            if (unknown.Count > 0) errors.Add($"パラメータ{Format(unknown)}はマクロで使われていません");
            // NOTE: ArgumentExceptionにparamNameを渡すと、メッセージ末尾に(Parameter '...')が表示されてしまうため、渡してはいけない
            if (errors.Count > 0) throw new ArgumentException(string.Join("; ", errors));

            var context = new Context(this, resolved);
            _resolvedHitPlan = _hitPlan
                .Select(_ => (_.Label, Frame: HitCommand.ResolveFrame(_.Frame, _.Correct, context)))
                .ToArray();

            return context;
        }

        public Task RunOnceAsync(IWritable port, CancellationToken token, IReadOnlyDictionary<string, int> arguments = null)
        {
            var context = CreateContext(arguments);

            return Task.Run(() =>
            {
                _hitIndex = -1;
                foreach (var (i, line, command) in _commands)
                {
                    if (token.IsCancellationRequested) break;

                    (CurrentCommandIndex, CurrentLine) = (i, line);
                    command.Execute(port, token, context);
                }
                _hitIndex = CurrentCommandIndex = -1;
            }, token);
        }

        public Task RunLoopAsync(IWritable port, CancellationToken token, int times = -1, IReadOnlyDictionary<string, int> arguments = null)
        {
            var context = CreateContext(arguments);

            return Task.Run(() =>
            {
                _hitIndex = -1;
                CurrentLoopIndex = times >= 0 ? 0 : -1;
                while (!token.IsCancellationRequested)
                {
                    foreach (var (i, line, command) in _commands)
                    {
                        if (token.IsCancellationRequested) break;

                        (CurrentCommandIndex, CurrentLine) = (i, line);
                        command.Execute(port, token, context);
                    }
                    _hitIndex = CurrentCommandIndex = -1;
                    if (CurrentLoopIndex != -1)
                    {
                        if (++CurrentLoopIndex >= times) break;
                    }
                }
                _hitIndex = CurrentCommandIndex = -1;
            }, token);
        }
    }
}
