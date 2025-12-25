using Asher.Services.Interfaces;
using Asher.Models;

namespace Asher.Services.Implementations
{
    public class PatchManagerService : IPatchManagerService
    {
        public Task<bool> CreateBackupAsync(string backupName = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetAvailableBackupsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<HarmonyPatchInfo>> GetAvailablePatchesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<HarmonyPatchInfo>> GetInstalledPatchesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> InstallPatchesAsync(List<HarmonyPatchInfo> patches, IProgress<PatchProgress> progress = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RestoreFromBackupAsync(string backupName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UninstallPatchesAsync(List<HarmonyPatchInfo> patches, IProgress<PatchProgress> progress = null)
        {
            throw new NotImplementedException();
        }

        public Task<HarmonyValidationResult> ValidatePatchesAsync(List<HarmonyPatchInfo> patches)
        {
            throw new NotImplementedException();
        }
    }
}
