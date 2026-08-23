#pragma warning disable IDE1006

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ORCA.Headless.Tests
{
    public class DaemonTests
    {
        private static string NewPipeName() => $"ORCA.Test.{Guid.NewGuid():N}";

        private static Service NewService() => new(() => new RecordingPort());

        private static Task<T> OnDedicatedThread<T>(Func<T> action)
            => Task.Factory.StartNew(action, TaskCreationOptions.LongRunning);

        private static Task<int> StartDaemon(string pipeName, Service service)
        {
            var daemon = OnDedicatedThread(() => Daemon.Listen(pipeName, service));

            Assert.True(TestClient.Call(pipeName, new Request("status", [])).Response.Ok);

            return daemon;
        }

        private static Task<(Response Response, string[] Progress)> RunInBackground(string pipeName, Service service, string macro)
        {
            var running = OnDedicatedThread(() => TestClient.Call(pipeName, new Request("run", ["macro.txt", "--dry-run"], macro)));
            Assert.True(SpinWait.SpinUntil(() => service.HasRunningMacro, 5000));

            return running;
        }

        [Fact]
        public async Task マクロ実行中でも別のクライアントがstatusなどの非干渉コマンドを実行できること()
        {
            var pipeName = NewPipeName();
            var service = NewService();
            var daemon = StartDaemon(pipeName, service);
            var running = RunInBackground(pipeName, service, "Press A -d=5000");

            var status = TestClient.Call(pipeName, new Request("status", [])).Response;

            Assert.True(status.Ok);
            Assert.Contains("running", status.Lines);

            Assert.True(TestClient.Call(pipeName, new Request("shutdown", [])).Response.Ok);
            Assert.Contains("macro cancelled", (await running).Response.Lines);
            Assert.Equal(0, await daemon);
        }

        [Fact]
        public async Task 同じパイプ名でデーモンを二重に起動すると2つ目が拒否されること()
        {
            var pipeName = NewPipeName();
            var service = NewService();
            var daemon = StartDaemon(pipeName, service);

            Assert.Equal(1, Daemon.Listen(pipeName, NewService()));

            Assert.True(TestClient.Call(pipeName, new Request("shutdown", [])).Response.Ok);
            Assert.Equal(0, await daemon);
        }

        [Fact]
        public async Task デーモンが停止した後は同じパイプ名で起動し直せること()
        {
            var pipeName = NewPipeName();
            var daemon = StartDaemon(pipeName, NewService());
            Assert.True(TestClient.Call(pipeName, new Request("shutdown", [])).Response.Ok);
            Assert.Equal(0, await daemon);

            var restarted = StartDaemon(pipeName, NewService());

            Assert.True(TestClient.Call(pipeName, new Request("shutdown", [])).Response.Ok);
            Assert.Equal(0, await restarted);
        }

        [Fact]
        public async Task 進捗はマクロを投げたクライアントにだけ届くこと()
        {
            var pipeName = NewPipeName();
            var service = NewService();
            var daemon = StartDaemon(pipeName, service);

            var running = RunInBackground(pipeName, service, "Wait 300\nWait 300");

            var status = TestClient.Call(pipeName, new Request("status", []));
            var (response, progress) = await running;

            Assert.Empty(status.Progress);
            Assert.Contains("macro finished", response.Lines);
            Assert.Contains("Wait 300", progress);

            Assert.True(TestClient.Call(pipeName, new Request("shutdown", [])).Response.Ok);
            Assert.Equal(0, await daemon);
        }
    }
}
