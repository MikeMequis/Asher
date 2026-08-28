using System.Text.Json;
using System.Text.Json.Serialization;

namespace Asher.Host.Jsonl
{
    internal static class JsonlProtocol
    {
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static class ErrorCodes
        {
            public const string InvalidRequest = "invalid_request";
            public const string UnknownMethod = "unknown_method";
            public const string ApplicationError = "application_error";
            public const string Cancelled = "cancelled";
            public const string NotFound = "not_found";
            public const string InternalError = "internal_error";
        }

        public static class Methods
        {
            public const string GetSettings = "getSettings";
            public const string SaveSettings = "saveSettings";
            public const string GetApplicationMode = "getApplicationMode";
            public const string DetectGameFolder = "detectGameFolder";
            public const string GetGameFolderInfo = "getGameFolderInfo";
            public const string ResolveGameFolderPath = "resolveGameFolderPath";
            public const string IsGameInstalled = "isGameInstalled";
            public const string HasRestorableBackup = "hasRestorableBackup";
            public const string GetMods = "getMods";
            public const string SetModEnabled = "setModEnabled";
            public const string Install = "install";
            public const string Uninstall = "uninstall";
            public const string LaunchGame = "launchGame";
            public const string Cancel = "cancel";
            public const string Shutdown = "shutdown";
        }
    }
}
