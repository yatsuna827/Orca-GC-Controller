using System.Collections.Generic;
using System.Linq;
using ORCA.Core;

namespace ORCA.Headless.Tests
{
    class RecordingPort : IPort
    {
        public bool IsOpen { get; private set; }
        public void Open(string portName, bool rts, bool dtr) => IsOpen = true;
        public void Close() => IsOpen = false;

        private readonly List<string> _entries = [];
        private readonly object _lock = new();

        public void Write(byte[] buffer, int offset, int count)
        {
            var hex = string.Join(" ", buffer.Skip(offset).Take(count).Select(_ => $"{_:X2}"));

            lock (_lock) _entries.Add(hex);
        }

        public string[] HexLines
        {
            get { lock (_lock) return [.. _entries]; }
        }
    }
}
