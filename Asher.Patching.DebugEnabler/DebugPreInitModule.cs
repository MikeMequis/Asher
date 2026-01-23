using Asher.SDK.Patching;
using Asher.SDK.Logging;

namespace Asher.Patching.DebugEnabler
{
    public sealed class DebugPreInitModule : IAsherPreInitModule
    {
        public string Name => "Debug Enabler (PreInit)";

        public void Execute()
        {
            DebugState.EnableDebug = true;
            AsherLog.Info("Debug flag marcada para ativação (PreInit).");
        }
    }
}
