using Asher.Runtime.Bootstrap;
using Asher.Runtime.Core;
using Asher.Runtime.Logging;
using Asher.SDK.Logging;
using System;

namespace Asher.Runtime
{
    public static class RuntimeEntry
    {
        private static RuntimeController? _controller;
        private static readonly object _lockObj = new object();
        private static bool _isInitialized = false;

        public static bool IsInitialized => _isInitialized;

        public static void Init(RuntimeContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            AsherLog.Logger = new RuntimeLoggerAdapter();

            RuntimeLogger.Info("Asher SDK Logger conectado ao Runtime.");

            lock (_lockObj)
            {
                if (_controller != null)
                {
                    RuntimeLogger.Warning("Runtime already initialized, ignoring duplicate Init call");
                    return;
                }

                try
                {
                    _controller = new RuntimeController();
                    _controller.Init(context);
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Fatal(ex);
                    _controller = null;
                    _isInitialized = false;
                    throw;
                }
            }
        }

        public static RuntimeResult Execute(string operation, Action action)
        {
            if (!_isInitialized || _controller == null)
                return RuntimeResult.Fail("Runtime not initialized");

            return _controller.Execute(operation, action);
        }

        public static RuntimeResult ExecuteWithResult(string operation, Func<RuntimeResult> func)
        {
            if (!_isInitialized || _controller == null)
                return RuntimeResult.Fail("Runtime not initialized");

            return _controller.ExecuteWithResult(operation, func);
        }

        public static void Shutdown()
        {
            lock (_lockObj)
            {
                if (_controller != null)
                {
                    RuntimeLogger.Info("Shutting down runtime");
                    _controller.Shutdown();
                    _controller = null;
                    _isInitialized = false;
                    RuntimeLogger.Shutdown();
                }
            }
        }

        public static void OnGameAssemblyLoaded()
        {
            if (!_isInitialized)
            {
                RuntimeLogger.Warning("Game assembly loaded before runtime initialization.");
                return;
            }

            HarmonyBootstrap.Initialize();
        }
    }
}