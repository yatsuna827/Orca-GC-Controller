using System.Threading;
using System.Threading.Tasks;
using ORCA.Core.Macro;
using Xunit;

namespace ORCA.Tests
{
    [Trait("Category", "Timing")]
    public class TimingTests
    {
        private const double FrameRate = 59.7275;

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

            var expected = (long)(30 * 1000 / FrameRate);
            Assert.InRange(port.Entries[0].ElapsedMs, expected - 30, expected + 30);
        }

        [Fact]
        public async Task Waitコマンドは指定ミリ秒の待機が発生すること()
        {
            var port = await RunOnce(Compile("Press A -d=1", "Wait 200", "Press B -d=1"));

            var waited = port.Entries[2].ElapsedMs - port.Entries[1].ElapsedMs;
            Assert.InRange(waited, 190L, 240L);
        }

        // 以下2件は既存の不具合

        [Fact]
        public async Task 残りフレームの表示はHit計画のひとつ先を指している()
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

                var elapsedFrame = (int)(100 * FrameRate / 1000);
                Assert.InRange(remaining.Value, 120 - elapsedFrame - 5, 120 - elapsedFrame + 5);

                await task;
            }

            Assert.Null(script.GetRemainingFrame());
        }

        [Fact]
        public async Task 最後のHitを待つ間は残りフレームが取れない()
        {
            var script = Compile("Start -s=0", "Hit A 10 -d=1", "Hit B 20 -d=1");
            var port = new RecordingPort();

            using var cts = new CancellationTokenSource();
            var task = script.RunOnceAsync(port, cts.Token);

            // 1つ目のHit(10F)が終わるまで待つ
            while (port.Entries.Length < 2) await Task.Delay(1, TestContext.Current.CancellationToken);

            Assert.Null(script.GetRemainingFrame());

            await task;
        }
    }
}
