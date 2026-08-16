using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArduinoAPI;
using ORCA.Core;
using ORCA.Core.Macro;
using ORCA.Plugin;

namespace ORCA.Headless
{
    public class Service
    {
        private readonly Func<IPort> _newPort;
        private readonly Dictionary<string, IMacroCommandParser<MacroCommand>> _parsers;

        private readonly Lock _gate = new();

        private IPort _port;
        private string _defaultPortName;
        private Task _task;
        private CancellationTokenSource _cts;
        private MacroScript _script;
        private string[] _sourceLines;

        private readonly MacroHistory _history = new();

        public string[] PluginCommands { get; }
        public bool Running { get; private set; } = true;
        public bool HasRunningMacro => _task != null && !_task.IsCompleted;

        public Service(Func<IPort> newPort = null, string defaultPort = null)
        {
            _newPort = newPort ?? (() => new SerialControllerPort());
            _defaultPortName = defaultPort;

            _parsers = MacroScript.GetDefaultParsers();
            var plugins = new List<string>();
            foreach (var (commandName, parser) in PluginLoader.Load(PluginLoader.DefaultDirectory))
            {
                if (!_parsers.TryAdd(commandName, parser)) continue;

                plugins.Add(commandName);
            }
            PluginCommands = [.. plugins];
        }

        public Response Handle(Request request, Action<Progress> onProgress = null, CancellationToken clientGone = default)
        {
            var args = request.Args ?? [];
            try
            {
                return request.Command switch
                {
                    "ports" => Ports(),
                    "connect" => Connect(args),
                    "disconnect" => Disconnect(),
                    "run" => Run(args, request.Body, CancellationTokenSource.CreateLinkedTokenSource(clientGone), onProgress),
                    "rerun" => Rerun(args, CancellationTokenSource.CreateLinkedTokenSource(clientGone), onProgress),
                    "set-port" => SetPort(args),
                    "history" => History(),
                    "status" => Status(),
                    "shutdown" => Shutdown(),
                    _ => Response.Fail($"unknown command: {request.Command}"),
                };
            }
            catch (Exception ex)
            {
                return Response.Fail($"error: {ex.Message}");
            }
        }

        private static Response Ports()
        {
            var names = SerialPort.GetPortNames();
            string[] lines = names.Length == 0 ? ["no ports available"] : names;

            return Response.Text(lines) with { Data = names };
        }

        private Response Connect(string[] args)
        {
            lock (_gate)
            {
                if (_port != null && _port.IsOpen) return Response.Fail("already connected");

                var portName = args.Length > 0 && !args[0].StartsWith('-') ? args[0] : _defaultPortName;
                if (portName is null) return Response.Fail("usage: connect <port> [--no-rts] [--no-dtr]");

                var port = _newPort();
                try
                {
                    port.Open(portName, !args.Contains("--no-rts"), !args.Contains("--no-dtr"));
                }
                catch (Exception ex)
                {
                    return Response.Fail($"failed to open {portName}: {ex.Message}");
                }

                _port = port;
                _defaultPortName ??= portName;
                return Response.Text($"connected to {portName}");
            }
        }

        private Response SetPort(string[] args)
        {
            lock (_gate)
            {
                if (args.Length == 0) return Response.Fail("usage: set-port <port>");

                _defaultPortName = args[0];
                return Response.Text($"default port: {args[0]}");
            }
        }

        private Response Disconnect()
        {
            lock (_gate)
            {
                if (_port is null || !_port.IsOpen) return Response.Fail("not connected");

                StopMacro();
                _port.Close();
                return Response.Text("disconnected");
            }
        }

        private Response Run(string[] args, string body, CancellationTokenSource cts, Action<Progress> onProgress)
        {
            lock (_gate)
            {
                var dryRun = args.Contains("--dry-run");
                if (!dryRun && (_port is null || !_port.IsOpen)) return Response.Fail("not connected");
                if (HasRunningMacro) return Response.Fail("macro already running");
                if (body is null) return Response.Fail("usage: run <path> [--loop [count]]");

                var port = dryRun ? new NullPort() : _port;
                var sourceLines = body.Replace("\r\n", "\n").Split(['\n', '\r']);
                var entry = new MacroHistory.Entry(args.Length > 0 ? args[0] : "(unnamed)", dryRun, MacroScript.Compile(sourceLines, _parsers), sourceLines);
                
                _history.Remember(entry);

                _cts = cts;
                _script = entry.Script;
                _sourceLines = sourceLines;
                var loop = ParseLoop(args);
                _task = loop.HasValue
                    ? entry.Script.RunLoopAsync(port, _cts.Token, loop.Value)
                    : entry.Script.RunOnceAsync(port, _cts.Token);
            }

            return WaitForMacro(onProgress);
        }

