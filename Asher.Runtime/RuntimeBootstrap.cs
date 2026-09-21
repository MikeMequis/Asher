using Asher.Runtime.Core;
using Asher.Runtime.Diagnostics;
using System;
using System.IO;
using System.Reflection;

namespace Asher.Runtime
{
    /// <summary>
    /// Native-facing entry point for hosts that attach to an already-running Mono runtime
    /// (for example the Linux DustAET process). It builds a default <see cref="RuntimeContext"/>
    /// and initializes the real Asher runtime through <see cref="RuntimeEntry.Init"/>.
    ///
    /// It also contains the optional, environment-gated load step for the isolated Harmony PoC
    /// assembly. This type has no compile-time reference to 0Harmony or Asher.HarmonyPoc; the PoC
    /// assembly is loaded by reflection only after Asher.Runtime has fully initialized.
    /// </summary>
    public static class RuntimeBootstrap
    {
        private const string HarmonyPocTypeName = "Asher.HarmonyPoc.HarmonyPocBootstrap";
        private const string HarmonyPocMethodName = "Initialize";

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

            string introspect = Environment.GetEnvironmentVariable("ASHER_INTROSPECT");
            if (string.IsNullOrEmpty(introspect) || introspect != "0")
            {
                DustAssemblyProbe.Run();
                Console.WriteLine("[Asher] Dust assembly introspection completed successfully");
            }

            string harmonyPoc = Environment.GetEnvironmentVariable("ASHER_HARMONY_POC");
            if (harmonyPoc == "1")
            {
                LoadAndRunHarmonyPoc();
            }
        }

        private static void LoadAndRunHarmonyPoc()
        {
            string assemblyPath = Environment.GetEnvironmentVariable("ASHER_HARMONY_POC_ASSEMBLY");
            if (string.IsNullOrEmpty(assemblyPath))
            {
                Console.WriteLine("[Asher] Harmony PoC assembly path not configured");
                return;
            }

            Assembly pocAssembly;
            try
            {
                pocAssembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Asher] Harmony PoC assembly load failed");
                Console.WriteLine($"[Asher] {ex}");
                return;
            }

            Console.WriteLine("[Asher] Harmony PoC assembly loaded");

            MethodInfo entryPoint;
            try
            {
                var type = pocAssembly.GetType(HarmonyPocTypeName, throwOnError: false);
                if (type == null)
                {
                    Console.WriteLine(
                        $"[Asher] Harmony PoC entry point type not found: {HarmonyPocTypeName}");
                    return;
                }

                entryPoint = type.GetMethod(
                    HarmonyPocMethodName,
                    BindingFlags.Public | BindingFlags.Static);

                if (entryPoint == null)
                {
                    Console.WriteLine(
                        $"[Asher] Harmony PoC entry point method not found: " +
                        $"{HarmonyPocTypeName}.{HarmonyPocMethodName}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Asher] Harmony PoC entry point resolution failed");
                Console.WriteLine($"[Asher] {ex}");
                return;
            }

            try
            {
                entryPoint.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Asher] Harmony PoC initialization failed");
                Console.WriteLine($"[Asher] {ex}");
            }
        }
    }
}
