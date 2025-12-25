using Asher.Runtime.Implementations;
using Asher.Runtime.Interfaces;
using System.Diagnostics;
using System.IO;

namespace Asher.App.Modules.Bootstrap
{
    public class RuntimeBootstrapper
    {
        public void Initialize()
        {
            var launcherDir = AppContext.BaseDirectory;

            var gameExePath = LocateGameExecutable();
            var logPath = Path.Combine(launcherDir, "Logs");

            var context = new RuntimeContext(launcherDir, gameExePath, logPath);

            IAsherRuntime runtime = new AsherRuntime();
            runtime.Initialize(context);

            LaunchGame(gameExePath);
        }

        private string LocateGameExecutable()
        {
            // por enquanto: caminho fixo ou configurável
            return @"C:\Games\Steam\steamapps\common\Dust An Elysian Tail\DustAET.exe";
        }

        private void LaunchGame(string exePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true // 🔑 Steam feliz
            };

            Process.Start(startInfo);
        }
    }
}
