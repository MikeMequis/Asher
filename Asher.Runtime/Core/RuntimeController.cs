using System;
using System.IO;

namespace Asher.Runtime.Core
{
    public sealed class RuntimeController
    {
        private bool _initialized;
        private RuntimeContext? _context;
        private readonly object _lockObj = new object();

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

        public RuntimeResult Execute(string operation, Action action)
        {
            if (!_initialized)
                return RuntimeResult.Fail("Runtime not initialized");

            try
            {
                RuntimeLogger.Info($"Executing operation: {operation}");
                action?.Invoke();
                RuntimeLogger.Info($"Operation completed: {operation}");
                return RuntimeResult.Ok();
            }
            catch (Exception ex)
            {
                RuntimeLogger.Error($"Operation failed: {operation}", ex);
                return RuntimeResult.Fail(ex);
            }
        }

        public RuntimeResult ExecuteWithResult(string operation, Func<RuntimeResult> func)
        {
            if (!_initialized)
                return RuntimeResult.Fail("Runtime not initialized");

            try
            {
                RuntimeLogger.Info($"Executing operation: {operation}");
                var result = func?.Invoke() ?? RuntimeResult.Fail("No function provided");

                if (result.Success)
                    RuntimeLogger.Info($"Operation completed successfully: {operation}");
                else
                    RuntimeLogger.Error($"Operation failed: {operation} - {result.ErrorMessage}");

                return result;
            }
            catch (Exception ex)
            {
                RuntimeLogger.Error($"Operation threw exception: {operation}", ex);
                return RuntimeResult.Fail(ex);
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

            // Validate paths are not empty or whitespace
            if (string.IsNullOrWhiteSpace(context.GamePath))
                throw new ArgumentException("GamePath cannot be empty", nameof(context.GamePath));

            RuntimeLogger.Info("Context validation successful");
        }

        private void PrepareDirectories(RuntimeContext context)
        {
            RuntimeLogger.Info("Preparing directories...");

            // Create mods directory if needed
            if (!Directory.Exists(context.ModsPath))
            {
                Directory.CreateDirectory(context.ModsPath);
                RuntimeLogger.Info($"Created mods directory: {context.ModsPath}");
            }
            else
                RuntimeLogger.Info($"Mods directory exists: {context.ModsPath}");

            // Create subdirectories for organization
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
                    RuntimeLogger.Info("Performing cleanup..."); // Perform cleanup operations

                    RuntimeLogger.Flush(); // Flush any pending operations

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