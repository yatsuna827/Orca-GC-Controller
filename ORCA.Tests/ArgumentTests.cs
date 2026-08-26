using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ORCA.Core;
using ORCA.Runtime.Macro;
using Xunit;

namespace ORCA.Tests
{
    [Collection("Timing")]
    public class ArgumentTests
    {
        private static MacroScript Compile(params string[] lines)
            => MacroScript.Compile(lines, MacroScript.GetDefaultParsers());

        private static (MacroScript Script, RecordArgParser Parser) CompileRecord(params string[] lines)
        {
            var parser = new RecordArgParser();
            var script = MacroScript.Compile(
                lines,
                new Dictionary<string, IMacroCommandParser<MacroCommand>> { { "Record", parser } });

            return (script, parser);
        }

        private static async Task<RecordingPort> RunOnce(MacroScript script, IReadOnlyDictionary<string, int> arguments = null)
        {
            var port = new RecordingPort();
            await script.RunOnceAsync(port, CancellationToken.None, arguments);
            return port;
        }

        [Fact]
        public void プレースホルダが出現順にパラメータとして公開されること()
        {
            var script = Compile("Wait {wait}", "Press A -d={press:50}");

            Assert.Equal(["wait", "press"], script.Parameters.Select(_ => _.Name));
            Assert.Null(script.Parameters[0].DefaultValue);
            Assert.Equal(50, script.Parameters[1].DefaultValue);
        }

        [Fact]
        public void 同名のプレースホルダは1つのパラメータにまとめられること()
        {
            var script = Compile("Wait {d}", "Press A -d={d} -i={d}");

            Assert.Equal("d", Assert.Single(script.Parameters).Name);
        }

        [Fact]
        public void デフォルト値の異なる同名プレースホルダはコンパイルエラーになること()
        {
            var ex = Assert.Throws<Exception>(() => Compile("Wait {d:100}", "Wait {d:200}"));

            Assert.Equal("[2行目] パラメータ{d}のデフォルト値が宣言ごとに異なります", ex.Message);
        }

        [Fact]
        public void 負のデフォルト値はリテラルと同じくコンパイルエラーになること()
        {
            var ex = Assert.Throws<Exception>(() => Compile("Wait {d:-1}"));

            Assert.Equal("[1行目] Waitコマンド 第1引数(待機時間[ms]指定)は32bit符号あり整数に収まる負でない数値である必要があります", ex.Message);
        }

        [Fact]
        public void 負の値を許さない位置のパラメータに負の値を渡すと実行前に例外になること()
        {
            var script = Compile("Wait {d}");
            var port = new RecordingPort();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                _ = script.RunOnceAsync(port, CancellationToken.None, new Dictionary<string, int> { ["d"] = -5 });
            });

