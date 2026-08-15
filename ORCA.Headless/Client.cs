using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace ORCA.Headless
{
    static class Client
    {
        public static int Send(Request request, bool json, bool quiet)
        {
            using var pipe = new NamedPipeClientStream(".", Protocol.PipeName, PipeDirection.InOut);
            try { pipe.Connect(2000); }
            catch (TimeoutException)
            {
                Console.Error.WriteLine("could not connect to service. start it with: orca daemon");
                return 1;
            }

            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);

            writer.WriteLine(JsonSerializer.Serialize(request, Protocol.Json));

            while (true)
            {
                var line = reader.ReadLine();
                if (line is null)
                {
                    Console.Error.WriteLine("no response from service");
                    return 1;
                }

                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("ok", out _))
                {
                    var response = JsonSerializer.Deserialize<Response>(line, Protocol.Json);
                    if (json)
                        Console.WriteLine(line);
                    else
                        foreach (var l in response.Lines)
                            (response.Ok ? Console.Out : Console.Error).WriteLine(l);
                    return response.Ok ? 0 : 1;
                }

                if (quiet) continue;

                if (json)
                    Console.WriteLine(line);
                else
                {
                    var progress = JsonSerializer.Deserialize<Progress>(line, Protocol.Json);
                    Console.WriteLine($"| {progress.Text}");
                }
            }
        }
    }
}
