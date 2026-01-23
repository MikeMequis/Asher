using Asher.SDK.Patching;
using Asher.SDK.Logging;

namespace Asher.Patching.DebugEnabler
{
    /// <summary>
    /// Módulo PreInit para configurar o estado do debug antes dos patches.
    /// </summary>
    public sealed class DebugEnablerConfig : IAsherPreInitModule
    {
        public string Name => "Debug Enabler Config";

        public void Execute()
        {
            // Aqui você pode ler de um arquivo de config no futuro
            DebugEnablerPatch.Enabled = true;
            AsherLog.Info("[DebugEnabler] Debug menu será habilitado");
        }
    }
}
