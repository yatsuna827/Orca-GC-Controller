using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArduinoAPI;
using ORCA.Core;
using static ORCA.Core.Utils;

namespace ORCA.Runtime.Macro
{
    class PressCommandParser : IMacroCommandParser<PressCommand>
    {
        private static readonly char[] optionCharactors = new char[] { 'd', 'i', 's' };

        private static PressCommand ReturnError(int line, string message, out string o)
        {
            o = $"[{line + 1}行目] Pressコマンド {message}";
            return null;
        }

        public PressCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            if (args.Length == 0 || args.Length > 4)
                return ReturnError(context.CurrentLine, "引数の数が不正です", out errorMessage);

            // 第1引数はボタン系列指定.
            var buttonSymbols = args[0].Split(',');
            if (buttonSymbols.Length == 0)
                return ReturnError(context.CurrentLine, "第1引数(ボタン指定)が空です", out errorMessage);

            var buttonInputs = new List<ControllerInput>();
            foreach (var symbol in buttonSymbols)
            {
                var s = symbol.Split('+');
                if (s.Any(_ => !buttonMap.ContainsKey(_)))
                    return ReturnError(context.CurrentLine, "第1引数(ボタン指定)に不正な文字が含まれています", out errorMessage);

                var b = (ControllerInput)0;
                foreach (var _s in s)
                    b |= buttonMap[_s];

                buttonInputs.Add(b);
            }

            // 許可されているオプション引数は-d, -i, -s
            var options = optionCharactors.ToDictionary(_ => _, _ => "");

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Length < 4 || !Regex.IsMatch(args[i].Substring(0, 3), $"-[{new string(optionCharactors)}]="))
                    return ReturnError(context.CurrentLine, "オプション指定子が不正です", out errorMessage);

                if (options[args[i][1]] != "")
                    return ReturnError(context.CurrentLine, "オプションが重複しています", out errorMessage);

