#pragma warning disable IDE1006

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ORCA.Core;
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
        private static Task<Response> RunInBackground(Service service, string macro, CancellationTokenSource clientGone = null)
        {
            var task = Task.Run(() => service.Handle(new Request("run", ["macro.txt"], macro), clientGone: clientGone?.Token ?? default));
            SpinWait.SpinUntil(() => service.HasRunningMacro, 2000);

            return task;
        }

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
            var running = RunInBackground(service, "Press A -d=5000");
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

            Assert.True(service.Running);
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

            Assert.True(service.Running);
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
            SpinWait.SpinUntil(() => service.HasRunningMacro, 2000);

            Assert.True(service.HasRunningMacro);
            cts.Cancel();
            var response = await running;
            Assert.Contains("macro cancelled", response.Lines);
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
            Assert.True(service.Running);

            Do(service, "shutdown");

            Assert.False(service.Running);
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
            Assert.Equal(["1: a.txt", "2: b.txt"], Do(service, "history").Lines);
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
            SpinWait.SpinUntil(() => service.HasRunningMacro, 2000);

            Assert.True(service.HasRunningMacro);
            cts.Cancel();
            var response = await running;
            Assert.Contains("macro cancelled", response.Lines);
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
            SpinWait.SpinUntil(() => _port.HexLines.Length > 0, 2000);

            clientGone.Cancel();
            var response = await running;

            Assert.False(service.HasRunningMacro);
            Assert.Contains("macro cancelled", response.Lines);
            Assert.Equal(["80 81 80", "80 80 80"], _port.HexLines);
        }

    }
}
