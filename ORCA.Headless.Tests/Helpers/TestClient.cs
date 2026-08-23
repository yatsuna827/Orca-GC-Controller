using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace ORCA.Headless.Tests
{
    static class TestClient
    {
        public static (Response Response, string[] Progress) Call(string pipeName, Request request, int connectTimeout = 5000)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            pipe.Connect(connectTimeout);

            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);

            writer.WriteLine(JsonSerializer.Serialize(request, Protocol.Json));

            var progress = new List<string>();
            while (true)
            {
                var line = reader.ReadLine() ?? throw new IOException("no response from service");
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("ok", out _))
                    return (JsonSerializer.Deserialize<Response>(line, Protocol.Json), [.. progress]);

                progress.Add(JsonSerializer.Deserialize<Progress>(line, Protocol.Json).Text);
            }
        }
    }
}
