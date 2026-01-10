using Asher.Runtime;
using Asher.Runtime.Core;
using System.Reflection;

namespace Asher.Launcher
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var gameExe = Path.Combine(baseDir, "DustAET.real.exe");

            if (!File.Exists(gameExe))
                throw new FileNotFoundException("DustAET.real.exe não encontrado.");

            // 1️ - Inicializa Runtime (infra, logs, contexto)
            var context = new RuntimeContext(
                gamePath: baseDir,
                modsPath: Path.Combine(baseDir, "Mods"),
                profileName: "default",
                logPath: Path.Combine(baseDir, "AsherLogs")
            );

            RuntimeEntry.Init(context);

            // 2 - Carrega o Assembly do jogo
            var gameAssembly = Assembly.LoadFrom(gameExe);

            // 3️ - Notifica o Runtime
            RuntimeEntry.OnGameAssemblyLoaded();

            // 4️ - Localiza Dust.Program.Main
            var programType = gameAssembly.GetType("Dust.Program");
            var mainMethod = programType?.GetMethod(
                "Main",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (mainMethod == null)
                throw new InvalidOperationException("Dust.Program.Main não encontrado.");

            // 5️ - Executa o jogo
            mainMethod.Invoke(null, new object[] { args });
        }
    }
}