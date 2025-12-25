using System;
using System.IO;
using System.Reflection;

namespace Asher.Bootstrap
{
    public static class AsherBootstrap
    {
        public static void Initialize()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logDir = Path.Combine(baseDir, "AsherLogs");

                Directory.CreateDirectory(logDir);

                File.AppendAllText(
                    Path.Combine(logDir, "bootstrap.log"),
                    "[Asher] Bootstrap carregado dentro do processo do jogo.\n"
                );

                LoadRuntime(baseDir, logDir);
            }
            catch (Exception ex)
            {
                File.AppendAllText("asher_fatal.log", ex.ToString());
            }
        }

        private static void LoadRuntime(string baseDir, string logDir)
        {
            var runtimePath = Path.Combine(baseDir, "Asher.Runtime.dll");

            var asm = Assembly.LoadFrom(runtimePath);

            var runtimeType = asm.GetType("Asher.Runtime.AsherRuntime");
            var runtime = Activator.CreateInstance(runtimeType);

            var contextType = asm.GetType("Asher.Runtime.RuntimeContext");

            var context = Activator.CreateInstance(contextType);

            contextType.GetProperty("GamePath")
                ?.SetValue(context, baseDir);

            contextType.GetProperty("LogPath")
                ?.SetValue(context, logDir);

            runtimeType
                .GetMethod("Initialize")
                ?.Invoke(runtime, new[] { context });
        }
    }
}
