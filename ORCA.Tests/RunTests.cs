using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ORCA.Core.Macro;
using Xunit;

namespace ORCA.Tests
{
    public class RunTests
    {
        public static IEnumerable<object[]> Cases => MacroCase.RunNames();

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task 送信されるバイト列のテスト(string name)
        {
            var testCase = MacroCase.Load("run", name);
            var script = MacroScript.Compile(testCase.MacroLines, MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            await script.RunOnceAsync(port, CancellationToken.None);

            Assert.Equal(testCase.ExpectedWrites, port.HexLines);
        }

        [Fact]
        public async Task 指定した回数だけ繰り返し実行されること()
        {
            // Arrange
            var script = MacroScript.Compile(
                ["Press A -d=1"],
                MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            // Act
            await script.RunLoopAsync(port, CancellationToken.None, 3);

            // Assert
            Assert.Equal(
                [
                    "80 81 80", "80 80 80",
                    "80 81 80", "80 80 80",
                    "80 81 80", "80 80 80"
                ],
                port.HexLines);
        }

        [Fact]
        public async Task 実行中にキャンセルされるとボタンの解放を送信することなく動作が停止すること()
        {
            // Arrange
            var script = MacroScript.Compile(
                [
                    "Press A -d=5000", 
                    "Press B -d=1"
                ],
                MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            // Act
            using (var cts = new CancellationTokenSource())
            {
                var task = script.RunOnceAsync(port, cts.Token);
                while (port.Entries.Length == 0) await Task.Delay(1);

                cts.Cancel();
                await task;
            }

            // Assert
            // NOTE: ボタンが押されたままになるが、既存の挙動なので維持する
            Assert.Equal(["80 81 80"], port.HexLines);
        }

        [Fact]
        public async Task 回数無指定の繰り返しであってもキャンセルで動作が停止すること()
        {
            // Arrange
            var script = MacroScript.Compile(
                ["Press A -d=1"],
                MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            // Act
            using (var cts = new CancellationTokenSource())
            {
                var task = script.RunLoopAsync(port, cts.Token);
                while (port.Entries.Length < 6) await Task.Delay(1);

                cts.Cancel();
                await task;
            }

            // Assert
            // 何周するかは時間次第なので、押下と解放が交互に並ぶことだけを検証する
            Assert.All(port.HexLines.Where((_, i) => i % 2 == 0), _ => Assert.Equal("80 81 80", _));
        }
    }
}
