using System.Threading;
using System.Threading.Tasks;
using GCController.Macro;
using Xunit;

namespace ORCA.Legacy.Tests
{
    // 実時間に依存して揺れるので, 通常は dotnet test --filter Category!=Timing で外す.
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
        public async Task Hitは指定フレーム分だけタイマー起動から待つ()
        {
            var port = await RunOnce(Compile("Start -s=0", "Hit A 30 -d=1"));

            var expected = (long)(30 * 1000 / FrameRate);
            Assert.InRange(port.Entries[0].ElapsedMs, expected - 30, expected + 30);
        }

        [Fact]
        public async Task Hitの補正はフレーム数に足される()
        {
            var port = await RunOnce(Compile("Start -s=0", "Hit A 10 -c=20 -d=1"));

            var expected = (long)(30 * 1000 / FrameRate);
            Assert.InRange(port.Entries[0].ElapsedMs, expected - 30, expected + 30);
        }

        [Fact]
        public async Task 補正でフレーム数が負になったら0に丸める()
        {
            var port = await RunOnce(Compile("Start -s=0", "Hit A 30 -c=-60 -d=1"));

            Assert.InRange(port.Entries[0].ElapsedMs, 0L, 30L);
        }

        [Fact]
        public async Task Pressの押下時間は既定で200msである()
        {
            var port = await RunOnce(Compile("Press A"));

            var held = port.Entries[1].ElapsedMs - port.Entries[0].ElapsedMs;
            Assert.InRange(held, 190L, 240L);
        }

        [Fact]
        public async Task Pressの系列の間隔は既定で0msである()
        {
            var port = await RunOnce(Compile("Press A,B -d=10"));

            var interval = port.Entries[2].ElapsedMs - port.Entries[1].ElapsedMs;
            Assert.InRange(interval, 0L, 20L);
        }

        [Fact]
        public async Task Waitは指定したミリ秒だけ待つ()
        {
            var port = await RunOnce(Compile("Press A -d=1", "Wait 200", "Press B -d=1"));

            var waited = port.Entries[2].ElapsedMs - port.Entries[1].ElapsedMs;
            Assert.InRange(waited, 190L, 240L);
        }

        // StartがHit計画のインデックスを1つ進めてしまうため, 1つ目のHitを待っている間に
        // 表示される残り時間は2つ目のHitのものになる. 既存の挙動なのでそのまま固定する.
        [Fact]
        public async Task 残りフレームの表示はHit計画のひとつ先を指す()
        {
            var script = Compile("Start -s=0", "Hit A 60 -d=1", "Hit B 120 -d=1");
            var port = new RecordingPort();

            Assert.Null(script.GetRemainingFrame());

            using (var cts = new CancellationTokenSource())
            {
                var task = script.RunOnceAsync(port, cts.Token);
                await Task.Delay(100);

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

            using (var cts = new CancellationTokenSource())
            {
                var task = script.RunOnceAsync(port, cts.Token);

                // 1つ目のHit(10F)が終わるまで待つ
                while (port.Entries.Length < 2) await Task.Delay(1);

                Assert.Null(script.GetRemainingFrame());

                await task;
            }
        }
    }
}
