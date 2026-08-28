using Asher.Core;

namespace Asher.Services.Interfaces
{
    public interface ISettingsService
    {
        AsherSettings Load();
        void Save(AsherSettings settings);
    }
}
