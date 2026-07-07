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
    /// Este patch DEVE ser aplicado no PreInit, não no PatchModuleLoader normal.
    /// </summary>
    public sealed class GraphicsDeprofilerPatch : IAsherPreInitModule
    {
        /// <summary>
        /// Define se o patch será aplicado.
        /// Configurado via GraphicsDeprofilerConfig no PreInit.
        /// </summary>
        public static bool Enabled { get; set; }

        public string Name => "Graphics Deprofiler Patch";

        public void Execute()
        {
            if (!Enabled)
                return;

            try
            {
                var harmony = new Harmony("com.asher.deprofiler");
                int patchCount = PatchAllIsProfileSupportedMethods(harmony);

                if (patchCount == 0)
                    AsherLog.Warning("[Deprofiler] No methods found to patch");
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