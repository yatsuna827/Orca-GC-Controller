using System.Threading;
using System.Threading.Tasks;
using ORCA.Core.Macro;
using Xunit;

namespace ORCA.Tests
{
    [Trait("Category", "Timing")]
    public class TimingTests
    {
        private static MacroScript Compile(params string[] lines)
            => MacroScript.Compile(lines, MacroScript.GetDefaultParsers());

        private static async Task<RecordingPort> RunOnce(MacroScript script)
        {
            var port = new RecordingPort();
            await script.RunOnceAsync(port, CancellationToken.None);
            return port;
        }

        [Fact]
        public async Task Hitコマンドはタイマー起動から指定フレーム経過後に押下が発生すること()
        {
            var port = await RunOnce(Compile("Start -s=0", "Hit A 30 -d=1"));

            // 30Fは502ms
            Assert.InRange(port.Entries[0].ElapsedMs, 472L, 532L);
        }

        [Fact]
        public async Task Waitコマンドは指定ミリ秒の待機が発生すること()
        {
            var port = await RunOnce(Compile("Press A -d=1", "Wait 200", "Press B -d=1"));

            var waited = port.Entries[2].ElapsedMs - port.Entries[1].ElapsedMs;
            Assert.InRange(waited, 190L, 240L);
        }

        [Fact]
        public async Task GetRemainingFrameは次のHitまでの残りフレーム数を返す()
        {
            var script = Compile("Start -s=0", "Hit A 60 -d=1", "Hit B 120 -d=1");
            var port = new RecordingPort();

            Assert.Null(script.GetRemainingFrame());

            using (var cts = new CancellationTokenSource())
            {
                var task = script.RunOnceAsync(port, cts.Token);
                await Task.Delay(100, TestContext.Current.CancellationToken);

                var remaining = script.GetRemainingFrame();
                Assert.NotNull(remaining);

                // 100msで5F経過するので残りは55F前後
                Assert.InRange(remaining.Value, 50, 60);

                await task;
            }

            Assert.Null(script.GetRemainingFrame());
        }

        [Fact]
        public async Task 最後のHitが完了するまでGetRemainingFrameはnullにならない()
        {
            var script = Compile("Start -s=0", "Hit A 10 -d=1", "Hit B 20 -d=1");
            var port = new RecordingPort();

            using var cts = new CancellationTokenSource();
            var task = script.RunOnceAsync(port, cts.Token);

            // 1つ目のHit(10F)が終わるまで待つ
            while (port.Entries.Length < 2) await Task.Delay(1, TestContext.Current.CancellationToken);

            Assert.NotNull(script.GetRemainingFrame());

            await task;
        }
        
        [Fact]
        public async Task すべてのHitが実行されたあとは残りフレームがnullになる()
        {
            var script = Compile("Start -s=0", "Hit A 10 -d=1", "Wait 500");
            var port = new RecordingPort();

            using var cts = new CancellationTokenSource();
            var task = script.RunOnceAsync(port, cts.Token);

            while (port.Entries.Length < 2) await Task.Delay(1, TestContext.Current.CancellationToken);

            Assert.Null(script.GetRemainingFrame());

            await task;
        }

    }
}
