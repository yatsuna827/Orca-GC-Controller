using System;
using ORCA.Core.Macro;
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
    }
}
