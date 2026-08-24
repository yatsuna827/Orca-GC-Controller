using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ORCA.Runtime;

namespace GCController
{
    static class Program
    {
        public const bool IsDebug =
#if DEBUG
            true;
#else
    false;
#endif

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(IsDebug ? new DebugPort() : new MyPort() as IPort));
        }
    }
}
