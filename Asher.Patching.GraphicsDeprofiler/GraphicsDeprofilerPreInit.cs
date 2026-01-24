using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.GraphicsDeprofiler
{
    /// <summary>
    /// PATCH ANTECIPADO via PreInit para evitar JIT prematuro.
    /// Aplica o patch ANTES de qualquer chamada a IsProfileSupported.
    /// </summary>
    public sealed class GraphicsDeprofilerPreInit : IAsherPreInitModule
    {
        public string Name => "Graphics Deprofiler (PreInit)";

        public void Execute()
        {
            try
            {
                AsherLog.Info("[Deprofiler] Aplicando patch antecipado...");

                // Cria instância Harmony ANTES de qualquer chamada ao método
                var harmony = new Harmony("com.asher.deprofiler");

                // Patcha TODOS os overloads de IsProfileSupported
                int patchCount = PatchAllIsProfileSupportedMethods(harmony);

                if (patchCount > 0)
                {
                    AsherLog.Info($"[Deprofiler] ✓ {patchCount} método(s) patchado(s)");
                    AsherLog.Warning("[Deprofiler] ⚠️ Bypass de HiDef ativo - podem ocorrer problemas gráficos");
                }
                else
                {
                    AsherLog.Warning("[Deprofiler] Nenhum método encontrado para patch");
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[Deprofiler] Erro ao aplicar patch: {ex.Message}");
            }
        }

        private int PatchAllIsProfileSupportedMethods(Harmony harmony)
        {
            int count = 0;
            var graphicsAdapterType = typeof(GraphicsAdapter);

            // Lista TODOS os métodos chamados IsProfileSupported
            var allMethods = graphicsAdapterType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            ).Where(m => m.Name == "IsProfileSupported");

            foreach (var method in allMethods)
            {
                try
                {
                    // Log da assinatura encontrada
                    var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                    AsherLog.Info($"[Deprofiler] Encontrado: {method.Name}({parameters}) - {(method.IsPublic ? "public" : "internal")}");

                    // Aplica o prefix
                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(GraphicsDeprofilerPreInit), nameof(AlwaysReturnTrue))
                    );

                    count++;
                    AsherLog.Info($"[Deprofiler] ✓ Patch aplicado em {method.Name}");
                }
                catch (Exception ex)
                {
                    AsherLog.Warning($"[Deprofiler] Falha ao patchar método: {ex.Message}");
                }
            }

            return count;
        }

        /// <summary>
        /// Prefix universal que força retorno true.
        /// Funciona para qualquer assinatura de IsProfileSupported.
        /// </summary>
        private static bool AlwaysReturnTrue(ref bool __result)
        {
            __result = true;
            return false; // Skip original method
        }
    }
}