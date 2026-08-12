using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArduinoAPI;
using ORCA.Plugin;
using PokemonPRNG.LCG32.StandardLCG;

namespace SamplePlugin
{
    public class SampleCommand : MacroCommand
    {
        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            var seed = (uint)(context.GetIntContext("SampleCommand seed") ?? 0);
            var rand = seed.GetRand();
            System.Diagnostics.Debug.Print($"{rand:X}");
            context.SetIntContext("SampleCommand seed", (int)seed);

            port.PressButton(ControllerInput.Z);
            context.Wait(2000, token);
            port.PressButton(ControllerInput.Left);
            context.Wait(1000, token);
            port.PressButton(ControllerInput.A);
            context.Wait(1000, token);
            port.PressButton(ControllerInput.A);
            context.Wait(1000, token);
            port.PressButton(ControllerInput.Start | ControllerInput.Y, 4000);
        }
    }

    [MacroCommand(commandName: "Reset")]
    public class SampleCommandParser : IMacroCommandParser<SampleCommand>
    {
        public SampleCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            errorMessage = "";
            return new SampleCommand();
        }
    }

    // 型引数を残しているのでダメ
    [MacroCommand(commandName: "Bad1")]
    public class BadCommandParser1<T> : IMacroCommandParser<SampleCommand>
    {
        public SampleCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            errorMessage = "";
            return new SampleCommand();
        }
    }

    // 具象クラスじゃないのでダメ
    [MacroCommand(commandName: "Bad2")]
    public abstract class BadCommandParser2 : IMacroCommandParser<SampleCommand>
    {
        public SampleCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            errorMessage = "";
            return new SampleCommand();
        }
    }

    // MacroCommandAttributeを付与していないのでダメ
    public class BadCommandParser3 : IMacroCommandParser<SampleCommand>
    {
        public SampleCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            errorMessage = "";
            return new SampleCommand();
        }
    }
}
