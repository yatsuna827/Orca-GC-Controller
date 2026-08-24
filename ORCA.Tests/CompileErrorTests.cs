using System;
using ORCA.Runtime.Macro;
using Xunit;

namespace ORCA.Tests
{
    public class CompileErrorTests
    {
        public static TheoryData<string> Cases => MacroCase.CompileErrorNames();

        [Theory]
        [MemberData(nameof(Cases))]
        public void GUI版と同じコンパイルエラーを出力すること(string name)
        {
            var testCase = MacroCase.Load("compile_error", name);

            var ex = Assert.Throws<Exception>(
                () => MacroScript.Compile(testCase.MacroLines, MacroScript.GetDefaultParsers()));

            Assert.Equal(testCase.ExpectedMessage, ex.Message);
        }

        [Fact]
        public void 未知のオプション文字にカンマを指定するとコンパイルエラーになること()
        {
            var ex = Assert.Throws<Exception>(
                () => MacroScript.Compile(["Press A -,=1"], MacroScript.GetDefaultParsers()));

            Assert.Equal("[1行目] Pressコマンド オプション指定子が不正です", ex.Message);
        }

        [Fact]
        public void Startのオプションが3文字未満にコンパイルエラーになること()
        {
            var ex = Assert.Throws<Exception>(
                () => MacroScript.Compile(["Start -s"], MacroScript.GetDefaultParsers()));

            Assert.Equal("[1行目] Startコマンド オプション指定子が不正です", ex.Message);
        }
    }
}
