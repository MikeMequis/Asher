using Newtonsoft.Json;
using System.IO;

namespace Asher.Core
{
    /// <summary>
    /// Configurações do Asher persistidas em arquivo JSON
    /// </summary>
    public class AsherSettings
    {
        private static readonly string AppDataSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Asher",
            AsherPaths.SettingsFileName
        );

        public string GameFolderPath { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
        public DateTime? InstallationDate { get; set; }
        public string GameVersion { get; set; } = string.Empty;
        public bool FirstRun { get; set; } = true;
        public string Language { get; set; } = "en-US";
        public bool AutoLaunchEnabled { get; set; } = true;
        public bool BackupEnabled { get; set; } = true;
        public string Theme { get; set; } = "Light";
        public bool CheckForUpdatesEnabled { get; set; } = true;

        public static AsherSettings Load()
        {
            foreach (var path in GetSettingsLoadOrder())
            {
                var settings = TryLoadFrom(path);
                if (settings != null)
                    return settings;
            }

            return new AsherSettings();
        }

        public void Save()
        {
            SaveToPath(AppDataSettingsPath);

            var localPath = AsherPaths.GetLocalSettingsPath();
            if (!string.Equals(localPath, AppDataSettingsPath, StringComparison.OrdinalIgnoreCase))
                SaveToPath(localPath);
        }

        public void MarkAsInstalled(string gameFolderPath, string gameVersion)
        {
            GameFolderPath = gameFolderPath;
            IsInstalled = true;
            InstallationDate = DateTime.Now;
            GameVersion = gameVersion ?? string.Empty;
            FirstRun = false;
            Save();
        }

        public void MarkAsUninstalled()
        {
            IsInstalled = false;
            InstallationDate = null;
            Save();
        }

        public void Clear()
        {
            GameFolderPath = string.Empty;
            IsInstalled = false;
            InstallationDate = null;
            GameVersion = string.Empty;
            FirstRun = true;
            Save();
        }

        private static IEnumerable<string> GetSettingsLoadOrder()
        {
            yield return AsherPaths.GetLocalSettingsPath();
            yield return AppDataSettingsPath;

            var legacyPath = TryGetLegacyManagerSettingsPath();
            if (legacyPath != null)
                yield return legacyPath;
        }

        private static string? TryGetLegacyManagerSettingsPath()
        {
            var gameFolder = AsherPaths.TryGetGameFolderFromManagerLocation();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return null;

            var path = Path.Combine(
                AsherPaths.GetManagerFolderPath(gameFolder),
                AsherPaths.SettingsFileName);

            return File.Exists(path) ? path : null;
        }

        private static AsherSettings? TryLoadFrom(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<AsherSettings>(json);
            }
            catch
            {
                return null;
            }
        }

        private void SaveToPath(string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar settings em {path}: {ex.Message}");
            }
        }
    }
}
