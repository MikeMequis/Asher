using Asher.Host.Jsonl;

namespace Asher.Host
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Contains("--jsonl", StringComparer.OrdinalIgnoreCase))
                return JsonlHostSession.RunFromHostAsync().GetAwaiter().GetResult();

            return SmokeRunner.Run(args);
        }
    }
}
