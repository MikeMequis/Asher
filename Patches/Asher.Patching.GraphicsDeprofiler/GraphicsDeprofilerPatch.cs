using Asher.SDK.Logging;
using Asher.SDK.Patching.Core;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.GraphicsDeprofiler
{
    /// <summary>
    /// Este patch DEVE ser aplicado no PreInit, não no PatchModuleLoader normal.
    /// </summary>
    public sealed class GraphicsDeprofilerPatch : BaseAsherPreInitModule
    {
        /// <summary>
        /// Define se o patch será aplicado.
        /// Configurado via GraphicsDeprofilerConfig no PreInit.
        /// </summary>
        public static bool Enabled { get; set; }

        public override void Execute()
        {
            if (!Enabled)
            {
                AsherLog.Info("[Deprofiler] Patch desabilitado - pulando aplicação");
                return;
            }

            try
            {
                AsherLog.Info("[Deprofiler] Aplicando patch antecipado...");

                var harmony = new Harmony("com.asher.deprofiler");
                int patchCount = PatchAllIsProfileSupportedMethods(harmony);

                if (patchCount > 0)
                    AsherLog.Info($"[Deprofiler] ✓ {patchCount} método(s) patchado(s)");
                else
                    AsherLog.Warning("[Deprofiler] Nenhum método encontrado para patch");
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

            // Busca TODOS os overloads de IsProfileSupported
            var allMethods = graphicsAdapterType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            ).Where(m => m.Name == "IsProfileSupported");

            foreach (var method in allMethods)
            {
                try
                {
                    var parameters = string.Join(", ",
                        method.GetParameters().Select(p => p.ParameterType.Name));

                    AsherLog.Info($"[Deprofiler] Patchando: {method.Name}({parameters}) - " +
                        $"{(method.IsPublic ? "public" : "internal")}");

                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(GraphicsDeprofilerPatch), nameof(AlwaysReturnTrue))
                    );

                    count++;
                }
                catch (Exception ex)
                {
                    AsherLog.Warning($"[Deprofiler] Falha ao patchar método: {ex.Message}");
                }
            }

            return count;
        }

        /// <summary>
        /// Prefix que força retorno true para qualquer perfil gráfico.
        /// </summary>
        private static bool AlwaysReturnTrue(ref bool __result)
        {
            __result = true;
            return false; // Skip original method
        }
    }
}