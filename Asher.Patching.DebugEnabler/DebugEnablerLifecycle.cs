using Asher.SDK.Logging;
using Asher.SDK.Patching;

namespace Asher.Patching.DebugEnabler
{
    /// <summary>
    /// Monitora eventos do ciclo de vida do jogo.
    /// OPCIONAL: Pode ser removido se você não precisa desses logs.
    /// </summary>
    public sealed class DebugEnablerLifecycle : AsherLifecycleModuleBase
    {
        public override string Name => "Debug Enabler Lifecycle";

        public override void OnGameInitialized()
        {
            AsherLog.Info("[DebugEnabler] Game initialized - Debug menu should be active");
        }

        public override void OnContentLoaded()
        {
            AsherLog.Info("[DebugEnabler] Content loaded");
        }
    }
}