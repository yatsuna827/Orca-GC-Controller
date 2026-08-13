using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCController
{
    static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public static bool IsKeyPress(System.Windows.Forms.Keys keyCode)
        {
            return GetKeyState((int)keyCode) < 0;
        }
    }
}
