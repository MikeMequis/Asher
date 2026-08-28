using Asher.Core;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        public AsherSettings Load() => AsherSettings.Load();

        public void Save(AsherSettings settings) => settings.Save();
    }
}
