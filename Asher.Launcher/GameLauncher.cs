using Asher.Services.Interfaces;
using System.Diagnostics;

namespace Asher.Launcher
{
    public class GameLauncher
    {
        private readonly IDllInjectorService _dllInjectorService;

        public GameLauncher(IDllInjectorService dllInjectorService)
        {
            _dllInjectorService = dllInjectorService;
        }

        public void Launch(string gameExePath)
        {
            var gameFolder = Path.GetDirectoryName(gameExePath);
            if (string.IsNullOrEmpty(gameFolder))
                return;

            // Get paths to Bootstrap and Runtime DLLs
            var bootstrapPath = GetBootstrapDllPath();
            var runtimePath = GetRuntimeDllPath();

            bool canInject = !string.IsNullOrEmpty(bootstrapPath) && !string.IsNullOrEmpty(runtimePath);

            if (canInject)
            {
                // Copy DLLs to game folder
                var bootstrapTarget = Path.Combine(gameFolder, "Asher.Bootstrap.dll");
                var runtimeTarget = Path.Combine(gameFolder, "Asher.Runtime.dll");

                _dllInjectorService.CopyFilesToGameFolder(
                    gameFolder,
                    new[] { bootstrapPath, runtimePath },
                    new[] { "Asher.Bootstrap.dll", "Asher.Runtime.dll" }
                );
            }

            // Start the game process
            var startInfo = new ProcessStartInfo
            {
                FileName = gameExePath,
                WorkingDirectory = gameFolder,
                UseShellExecute = true // needed for Steam and other launchers
            };

            Process.Start(startInfo);

            // If we have DLLs, try to inject after the process initializes
            if (canInject)
            {
                var bootstrapTarget = Path.Combine(gameFolder, "Asher.Bootstrap.dll");
                var gameExeName = Path.GetFileName(gameExePath);
                
                // Wait a bit for the process to initialize, then inject
                Task.Run(async () =>
                {
                    // Wait a bit for the game to start
                    await Task.Delay(2000);
                    
                    // Find the actual game process (not the launcher)
                    Process gameProcess = null;
                    int attempts = 0;
                    while (attempts < 30 && gameProcess == null)
                    {
                        try
                        {
                            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gameExeName));
                            if (processes.Length > 0)
                            {
                                // Find the most recent process (the one we just started)
                                gameProcess = processes
                                    .OrderByDescending(p => p.StartTime)
                                    .FirstOrDefault(p => !p.HasExited && p.Id != Process.GetCurrentProcess().Id);
                                
                                if (gameProcess != null)
                                    break;
                            }
                        }
                        catch { }
                        
                        await Task.Delay(500);
                        attempts++;
                    }
                    
                    if (gameProcess != null && !gameProcess.HasExited)
                    {
                        // Wait a bit more for the process to fully initialize
                        await Task.Delay(2000);
                        
                        // Inject the Bootstrap DLL
                        bool injected = _dllInjectorService.InjectDll(gameProcess, bootstrapTarget);
                        
                        // Log injection result
                        try
                        {
                            var logPath = Path.Combine(gameFolder, "AsherLogs", "injection.log");
                            var logDir = Path.GetDirectoryName(logPath);
                            if (!Directory.Exists(logDir))
                                Directory.CreateDirectory(logDir);
                            
                            File.AppendAllText(
                                logPath,
                                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Injection attempt: {(injected ? "SUCCESS" : "FAILED")} for process {gameProcess.Id} ({gameProcess.ProcessName})\n"
                            );
                            File.AppendAllText(
                                logPath,
                                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Bootstrap DLL path: {bootstrapTarget}\n"
                            );
                            File.AppendAllText(
                                logPath,
                                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Bootstrap DLL exists: {File.Exists(bootstrapTarget)}\n"
                            );
                            
                            // Wait a bit more and check if logs were created
                            await Task.Delay(2000);
                            var bootstrapLog = Path.Combine(gameFolder, "AsherLogs", "bootstrap.log");
                            if (File.Exists(bootstrapLog))
                            {
                                File.AppendAllText(
                                    logPath,
                                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Bootstrap log created - initialization successful!\n"
                                );
                            }
                            else
                            {
                                File.AppendAllText(
                                    logPath,
                                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: Bootstrap log not found - type may not have been accessed yet\n"
                                );
                            }
                        }
                        catch { }
                    }
                });
            }
        }

        private string GetBootstrapDllPath()
        {
            // Try to find the Bootstrap DLL in common build output locations
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var solutionDir = FindSolutionDirectory(baseDir);
            
            var possiblePaths = new List<string>
            {
                Path.Combine(baseDir, "Asher.Bootstrap.dll"),
                Path.Combine(Path.GetDirectoryName(baseDir), "Asher.Bootstrap.dll")
            };

            if (solutionDir != null)
            {
                possiblePaths.AddRange(new[]
                {
                    Path.Combine(solutionDir, "Asher.Bootstrap", "bin", "Debug", "net472", "Asher.Bootstrap.dll"),
                    Path.Combine(solutionDir, "Asher.Bootstrap", "bin", "Release", "net472", "Asher.Bootstrap.dll"),
                    Path.Combine(solutionDir, "Asher.Bootstrap", "bin", "Debug", "Asher.Bootstrap.dll"),
                    Path.Combine(solutionDir, "Asher.Bootstrap", "bin", "Release", "Asher.Bootstrap.dll")
                });
            }

            return possiblePaths.FirstOrDefault(File.Exists);
        }

        private string GetRuntimeDllPath()
        {
            // Try to find the Runtime DLL in common build output locations
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var solutionDir = FindSolutionDirectory(baseDir);
            
            var possiblePaths = new List<string>
            {
                Path.Combine(baseDir, "Asher.Runtime.dll"),
                Path.Combine(Path.GetDirectoryName(baseDir), "Asher.Runtime.dll")
            };

            if (solutionDir != null)
            {
                possiblePaths.AddRange(new[]
                {
                    Path.Combine(solutionDir, "Asher.Runtime", "bin", "Debug", "Asher.Runtime.dll"),
                    Path.Combine(solutionDir, "Asher.Runtime", "bin", "Release", "Asher.Runtime.dll")
                });
            }

            return possiblePaths.FirstOrDefault(File.Exists);
        }

        private string FindSolutionDirectory(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Asher.sln")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }
}

