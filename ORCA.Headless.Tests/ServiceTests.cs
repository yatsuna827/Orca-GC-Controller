#pragma warning disable IDE1006

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ORCA.Runtime;
using Xunit;

namespace ORCA.Headless.Tests
{
    public class ServiceTests
    {
        private readonly RecordingPort _port = new();

        private Service NewService() => new(() => _port);

        private static Response Do(Service service, string command, params string[] args)
            => service.Handle(new Request(command, args));

        private static Response Run(Service service, string label, string macro)
            => service.Handle(new Request("run", [label], macro));

        // runが完了まで返ってこないので、実行中に別のコマンドを試すテストは別スレッドから起動する
        private static Task<Response> RunInBackground(Service service, string macro, CancellationTokenSource clientGone = null, params string[] extraArgs)
        {
            var task = Task.Run(() => service.Handle(new Request("run", ["macro.txt", .. extraArgs], macro), clientGone: clientGone?.Token ?? default));
            Assert.True(SpinWait.SpinUntil(() => service.HasRunningMacro, 5000), "マクロが始まらなかった");

            return task;
        }

        private static async Task<bool> CompletesWithin(Task task, int milliseconds)
            => await Task.WhenAny(task, Task.Delay(milliseconds, TestContext.Current.CancellationToken)) == task;

        [Fact]
        public void runコマンド_未接続の状態ならコマンドを拒否すること()
        {
            var service = NewService();

            var response = Run(service, "macro.txt", "Press A");

            Assert.False(response.Ok);
            Assert.Contains("not connected", response.Lines);
        }

        [Fact]
        public async Task runコマンド_マクロ実行中ならコマンドを拒否すること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            var running = RunInBackground(service, "Press A -d=5000", extraArgs: "--loop");
            var response = Run(service, "macro.txt", "Press A -d=5000");

            Assert.False(response.Ok);
            Assert.Contains("macro already running", response.Lines);

            Do(service, "shutdown");
            await running;
        }

        [Fact]
        public void runコマンド_ポート開放に失敗したらエラーを返すこと()
        {
            var service = new Service(() => new UnopenablePort());

            var response = Do(service, "connect", "COM99");

            Assert.False(response.Ok);
            Assert.Contains(response.Lines, l => l.StartsWith("failed to open COM99: "));
        }

        [Fact]
        public void runコマンド_ポート開放に失敗してもサービスは停止しないこと()
        {
            var service = new Service(() => new UnopenablePort());

            var response = Do(service, "connect", "COM99");

            Assert.False(service.Stopping.IsCancellationRequested);
        }

        class UnopenablePort : IPort
        {
            public bool IsOpen => false;
            public void Open(string portName, bool rts, bool dtr)
                => throw new FileNotFoundException($"Could not find file '{portName}'.");
            public void Close() { }
            public void Write(byte[] buffer, int offset, int count) { }
        }

        [Fact]
        public void runコマンド_マクロのコンパイルに失敗したらエラーを返すこと()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            var response = Run(service, "macro.txt", "Press Q");

