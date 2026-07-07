using Asher.SDK.Logging;
using Asher.SDK.Patching.Core;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Asher.Patching.GraphicsDeprofiler
{
    /// <summary>
    /// Monitora o estado do jogo após aplicar o patch de deprofiler.
    /// Valida se o bypass de HiDef está funcionando corretamente.
    /// </summary>
    public sealed class GraphicsDeprofilerLifecycle : BaseAsherPatchLifecycle<GraphicsDeprofilerPatch>
    {
        public override string Name => "Graphics Deprofiler";

        protected override string LogTag => "[Deprofiler]";

        public override void OnGameInitialized()
        {
            ExecuteIfEnabled(ValidateHiDefBypass);
        }

        public override void OnContentLoaded()
        {
        }

        /// <summary>
        /// Valida se o bypass de HiDef está funcionando.
        /// </summary>
        private void ValidateHiDefBypass()
        {
            try
            {
                bool result = GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.HiDef);

                if (result)
                    return;

                AsherLog.Error($"{LogTag} HiDef check failed after patch");
            }
            catch (Exception ex)
            {
                AsherLog.Error($"{LogTag} Erro na verificação: {ex.Message}");
            }
        }
    }
}