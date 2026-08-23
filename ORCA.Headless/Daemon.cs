using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ORCA.Headless
{
    static class Daemon
    {
        public static int Run(string defaultPort = null)
            => Listen(Protocol.PipeName, new Service(defaultPort: defaultPort));

        internal static int Listen(string pipeName, Service service)
        {
            // 多重起動の防止

            using var single = new Mutex(false, $@"Global\{pipeName}");

            var owned = false;
            try { owned = single.WaitOne(0); }
            // 前のプロセスがReleaseMutexを呼ばずに異常終了していた場合、
            // 例外は発生するが、Mutexの所有権自体は取得できている
            catch (AbandonedMutexException) { owned = true; }

            if (!owned)
            {
                Console.Error.WriteLine("service is already running");
                return 1;
            }

            // ほんたい

            try
            {
                foreach (var name in service.PluginCommands)
                    Console.WriteLine($"loaded plugin: {name}");

                Console.CancelKeyPress += (_, _) => service.Handle(new Request("shutdown", []));

                Console.WriteLine($@"listening: \\.\pipe\{pipeName}");

                var clients = new List<Task>();
                while (true)
                {
                    var pipe = NewServer(pipeName);
                    try { pipe.WaitForConnectionAsync(service.Stopping).GetAwaiter().GetResult(); }
                    catch (OperationCanceledException)
                    {
                        pipe.Dispose();
                        break;
                    }

                    clients.Add(Task.Factory.StartNew(() => ServeClient(pipe, service), TaskCreationOptions.LongRunning));
                    clients.RemoveAll(t => t.IsCompleted);
                }

                // サービスが停止に入ったあと、処理中のクライアントが応答を書き終えるために猶予を設ける
                Task.WaitAll([.. clients], 2000);

                return 0;
            }
            finally { single.ReleaseMutex(); }
        }

        private static NamedPipeServerStream NewServer(string pipeName) =>
            new(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        private static void ServeClient(NamedPipeServerStream pipe, Service service)
        {
            using (pipe)
            {
                try { Serve(pipe, service); }
                catch (IOException) { } // クライアント側が応答を待たずに終了した場合などに投げられる。正常な切断と同じ扱いでいい。
                catch (Exception ex) { Console.Error.WriteLine($"failed to serve a client: {ex.Message}"); }
            }
        }

        private static void Serve(NamedPipeServerStream pipe, Service service)
        {
            // MEMO: リクエストとレスポンスはどちらも1件あたり1行のJSON

            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };

            var line = reader.ReadLine();
            if (line is null) return;

            // pipeに送信されてきているデータは上の処理ですべて読み込まれているので、
            // クライアントがpipeを閉じた時にReadAsyncが発火されて、完了が検知できる
            var clientGone = new CancellationTokenSource();
            _ = pipe.ReadAsync(new byte[1]).AsTask().ContinueWith(t =>
            {
                _ = t.Exception;
                clientGone.Cancel();
            });

            Response response;
            try
            {
                var request = JsonSerializer.Deserialize<Request>(line, Protocol.Json);
                Console.WriteLine($"[{request?.Command}] {string.Join(" ", request?.Args ?? [])}");
                response = request?.Command is null
                    ? Response.Fail("missing command")
                    : service.Handle(request, progress =>
                    {
                        writer.WriteLine(JsonSerializer.Serialize(progress, Protocol.Json));
                    }, clientGone.Token);
            }
            catch (JsonException)
            {
                response = Response.Fail("invalid request");
            }

            writer.WriteLine(JsonSerializer.Serialize(response, Protocol.Json));
            pipe.WaitForPipeDrain();
        }

    }
}
