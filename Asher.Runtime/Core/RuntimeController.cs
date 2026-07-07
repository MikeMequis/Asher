using System;
using System.IO;

namespace Asher.Runtime.Core
{
    public sealed class RuntimeController
    {
        private RuntimeContext? _context;
        private readonly object _lockObj = new object();

        private bool _initialized;
        public bool IsInitialized => _initialized;

        public void Init(RuntimeContext context)
        {
            lock (_lockObj)
            {
                if (_initialized)
                {
                    RuntimeLogger.Warning("Runtime already initialized");
                    return;
                }

                try
                {
                    RuntimeLogger.Init(context.LogPath);
                    Validate(context);
                    PrepareDirectories(context);
                    LoadConfiguration(context);

                    _context = context;
                    _initialized = true;

                    RuntimeLogger.Info($"[Runtime] Ready (game: {context.GamePath})");
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error("Runtime initialization failed", ex);
                    throw;
                }
            }
        }

        private static void Validate(RuntimeContext context)
        {
            if (!Directory.Exists(context.GamePath))
            {
                var error = $"GamePath does not exist: {context.GamePath}";
                RuntimeLogger.Error(error);
                throw new DirectoryNotFoundException(error);
            }

            if (string.IsNullOrWhiteSpace(context.GamePath))
                throw new ArgumentException("GamePath cannot be empty", nameof(context.GamePath));
        }

        private static void PrepareDirectories(RuntimeContext context)
        {
            Directory.CreateDirectory(context.ModsPath);
            Directory.CreateDirectory(Path.Combine(context.ModsPath, "config"));
            Directory.CreateDirectory(Path.Combine(context.ModsPath, "cache"));
        }

        private static void LoadConfiguration(RuntimeContext context)
        {
            var configFile = Path.Combine(context.ModsPath, "config", "runtime.cfg");

            if (!File.Exists(configFile))
                return;

            try
            {
                File.ReadAllText(configFile);
                // TODO: Parse configuration
            }
            catch (Exception ex)
            {
                RuntimeLogger.Warning($"Failed to load configuration: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            lock (_lockObj)
            {
                if (!_initialized)
                    return;

                try
                {
                    RuntimeLogger.Flush();
                    _initialized = false;
                    _context = null;
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error("Error during shutdown", ex);
                }
            }
        }
    }
}
