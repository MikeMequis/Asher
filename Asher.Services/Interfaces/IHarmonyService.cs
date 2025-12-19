using Asher.Core.Models;

namespace Asher.Services.Interfaces
{
    public interface IHarmonyService
    {
        Task<bool> InitializeAsync();
        Task<bool> ApplyPatchAsync(HarmonyPatchInfo patch);
        Task<bool> RemovePatchAsync(string patchId);
        Task<IEnumerable<HarmonyPatchInfo>> GetAppliedPatchesAsync();
        Task<HarmonyValidationResult> ValidatePatchAsync(HarmonyPatchInfo patch);
        Task<bool> IsPatchCompatibleAsync(HarmonyPatchInfo patch);
    }
}
