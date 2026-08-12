using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using ORCA.Core;

namespace ORCA.Headless
{
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

    class DryRunPort : IPort
    {
        private StringBuilder _logger;

        public void Write(byte[] buffer, int offset, int count)
            => _logger.AppendLine($"{string.Join(" ", buffer.Select(_ => $"{_:X2}"))}");

        public void Open(string portName, bool rts, bool dtr) => _logger = new StringBuilder();

        public bool IsOpen => _logger != null;

        public void Close()
        {
            File.WriteAllText("./log.txt", _logger.ToString());
            _logger = null;
        }
    }
}
