using Asher.Models;

namespace Asher.Services.Interfaces
{
    public interface IPatchManagerService
    {
        Task<List<HarmonyPatchInfo>> GetAvailablePatchesAsync();
        Task<List<HarmonyPatchInfo>> GetInstalledPatchesAsync();
        Task<HarmonyValidationResult> ValidatePatchesAsync(List<HarmonyPatchInfo> patches);
        Task<bool> InstallPatchesAsync(List<HarmonyPatchInfo> patches, IProgress<PatchProgress> progress = null);
        Task<bool> UninstallPatchesAsync(List<HarmonyPatchInfo> patches, IProgress<PatchProgress> progress = null);
        Task<bool> CreateBackupAsync(string backupName = null);
        Task<bool> RestoreFromBackupAsync(string backupName);
        Task<List<string>> GetAvailableBackupsAsync();
    }
}
