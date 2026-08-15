using ORCA.Core.Macro;
using Xunit;

namespace ORCA.Headless.Tests
{
    public class MacroHistoryTests
    {
        private static MacroHistory.Entry MakeEntry(string label, bool dryRun = false)
            => new(label, dryRun, MacroScript.Compile(["Press A -d=10"], MacroScript.GetDefaultParsers()), ["Press A -d=10"]);

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
        public void 実行したエントリと同じラベルでDryRunするとそれぞれ別の履歴として扱われること()
        {
            var history = new MacroHistory();
            var entry1 = MakeEntry("a.txt", dryRun: false);
            var entry2 = MakeEntry("a.txt", dryRun: true);

            history.Remember(entry1);
            history.Remember(entry2);

            Assert.Equal(2, history.Count);
            Assert.Equal(entry2, history[0]);
            Assert.Equal(entry1, history[1]);
        }

        [Fact]
        public void 履歴にあるエントリと同一のエントリをRememberすると先頭に積み直されること()
        {
            var history = new MacroHistory();
            var entryA = MakeEntry("a.txt");
            var entryB = MakeEntry("b.txt");

            history.Remember(entryA);
            history.Remember(entryB);
            history.Remember(entryA);

            Assert.Equal(2, history.Count);
            Assert.Equal(entryA, history[0]);
            Assert.Equal(entryB, history[1]);
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

        [Fact]
        public void Entryの同一性_LabelとDryRunが同じEntryは同一()
        {
            var a = MakeEntry("a.txt", dryRun: false);
            var b = MakeEntry("a.txt", dryRun: false);

            Assert.Equal(a, b);
        }
        
        [Fact]
        public void Entryの同一性_Labelが異なれば同一でない()
        {
            var a = MakeEntry("a.txt", dryRun: true);
            var b = MakeEntry("b.txt", dryRun: true);

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Entryの同一性_Labelが同じでもDryRunが異なれば同一でない()
        {
            var a = MakeEntry("a.txt", dryRun: false);
            var b = MakeEntry("a.txt", dryRun: true);

            Assert.NotEqual(a, b);
        }
    }
}
