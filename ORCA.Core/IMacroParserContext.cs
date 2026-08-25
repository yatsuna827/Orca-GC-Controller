namespace ORCA.Core
{
    public interface IMacroParserContext
    {
        /// <summary>
        /// 現在読んでいるマクロの行数です.
        /// </summary>
        int CurrentLine { get; }

        /// <summary>
        /// 指定されたlabelのタイマーが起動済みかどうかを取得します.
        /// </summary>
        bool TimerStarted(int label);

        /// <summary>
        /// 指定されたlabelのタイマーを起動したことを表明します.
        /// </summary>
        void SetTimerStarted(int label);

        /// <summary>
        /// 指定されたlabelのタイマーの(frame + correct)FにHitを計画していることを表明します.
        /// </summary>
        void AddHitPlan(int label, MacroArg frame, MacroArg correct = default);

        /// <summary>
        /// マクロが受け取る引数を宣言します.
        /// </summary>
        void DeclareParameter(string name, int? defaultValue, bool allowsNegative);
    }
}
