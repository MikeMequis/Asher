using Asher.Services.Implementations;

namespace Asher.Launcher
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Create services
            var gameFolderService = new GameFolderService();
            var dllInjectorService = new DllInjectorService();
            var gameLauncher = new GameLauncher(dllInjectorService);

            // Detect game folder
            var gameFolder = gameFolderService.DetectGameFolder();

            if (!gameFolder.IsValid)
            {
                Console.WriteLine("Error: Game folder not found. Please ensure Dust: An Elysian Tail is installed.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            var gameExePath = Path.Combine(gameFolder.Path, "DustAET.exe");
            
            if (!File.Exists(gameExePath))
            {
                Console.WriteLine($"Error: Game executable not found at: {gameExePath}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Launching game from: {gameFolder.Path}");
            Console.WriteLine("Starting game with Asher modding support...");

            // Launch the game
            gameLauncher.Launch(gameExePath);

            Console.WriteLine("Game launched. You can close this window.");
            
            // Keep the console open for a moment to show status, then exit
            Task.Delay(3000).Wait();
        }
    }
}

