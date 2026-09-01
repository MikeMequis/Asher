using Asher.Core;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        public AsherSettings Load() => AsherSettings.Load();

        public void Save(AsherSettings settings) => settings.Save();

        public void MarkAsInstalled(string gameFolderPath, string gameVersion)
        {
            var settings = Load();
            settings.MarkAsInstalled(gameFolderPath, gameVersion);
        }

        public void MarkAsUninstalled()
        {
            var settings = Load();
            settings.MarkAsUninstalled();
        }
    }
}
