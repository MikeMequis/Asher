using System;
using System.IO;
using System.Threading;

namespace Asher.Runtime
{
    internal static class RuntimeLogger
    {
        private static string _logFile = string.Empty;
        private static readonly object _lockObj = new object();
        private static bool _initialized = false;

        public static void Init(string logDir)
        {
            if (_initialized)
                return;

            try
            {
                Directory.CreateDirectory(logDir);
                _logFile = Path.Combine(logDir, $"runtime_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                _initialized = true;
                Info("Logger initialized successfully");
            }
            catch (Exception ex)
            {
                // Fallback - cannot log initialization failure
                Console.Error.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Info(string message)
            => Write("INFO", message);

        public static void Warning(string message)
            => Write("WARN", message);

        public static void Error(string message)
            => Write("ERROR", message);

        public static void Error(string message, Exception ex)
        {
            var fullMessage = $"{message} | Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Write("ERROR", fullMessage);

            Console.Error.WriteLine($"[ERROR] {fullMessage}");
        }

        public static void Fatal(Exception ex)
            => Write("FATAL", $"{ex.Message}\n{ex.StackTrace}");

        public static void Fatal(string message, Exception ex)
        {
            var fullMessage = $"{message} | Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Write("FATAL", fullMessage);

            Console.Error.WriteLine($"[FATAL] {fullMessage}");
        }

        public static void Flush() =>
            Info("Log flush requested"); // Ensure all logs are written (placeholder for future buffering)

        private static void Write(string level, string message)
        {
            if (!_initialized || string.IsNullOrEmpty(_logFile))
                return;

            try
            {
                lock (_lockObj)
                {
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] [Thread:{Thread.CurrentThread.ManagedThreadId}] {message}\n";
                    File.AppendAllText(_logFile, logEntry);
                }
            }
            catch
            {
                // Silent failure to prevent log cascades
            }
        }

        public static void Shutdown()
        {
            if (_initialized)
            {
                Info("Logger shutting down");
                _initialized = false;
            }
        }
    }
}