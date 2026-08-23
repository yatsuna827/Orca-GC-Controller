using System.Diagnostics;
using System.Threading;

namespace ORCA.Core
{
    static class UtilExtensions
    {
        private const int SpinMargin = 20;

        internal static bool CancelableWait(this Stopwatch sw, int wait_ms, in CancellationToken token, bool withRestart = true)
        {
            if (withRestart) sw.Restart();

            // 待機完了の手前までは大雑把に待機し、直前だけwhileループにして精度を保証する

            var sleep = wait_ms - (int)sw.ElapsedMilliseconds - SpinMargin;
            if (sleep > 0 && token.WaitHandle.WaitOne(sleep)) return true;

            while (sw.ElapsedMilliseconds < wait_ms) if (token.IsCancellationRequested) return true;
            return false;
        }
    }
}