            Assert.StartsWith("パラメータ{d}は負でない数値である必要があります", ex.Message);
            Assert.Empty(port.Entries);
        }

        [Fact]
        public void 引数が複数個所で使われている場合_負の値を許さない位置での利用が1箇所でもあれば負の値を渡せないこと()
        {
            // -cは負を許すが, Waitの待機時間は許さない.
            var script = Compile("Start -s=0", "Wait {x}", "Hit A 60 -c={x} -d=1");
            var port = new RecordingPort();

            Assert.False(Assert.Single(script.Parameters).AllowsNegative);
            Assert.Throws<ArgumentException>(() =>
            {
                _ = script.RunOnceAsync(port, CancellationToken.None, new Dictionary<string, int> { ["x"] = -5 });
            });
        }

        [Fact]
        public void デフォルト値のないパラメータに値を渡さないとコマンドの実行前に例外になること()
        {
            var script = Compile("Press A -d={d}");
            var port = new RecordingPort();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                _ = script.RunOnceAsync(port, CancellationToken.None);
            });

            Assert.Equal("パラメータ{d}に値が指定されていません", ex.Message);
            Assert.Null(ex.ParamName);
            Assert.Empty(port.Entries);
        }

        [Fact]
        public void 値が指定されていないパラメータが複数あればまとめて出力されること()
        {
            var script = Compile("Wait {a}", "Press A -d={b}");
            var port = new RecordingPort();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                _ = script.RunOnceAsync(port, CancellationToken.None);
            });

            Assert.Equal("パラメータ{a}, {b}に値が指定されていません", ex.Message);
        }

        [Fact]
        public void マクロで使われていない名前の引数を渡すと実行しようとしたときに例外になること()
        {
            var script = Compile("Wait {d:0}");
            var port = new RecordingPort();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                _ = script.RunOnceAsync(port, CancellationToken.None, new Dictionary<string, int> { ["dd"] = 1 });
            });

            Assert.Equal("パラメータ{dd}はマクロで使われていません", ex.Message);
            Assert.Empty(port.Entries);
        }

        [Fact]
        public async Task 渡された引数がコマンドに反映されること()
        {
            var (script, parser) = CompileRecord("Record {x}");

            await RunOnce(script, new Dictionary<string, int> { ["x"] = 42 });

            Assert.Equal([42], parser.Resolved);
        }

        [Fact]
        public async Task 同名のプレースホルダはすべて同じ値に解決されること()
        {
            var (script, parser) = CompileRecord("Record {x}", "Record 7", "Record {x}");

            await RunOnce(script, new Dictionary<string, int> { ["x"] = 42 });

            Assert.Equal([42, 7, 42], parser.Resolved);
        }

        [Fact]
        public async Task 引数が省略されるとデフォルト値が使われること()
        {
            var (script, parser) = CompileRecord("Record {x:15}");

            await RunOnce(script);

            Assert.Equal([15], parser.Resolved);
        }

        [Fact]
        public async Task 渡された引数はデフォルト値より優先されること()
        {
            var (script, parser) = CompileRecord("Record {x:15}");

            await RunOnce(script, new Dictionary<string, int> { ["x"] = 42 });

            Assert.Equal([42], parser.Resolved);
        }

        [Fact]
        public async Task 再コンパイルなしに異なる引数で実行できること()
        {
            var (script, parser) = CompileRecord("Record {x:15}");

            await RunOnce(script, new Dictionary<string, int> { ["x"] = 1 });
            await RunOnce(script, new Dictionary<string, int> { ["x"] = 2 });
            await RunOnce(script);

            Assert.Equal([1, 2, 15], parser.Resolved);
        }

        [Fact]
        public async Task Waitコマンドの待機時間を引数で指定できること()
        {
            var script = Compile("Press A -d=1", "Wait {wait}", "Press B -d=1");

            var port = await RunOnce(script, new Dictionary<string, int> { ["wait"] = 200 });

            var waited = port.Entries[2].ElapsedMs - port.Entries[1].ElapsedMs;
            Assert.InRange(waited, 190L, 240L);
        }

        [Fact]
        public async Task Hitコマンドのフレームを引数で指定できること()
        {
            var script = Compile("Start -s=0", "Hit A {frame} -d=1");

            var port = await RunOnce(script, new Dictionary<string, int> { ["frame"] = 30 });

            // 30Fを59.7275fpsでmsに換算したら502ms
            Assert.InRange(port.Entries[0].ElapsedMs, 472L, 532L);
        }

        [Fact]
        public async Task Hitコマンドの補正値を引数で指定できること()
        {
            var script = Compile("Start -s=0", "Hit A 60 -c={correct} -d=1");

            var port = await RunOnce(script, new Dictionary<string, int> { ["correct"] = -30 });

            // 30Fを59.7275fpsでmsに換算したら502ms
            Assert.InRange(port.Entries[0].ElapsedMs, 472L, 532L);
        }

        [Fact]
        public async Task 補正値を加えた結果が負になる場合は0Fに丸められること()
        {
            var script = Compile("Start -s=0", "Wait 300", "Hit A 10 -c={correct} -d=1");
            var port = new RecordingPort();

            var task = script.RunOnceAsync(port, CancellationToken.None, new Dictionary<string, int> { ["correct"] = -100 });
            await Task.Delay(100, TestContext.Current.CancellationToken);

            var remaining = script.GetRemainingFrame();
            Assert.NotNull(remaining);
            Assert.InRange(remaining.Value, -30, 0);

            await task;
        }
    }
}
