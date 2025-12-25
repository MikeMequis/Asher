using Asher.Services.Interfaces;
using Asher.Models;

namespace Asher.Services.Implementations
{
    public class HarmonyService : IHarmonyService
    {
        public Task<bool> ApplyPatchAsync(HarmonyPatchInfo patch)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<HarmonyPatchInfo>> GetAppliedPatchesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsPatchCompatibleAsync(HarmonyPatchInfo patch)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemovePatchAsync(string patchId)
        {
            throw new NotImplementedException();
        }

        public Task<HarmonyValidationResult> ValidatePatchAsync(HarmonyPatchInfo patch)
        {
            throw new NotImplementedException();
        }
    }
}
