using ArduinoAPI;

namespace ORCA.Core
{
    public interface IPort : IWritable
    {
        void Open(string portName, bool rts, bool dtr);
        bool IsOpen { get; }
        void Close();
    }
}
