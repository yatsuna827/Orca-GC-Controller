using System;
using System.Linq;

namespace ORCA.Headless
{
    static class Program
    {
        static void Main(string[] args)
        {
            var dryRun = args.Contains("--dry-run");
            var session = dryRun
                ? new Session(Console.WriteLine, () => new DryRunPort())
                : new Session(Console.WriteLine);

            // 実行中のみ、Ctrl+Cをマクロの中断にあてる
            Console.CancelKeyPress += (_, e) =>
            {
                if (!session.IsMacroRunning) return;

                e.Cancel = true;
                session.Execute("cancel");
            };

            var portIndex = Array.IndexOf(args, "--port");
            if (dryRun)
                session.Execute("connect dry-run");
            else if (portIndex >= 0 && portIndex + 1 < args.Length)
                session.Execute($"connect {args[portIndex + 1]}");

            while (session.Running)
            {
                var line = Console.ReadLine();
                session.Execute(line ?? "quit");
            }
        }
    }
}
