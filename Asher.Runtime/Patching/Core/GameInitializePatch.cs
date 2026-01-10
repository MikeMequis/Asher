using Asher.Runtime.Lifecycle;
using HarmonyLib;
using System.Reflection;

namespace Asher.Runtime.Patching.Core
{
    [HarmonyPatch]
    internal class GameInitializePatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method("Dust.Game1:Initialize");
        }

        static void Postfix()
        {
            if (GameLifecycle.State != GameLifecycleState.None)
                return;

            GameLifecycle.SetState(GameLifecycleState.Initializing);
            RuntimeLogger.Info("[Harmony] Game1.Initialize executado.");
        }
    }
}