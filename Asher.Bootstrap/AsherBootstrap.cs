using System;
using System.IO;
using System.Reflection;

namespace Asher.Bootstrap
{
    public static class AsherBootstrap
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        static AsherBootstrap()
        {
            // Auto-initialize when the type is first accessed
            // This is called when the DLL is loaded and the type is first referenced
            try
            {
                EnsureInitialized();
            }
            catch (Exception ex)
            {
                // Log to a safe location
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AsherLogs", "bootstrap_static_ctor.log");
                    var logDir = Path.GetDirectoryName(logPath);
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);

                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Static constructor error: {ex}\n");
                }
                catch { }
            }
        }

        /// <summary>
        /// Public entry point that can be called after DLL injection to ensure initialization
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                Initialize();
                _initialized = true;
            }
        }

        public static void Initialize()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logDir = Path.Combine(baseDir, "AsherLogs");

                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                File.AppendAllText(
                    Path.Combine(logDir, "bootstrap.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Asher bootstrap inicializado no processo do jogo.\n"
                );

                LoadRuntime(baseDir, logDir);
            }
            catch (Exception ex)
            {
                File.AppendAllText(
                    "asher_fatal.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ERROR: {ex}\n"
                );
            }
        }

        private static void LoadRuntime(string baseDir, string logDir)
        {
            var runtimePath = Path.Combine(baseDir, "Asher.Runtime.dll");
            if (!File.Exists(runtimePath))
                throw new FileNotFoundException("Asher.Runtime.dll não encontrado no diretório base.", runtimePath);

            var asm = Assembly.LoadFrom(runtimePath);

            var runtimeType = asm.GetType("Asher.Runtime.Implementations.AsherRuntime", throwOnError: true);
            var contextType = asm.GetType("Asher.Runtime.Implementations.RuntimeContext", throwOnError: true);
            
            if (runtimeType == null || contextType == null)
                throw new InvalidOperationException("Failed to load required runtime types.");

            // RuntimeContext constructor: public RuntimeContext(string gamePath, string launcherPath, string logPath)
            var context = Activator.CreateInstance(contextType, new object[] { baseDir, string.Empty, logDir });

            var runtime = Activator.CreateInstance(runtimeType);

            runtimeType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)?.Invoke(runtime, new[] { context });
        }
    }
}
