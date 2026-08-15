using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ORCA.Headless
{
    static class Daemon
    {
        public static int Run(string defaultPort = null)
        {
            static NamedPipeServerStream NewServer() =>
                new(Protocol.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            NamedPipeServerStream pipe;
            try { pipe = NewServer(); }
            catch (IOException)
            {
                Console.Error.WriteLine("service is already running");
                return 1;
            }

            var service = new Service(defaultPort: defaultPort);
            foreach (var name in service.PluginCommands)
                Console.WriteLine($"loaded plugin: {name}");

            Console.CancelKeyPress += (_, _) => service.Handle(new Request("shutdown", []));

            Console.WriteLine($@"listening: \\.\pipe\{Protocol.PipeName}");

            while (true)
            {
                using (pipe)
                {
                    pipe.WaitForConnection();
                    try { Serve(pipe, service); }
                    // クライアントが応答を待たずに切断した
                    catch (IOException) { }
                }

                if (!service.Running) return 0;

                pipe = NewServer();
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