        private Response Rerun(string[] args, CancellationTokenSource cts, Action<Progress> onProgress)
        {
            lock (_gate)
            {
                if (HasRunningMacro) return Response.Fail("macro already running");

                var index = 1;
                if (args.Length > 0 && !args[0].StartsWith('-') && !int.TryParse(args[0], out index))
                    return Response.Fail("usage: rerun [number] [--loop [count]]");
                if (_history.Count == 0) return Response.Fail("no history");
                if (index < 1 || index > _history.Count)
                    return Response.Fail($"only {_history.Count} entries in history");

                var entry = _history[index - 1];
                var dryRun = !args.Contains("--no-dry-run") && (args.Contains("--dry-run") || entry.DryRun);
                if (!dryRun && (_port is null || !_port.IsOpen)) return Response.Fail("not connected");

                _history.Remember(entry);

                var port = dryRun ? new NullPort() : _port;
                var loop = ParseLoop(args);
                _cts = cts;
                _script = entry.Script;
                _sourceLines = entry.SourceLines;
                _task = loop.HasValue
                    ? entry.Script.RunLoopAsync(port, _cts.Token, loop.Value)
                    : entry.Script.RunOnceAsync(port, _cts.Token);
            }

            return WaitForMacro(onProgress);
        }

        private Response WaitForMacro(Action<Progress> onProgress)
        {
            int lastLine = -1;
            while (true)
            {
                var current = _script.CurrentLine;
                if (current != lastLine && current >= 0 && current < _sourceLines.Length)
                {
                    lastLine = current;
                    onProgress?.Invoke(new Progress(current + 1, _sourceLines[current]));
                }

                try { if (_task.Wait(100)) break; }
                catch (AggregateException) { break; }
            }

            if (!_cts.IsCancellationRequested)
                return Response.Text("macro finished");

            ReleaseButtons();
            return Response.Text("macro cancelled");
        }

        private void StopMacro()
        {
            if (!HasRunningMacro) return;

            _cts.Cancel();
            try { _task.Wait(); } catch (AggregateException) { }

            ReleaseButtons();
        }

        private void ReleaseButtons()
        {
            lock (_gate)
            {
                _port?.SetButtonState(ControllerInput.KeysAllUp);
            }
        }

        private Response History()
        {
            lock (_gate)
            {
                if (_history.Count == 0) return Response.Fail("no history");

                var labels = _history.Entries.Select(e => e.Label).ToArray();
                var lines = _history.Entries.Select((e, i) => e.DryRun ? $"{i + 1}: {e.Label} (dry-run)" : $"{i + 1}: {e.Label}").ToArray();
                return Response.Text(lines) with { Data = labels };
            }
        }

        private Response Status()
        {
            lock (_gate)
            {
                return Response.Text(HasRunningMacro ? "running" : "idle");
            }
        }

        private Response Shutdown()
        {
            lock (_gate)
            {
                StopMacro();
                _port?.Close();

                Running = false;
                return Response.Text("shutting down");
            }
        }
    
        private static int? ParseLoop(string[] args)
        {
            var i = Array.IndexOf(args, "--loop");
            if (i < 0) return null;
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out var count)) return count;

            return -1;
        }
    
    }

    internal sealed class MacroHistory
    {
        private const int Limit = 10;
        private readonly List<Entry> _entries = [];

        public sealed record Entry(string Label, bool DryRun, MacroScript Script, string[] SourceLines)
        {
            public bool Equals(Entry other) => other is not null && Label == other.Label && DryRun == other.DryRun;
            public override int GetHashCode() => HashCode.Combine(Label, DryRun);
        }

        public int Count => _entries.Count;
        public Entry this[int index] => _entries[index];
        public IReadOnlyList<Entry> Entries => _entries;

        public void Remember(Entry entry)
        {
            _entries.Remove(entry);
            _entries.Insert(0, entry);
            if (_entries.Count > Limit) _entries.RemoveAt(_entries.Count - 1);
        }

    }
    
}
