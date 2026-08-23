using System.Collections.Generic;
using System.Linq;
using ORCA.Core;

namespace ORCA.Headless.Tests
{
    class RecordingPort : IPort
    {
        public const string Closed = "CLOSE";

        public bool IsOpen { get; private set; }
        public void Open(string portName, bool rts, bool dtr) => IsOpen = true;

        public void Close()
        {
            IsOpen = false;

            lock (_lock) _log.Add(Closed);
        }

        private readonly List<string> _log = [];
        private readonly object _lock = new();

        public void Write(byte[] buffer, int offset, int count)
        {
            var hex = string.Join(" ", buffer.Skip(offset).Take(count).Select(_ => $"{_:X2}"));

            lock (_lock) _log.Add(hex);
        }

        public string[] Log
        {
            get { lock (_lock) return [.. _log]; }
        }

        public string[] HexLines
        {
            get { lock (_lock) return [.. _log.Where(e => e != Closed)]; }
        }
    }
}
