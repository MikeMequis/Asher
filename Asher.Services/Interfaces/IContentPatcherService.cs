using Asher.Core.Models;

namespace Asher.Services.Interfaces
{
    public interface IContentPatcherService
    {
        Task<IReadOnlyList<ContentReplacementInfo>> GetReplacementsAsync();
        Task<bool> AddReplacementAsync(string target, string sourceFilePath);
        Task<bool> RemoveReplacementAsync(string target);
        Task<bool> SetReplacementEnabledAsync(string target, bool enabled);
    }
}
