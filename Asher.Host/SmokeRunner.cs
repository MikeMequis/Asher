using Asher.Services.Application;
using Asher.Services.Hosting;

namespace Asher.Host
{
    internal static class SmokeRunner
    {
        public static int Run(string[] args)
        {
            var live = args.Contains("--live", StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("=== Asher Application Host Spike ===");
            Console.WriteLine($"Mode: {(live ? "live (mutating ops allowed)" : "read-only smoke")}");
            Console.WriteLine();

            try
            {
                var host = AsherServiceHost.Create();
                var app = host.Application;
                Console.WriteLine("[OK] Application contract initialized (no WPF).");
                Console.WriteLine();

                var failures = 0;
                failures += RunSettings(app);
                failures += RunApplicationMode(app);
                failures += RunGameDetection(app);
                failures += RunModDiscovery(app);
                failures += RunInstallUninstallPaths(app);

                if (live)
                {
                    failures += RunLiveOperations(app, args);
                }
                else
                {
                    PrintSkippedMutatingOperations();
                }

                Console.WriteLine();
                Console.WriteLine(failures == 0
                    ? "=== Smoke completed successfully ==="
                    : $"=== Smoke completed with {failures} failure(s) ===");

                return failures == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FATAL] {ex}");
                return 2;
            }
        }

        private static int RunSettings(IAsherApplication app)
        {
            Console.WriteLine("--- Settings ---");
            var settings = app.GetSettings();
            Console.WriteLine($"  GameFolderPath: {OrNone(settings.GameFolderPath)}");
            Console.WriteLine($"  IsInstalled: {settings.IsInstalled}");
            Console.WriteLine($"  Language: {settings.Language}");
            Console.WriteLine($"  Theme: {settings.Theme}");
            Console.WriteLine("[OK] IAsherApplication.GetSettings()");
            Console.WriteLine();
            return 0;
        }

        private static int RunApplicationMode(IAsherApplication app)
        {
            Console.WriteLine("--- Application mode ---");
            var mode = app.GetApplicationMode();
            var resolved = app.ResolveGameFolderPath();
            Console.WriteLine($"  Resolved path: {OrNone(resolved)}");
            Console.WriteLine($"  Mode: {mode}");
            Console.WriteLine("[OK] IAsherApplication.GetApplicationMode()");
            Console.WriteLine();
            return 0;
        }

        private static int RunGameDetection(IAsherApplication app)
        {
            Console.WriteLine("--- Game detection ---");
            var detected = app.DetectGameFolder();
            Console.WriteLine($"  Path: {OrNone(detected.Path)}");
            Console.WriteLine($"  Valid: {detected.IsValid}");
            Console.WriteLine($"  Source: {OrNone(detected.Source)}");
            Console.WriteLine($"  Version: {OrNone(detected.Version)}");

            var resolved = app.ResolveGameFolderPath();
            Console.WriteLine($"  Resolved game folder: {OrNone(resolved)}");

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                Console.WriteLine($"  IsInstalled: {app.IsGameInstalled(resolved)}");
                Console.WriteLine($"  HasRestorableBackup: {app.HasRestorableBackup(resolved)}");
            }

            Console.WriteLine("[OK] Game detection / validation via IAsherApplication");
            Console.WriteLine();
            return 0;
        }

        private static int RunInstallUninstallPaths(IAsherApplication app)
        {
            Console.WriteLine("--- Install / uninstall contract ---");
            var sample = app.GetGameFolderInfo(Environment.CurrentDirectory);
            Console.WriteLine($"  GetGameFolderInfo sample path: {OrNone(sample.Path)}");
            Console.WriteLine("  Uninstall without path (expected failure): available via IAsherApplication.UninstallAsync");
            Console.WriteLine("[OK] Install/uninstall operations exposed on contract");
            Console.WriteLine();
            return 0;
        }

        private static int RunModDiscovery(IAsherApplication app)
        {
            Console.WriteLine("--- Mod discovery ---");
            var mods = app.GetModsAsync().GetAwaiter().GetResult();
            Console.WriteLine($"  Mod count: {mods.Count}");

            foreach (var mod in mods)
                Console.WriteLine($"    - {mod.FileName} ({mod.Name}) enabled={mod.IsEnabled}");

            Console.WriteLine("[OK] IAsherApplication.GetModsAsync()");
            Console.WriteLine();
            return 0;
        }

        private static int RunLiveOperations(IAsherApplication app, string[] args)
        {
            Console.WriteLine("--- Live operations (mutating) ---");
            var failures = 0;

            if (args.Any(a => a.Equals("--launch", StringComparison.OrdinalIgnoreCase)))
            {
                var result = app.LaunchGame();
                if (!result.Success)
                {
                    Console.WriteLine($"[FAIL] Launch: {result.ErrorMessage}");
                    failures++;
                }
                else
                {
                    Console.WriteLine("[OK] IAsherApplication.LaunchGame()");
                }
            }

            if (args.Any(a => a.Equals("--toggle-mod", StringComparison.OrdinalIgnoreCase)))
            {
                failures += RunModToggle(app, args);
            }

            if (args.Any(a => a.Equals("--install", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("[SKIP] --install not run automatically (destructive). Validate InstallAsync via contract manually.");
            }

            if (args.Any(a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("[SKIP] --uninstall not run automatically (destructive). Validate UninstallAsync via contract manually.");
            }

            Console.WriteLine();
            return failures;
        }

        private static int RunModToggle(IAsherApplication app, string[] args)
        {
            var fileName = GetArgValue(args, "--mod");
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Console.WriteLine("[SKIP] --toggle-mod requires --mod <FileName.dll>");
                return 0;
            }

            var mods = app.GetModsAsync().GetAwaiter().GetResult();
            var mod = mods.FirstOrDefault(m =>
                m.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

            if (mod == null)
            {
                Console.WriteLine($"[FAIL] Mod not found: {fileName}");
                return 1;
            }

            var targetEnabled = !mod.IsEnabled;
            var toggleResult = app.SetModEnabledAsync(mod.FileName, targetEnabled).GetAwaiter().GetResult();
            if (!toggleResult.Success)
            {
                Console.WriteLine($"[FAIL] SetModEnabledAsync({mod.FileName}, {targetEnabled}): {toggleResult.ErrorMessage}");
                return 1;
            }

            var restoreResult = app.SetModEnabledAsync(mod.FileName, mod.IsEnabled).GetAwaiter().GetResult();
            if (!restoreResult.Success)
            {
                Console.WriteLine($"[FAIL] SetModEnabledAsync restore ({mod.FileName}, {mod.IsEnabled}): {restoreResult.ErrorMessage}");
                return 1;
            }

            Console.WriteLine($"[OK] Mod toggle round-trip for {mod.FileName}");
            return 0;
        }

        private static void PrintSkippedMutatingOperations()
        {
            Console.WriteLine("--- Mutating operations (skipped in read-only mode) ---");
            Console.WriteLine("  InstallAsync / UninstallAsync: available via IAsherApplication");
            Console.WriteLine("  LaunchGame: pass --live --launch");
            Console.WriteLine("  SetModEnabledAsync: pass --live --toggle-mod --mod <FileName.dll>");
            Console.WriteLine("  Use --live to enable optional mutating checks.");
            Console.WriteLine();
        }

        private static string OrNone(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "(none)" : value;

        private static string? GetArgValue(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }
    }
}
