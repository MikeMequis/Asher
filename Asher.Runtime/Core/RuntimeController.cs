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
                    RuntimeLogger.Info("=== Runtime Initialization Started ===");
                    RuntimeLogger.Info($"Version: 1.0.0");
                    RuntimeLogger.Info($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    Validate(context);
                    PrepareDirectories(context);
                    LoadConfiguration(context);

                    _context = context;
                    _initialized = true;

                    RuntimeLogger.Info("=== Runtime Initialization Complete ===");
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
            RuntimeLogger.Info("Validating runtime context...");

            if (!Directory.Exists(context.GamePath))
            {
                var error = $"GamePath does not exist: {context.GamePath}";
                RuntimeLogger.Error(error);
                throw new DirectoryNotFoundException(error);
            }

            RuntimeLogger.Info($"GamePath: {context.GamePath}");
            RuntimeLogger.Info($"ModsPath: {context.ModsPath}");
            RuntimeLogger.Info($"Profile: {context.ProfileName}");
            RuntimeLogger.Info($"LogPath: {context.LogPath}");

            if (string.IsNullOrWhiteSpace(context.GamePath))
                throw new ArgumentException("GamePath cannot be empty", nameof(context.GamePath));

            RuntimeLogger.Info("Context validation successful");
        }

        private void PrepareDirectories(RuntimeContext context)
        {
            RuntimeLogger.Info("Preparing directories...");

            if (!Directory.Exists(context.ModsPath))
            {
                Directory.CreateDirectory(context.ModsPath);
                RuntimeLogger.Info($"Created mods directory: {context.ModsPath}");
            }

            var configPath = Path.Combine(context.ModsPath, "config");
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
                RuntimeLogger.Info($"Created config directory: {configPath}");
            }

            var cachePath = Path.Combine(context.ModsPath, "cache");
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
                RuntimeLogger.Info($"Created cache directory: {cachePath}");
            }

            RuntimeLogger.Info("Directory preparation complete");
        }

        private void LoadConfiguration(RuntimeContext context)
        {
            RuntimeLogger.Info("Loading configuration...");

            var configFile = Path.Combine(context.ModsPath, "config", "runtime.cfg");

            if (File.Exists(configFile))
            {
                try
                {
                    var configContent = File.ReadAllText(configFile);
                    RuntimeLogger.Info($"Configuration loaded from: {configFile}");
                    // TODO: Parse configuration
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Warning($"Failed to load configuration: {ex.Message}");
                }
            }
            else
                RuntimeLogger.Info("No configuration file found, using defaults");
        }

        public void Shutdown()
        {
            lock (_lockObj)
            {
                if (!_initialized)
                    return;

                RuntimeLogger.Info("=== Runtime Shutdown Started ===");

                try
                {
                    RuntimeLogger.Flush();
                    _initialized = false;
                    _context = null;

                    RuntimeLogger.Info("=== Runtime Shutdown Complete ===");
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error("Error during shutdown", ex);
                }
            }
        }
    }
}