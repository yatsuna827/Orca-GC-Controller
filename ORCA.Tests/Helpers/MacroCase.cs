using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ORCA.Tests
{
    class MacroCase
    {
        public string[] MacroLines { get; private set; }
        public string ExpectedMessage { get; private set; }
        public string[] ExpectedWrites { get; private set; }

        public static string Root => Path.Combine(AppContext.BaseDirectory, "cases");

        public static TheoryData<string> RunNames() => Names("run");
        public static TheoryData<string> CompileErrorNames() => Names("compile_error");

        private static TheoryData<string> Names(string category)
            => [.. Directory.GetDirectories(Path.Combine(Root, category)).Select(Path.GetFileName)];

        public static MacroCase Load(string category, string name)
        {
            var dir = Path.Combine(Root, category, name);

            var result = new MacroCase
            {
                MacroLines = SplitLikeForm1(File.ReadAllText(Path.Combine(dir, "macro.txt"))),
            };

            var messagePath = Path.Combine(dir, "message.txt");
            if (File.Exists(messagePath))
                result.ExpectedMessage = File.ReadAllText(messagePath).TrimEnd('\r', '\n');

            var writesPath = Path.Combine(dir, "writes.txt");
            if (File.Exists(writesPath))
                result.ExpectedWrites = ReadLines(writesPath);

            return result;
        }

        private static string[] SplitLikeForm1(string text)
            => text.Replace("\r\n", "\n").Split(['\n', '\r']);

        // 期待値ファイルにコメントを書けるようにするため, 空行とコメント行を落とす.
        private static string[] ReadLines(string path)
            => [.. File.ReadAllLines(path)
                .Select(_ => _.Trim())
                .Where(_ => _.Length > 0 && _[0] != '#')];
    }
}
