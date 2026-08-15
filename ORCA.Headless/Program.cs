using System;
using System.IO;
using System.Linq;

namespace ORCA.Headless
{
    static class Program
    {
        private const string Usage = """
            usage: orca <command> [args...]

              daemon [--port <port>]                      start the service
              ports                                     list available ports
              connect [port] [--no-rts] [--no-dtr]      connect to a port
              disconnect                                disconnect the port
              set-port <port>                            set the default port
              run <path> [--loop [count]] [--dry-run]   run a macro; Ctrl+C to cancel
              rerun [number] [--loop [count]] [--no-dry-run] rerun from history; defaults to the last one
              history                                   list macro history
              shutdown                                  stop the service

            commands other than daemon are sent to a running service. use --json for raw JSON output.
            """;

        static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                Console.WriteLine(Usage);
                return args.Length == 0 ? 1 : 0;
            }

            if (args[0] == "daemon")
            {
                var portIndex = Array.IndexOf(args, "--port");
                var defaultPort = portIndex >= 0 && portIndex + 1 < args.Length ? args[portIndex + 1] : null;
                return Daemon.Run(defaultPort);
            }

            var json = args.Contains("--json");
            var quiet = args.Contains("--quiet");
            string[] rest = [.. args.Skip(1).Where(a => a is not "--json" and not "--quiet")];

            // マクロ本文はクライアントが読んで送る. デーモンからパスが見えている必要はない
            string body = null;
            if (args[0] == "run" && rest.Length > 0)
            {
                try
                {
                    body = File.ReadAllText(rest[0]);
                    rest[0] = Path.GetFullPath(rest[0]);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"failed to read {rest[0]}: {ex.Message}");
                    return 1;
                }
            }

            return Client.Send(new Request(args[0], rest, body), json, quiet);
        }
    }
}