            Assert.False(response.Ok);
            Assert.Contains(response.Lines, l => l.StartsWith("error: "));
        }

        [Fact]
        public void runコマンド_マクロのコンパイルに失敗してもサービスは停止しないこと()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            Run(service, "macro.txt", "Press Q");

            Assert.False(service.Stopping.IsCancellationRequested);
        }

        [Fact]
        public void runコマンド_マクロの実行が例外で落ちたらエラーを返すこと()
        {
            var service = new Service(() => new ExplodingPort());

            Do(service, "connect", "COM_TEST");
            var response = Run(service, "macro.txt", "Press A -d=10");

            Assert.False(response.Ok);
            Assert.Contains(response.Lines, l => l.StartsWith("error: "));
            Assert.False(service.HasRunningMacro);
        }

        class ExplodingPort : IPort
        {
            public bool IsOpen { get; private set; }
            public void Open(string portName, bool rts, bool dtr) => IsOpen = true;
            public void Close() => IsOpen = false;
            public void Write(byte[] buffer, int offset, int count) => throw new IOException("device gone");
        }

        [Fact]
        public void runコマンド_進捗がコールバックで通知されること()
        {
            var service = NewService();
            var progress = new List<string>();

            Do(service, "connect", "COM_TEST");
            service.Handle(new Request("run", ["macro.txt"], "Wait 200\nWait 200"), p => progress.Add(p.Text), clientGone: TestContext.Current.CancellationToken);

            Assert.NotEmpty(progress);
            Assert.Contains("Wait 200", progress);
        }

        [Fact]
        public async Task runコマンド_回数指定なしのloopオプションの後に別のオプションを指定してもパースエラーにならないこと()
        {
            var service = NewService();
            var cts = new CancellationTokenSource();

            var running = Task.Run(() => service.Handle(new Request("run", ["macro.txt", "--loop", "--dry-run"], "Press A -d=10"), clientGone: cts.Token));

            Assert.True(SpinWait.SpinUntil(() => service.HasRunningMacro, 5000), "マクロが始まらなかった");
            cts.Cancel();
            var response = await running;
            Assert.Contains("macro cancelled", response.Lines);
        }

        [Fact]
        public async Task runコマンド_マクロ完了直後に別のrunが始まっても自分のマクロの完了時点で応答すること()
        {
            var service = NewService();

            Task<Response> later = null;
            var laterCts = new CancellationTokenSource();
            var interleaved = false;
            void startLaterScript()
            {
                later = Task.Run(() => service.Handle(new Request("run", ["b.txt", "--dry-run"], "Press A -d=5000"), clientGone: laterCts.Token));
                interleaved = SpinWait.SpinUntil(() => service.HasRunningMacro, 5000);
            }

            // 進捗の監視が100ms間隔のポーリングなので、観測し損ねないよう余裕を持たせるために、2行目のWaitを長めに取っている
            var response = service.Handle(new Request("run", ["a.txt", "--dry-run"], "Wait 300\nWait 800"),
                // HACK: onProgressの乱用だが、レースコンディションを確実に再現する方法がこれしかない
                onProgress: p =>
                {
                    if (p.Line != 2 || later != null) return;
                    if (!SpinWait.SpinUntil(() => !service.HasRunningMacro, 5000)) return;

                    startLaterScript();
                },
                clientGone: TestContext.Current.CancellationToken);
            Assert.True(interleaved, "割り込み失敗");

            Assert.Contains("macro finished", response.Lines);
            Assert.True(service.HasRunningMacro);

            laterCts.Cancel();
            Assert.Contains("macro cancelled", (await later).Lines);
        }

        [Fact]
        public void connectコマンド_接続済みならコマンドを拒否すること()
        {
            var service = NewService();

            Assert.True(Do(service, "connect", "COM_TEST").Ok);
            var response = Do(service, "connect", "COM_TEST");

            Assert.False(response.Ok);
            Assert.Contains("already connected", response.Lines);
        }

        [Fact]
        public void connectコマンド_デフォルトポートが設定されていれば接続できること()
        {
            var service = NewService();

            Do(service, "set-port", "COM_TEST");
            var response = Do(service, "connect");

            Assert.True(response.Ok);
            Assert.Contains("connected to COM_TEST", response.Lines);
        }

        [Fact]
        public void connectコマンド_引数なしかつデフォルトポート未設定なら拒否すること()
        {
            var service = NewService();

            var response = Do(service, "connect");

            Assert.False(response.Ok);
        }

        [Fact]
        public void connectコマンド_デフォルトポート未設定のとき接続が成功すると接続先がデフォルトポートとして記憶されること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            Do(service, "disconnect");
            var response = Do(service, "connect");

            Assert.True(response.Ok);
            Assert.Contains("connected to COM_TEST", response.Lines);
        }

        [Fact]
        public void connectコマンド_デフォルトポート設定済みのとき接続が成功してもデフォルトポートは上書きされないこと()
        {
            var service = new Service(() => _port, defaultPort: "COM_DEFAULT");

            Do(service, "connect", "COM_TEST");
            Do(service, "disconnect");
            var response = Do(service, "connect");

            Assert.True(response.Ok);
            Assert.Contains("connected to COM_DEFAULT", response.Lines);
        }

        [Fact]
        public void disconnectコマンド_接続していないならコマンドを拒否すること()
        {
            var service = NewService();

            var response = Do(service, "disconnect");

            Assert.False(response.Ok);
            Assert.Contains("not connected", response.Lines);
        }

        [Fact]
        public void shutdownコマンド_受理するとサービスが停止すること()
        {
            var service = NewService();
            Assert.False(service.Stopping.IsCancellationRequested);

            Do(service, "shutdown");

            Assert.True(service.Stopping.IsCancellationRequested);
        }

        [Fact]
        public void rerunコマンド_指定された履歴のマクロを実行すること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            Run(service, "a.txt", "Press A -d=10");
            Run(service, "b.txt", "Press B -d=10");

            var response = Do(service, "rerun", "2");

            Assert.True(response.Ok);
            Assert.Equal(["80 81 80", "80 80 80", "80 82 80", "80 80 80", "80 81 80", "80 80 80"], _port.HexLines);
            Assert.Equal(["1: a.txt", "2: b.txt", "3: a.txt"], Do(service, "history").Lines);
        }

        [Fact]
        public void rerunコマンド_番号が省略された場合は直前のマクロを実行すること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            Run(service, "a.txt", "Press A -d=10");
            Run(service, "b.txt", "Press B -d=10");

            var response = Do(service, "rerun");

            Assert.True(response.Ok);
            Assert.Equal(["80 81 80", "80 80 80", "80 82 80", "80 80 80", "80 82 80", "80 80 80"], _port.HexLines);
        }

        [Fact]
        public void rerunコマンド_履歴に無い番号が指定された場合はコマンドを拒否すること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            Assert.Contains("no history", Do(service, "rerun").Lines);

            Run(service, "a.txt", "Press A -d=10");
            Run(service, "b.txt", "Press B -d=10");
            var response = Do(service, "rerun", "9");

            Assert.False(response.Ok);
            Assert.Contains("only 2 entries in history", response.Lines);
        }

        [Fact]
        public async Task rerunコマンド_回数指定なしのloopオプションの後に別のオプションを指定してもパースエラーにならないこと()
        {
            var service = NewService();
            var cts = new CancellationTokenSource();

            service.Handle(new Request("run", ["macro.txt", "--dry-run"], "Press A -d=10"), clientGone: TestContext.Current.CancellationToken);
            var running = Task.Run(() => service.Handle(new Request("rerun", ["--loop", "--dry-run"]), clientGone: cts.Token));

            Assert.True(SpinWait.SpinUntil(() => service.HasRunningMacro, 5000), "マクロが始まらなかった");
            cts.Cancel();
            var response = await running;
            Assert.Contains("macro cancelled", response.Lines);
        }

        [Fact]
        public void runコマンド_argオプションで渡した値がマクロに渡されること()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");

            var missing = service.Handle(new Request("run", ["macro.txt"], "Wait {d}"), clientGone: TestContext.Current.CancellationToken);
            Assert.False(missing.Ok);
            Assert.Contains("パラメータ{d}に値が指定されていません", missing.Lines[0]);

            Assert.True(service.Handle(new Request("run", ["macro.txt", "--arg", "d=0"], "Wait {d}"), clientGone: TestContext.Current.CancellationToken).Ok);
        }

        [Fact]
        public void runコマンド_負の値にできないパラメータに負の値が渡された場合はコマンドを拒否すること()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");

            var response = service.Handle(new Request("run", ["macro.txt", "--arg", "d=-5"], "Press A -d={d}"), clientGone: TestContext.Current.CancellationToken);

            Assert.False(response.Ok);
            Assert.Contains("パラメータ{d}は負でない数値である必要があります", response.Lines[0]);
        }

        [Fact]
        public void runコマンド_マクロで使われていない名前の引数が渡された場合はコマンドを拒否すること()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");

            var response = service.Handle(new Request("run", ["macro.txt", "--arg", "dd=10"], "Press A -d={d:10}"), clientGone: TestContext.Current.CancellationToken);

            Assert.False(response.Ok);
            Assert.Contains("パラメータ{dd}はマクロで使われていません", response.Lines[0]);
        }

        [Fact]
        public void runコマンド_同じ名前の引数が複数回指定された場合はコマンドを拒否すること()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");

            var response = service.Handle(new Request("run", ["macro.txt", "--arg", "d=1", "--arg", "d=2"], "Wait {d}"), clientGone: TestContext.Current.CancellationToken);

            Assert.False(response.Ok);
            Assert.Contains("duplicated argument: d", response.Lines[0]);
        }

        [Fact]
        public void runコマンド_argの書式不正()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");

            Assert.Contains("invalid argument: d", service.Handle(new Request("run", ["macro.txt", "--arg", "d"], "Wait {d}"), clientGone: TestContext.Current.CancellationToken).Lines[0]);
            Assert.Contains("invalid argument value: d=x", service.Handle(new Request("run", ["macro.txt", "--arg", "d=x"], "Wait {d}"), clientGone: TestContext.Current.CancellationToken).Lines[0]);
            Assert.Contains("usage: --arg", service.Handle(new Request("run", ["macro.txt", "--arg"], "Wait {d}"), clientGone: TestContext.Current.CancellationToken).Lines[0]);
        }

        [Fact]
        public void runコマンド_引数の解決に失敗した実行は履歴に積まれないこと()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");

            var response = service.Handle(new Request("run", ["macro.txt"], "Wait {d}"), clientGone: TestContext.Current.CancellationToken);

            Assert.False(response.Ok);
            Assert.Contains("no history", Do(service, "history").Lines);
        }

        [Fact]
        public void rerunコマンド_前回渡された引数をベースに_指定された引数だけ値を上書きして実行すること()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");
            const string macro = "Wait {a}\nPress A -d={b}";
            service.Handle(new Request("run", ["macro.txt", "--arg", "a=0", "--arg", "b=1"], macro), clientGone: TestContext.Current.CancellationToken);

            Assert.True(Do(service, "rerun", "--arg", "b=2").Ok);

            var response = Do(service, "rerun", "--arg", "b=-1");
            Assert.False(response.Ok);
            Assert.Contains("パラメータ{b}は負でない数値である必要があります", response.Lines[0]);
        }

        [Fact]
        public void statusコマンド_マクロが実行中でなければidleを返すこと()
        {
            var service = NewService();

            var response = Do(service, "status");

            Assert.True(response.Ok);
            Assert.Contains("idle", response.Lines);
        }

        [Fact]
        public async Task statusコマンド_マクロ実行中ならばrunningを返すこと()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            var running = RunInBackground(service, "Press A -d=5000", extraArgs: "--loop");
            var response = Do(service, "status");

            Assert.True(response.Ok);
            Assert.Contains("running", response.Lines);

            Do(service, "shutdown");
            await running;
        }

        [Fact]
        public void dryrunはポート未接続でも実行できること()
        {
            var service = NewService();

            var response = service.Handle(new Request("run", ["macro.txt", "--dry-run"], "Press A -d=10"), clientGone: TestContext.Current.CancellationToken);

            Assert.True(response.Ok);
            Assert.Contains("macro finished", response.Lines);
            Assert.Empty(_port.HexLines);
        }

        [Fact]
        public void dryrunの履歴はdryrunとして表示されること()
        {
            var service = NewService();

            service.Handle(new Request("run", ["macro.txt", "--dry-run"], "Press A -d=10"), clientGone: TestContext.Current.CancellationToken);

            Assert.Equal(["1: macro.txt (dry-run)"], Do(service, "history").Lines);
        }

        [Fact]
        public void dryrunと通常実行は別の履歴エントリになること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            Run(service, "a.txt", "Press A -d=10");
            service.Handle(new Request("run", ["a.txt", "--dry-run"], "Press A -d=10"), clientGone: TestContext.Current.CancellationToken);

            Assert.Equal(["1: a.txt (dry-run)", "2: a.txt"], Do(service, "history").Lines);
        }

        [Fact]
        public void dryrunのrerunはデフォルトでdryrunになること()
        {
            var service = NewService();

            service.Handle(new Request("run", ["macro.txt", "--dry-run"], "Press A -d=10"), clientGone: TestContext.Current.CancellationToken);
            var response = Do(service, "rerun");

            Assert.True(response.Ok);
            Assert.Empty(_port.HexLines);
        }

        [Fact]
        public void dryrunをno_dry_runオプションつきでrerunすると本実行できること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            service.Handle(new Request("run", ["macro.txt", "--dry-run"], "Press A -d=10"), clientGone: TestContext.Current.CancellationToken);
            var response = Do(service, "rerun", "--no-dry-run");

            Assert.True(response.Ok);
            Assert.NotEmpty(_port.HexLines);
        }

        [Fact]
        public async Task マクロ完了直後に別のrunが始まったら古いrunはボタンを解放しないこと()
        {
            var service = NewService();
            Do(service, "connect", "COM_TEST");

            Task<Response> later = null;
            var laterCts = new CancellationTokenSource();
            var earlierCts = new CancellationTokenSource();
            var interleaved = false;

            // 進捗の監視が100ms間隔のポーリングなので、観測し損ねないよう余裕を持たせるために、2行目のWaitを長めに取っている
            var response = service.Handle(new Request("run", ["a.txt"], "Wait 300\nWait 800"),
                // HACK: onProgressの乱用だが、レースコンディションを確実に再現する方法がこれしかない
                onProgress: p =>
                {
                    if (p.Line != 2 || later != null) return;
                    if (!SpinWait.SpinUntil(() => !service.HasRunningMacro, 5000)) return;

                    later = Task.Run(() => service.Handle(new Request("run", ["b.txt", "--dry-run"], "Press A -d=5000"), clientGone: laterCts.Token));
                    interleaved = SpinWait.SpinUntil(() => service.HasRunningMacro, 5000);
                    earlierCts.Cancel();
                },
                clientGone: earlierCts.Token);
            Assert.True(interleaved, "割り込み失敗");

            Assert.Contains("macro cancelled", response.Lines);
            Assert.Empty(_port.HexLines);

            laterCts.Cancel();
            await later;
        }

        [Fact]
        public async Task disconnectコマンド_実行中のマクロを止めてボタンを解放してから切断すること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            var running = RunInBackground(service, "Press A -d=5000");
            Assert.True(SpinWait.SpinUntil(() => _port.HexLines.Length > 0, 5000), "ボタン入力が確認できなかった");

            Assert.True(Do(service, "disconnect").Ok);
            Assert.Contains("macro cancelled", (await running).Lines);

            Assert.False(service.HasRunningMacro);
            Assert.False(_port.IsOpen);
            Assert.Equal(["80 81 80", "80 80 80", RecordingPort.Closed], _port.Log);
        }

        [Fact]
        public void disconnectコマンド_キャンセル済みのマクロが完了していてもポートを閉じる前にボタンを解放すること()
        {
            var service = NewService();
            var cts = new CancellationTokenSource();

            Do(service, "connect", "COM_TEST");

            var pressed = false;
            var completed = false;
            var response = service.Handle(new Request("run", ["a.txt"], "Press A -d=5000"),
                // HACK: onProgressの乱用だが、「キャンセル済みのマクロが完了してから解放を送るまで」の間にdisconnectを差し込むにはこれしかない
                onProgress: _ =>
                {
                    pressed = SpinWait.SpinUntil(() => _port.HexLines.Length > 0, 5000);
                    cts.Cancel();
                    completed = SpinWait.SpinUntil(() => !service.HasRunningMacro, 5000);
                    Do(service, "disconnect");
                },
                clientGone: cts.Token);
            Assert.True(pressed, "ボタン入力が確認できなかった");
            Assert.True(completed, "マクロが完了しなかった");

            Assert.Contains("macro cancelled", response.Lines);

            var log = _port.Log;
            Assert.Equal(RecordingPort.Closed, log[^1]);
            Assert.Equal("80 80 80", log[^2]);
        }

        [Fact]
        public async Task dryrunのキャンセルは接続中のポートにボタンの解放を送らないこと()
        {
            var service = NewService();
            var cts = new CancellationTokenSource();

            Do(service, "connect", "COM_TEST");
            var running = Task.Run(() => service.Handle(new Request("run", ["a.txt", "--dry-run"], "Press A -d=5000"), clientGone: cts.Token));
            Assert.True(SpinWait.SpinUntil(() => service.HasRunningMacro, 5000), "マクロが始まらなかった");

            cts.Cancel();
            Assert.Contains("macro cancelled", (await running).Lines);
            Assert.Empty(_port.HexLines);
        }

        [Fact]
        public async Task shutdownコマンド_ポートを閉じ終えてからデーモンに停止を伝えること()
        {
            var port = new BlockingPort();
            var service = new Service(() => port);

            var connecting = Task.Run(() => Do(service, "connect", "COM_TEST"));
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "connectが始まらなかった");
            port.Release();
            Assert.True((await connecting).Ok);

            var shuttingDown = Task.Run(() => Do(service, "shutdown"));
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "ポートが閉じられなかった");

            Assert.False(service.Stopping.IsCancellationRequested);

            port.Release();
            Assert.True((await shuttingDown).Ok);
            Assert.True(service.Stopping.IsCancellationRequested);
        }

        [Fact]
        public void shutdownコマンド_ポートを閉じるのに失敗してもデーモンに停止が伝わること()
        {
            var service = new Service(() => new UnclosablePort());

            Assert.True(Do(service, "connect", "COM_TEST").Ok);
            var response = Do(service, "shutdown");

            Assert.False(response.Ok);
            Assert.True(service.Stopping.IsCancellationRequested);
        }

        class UnclosablePort : IPort
        {
            public bool IsOpen { get; private set; }
            public void Open(string portName, bool rts, bool dtr) => IsOpen = true;
            public void Close() => throw new IOException("failed to close");
            public void Write(byte[] buffer, int offset, int count) { }
        }

        [Fact]
        public async Task connect実行中でもportsとstatusは待たされないこと()
        {
            var port = new BlockingPort();
            var service = new Service(() => port);

            var connecting = Task.Run(() => Do(service, "connect", "COM_TEST"));
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "connectが始まらなかった");

            foreach (var command in new[] { "ports", "status" })
                Assert.True(await CompletesWithin(Task.Run(() => Do(service, command)), 5000), $"{command}がconnectの完了を待たされた");

            port.Release();
            Assert.True((await connecting).Ok);
        }

        [Fact]
        public async Task connect実行中のrunはconnectの完了を待つこと()
        {
            var port = new BlockingPort();
            var service = new Service(() => port);

            var connecting = Task.Run(() => Do(service, "connect", "COM_TEST"));
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "connectが始まらなかった");

            var running = Task.Run(() => Run(service, "macro.txt", "Press A -d=10"));
            Assert.False(await CompletesWithin(running, 300), "connectの完了を待たずにrunが実行された");

            port.Release();
            Assert.True((await connecting).Ok);
            Assert.True((await running).Ok);
        }

        class BlockingPort : IPort
        {
            private readonly SemaphoreSlim _called = new(0);
            private readonly SemaphoreSlim _release = new(0);

            public bool IsOpen { get; private set; }

            public void Open(string portName, bool rts, bool dtr)
            {
                Block();
                IsOpen = true;
            }

            public void Close()
            {
                Block();
                IsOpen = false;
            }

            public bool WaitForCall(CancellationToken token) => _called.Wait(5000, token);

            public void Release() => _release.Release();

            private void Block()
            {
                _called.Release();
                _release.Wait();
            }

            public void Write(byte[] buffer, int offset, int count) { }
        }

        [Fact]
        public async Task マクロ実行中でもポートに干渉しないコマンドは待たされないこと()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            // 回数指定なしのloopなので、gateを待ってしまったコマンドはshutdownまで返ってこない
            var running = RunInBackground(service, "Press A -d=5000", extraArgs: "--loop");

            foreach (var command in new[] { "ports", "status", "history", "set-port" })
            {
                var task = Task.Run(() => Do(service, command, "COM_TEST"));
                Assert.True(await CompletesWithin(task, 5000), $"{command}がマクロの完了を待たされた");
            }

            Do(service, "shutdown");
            await running;
        }

        [Fact]
        public async Task マクロ実行中はdryrunかどうかによらずrunもrerunも拒否されること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            Run(service, "a.txt", "Press A -d=10");
            var running = RunInBackground(service, "Press A -d=5000", extraArgs: "--loop");

            Assert.Contains("macro already running", Run(service, "b.txt", "Press B -d=10").Lines);
            Assert.Contains("macro already running", Do(service, "rerun").Lines);
            Assert.Contains("macro already running", service.Handle(new Request("run", ["c.txt", "--dry-run"], "Press B -d=10"), clientGone: TestContext.Current.CancellationToken).Lines);
            Assert.Contains("macro already running", Do(service, "rerun", "--dry-run").Lines);

            Do(service, "shutdown");
            await running;
        }

        [Fact]
        public async Task dryrun実行中でも次のマクロは拒否されること()
        {
            var service = NewService();
            var cts = new CancellationTokenSource();

            Do(service, "connect", "COM_TEST");
            var running = Task.Run(() => service.Handle(new Request("run", ["a.txt", "--dry-run"], "Press A -d=5000"), clientGone: cts.Token));
            Assert.True(SpinWait.SpinUntil(() => service.HasRunningMacro, 5000), "マクロが始まらなかった");

            Assert.Contains("macro already running", Run(service, "b.txt", "Press B -d=10").Lines);
            Assert.Contains("macro already running", service.Handle(new Request("run", ["c.txt", "--dry-run"], "Press B -d=10"), clientGone: TestContext.Current.CancellationToken).Lines);

            cts.Cancel();
            await running;
        }

        [Fact]
        public async Task shutdownコマンド_実行中のマクロを止めてボタンを解放してからポートを閉じること()
        {
            var service = NewService();

            Do(service, "connect", "COM_TEST");
            var running = RunInBackground(service, "Press A -d=5000");
            Assert.True(SpinWait.SpinUntil(() => _port.HexLines.Length > 0, 5000), "ボタン入力が確認できなかった");

            Assert.True(Do(service, "shutdown").Ok);
            Assert.Contains("macro cancelled", (await running).Lines);

            Assert.False(service.HasRunningMacro);
            Assert.False(_port.IsOpen);
            Assert.Equal(["80 81 80", "80 80 80", RecordingPort.Closed], _port.Log);
        }

        [Fact]
        public async Task shutdownの処理中に届いたコマンドは待たされずに拒否されること()
        {
            var port = new BlockingPort();
            var service = new Service(() => port);

            var connecting = Task.Run(() => Do(service, "connect", "COM_TEST"));
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "connectが始まらなかった");
            port.Release();
            Assert.True((await connecting).Ok);

            var shuttingDown = Task.Run(() => Do(service, "shutdown"));
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "ポートが閉じられなかった");

            foreach (var command in new[] { "status", "connect", "run" })
            {
                var task = Task.Run(() => Do(service, command, "COM_TEST"));
                Assert.True(await CompletesWithin(task, 5000), $"{command}がshutdownの完了を待たされた");
                Assert.Contains("shutting down", (await task).Lines);
            }

            port.Release();
            Assert.True((await shuttingDown).Ok);
        }

        [Fact]
        public async Task shutdownの前からgateを待っていたrunも実行されずに拒否されること()
        {
            var port = new BlockingPort();
            var service = new Service(() => port);

            var connecting = Task.Run(() => Do(service, "connect", "COM_TEST"));
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "connectが始まらなかった");

            // connectがgateを持っている間に、runとshutdownをgate待ちに並ばせる
            var running = Task.Run(() => service.Handle(new Request("run", ["a.txt", "--dry-run"], "Press A -d=5000"), clientGone: TestContext.Current.CancellationToken));
            var shuttingDown = Task.Run(() => Do(service, "shutdown"));
            Assert.True(SpinWait.SpinUntil(() => !Do(service, "status").Ok, 5000), "shutdownが始まらなかった");

            port.Release();
            Assert.True(port.WaitForCall(TestContext.Current.CancellationToken), "ポートが閉じられなかった");
            port.Release();

            Assert.True((await connecting).Ok);
            Assert.True((await shuttingDown).Ok);
            Assert.Contains("shutting down", (await running).Lines);
            Assert.False(service.HasRunningMacro);
        }

        [Fact]
        public void shutdown後に届いたコマンドはすべて拒否されること()
        {
            var service = NewService();

            Assert.True(Do(service, "shutdown").Ok);

            foreach (var command in new[] { "ports", "connect", "disconnect", "run", "rerun", "set-port", "history", "status", "shutdown" })
            {
                var response = Do(service, command, "COM_TEST");

                Assert.False(response.Ok, command);
                Assert.Contains("shutting down", response.Lines);
            }
        }

        [Fact]
        public void 未知のコマンドは拒否されること()
        {
            var service = NewService();

            var response = Do(service, "foo");

            Assert.False(response.Ok);
            Assert.Contains("unknown command: foo", response.Lines);
        }

        [Fact]
        public async Task クライアントとの接続が切れたらマクロが止まり_全ボタンの解放が送信されること()
        {
            var service = NewService();
            var clientGone = new CancellationTokenSource();

            Do(service, "connect", "COM_TEST");
            var running = RunInBackground(service, "Press A -d=5000", clientGone);
            Assert.True(SpinWait.SpinUntil(() => _port.HexLines.Length > 0, 5000), "ボタン入力が確認できなかった");

            clientGone.Cancel();
            var response = await running;

            Assert.False(service.HasRunningMacro);
            Assert.Contains("macro cancelled", response.Lines);
            Assert.Equal(["80 81 80", "80 80 80"], _port.HexLines);
        }

    }
}
