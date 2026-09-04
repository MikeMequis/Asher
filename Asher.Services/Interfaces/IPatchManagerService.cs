using Asher.Core.Models;

namespace Asher.Services.Interfaces
{
    public interface IPatchManagerService
    {
        Task<IReadOnlyList<ManagedModInfo>> GetModsAsync();
        Task<bool> SetModEnabledAsync(string modFileName, bool enabled);
    }
}
