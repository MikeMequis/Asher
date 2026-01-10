using Asher.Runtime.Core;
using Asher.Runtime.Lifecycle;
using HarmonyLib;
using System.Reflection;

namespace Asher.Runtime.Patching.Core
{
    [HarmonyPatch]
    internal class GameInitWatcherPatch
    {
        private static bool _initialized;

        static MethodBase TargetMethod()
        {
            return AccessTools.Method("Dust.Game1:Update");
        }

        static void Postfix()
        {
            if (_initialized)
                return;

            var game = GameContext.GameInstance;
            if (game == null)
                return;

            // Reflection tardia
            var pcManagerField = game.GetType()
                .GetField("pcManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var pcManager = pcManagerField?.GetValue(null);
            if (pcManager == null)
                return;

            _initialized = true;
            GameLifecycle.SetState(GameLifecycleState.Initialized);
            RuntimeLogger.Info("[Asher] Jogo totalmente inicializado (pcManager disponível).");
        }

    }
}