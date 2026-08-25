using System.Collections.Generic;
using System.Threading;
using ArduinoAPI;
using ORCA.Core;

namespace ORCA.Tests
{
    // 引数を使うプラグインコマンド
    class RecordArgCommand(MacroArg arg, List<int> resolved) : MacroCommand
    {
        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
            => resolved.Add(arg.Resolve(context));
    }

    class RecordArgParser : IMacroCommandParser<MacroCommand>
    {
        public List<int> Resolved { get; } = [];

        public MacroCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            if (args.Length != 1 || !MacroArg.TryParse(args[0], context, out var arg))
            {
                errorMessage = $"[{context.CurrentLine + 1}行目] Recordコマンド 引数が不正です";
                return null;
            }

            errorMessage = "";
            return new RecordArgCommand(arg, Resolved);
        }
    }
}
