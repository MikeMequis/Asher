using Asher.SDK.Logging;
using Asher.SDK.Patching;

namespace Asher.Patching.DebugEnabler
{
    public sealed class DebugLifecycleModule : AsherLifecycleModuleBase
    {
        public override string Name => "Debug Lifecycle Monitor";

        public override void OnGameInitialized()
        {
            AsherLog.Info("[DebugLifecycle] ✓ Game1.Initialize concluído!");
        }

        public override void OnContentLoaded()
        {
            AsherLog.Info("[DebugLifecycle] ✓ Game1.LoadContent concluído!");
        }

        public override void OnGameExiting()
        {
            AsherLog.Info("[DebugLifecycle] ✓ Jogo está finalizando!");
        }

        // OnGamePaused não é sobrescrito, então não faz nada
    }
}