                options[args[i][1]] = args[i].Substring(3);
            }

            // Duration
            MacroArg duration = 200;
            if (options['d'] != "")
            {
                if (!MacroArg.TryParse(options['d'], context, out duration))
                    return ReturnError(context.CurrentLine, "-dオプションは32bit符号あり整数に収まる負でない数値である必要があります", out errorMessage);
            }

            // Interval
            MacroArg interval = 0;
            if (options['i'] != "")
            {
                if (!MacroArg.TryParse(options['i'], context, out interval))
                    return ReturnError(context.CurrentLine, "-iオプションは32bit符号あり整数に収まる負でない数値である必要があります", out errorMessage);
            }

            // StartTimer
            var label = -1;
            if (options['s'] != "")
            {
                if (!int.TryParse(options['s'], out label) || options['s'].Length != 1)
                    return ReturnError(context.CurrentLine, "-sオプションは1桁の数字である必要があります", out errorMessage);

                if (context.TimerStarted(label))
                    return ReturnError(context.CurrentLine, $"タイマー{label}の開始命令が重複しています", out errorMessage);

                context.SetTimerStarted(label);
            }

            errorMessage = "";
            return new PressCommand(buttonInputs, duration, interval, label);
        }
    }

    class WaitCommandParser : IMacroCommandParser<WaitCommand>
    {
        private static WaitCommand ReturnError(int line, string message, out string o)
        {
            o = $"[{line + 1}行目] Waitコマンド {message}";
            return null;
        }

        public WaitCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            if (args.Length != 1)
                return ReturnError(context.CurrentLine, "引数の数が不正です", out errorMessage);

            // 第1引数はボタン系列指定.
            if (!MacroArg.TryParse(args[0], context, out var duration))
                return ReturnError(context.CurrentLine, "第1引数(待機時間[ms]指定)は32bit符号あり整数に収まる負でない数値である必要があります", out errorMessage);

            errorMessage = "";
            return new WaitCommand(duration);
        }
    }

    class StartCommandParser : IMacroCommandParser<StartCommand>
    {
        private static StartCommand ReturnError(int line, string message, out string o)
        {
            o = $"[{line + 1}行目] Startコマンド {message}";
            return null;
        }

        public StartCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            if (args.Length > 1)
                return ReturnError(context.CurrentLine, "引数の数が不正です", out errorMessage);

            var label = 0;
            if (args.Length == 1)
            {
                if (!args[0].StartsWith("-s="))
                    return ReturnError(context.CurrentLine, "オプション指定子が不正です", out errorMessage);

                var val = args[0].Substring(3);
                if (val.Length != 1 || !int.TryParse(val, out label))
                    return ReturnError(context.CurrentLine, "-sオプションは1桁の数字である必要があります", out errorMessage);
            }

            // タイマーの重複起動を弾く.
            if (context.TimerStarted(label))
                return ReturnError(context.CurrentLine, $"タイマー{label}の開始命令が重複しています", out errorMessage);

            context.SetTimerStarted(label);

            errorMessage = "";
            return new StartCommand(label);
        }
    }

    class HitCommandParser : IMacroCommandParser<HitCommand>
    {
        private static readonly char[] optionCharactors = new char[] { 'l', 'd', 's', 'c' };

        private static HitCommand ReturnError(int line, string message, out string o)
        {
            o = $"[{line + 1}行目] Hitコマンド {message}";
            return null;
        }

        public HitCommand Parse(string[] args, IMacroParserContext context, out string errorMessage)
        {
            if (args.Length < 2 || args.Length > 6)
                return ReturnError(context.CurrentLine, "引数の数が不正です", out errorMessage);

            // 第1引数はボタン指定.
            if (!buttonMap.ContainsKey(args[0]))
                return ReturnError(context.CurrentLine, "第1引数(ボタン指定)に不正な文字が含まれています", out errorMessage);

            var button = buttonMap[args[0]];

            // 第2引数はフレーム.
            if (!MacroArg.TryParse(args[1], context, out var frame))
                return ReturnError(context.CurrentLine, "第2引数(フレーム指定)は32bit符号あり整数に収まる負でない数値である必要があります", out errorMessage);

            // 許可されているオプション引数は-l, -d, -s, -c
            var options = optionCharactors.ToDictionary(_ => _, _ => "");
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i].Length < 4 || !Regex.IsMatch(args[i].Substring(0, 3), $"-[{new string(optionCharactors)}]="))
                    return ReturnError(context.CurrentLine, "オプション指定子が不正です", out errorMessage);

                if (options[args[i][1]] != "")
                    return ReturnError(context.CurrentLine, "オプションが重複しています", out errorMessage);

                options[args[i][1]] = args[i].Substring(3);
            }

            // Duration
            MacroArg duration = 200;
            if (options['d'] != "")
            {
                if (!MacroArg.TryParse(options['d'], context, out duration))
                    return ReturnError(context.CurrentLine, "-dオプションは32bit符号あり整数に収まる負でない数値である必要があります", out errorMessage);
            }

            // Label
            var label = 0;
            if (options['l'] != "")
            {
                if (!int.TryParse(options['l'], out label) || options['l'].Length != 1)
                    return ReturnError(context.CurrentLine, "-lオプションは1桁の数字である必要があります", out errorMessage);
            }

            if (!context.TimerStarted(label))
                return ReturnError(context.CurrentLine, $"タイマー[{label}] Hitコマンドはタイマー起動より後に書かれる必要があります", out errorMessage);

            // Correct
            MacroArg correct = 0;
            if (options['c'] != "")
            {
                if (!MacroArg.TryParse(options['c'], context, out correct, allowNegative: true))
                    return ReturnError(context.CurrentLine, "-cオプションは32bit符号あり整数に収まる数値である必要があります", out errorMessage);
            }

            // StartTimer
            var startLabel = -1;
            if (options['s'] != "")
            {
                if (!int.TryParse(options['s'], out startLabel) || options['s'].Length != 1)
                    return ReturnError(context.CurrentLine, "-sオプションは1桁の数字である必要があります", out errorMessage);

                // タイマーの重複起動を弾く.
                if (context.TimerStarted(startLabel))
                    return ReturnError(context.CurrentLine, $"タイマー{startLabel}の開始命令が重複しています", out errorMessage);

                context.SetTimerStarted(startLabel);
            }

            context.AddHitPlan(label, frame, correct);

            errorMessage = "";
            return new HitCommand(button, frame, correct, label, duration, startLabel);
        }
    }
}
