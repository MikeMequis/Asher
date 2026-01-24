using Asher.SDK.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.IntroSkipper
{
    /// <summary>
    /// Pula completamente a intro forçando o startUpStage antes do primeiro draw.
    /// Remove ESRB, logos e vídeos sem causar flicker.
    /// </summary>
    public sealed class IntroSkipperPatch : IAsherPatchModule
    {
        /// <summary>
        /// Define se o patch será aplicado. Configurado via PreInit.
        /// </summary>
        public static bool Enabled { get; set; }

        public string Name => "Intro Skipper (Option 1 - Startup Stage Skip)";

        public void Apply(Harmony harmony)
        {
            if (!Enabled)
            {
                AsherLog.Info("[IntroSkipper] Patch desabilitado por configuração");
                return;
            }

            try
            {
                var game1Type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "DustAET")
                    ?.GetType("Dust.Game1");

                var drawStartupMethod = game1Type?.GetMethod(
                    "DrawStartup",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (drawStartupMethod == null)
                {
                    AsherLog.Warning("[IntroSkipper] Não foi possível encontrar Game1.DrawStartup");
                    return;
                }

                harmony.Patch(
                    drawStartupMethod,
                    prefix: new HarmonyMethod(
                        typeof(IntroSkipperPatch),
                        nameof(DrawStartupPrefix)));

                AsherLog.Info("[IntroSkipper] ✓ Patch aplicado (Opção 1 - pulo de estágio)");
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Erro ao aplicar patch: {ex.Message}");
            }
        }

        private static void DrawStartupPrefix()
        {
            try
            {
                var game1Type = typeof(Dust.Game1);

                var stageField = game1Type.GetField(
                    "startUpStage",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                var timerField = game1Type.GetField(
                    "startUpTimer",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (stageField == null || timerField == null)
                    return;

                int currentStage = (int)stageField.GetValue(null);

                // Se ainda está nos estágios da intro, pula tudo
                if (currentStage < 6)
                {
                    stageField.SetValue(null, 6);
                    timerField.SetValue(null, 0f);

                    AsherLog.Info("[IntroSkipper] Startup stages pulados (ESRB + logos removidos)");
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Erro durante execução: {ex.Message}");
            }
        }

        public IEnumerable<Type> GetPatchTypes() => Array.Empty<Type>();
    }
}
