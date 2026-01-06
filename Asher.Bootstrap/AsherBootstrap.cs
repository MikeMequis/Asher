using System;
using System.IO;
using System.Reflection;

namespace Asher.Bootstrap
{
    public static class AsherBootstrap
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        // Force initialization by accessing a static member
        // This static field initializer runs when the type is first accessed
        private static readonly bool _forceInit = ForceInitialization();

        private static bool ForceInitialization()
        {
            try
            {
                EnsureInitialized();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AsherLogs", "bootstrap_force_init.log");
                    var logDir = Path.GetDirectoryName(logPath);
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Force init error: {ex}\n");
                }
                catch { }
                return false;
            }
        }

        static AsherBootstrap()
        {
            // Auto-initialize when the type is first accessed
            try
            {
                EnsureInitialized();
            }
            catch (Exception ex)
            {
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

        // Public method that can be called via CLR hosting or reflection
        // This ensures initialization happens even if static constructor doesn't run
        public static void EntryPoint()
        {
            EnsureInitialized();
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

                var bootstrapLog = Path.Combine(logDir, "bootstrap.log");
                File.AppendAllText(
                    bootstrapLog,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Asher bootstrap inicializado no processo do jogo.\n"
                );
                File.AppendAllText(
                    bootstrapLog,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Base Directory: {baseDir}\n"
                );
                File.AppendAllText(
                    bootstrapLog,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}\n"
                );

                LoadRuntime(baseDir, logDir);
            }
            catch (Exception ex)
            {
                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var logDir = Path.Combine(baseDir, "AsherLogs");
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    
                    File.AppendAllText(
                        Path.Combine(logDir, "bootstrap_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ERROR: {ex}\n{ex.StackTrace}\n"
                    );
                }
                catch { }
                
                // Fallback log
                try
                {
                    File.AppendAllText(
                        "asher_fatal.log",
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ERROR: {ex}\n"
                    );
                }
                catch { }
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
