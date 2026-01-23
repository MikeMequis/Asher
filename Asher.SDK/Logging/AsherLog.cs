namespace Asher.SDK.Logging
{
    /// <summary>
    /// Fachada de logging acessível por SDK, Patching e Mods.
    /// Implementação real vem do Runtime.
    /// </summary>
    public static class AsherLog
    {
        public static IAsherLogger? Logger { get; set; }
        public static void Info(string message) => Logger?.Info(message);
        public static void Warning(string message) => Logger?.Warning(message);
        public static void Error(string message) => Logger?.Error(message);
    }
}
