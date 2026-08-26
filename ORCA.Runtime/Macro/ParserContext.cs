using System;
using System.Collections.Generic;
using System.Linq;
using ORCA.Core;

namespace ORCA.Runtime.Macro
{
    class ParserContext : IMacroParserContext
    {
        public int CurrentLine { get; set; }

        private readonly bool[] _timerStarted = new bool[10];
        private readonly List<(int Label, MacroArg Frame, MacroArg Correct)> _hitPlan = new List<(int, MacroArg, MacroArg)>();
        private readonly List<MacroParameter> _parameters = new List<MacroParameter>();

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

        public void AddHitPlan(int label, MacroArg frame, MacroArg correct = default)
            => _hitPlan.Add((label, frame, correct));

        public (int Label, MacroArg Frame, MacroArg Correct)[] GetHitPlan() => _hitPlan.ToArray();

        public void DeclareParameter(string name, int? defaultValue, bool allowsNegative)
        {
            var index = _parameters.FindIndex(_ => _.Name == name);
            if (index < 0)
            {
                _parameters.Add(new MacroParameter(name, defaultValue, allowsNegative));
                return;
            }

            var declared = _parameters[index];
            if (declared.DefaultValue != defaultValue)
                throw new Exception($"[{CurrentLine + 1}行目] パラメータ{{{name}}}のデフォルト値が宣言ごとに異なります");

            // 負の値が許されない位置に1か所でも出現していれば、それに揃える
            if (declared.AllowsNegative && !allowsNegative)
                _parameters[index] = new MacroParameter(name, defaultValue, false);
        }

        public MacroParameter[] GetParameters() => _parameters.ToArray();
    }
}
