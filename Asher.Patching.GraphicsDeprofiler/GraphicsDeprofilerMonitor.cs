using Asher.SDK.Logging;
using Asher.SDK.Patching;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// Lifecycle monitor OPCIONAL para verificar se o patch funcionou.
/// </summary>
public sealed class GraphicsDeprofilerMonitor : AsherLifecycleModuleBase
{
    public override string Name => "Deprofiler Monitor";

    public override void OnGameInitialized()
    {
        // AGORA é seguro verificar, porque o patch já foi aplicado no PreInit
        try
        {
            bool result = GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.HiDef);

            if (result)
            {
                AsherLog.Info("[Deprofiler] ✓ Verificação retornou TRUE (patch funcionando)");
            }
            else
            {
                AsherLog.Error("[Deprofiler] ✗ Verificação retornou FALSE (patch FALHOU)");
            }
        }
        catch (Exception ex)
        {
            AsherLog.Error($"[Deprofiler] Erro na verificação: {ex.Message}");
        }
    }

    public override void OnContentLoaded()
    {
        AsherLog.Info("[Deprofiler] Conteúdo carregado - monitorando estabilidade gráfica");
    }
}