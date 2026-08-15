using System;
using System.IO.Ports;
using System.Linq;
using ORCA.Core;

namespace ORCA.Headless
{
    /// <summary>
    /// Writeされたデータをシリアルポートに流すIPort実装
    /// </summary>
    class SerialControllerPort : IPort
    {
        private SerialPort _port;

        public void Write(byte[] buffer, int offset, int count)
            => _port?.Write(buffer, offset, count);

        public void Open(string portName, bool rts, bool dtr)
        {
            if (IsOpen) return;

            _port = new SerialPort(portName, 4800) { RtsEnable = rts, DtrEnable = dtr };
            _port.Open();
        }

        public bool IsOpen => _port?.IsOpen ?? false;

        public void Close() => _port?.Close();
    }

    /// <summary>
    /// Writeされたデータを標準出力に流すIPort実装
    /// </summary>
    class ConsolePort : IPort
    {
        public void Write(byte[] buffer, int offset, int count)
            => Console.WriteLine(string.Join(" ", buffer.Skip(offset).Take(count).Select(_ => $"{_:X2}")));

        public void Open(string portName, bool rts, bool dtr) => IsOpen = true;

        public bool IsOpen { get; private set; }

        public void Close() => IsOpen = false;
    }

    class NullPort : IPort
    {
        public void Write(byte[] buffer, int offset, int count) { }

        public void Open(string portName, bool rts, bool dtr) => IsOpen = true;

        public bool IsOpen { get; private set; }

        public void Close() => IsOpen = false;
    }
}
