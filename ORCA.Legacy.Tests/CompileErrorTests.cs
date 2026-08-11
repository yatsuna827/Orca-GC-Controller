using System;
using System.Collections.Generic;
using GCController.Macro;
using Xunit;

namespace ORCA.Legacy.Tests
{
    // ヘッドレス版でも一字一句同じ文言が出ることを求める.
    public class CompileErrorTests
    {
        public static IEnumerable<object[]> Cases => MacroCase.CompileErrorNames();

        [Theory]
        [MemberData(nameof(Cases))]
        public void 文言が期待通りである(string name)
        {
            var testCase = MacroCase.Load("compile_error", name);

            var ex = Assert.Throws<Exception>(
                () => MacroScript.Compile(testCase.MacroLines, MacroScript.GetDefaultParsers()));

            Assert.Equal(testCase.ExpectedMessage, ex.Message);
        }
    }
}
