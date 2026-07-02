namespace Asher.Services.Interfaces
{
    public interface IShortcutService
    {
        bool TryCreateDesktopShortcut(string targetExePath, string shortcutName, out string? errorMessage);
    }
}
