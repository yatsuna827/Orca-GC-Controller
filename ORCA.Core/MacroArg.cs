using System;
using System.Text.RegularExpressions;

namespace ORCA.Core
{
    /// <summary>
    /// マクロコマンドが保持する整数の引数です.
    /// コンパイル時に確定するリテラル値か, 実行時に解決されるプレースホルダのいずれかを表します.
    /// </summary>
    public readonly struct MacroArg
    {
        // {name} と {name:default} を検出する正規表現
        private static readonly Regex placeholder = new Regex(@"^\{([A-Za-z_][A-Za-z0-9_]*)(?::(-?[0-9]+))?\}$");

        public string Name { get; }

        private readonly int _value;

        private MacroArg(string name, int value)
        {
            Name = name;
            _value = value;
        }

        public static implicit operator MacroArg(int value) => new MacroArg(null, value);

        public static bool TryParse(string text, IMacroParserContext context, out MacroArg arg, bool allowNegative = false)
        {
            arg = default;

            var match = placeholder.Match(text);
            if (match.Success)
            {
                int? defaultValue = null;
                if (match.Groups[2].Success)
                {
                    if (!int.TryParse(match.Groups[2].Value, out var parsed)) return false;
                    if (!allowNegative && parsed < 0) return false;

                    defaultValue = parsed;
                }

                var name = match.Groups[1].Value;
                context.DeclareParameter(name, defaultValue, allowNegative);

                arg = new MacroArg(name, 0);
                return true;
            }

            if (!int.TryParse(text, out var value)) return false;
            if (!allowNegative && value < 0) return false;

            arg = value;
            return true;
        }

        public int Resolve(IMacroContext context)
        {
            if (Name is null) return _value;

            return context.GetArgument(Name)
                ?? throw new InvalidOperationException($"パラメータ{{{Name}}}の値が解決されていません");
        }
    }
}
