using System.Collections.Generic;
using System.Linq;
using ORCA.Core;

namespace ORCA.Runtime.Macro
{
    class ParserContext : IMacroParserContext
    {
        public int CurrentLine { get; set; }

        private readonly bool[] _timerStarted = new bool[10];
        private readonly List<(int Label, int Frame)> _hitPlan = new List<(int Label, int Frame)>();

        public bool TimerStarted(int label)
        {
            if (label < 0 || _timerStarted.Length <= label) return false;
            return _timerStarted[label];
        }

        public void SetTimerStarted(int label)
        {
            if (0 <= label && label < _timerStarted.Length)
                _timerStarted[label] = true;
        }

        public void AddHitPlan(int label, int frame)
            => _hitPlan.Add((label, frame));

        public (int Label, int Frame)[] GetHitPlan() => _hitPlan.ToArray();
    }
}
