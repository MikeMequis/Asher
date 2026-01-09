using System;
using System.IO;

namespace Asher.Runtime.Logging
{
    internal static class RuntimeLogger
    {
        private static string _logFile = string.Empty;

        public static void Init(string logDir)
        {
            Directory.CreateDirectory(logDir);
            _logFile = Path.Combine(logDir, "runtime.log");

            Info("Logger inicializado");
        }

        public static void Info(string message)
            => Write("INFO", message);

        public static void Error(string message)
            => Write("ERROR", message);

        public static void Fatal(Exception ex)
            => Write("FATAL", ex.ToString());

        private static void Write(string level, string message)
        {
            try
            {
                File.AppendAllText(
                    _logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}\n"
                );
            }
            catch
            {
                // silêncio absoluto
            }
        }
    }
}
