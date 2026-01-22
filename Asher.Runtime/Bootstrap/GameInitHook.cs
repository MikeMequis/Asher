namespace Asher.Runtime
{
    internal static class GameInitHook
    {
        public static void Postfix()
        {
            RuntimeLogger.Info("Game1.Initialize atingido — inicializando patches.");

            PatchModuleLoader.Load();
        }
    }
}
