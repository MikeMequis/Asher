using Asher.SDK.Logging;
using Asher.SDK.Patching.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;

namespace Asher.Patching.GraphicsDeprofiler
{
    /// <summary>
    /// Configuração do Graphics Deprofiler.
    /// Detecta automaticamente se o patch é necessário.
    /// </summary>
    public sealed class GraphicsDeprofilerConfig : BaseAsherPatchConfig<GraphicsDeprofilerPatch>
    {
        protected override string LogTag => "[Deprofiler]";

        /// <summary>
        /// Detecta se o patch é necessário usando heurísticas seguras.
        /// NÃO chama IsProfileSupported para evitar JIT prematuro.
        /// </summary>
        protected override bool ShouldEnable()
        {
            try
            {
                var adapter = GraphicsAdapter.DefaultAdapter;

                // Heurística 1: Verifica resolução mínima
                bool hasMinResolution =
                    adapter.CurrentDisplayMode.Width >= 800 &&
                    adapter.CurrentDisplayMode.Height >= 600;

                if (!hasMinResolution)
                {
                    AsherLog.Info($"{LogTag} Resolução baixa detectada");
                    return true;
                }

                // Heurística 2: Verifica device type via reflection
                var adapterType = adapter.GetType();
                var deviceTypeField = adapterType.GetField("deviceType",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (deviceTypeField != null)
                {
                    var deviceType = deviceTypeField.GetValue(adapter);
                    string deviceTypeName = deviceType?.ToString() ?? "Unknown";

                    AsherLog.Info($"{LogTag} DeviceType: {deviceTypeName}");

                    // Se for Reference/Software, definitivamente precisa do patch
                    if (deviceTypeName.Contains("Reference") ||
                        deviceTypeName.Contains("Software"))
                    {
                        AsherLog.Info($"{LogTag} Software renderer detectado");
                        return true;
                    }
                }

                // Heurística 3: Fallback - aplica em caso de dúvida
                // Melhor aplicar e funcionar do que não aplicar e crashar
                AsherLog.Info($"{LogTag} Detecção inconclusiva - habilitando por segurança");
                return true;
            }
            catch (Exception ex)
            {
                AsherLog.Warning($"{LogTag} Erro na detecção: {ex.Message}");
                return true; // Em caso de erro, habilita
            }
        }

        protected override string EnabledMessage =>
            "Patch será aplicado - HiDef não suportado";

        protected override string DisabledMessage =>
            "Patch desnecessário - HiDef suportado nativamente";

        protected override void OnEnabled()
        {
            AsherLog.Warning($"{LogTag} ⚠️ Podem ocorrer problemas gráficos");
        }
    }
}