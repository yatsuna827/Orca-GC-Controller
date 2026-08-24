using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ORCA.Runtime;

namespace ORCA.Tests
{
    class RecordingPort : IPort
    {
        public bool IsOpen { get; private set; }
        public void Open(string portName, bool rts, bool dtr) => IsOpen = true;
        public void Close() => IsOpen = false;

        private readonly List<(string Hex, long ElapsedMs)> _entries = [];
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly object _lock = new();

        public void Write(byte[] buffer, int offset, int count)
        {
            var hex = string.Join(" ", buffer.Skip(offset).Take(count).Select(_ => $"{_:X2}"));

            lock (_lock) _entries.Add((hex, _stopwatch.ElapsedMilliseconds));
        }

        public (string Hex, long ElapsedMs)[] Entries
        {
            get { lock (_lock) return [.. _entries]; }
        }

        public string[] HexLines => [.. Entries.Select(_ => _.Hex)];
    }
}
