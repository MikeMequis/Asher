using Asher.Services.Interfaces;
using System.Diagnostics;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Windows launches the swapped launcher (DustAET.exe) via the shell, preserving the
    /// original behavior.
    /// </summary>
    public sealed class WindowsGameProcessLauncher : IGameProcessLauncher
    {
        private readonly IProcessStarter _starter;

        public WindowsGameProcessLauncher(IProcessStarter starter)
        {
            _starter = starter;
        }

        public bool TryStart(string executablePath, string workingDirectory, out string? errorMessage)
        {
            try
            {
                _starter.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                });

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Falha ao iniciar o jogo: {ex.Message}";
                return false;
            }
        }
    }
}
