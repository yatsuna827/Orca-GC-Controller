using System.Threading;

namespace ORCA.Core
{
    public interface IMacroContext
    {
        void StartTimer();
        void StartTimer(int label);
        bool Wait(int duration, in CancellationToken token, bool withRestart = true);
        bool Wait(int label, int duration, in CancellationToken token, bool withRestart = true);
        void GetNextHitIndex();

        /// <summary>
        /// コマンド間で保持される変数です.
        /// </summary>
        string GetStringContext(string key);
        void SetStringContext(string key, string value);

        /// <summary>
        /// コマンド間で保持される変数です.
        /// </summary>
        int? GetIntContext(string key);
        void SetIntContext(string key, int value);

        /// <summary>
        /// コマンド間で保持される変数です.
        /// </summary>
        object GetObjectContext(string key);
        void SetObjectContext(string key, object value);
    }
}
