using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class ShortcutService : IShortcutService
    {
        public bool TryCreateDesktopShortcut(string targetExePath, string shortcutName, out string? errorMessage)
        {
            errorMessage = null;

            if (!File.Exists(targetExePath))
            {
                errorMessage = $"Executável não encontrado: {targetExePath}";
                return false;
            }

            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    errorMessage = "WScript.Shell não está disponível neste sistema.";
                    return false;
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetExePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetExePath);
                shortcut.Description = "Asher Mod Manager";
                shortcut.Save();

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
