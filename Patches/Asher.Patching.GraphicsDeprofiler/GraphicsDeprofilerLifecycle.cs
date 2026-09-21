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
        protected override string LogTag => "[Deprofiler]";

        public override void OnGameInitialized()
        {
            ExecuteIfEnabled(
                action: ValidateHiDefBypass,
                notEnabledMessage: "Monitor: Patch não foi aplicado"
            );
        }

        public override void OnContentLoaded()
        {
            ExecuteIfEnabled(() =>
            {
                AsherLog.Info($"{LogTag} Conteúdo carregado - monitorando estabilidade gráfica");
            });
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
                {
                    AsherLog.Info($"{LogTag} ✓ Verificação OK - HiDef retornou TRUE");
                }
                else
                {
                    AsherLog.Error($"{LogTag} ✗ FALHA - HiDef retornou FALSE (patch não funcionou)");
                    AsherLog.Warning($"{LogTag} O jogo pode crashar ao tentar usar recursos HiDef");
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"{LogTag} Erro na verificação: {ex.Message}");
            }
        }
    }
}