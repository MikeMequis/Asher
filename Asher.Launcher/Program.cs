using Asher.Runtime;
using Asher.Runtime.Bootstrap;
using Asher.Runtime.Core;
using Asher.SDK.Logging;
using System.Reflection;

namespace Asher.Launcher
{
    /// <summary>
    /// Ponto de entrada do Asher Launcher.
    /// Este executável substitui o DustAET.exe original e carrega o runtime + mods antes de iniciar o jogo.
    /// </summary>
    internal static class Program
    {
        private const string AsherFolderName = "Asher";
        private const string ModsFolderName = "Mods";
        private const string LogsFolderName = "AsherLogs";
        private const string OriginalGameExe = "DustAET.real.exe";

        static void Main(string[] args)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var asherDir = Path.Combine(baseDir, AsherFolderName);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
                ResolveAsherAssembly(asherDir, resolveArgs);

            try
            {
                ValidateInstallation(baseDir);

                // Caminhos da estrutura Asher/
                var modsPath = Path.Combine(asherDir, ModsFolderName);
                var logsPath = Path.Combine(asherDir, LogsFolderName);
                var gameExe = Path.Combine(baseDir, OriginalGameExe);

                // 1 - Inicializar Runtime
                AsherLog.Info("=== Asher Launcher iniciado ===");
                AsherLog.Info($"Diretório do jogo: {baseDir}");
                AsherLog.Info($"Diretório Asher: {asherDir}");

                var context = new RuntimeContext(
                    gamePath: baseDir,
                    modsPath: modsPath,
                    profileName: "default",
                    logPath: logsPath
                );

                RuntimeEntry.Init(context);
                AsherLog.Info("Runtime inicializado com sucesso.");

                // 2 - Carregar assembly do jogo original
                AsherLog.Info($"Carregando assembly do jogo: {gameExe}");
                var gameAssembly = Assembly.LoadFrom(gameExe);
                AsherLog.Info($"Assembly do jogo carregado: {gameAssembly.FullName}");

                // 3 - Carregar Mods
                AsherLog.Info($"Carregando mods de: {modsPath}");
                AssemblyLoader.LoadAssembliesFrom(modsPath);
                AsherLog.Info("Mods carregados com sucesso.");

                // 4 - Executar PreInit de todos os módulos
                AsherLog.Info("Executando PreInit dos módulos...");
                PreInitBootstrap.ExecutePreInitModules();
                AsherLog.Info("PreInit concluído.");

                // 5 - Aplicar patches (Harmony)
                AsherLog.Info("Aplicando patches de módulos...");
                PatchModuleLoader.Load();
                AsherLog.Info("Patches aplicados com sucesso.");

                // 6 - Executar Dust.Program.Main
                AsherLog.Info("Procurando Dust.Program.Main...");
                var programType = gameAssembly.GetType("Dust.Program");
                if (programType == null)
                    throw new InvalidOperationException("Tipo 'Dust.Program' não encontrado no assembly do jogo.");

                var mainMethod = programType.GetMethod("Main",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );
                if (mainMethod == null)
                    throw new InvalidOperationException("Método 'Dust.Program.Main' não encontrado.");

                AsherLog.Info("Iniciando Dust.Program.Main...");
                AsherLog.Info("=== Transferindo controle para o jogo ===");

                mainMethod.Invoke(null, new object[] { args });
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
        }

        /// <summary>
        /// Valida se a instalação do Asher está correta
        /// </summary>
        private static void ValidateInstallation(string baseDir)
        {
            var gameExe = Path.Combine(baseDir, OriginalGameExe);
            if (!File.Exists(gameExe))
            {
                throw new FileNotFoundException(
                    $"Executável original do jogo não encontrado: {OriginalGameExe}\n" +
                    "A instalação do Asher pode estar corrompida. Execute o instalador novamente."
                );
            }

            var asherDir = Path.Combine(baseDir, AsherFolderName);
            if (!Directory.Exists(asherDir))
            {
                throw new DirectoryNotFoundException(
                    $"Pasta Asher não encontrada: {asherDir}\n" +
                    "A instalação do Asher pode estar corrompida. Execute o instalador novamente."
                );
            }

            // Verifica arquivos essenciais do runtime
            var requiredFiles = new[]
            {
                Path.Combine(asherDir, "Asher.Runtime.dll"),
                Path.Combine(asherDir, "Asher.SDK.dll"),
                Path.Combine(asherDir, "0Harmony.dll")
            };

            foreach (var file in requiredFiles)
            {
                if (!File.Exists(file))
                {
                    throw new FileNotFoundException(
                        $"Arquivo essencial não encontrado: {Path.GetFileName(file)}\n" +
                        "A instalação do Asher pode estar corrompida. Execute o instalador novamente."
                    );
                }
            }
        }

        /// <summary>
        /// Resolve assemblies da pasta Asher/ quando o CLR não consegue encontrá-los
        /// </summary>
        private static Assembly ResolveAsherAssembly(string asherDir, ResolveEventArgs args)
        {
            try
            {
                // Extrai o nome simples do assembly (sem versão, cultura, etc)
                var assemblyName = new AssemblyName(args.Name).Name;

                // Procura na pasta Asher/
                var assemblyPath = Path.Combine(asherDir, assemblyName + ".dll");

                if (File.Exists(assemblyPath))
                {
                    AsherLog.Info($"Resolvendo assembly: {assemblyName} de {assemblyPath}");
                    return Assembly.LoadFrom(assemblyPath);
                }

                // Procura na pasta Mods/
                var modsPath = Path.Combine(asherDir, ModsFolderName);
                var modAssemblyPath = Path.Combine(modsPath, assemblyName + ".dll");

                if (File.Exists(modAssemblyPath))
                {
                    AsherLog.Info($"Resolvendo assembly: {assemblyName} de {modAssemblyPath}");
                    return Assembly.LoadFrom(modAssemblyPath);
                }
            }
            catch (Exception ex)
            {
                AsherLog.Warning($"Erro ao resolver assembly '{args.Name}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Trata erros fatais que impedem a execução
        /// </summary>
        private static void HandleFatalError(Exception ex)
        {
            try
            {
                AsherLog.Error($"ERRO FATAL no Asher Launcher:");
                AsherLog.Error($"Tipo: {ex.GetType().Name}");
                AsherLog.Error($"Mensagem: {ex.Message}");
                AsherLog.Error($"Stack Trace:\n{ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    AsherLog.Error($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            catch
            {
                // Se o log falhar, tenta escrever no console
                Console.Error.WriteLine($"ERRO FATAL: {ex}");
            }

            // Mostra mensagem ao usuário
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("ERRO FATAL - O Asher não pôde iniciar o jogo");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"\n{ex.GetType().Name}: {ex.Message}\n");
            Console.WriteLine("Verifique o arquivo de log em Asher/AsherLogs/ para mais detalhes.");
            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();

            Environment.Exit(1);
        }
    }
}