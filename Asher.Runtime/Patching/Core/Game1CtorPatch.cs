using Asher.Runtime.Core;
using HarmonyLib;
using System.Reflection;

namespace Asher.Runtime.Patching.Core
{
    [HarmonyPatch]
    internal class Game1CtorPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Constructor(
                AccessTools.TypeByName("Dust.Game1")
            );
        }

        static void Postfix(object __instance)
        {
            if (GameContext.GameInstance != null)
                return;

            GameContext.GameInstance = __instance;
            RuntimeLogger.Info("[Harmony] Game1 capturado via construtor.");
        }

    }
}