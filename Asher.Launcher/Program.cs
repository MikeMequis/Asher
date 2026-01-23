using Asher.Runtime;
using Asher.Runtime.Bootstrap;
using Asher.Runtime.Core;
using Asher.SDK.Logging;
using System.Reflection;

namespace Asher.Launcher
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var gameExe = Path.Combine(baseDir, "DustAET.real.exe");

                if (!File.Exists(gameExe))
                    throw new FileNotFoundException("DustAET.real.exe não encontrado.");

                // 1 - Runtime base
                var context = new RuntimeContext(
                    gamePath: baseDir,
                    modsPath: Path.Combine(baseDir, "Mods"),
                    profileName: "default",
                    logPath: Path.Combine(baseDir, "AsherLogs")
                );

                RuntimeEntry.Init(context);
                AsherLog.Info("Runtime inicializado.");

                // 2 - Carrega o assembly do jogo
                var gameAssembly = Assembly.LoadFrom(gameExe);
                AsherLog.Info("Assembly do jogo carregado.");

                // 3 - Carrega Mods
                AsherLog.Info("Iniciando carregamento de mods...");
                AssemblyLoader.LoadAssembliesFrom(context.ModsPath);
                AsherLog.Info("Mods carregados.");

                // 4 - PreInit
                AsherLog.Info("Iniciando PreInit...");
                PreInitBootstrap.ExecutePreInitModules();
                AsherLog.Info("PreInit concluído.");

                // 5 - Aplica TODOS os patches ANTES de iniciar o jogo
                AsherLog.Info("Aplicando patches de módulos...");
                PatchModuleLoader.Load();
                AsherLog.Info("Patches aplicados.");

                // 6 - Executa Dust.Program.Main
                var programType = gameAssembly.GetType("Dust.Program");
                if (programType == null)
                    throw new InvalidOperationException("Tipo Dust.Program não encontrado.");

                var mainMethod = programType.GetMethod("Main",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (mainMethod == null)
                    throw new InvalidOperationException("Dust.Program.Main não encontrado.");

                AsherLog.Info("Iniciando Dust.Program.Main...");
                mainMethod.Invoke(null, new object[] { args });
            }
            catch (Exception ex)
            {
                try
                {
                    AsherLog.Error($"Erro fatal no launcher:: {ex}");
                }
                catch
                {
                    Console.Error.WriteLine($"ERRO FATAL: {ex}");
                }

                Console.WriteLine("\nPressione qualquer tecla para sair...");
                Console.ReadKey();

                Environment.Exit(1);
            }
        }
    }
}