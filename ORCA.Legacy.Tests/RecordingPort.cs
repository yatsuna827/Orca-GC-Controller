using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ArduinoAPI;

namespace ORCA.Legacy.Tests
{
    // Write(buffer, offset, count)のcountを尊重するので, 実際に線に乗る3バイトだけが残る.
    class RecordingPort : IWritable
    {
        private readonly List<(string Hex, long ElapsedMs)> _entries = new List<(string, long)>();
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly object _lock = new object();

        public void Write(byte[] buffer, int offset, int count)
        {
            var hex = string.Join(" ", buffer.Skip(offset).Take(count).Select(_ => $"{_:X2}"));

            lock (_lock) _entries.Add((hex, _stopwatch.ElapsedMilliseconds));
        }

        public (string Hex, long ElapsedMs)[] Entries
        {
            get { lock (_lock) return _entries.ToArray(); }
        }

        public string[] HexLines => Entries.Select(_ => _.Hex).ToArray();
    }
}
