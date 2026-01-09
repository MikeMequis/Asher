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

            // 1️⃣ Inicializa Runtime ANTES do jogo
            var context = new RuntimeContext(
                gamePath: baseDir,
                modsPath: Path.Combine(baseDir, "Mods"),
                profileName: "default",
                logPath: Path.Combine(baseDir, "AsherLogs")
            );

            RuntimeEntry.Init(context);

            // 2️⃣ Carrega o jogo como Assembly
            var gameAssembly = Assembly.LoadFrom(gameExe);

            // 3️⃣ Localiza Program.Main
            var programType = gameAssembly.GetType("Dust.Program");
            var mainMethod = programType?.GetMethod(
                "Main",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (mainMethod == null)
                throw new InvalidOperationException("Dust.Program.Main não encontrado.");

            // 4️⃣ Executa o jogo
            mainMethod.Invoke(null, new object[] { args });
        }
    }
}
