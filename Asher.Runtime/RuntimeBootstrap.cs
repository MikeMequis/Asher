using Asher.Runtime.Bootstrap;
using Asher.Runtime.Core;
using System;
using System.IO;

namespace Asher.Runtime
{
    /// <summary>
    /// Native-facing entry point for the Linux Dust process.
    ///
    /// It initializes the real Asher runtime and then runs the same generic bootstrap sequence as
    /// the Windows launcher: load mod assemblies, run PreInit modules, apply patch modules through
    /// Harmony. Patch implementations remain in their own Asher.Patching.* assemblies; this type
    /// contains no patch-specific logic.
    /// </summary>
    public static class RuntimeBootstrap
    {
        public static void Initialize()
        {
            string gamePath = AppDomain.CurrentDomain.BaseDirectory;

            string asherHome = Environment.GetEnvironmentVariable("ASHER_HOME");
            if (string.IsNullOrEmpty(asherHome))
                asherHome = Path.Combine(gamePath, "Asher");

            string modsPath = Environment.GetEnvironmentVariable("ASHER_MODS_PATH");
            if (string.IsNullOrEmpty(modsPath))
                modsPath = Path.Combine(asherHome, "Mods");

            string logPath = Environment.GetEnvironmentVariable("ASHER_LOG_PATH");
            if (string.IsNullOrEmpty(logPath))
                logPath = Path.Combine(asherHome, "Logs");

            string profileName = Environment.GetEnvironmentVariable("ASHER_PROFILE");
            if (string.IsNullOrEmpty(profileName))
                profileName = "default";

            var context = new RuntimeContext(gamePath, modsPath, profileName, logPath);

            RuntimeEntry.Init(context);
            Console.WriteLine("[Asher] Runtime initialized");

            AssemblyLoader.LoadAssembliesFrom(context.ModsPath);
            PreInitBootstrap.ExecutePreInitModules();
            PatchModuleLoader.Load();

            Console.WriteLine("[Asher] Bootstrap completed");
        }
    }
}
