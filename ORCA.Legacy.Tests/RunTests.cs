using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GCController.Macro;
using Xunit;

namespace ORCA.Legacy.Tests
{
    // マクロを最後まで流したときに線に乗るバイト列を固定する.
    public class RunTests
    {
        public static IEnumerable<object[]> Cases => MacroCase.RunNames();

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task 送信バイト列が期待通りである(string name)
        {
            var testCase = MacroCase.Load("run", name);
            var script = MacroScript.Compile(testCase.MacroLines, MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            await script.RunOnceAsync(port, CancellationToken.None);

            Assert.Equal(testCase.ExpectedWrites, port.HexLines);
        }

        [Fact]
        public async Task 指定した回数だけループする()
        {
            var script = MacroScript.Compile(
                new[] { "Press A -d=1" },
                MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            await script.RunLoopAsync(port, CancellationToken.None, 3);

            Assert.Equal(
                new[] { "80 81 80", "80 80 80", "80 81 80", "80 80 80", "80 81 80", "80 80 80" },
                port.HexLines);
        }

        // 押しっぱなしのまま終わるが, これが既存の挙動なので揃える.
        [Fact]
        public async Task キャンセルすると解放を送らずに止まる()
        {
            var script = MacroScript.Compile(
                new[] { "Press A -d=5000", "Press B -d=1" },
                MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            using (var cts = new CancellationTokenSource())
            {
                var task = script.RunOnceAsync(port, cts.Token);
                while (port.Entries.Length == 0) await Task.Delay(1);

                cts.Cancel();
                await task;
            }

            Assert.Equal(new[] { "80 81 80" }, port.HexLines);
        }

        [Fact]
        public async Task 回数無指定のループはキャンセルで止まる()
        {
            var script = MacroScript.Compile(
                new[] { "Press A -d=1" },
                MacroScript.GetDefaultParsers());
            var port = new RecordingPort();

            using (var cts = new CancellationTokenSource())
            {
                var task = script.RunLoopAsync(port, cts.Token);
                while (port.Entries.Length < 6) await Task.Delay(1);

                cts.Cancel();
                await task;
            }

            // 押下と解放が交互に並ぶ. 何周したかは時間次第なので, 並び方だけを見る.
            Assert.All(port.HexLines.Where((_, i) => i % 2 == 0), _ => Assert.Equal("80 81 80", _));
        }
    }
}
