using System.Diagnostics;
using System.IO.Ports;

namespace ArduinoAPI
{
    public interface IWritable
    {
        void Write(byte[] buffer, int offset, int count);
    }
    /// <summary>
    /// 押下されるコントローラのボタンを表す列挙子.
    /// 論理和(OR)で繋げると複数キーが同時に押下されている状態を表せる.
    /// </summary>
    public enum ControllerInput : ushort
    {
        KeysAllUp = 0x8080,

        A = 0x8080 | 1,
        B = 0x8080 | (1 << 1),
        X = 0x8080 | (1 << 2),
        Y = 0x8080 | (1 << 3),
        L = 0x8080 | (1 << 4),
        R = 0x8080 | (1 << 5),

        Z = 0x8080 | (1 << 8),
        Start = 0x8080 | (1 << 9),
        Left = 0x8080 | (1 << 10),
        Right = 0x8080 | (1 << 11),
        Up = 0x8080 | (1 << 12),
        Down = 0x8080 | (1 << 13),
    }
    public static class GCControllerExtension
    {
        private const byte HEAD = 0x80;
        private static readonly Stopwatch sw = new Stopwatch();
        private static readonly byte[] keysAllUp = new byte[] { 0x80, 0x80, 0x80, 0 };
        /// <summary>
        /// 指定されたボタンを一定時間だけ押下します.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="button"></param>
        /// <param name="wait_ms"></param>
        public static void PressButton(this IWritable port, ControllerInput button, int wait_ms = 200)
        {
            var buttonState1 = (byte)((ushort)button & 0xFF);
            var buttonState2 = (byte)((ushort)button >> 8);
            var sendKeys = new byte[4] { HEAD, buttonState1, buttonState2, 0 };

            port?.Write(sendKeys, 0, 3);
            sw.Restart();
            while (sw.ElapsedMilliseconds < wait_ms) { }

            port?.Write(keysAllUp, 0, 3);
        }

        /// <summary>
        /// 指定されたボタンのみ押下した状態に変更します.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="buttonsState"></param>
        public static void SetButtonState(this IWritable port, ControllerInput buttonsState)
        {
            var buttonState1 = (byte)((ushort)buttonsState & 0xFF);
            var buttonState2 = (byte)((ushort)buttonsState >> 8);
            var sendKeys = new byte[3] { HEAD, buttonState1, buttonState2 };

            port?.Write(sendKeys, 0, 3);
        }
    }
}
