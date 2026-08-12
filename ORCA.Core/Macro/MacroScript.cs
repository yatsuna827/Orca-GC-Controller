using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArduinoAPI;
using ORCA.Plugin;

namespace ORCA.Core.Macro
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
            public Context(MacroScript parent)
            {
                _parent = parent;
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

            var macro = new MacroScript(commands, context.GetHitPlan());

            return macro;
        }

        private MacroScript(IEnumerable<(int Line, MacroCommand Command)> commands, (int, int)[] hitPlan)
        {
            this._commands = commands.Select((_, i) => (i, _.Line, _.Command));
            this._hitPlan = hitPlan;
        }

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
            if (_hitIndex < 0 || _hitIndex >= _hitPlan.Length) return null;

            var (label, frame) = _hitPlan[_hitIndex];
            var remain = frame - (int)(frameTimers[label].ElapsedMilliseconds * 59.7275 / 1000);

            return remain;
        }
        private int _hitIndex = -1;
        private readonly (int Label, int Frame)[] _hitPlan;

        public Task RunOnceAsync(IWritable port, CancellationToken token)
        {
            return Task.Run(() =>
            {
                var context = new Context(this);

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

        public Task RunLoopAsync(IWritable port, CancellationToken token, int times = -1)
        {
            return Task.Run(() =>
            {
                var context = new Context(this);

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
