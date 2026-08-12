using System.Diagnostics;
using System.Threading;

namespace ORCA.Core
{
    static class UtilExtensions
    {
        internal static bool CancelableWait(this Stopwatch sw, int wait_ms, in CancellationToken token, bool withRestart = true)
        {
            if (withRestart) sw.Restart();
            while (sw.ElapsedMilliseconds < wait_ms) if (token.IsCancellationRequested) return true;
            return false;
        }
    }
}
