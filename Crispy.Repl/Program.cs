using System;

namespace Crispy.Repl
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0 &&
                (string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Usage: dotnet run --project Crispy.Repl");
                Console.WriteLine("Starts an interactive Crispy REPL session.");
                return 0;
            }

            var session = new ReplSession();

            Console.WriteLine("Crispy REPL");
            Console.WriteLine("Type :help for commands. Ctrl+D or :quit exits.");

            for (; ; )
            {
                Console.Write(session.Prompt);
                var line = Console.ReadLine();
                if (line == null)
                {
                    Console.WriteLine();
                    return 0;
                }

                var result = session.SubmitLine(line);
                switch (result.Kind)
                {
                    case ReplSubmissionKind.None:
                    case ReplSubmissionKind.Incomplete:
                        continue;
                    case ReplSubmissionKind.Executed:
                        Console.WriteLine("=> " + result.DisplayText);
                        break;
                    case ReplSubmissionKind.Info:
                        Console.WriteLine(result.DisplayText);
                        break;
                    case ReplSubmissionKind.Error:
                        Console.Error.WriteLine("error: " + result.DisplayText);
                        break;
                    case ReplSubmissionKind.Exit:
                        return 0;
                }
            }
        }
    }
}
