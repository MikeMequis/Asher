using Newtonsoft.Json;
using System.IO;

namespace Asher.Core
{
    /// <summary>
    /// Configurações do Asher persistidas em arquivo JSON
    /// </summary>
    public class AsherSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Asher",
            "settings.json"
        );

        public string GameFolderPath { get; set; }
        public bool IsInstalled { get; set; }
        public DateTime? InstallationDate { get; set; }
        public string GameVersion { get; set; }
        public bool FirstRun { get; set; } = true;

        /// <summary>
        /// Carrega as configurações do arquivo
        /// </summary>
        public static AsherSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonConvert.DeserializeObject<AsherSettings>(json) ?? new AsherSettings();
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Erro ao carregar settings: {ex.Message}");
            }

            return new AsherSettings();
        }

        /// <summary>
        /// Salva as configurações no arquivo
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Erro ao salvar settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Marca a instalação como concluída
        /// </summary>
        public void MarkAsInstalled(string gameFolderPath, string gameVersion)
        {
            GameFolderPath = gameFolderPath;
            IsInstalled = true;
            InstallationDate = DateTime.Now;
            GameVersion = gameVersion;
            FirstRun = false;
            Save();
        }

        /// <summary>
        /// Marca como desinstalado
        /// </summary>
        public void MarkAsUninstalled()
        {
            IsInstalled = false;
            InstallationDate = null;
            Save();
        }

        /// <summary>
        /// Limpa todas as configurações
        /// </summary>
        public void Clear()
        {
            GameFolderPath = null;
            IsInstalled = false;
            InstallationDate = null;
            GameVersion = null;
            FirstRun = true;
            Save();
        }
    }
}