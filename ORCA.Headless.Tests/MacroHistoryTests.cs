using ORCA.Runtime.Macro;
using Xunit;

namespace ORCA.Headless.Tests
{
    public class MacroHistoryTests
    {
        private static MacroHistory.Entry MakeEntry(string label, bool dryRun = false)
            => new(label, dryRun, MacroScript.Compile(["Press A -d=10"], MacroScript.GetDefaultParsers()), ["Press A -d=10"], []);

        [Fact]
        public void Rememberしたエントリが履歴に追加されること()
        {
            var history = new MacroHistory();
            var entry = MakeEntry("a.txt");

            history.Remember(entry);

            Assert.Equal(1, history.Count);
            Assert.Equal(entry, history[0]);
        }

        [Fact]
        public void 履歴にあるエントリと異なるエントリをRememberすると履歴の先頭に追加されること()
        {
            var history = new MacroHistory();
            var entryA = MakeEntry("a.txt");
            var entryB = MakeEntry("b.txt");

            history.Remember(entryA);
            history.Remember(entryB);

            Assert.Equal(2, history.Count);
            Assert.Equal(entryB, history[0]);
            Assert.Equal(entryA, history[1]);
        }

        [Fact]
        public void 同じエントリを複数回Rememberするとそれぞれ別の実行として履歴に追加されること()
        {
            var history = new MacroHistory();
            var entry = MakeEntry("a.txt");

            history.Remember(entry);

            Assert.Equal(2, history.Count);
            Assert.Equal(entry, history[0]);
            Assert.Equal(entry, history[1]);
        }

        [Fact]
        public void 上限の10件を超えると古いエントリが捨てられること()
        {
            var history = new MacroHistory();
            history.Remember(MakeEntry("oldest.txt"));
            for (var i = 0; i < 10; i++)
                history.Remember(MakeEntry($"macro{i}.txt"));

            Assert.Equal(10, history.Count);
            for (var i = 0; i < 10; i++)
                Assert.DoesNotContain("oldest.txt", history[i].Label);
        }
    }
}
