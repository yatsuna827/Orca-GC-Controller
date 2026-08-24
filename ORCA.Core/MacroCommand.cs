using System.Threading;
using ArduinoAPI;

namespace ORCA.Core
{
    public abstract class MacroCommand
    {
        public abstract void Execute(IWritable port, in CancellationToken token, IMacroContext context);
    }

    public interface IMacroCommandParser<out T>
        where T : MacroCommand
    {
        /// <summary>
        /// 解釈に失敗したらnullを返します.
        /// </summary>
        T Parse(string[] args, IMacroParserContext context, out string errorMessage);
    }

    public sealed class MacroCommandAttribute : System.Attribute
    {
        /// <summary>
        /// コマンド名として使う文字列です.
        /// 既定のコマンドである Press, Wait, Start, Hit は使うことはできません.
        /// </summary>
        public string CommandName { get; set; }

        public MacroCommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}
