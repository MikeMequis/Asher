using Asher.SDK.Logging;
using Asher.SDK.Patching;

namespace Asher.Patching.IntroSkipper
{
    /// <summary>
    /// Módulo PreInit para configurar o Intro Skipper antes da aplicação dos patches.
    /// </summary>
    public sealed class IntroSkipperConfig : IAsherPreInitModule
    {
        public string Name => "Intro Skipper Config";

        public void Execute()
        {
            // Futuramente pode ler de arquivo de config
            IntroSkipperPatch.Enabled = true;

            AsherLog.Info("[IntroSkipper] Intro Skipper será habilitado");
        }
    }
}
