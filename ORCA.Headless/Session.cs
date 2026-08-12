using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArduinoAPI;
using ORCA.Core;
using ORCA.Core.Macro;

namespace ORCA.Headless
{
    public class Session(Action<string> output, Func<IPort> newPort = null)
    {
        private readonly Action<string> _output = output;
        private readonly Func<IPort> _newPort = newPort ?? (() => new SerialControllerPort());

        private IPort _port;
        private MacroScript _script;
        private Task _task;
        private CancellationTokenSource _cts;

        public bool Running { get; private set; } = true;
        public bool IsMacroRunning => _task != null && !_task.IsCompleted;

        public void Execute(string line)
        {
            var args = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0) return;

            try
            {
                switch (args[0])
                {
                    case "ports": Ports(); break;
                    case "connect": Connect(args); break;
                    case "disconnect": Disconnect(); break;
                    case "load": Load(args); break;
                    case "run": Run(args); break;
                    case "cancel": Cancel(); break;
                    case "status": Status(); break;
                    case "quit": Quit(); break;
                    default: _output($"未知のコマンドです: {args[0]}"); break;
                }
            }
            catch (Exception ex)
            {
                _output($"エラー: {ex.Message}");
            }
        }

        private void Ports()
        {
            var names = SerialPort.GetPortNames();
            if (names.Length == 0)
            {
                _output("接続可能なポートが見つかりませんでした");
                return;
            }

            foreach (var name in names) _output(name);
        }

        private void Connect(string[] args)
        {
            if (_port != null && _port.IsOpen)
            {
                _output("すでに接続しています");
                return;
            }
            if (args.Length < 2)
            {
                _output("使い方: connect <ポート名> [--no-rts] [--no-dtr]");
                return;
            }

            var port = _newPort();
            try
            {
                port.Open(args[1], !args.Contains("--no-rts"), !args.Contains("--no-dtr"));
            }
            catch (Exception ex)
            {
                _output($"{args[1]} を開けませんでした: {ex.Message}");
                return;
            }

            _port = port;
            _output($"{args[1]} に接続しました");
        }

        private void Disconnect()
        {
            if (_port is null || !_port.IsOpen)
            {
                _output("接続していません");
                return;
            }

            StopMacro();
            _port.Close();
            _output("切断しました");
        }

        private void Load(string[] args)
        {
            if (args.Length < 2)
            {
                _output("使い方: load <パス>");
                return;
            }

            var text = File.ReadAllText(string.Join(" ", args.Skip(1)));
            var lines = text.Replace("\r\n", "\n").Split(['\n', '\r']);

            var parsers = MacroScript.GetDefaultParsers();
            foreach (var (commandName, parser) in PluginLoader.Load(PluginLoader.DefaultDirectory))
            {
                if (parsers.ContainsKey(commandName)) continue;

                parsers.Add(commandName, parser);
                _output($"Plugin {commandName}コマンドを読み込みました");
            }

            _script = MacroScript.Compile(lines, parsers);
            _output("コンパイルに成功しました");
        }

        private void Run(string[] args)
        {
            if (_port is null || !_port.IsOpen)
            {
                _output("ポートが開放されていません");
                return;
            }
            if (_script is null)
            {
                _output("マクロが読み込まれていません");
                return;
            }
            if (IsMacroRunning)
            {
                _output("すでに実行中です");
                return;
            }

            var loop = ParseLoop(args);
            _cts = new CancellationTokenSource();
            var cts = _cts;

            _task = loop.HasValue
                ? _script.RunLoopAsync(_port, cts.Token, loop.Value)
                : _script.RunOnceAsync(_port, cts.Token);

            _ = _task.ContinueWith(
                t => _output(cts.IsCancellationRequested ? "マクロが中断されました" : "マクロが終了しました"));

            _output("マクロを実行します");
        }

        private static int? ParseLoop(string[] args)
        {
            var i = Array.IndexOf(args, "--loop");
            if (i < 0) return null;
            if (i + 1 >= args.Length) return -1;

            return int.Parse(args[i + 1]);
        }

        private void Cancel()
        {
            if (!IsMacroRunning)
            {
                _output("実行中のマクロがありません");
                return;
            }

            StopMacro();
        }

        private void StopMacro()
        {
            if (!IsMacroRunning) return;

            _cts.Cancel();

            try { _task.Wait(); } catch (AggregateException) { }

            // 押したままになるのを防止するために全ボタンを解放する
            _port.SetButtonState(ControllerInput.KeysAllUp);
        }

        private void Status()
        {
            _output($"接続: {((_port?.IsOpen ?? false) ? "あり" : "なし")}");
            _output($"マクロ: {(_script is null ? "未読込" : "読込済")}");

            if (!IsMacroRunning)
            {
                _output("実行: なし");
                return;
            }

            _output($"実行: {_script.CurrentLine + 1}行目"
                + (_script.CurrentLoopIndex != -1 ? $" ({_script.CurrentLoopIndex + 1}回目)" : ""));

            var remaining = _script.GetRemainingFrame();
            if (!remaining.HasValue) return;

            var sec = remaining.Value / 60;
            _output(sec >= 0 ? $"次のHitまで {sec} [sec]" : "Not in Time !!!");
        }

        private void Quit()
        {
            StopMacro();
            _port?.Close();

            Running = false;
            _output("終了します");
        }
    }
}
