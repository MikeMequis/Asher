using System;

namespace Asher.HarmonyPoc
{
    /// <summary>
    /// Entry point of the isolated Harmony PoC assembly.
    ///
    /// Asher.Runtime loads this assembly only when ASHER_HARMONY_POC=1 and invokes this method
    /// reflectively, so Asher.Runtime itself never references 0Harmony.
    /// </summary>
    public static class HarmonyPocBootstrap
    {
        public static void Initialize()
        {
            DebugEnablerHarmonyPatch.Apply();
            Console.WriteLine("[Asher] Harmony PoC completed successfully");
        }
    }
}
