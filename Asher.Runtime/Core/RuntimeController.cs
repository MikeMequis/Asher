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
                    RuntimeLogger.Warning("RuntimeController already initialized");
                    return;
                }

                try
                {
                    RuntimeLogger.Init(context.LogPath);
                    RuntimeLogger.Info("Runtime initialization started");

                    Validate(context);
                    PrepareDirectories(context);
                    LoadConfiguration(context);

                    _context = context;
                    _initialized = true;

                    RuntimeLogger.Info("Runtime initialization complete");
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error("Runtime initialization failed", ex);
                    throw;
                }
            }
        }

        private void Validate(RuntimeContext context)
        {
            if (string.IsNullOrWhiteSpace(context.GamePath))
                throw new ArgumentException("GamePath cannot be empty", nameof(context.GamePath));

            if (!Directory.Exists(context.GamePath))
            {
                var error = $"GamePath does not exist: {context.GamePath}";
                RuntimeLogger.Error(error);
                throw new DirectoryNotFoundException(error);
            }

            RuntimeLogger.Info(
                $"Context OK | game={context.GamePath} | mods={context.ModsPath} | profile={context.ProfileName}");
        }

        private void PrepareDirectories(RuntimeContext context)
        {
            EnsureDirectory(context.ModsPath);
            EnsureDirectory(Path.Combine(context.ModsPath, "config"));
            EnsureDirectory(Path.Combine(context.ModsPath, "cache"));
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private void LoadConfiguration(RuntimeContext context)
        {
            var configFile = Path.Combine(context.ModsPath, "config", "runtime.cfg");

            if (!File.Exists(configFile))
                return;

            try
            {
                File.ReadAllText(configFile);
                RuntimeLogger.Info($"Loaded configuration from {configFile}");
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

                RuntimeLogger.Info("Runtime shutdown started");

                try
                {
                    RuntimeLogger.Flush();
                    _initialized = false;
                    _context = null;

                    RuntimeLogger.Info("Runtime shutdown complete");
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error("Error during shutdown", ex);
                }
            }
        }
    }
}