using Asher.SDK.Logging;
using Asher.SDK.Patching.Core;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.DebugEnabler
{
    /// <summary>
    /// Habilita o menu de debug do jogo (Tab no menu de pausa).
    /// </summary>
    public sealed class DebugEnablerPatch : BaseAsherPatchModule
    {
        public static bool Enabled { get; set; }

        public override void Apply(Harmony harmony)
        {
            if (!Enabled)
            {
                AsherLog.Info("[DebugEnabler] Patch desabilitado por configuração");
                return;
            }

            try
            {
                var game1Type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "DustAET")
                    ?.GetType("Dust.Game1");

                var initMethod = game1Type?.GetMethod("Initialize",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (initMethod == null)
                {
                    AsherLog.Warning("[DebugEnabler] Não foi possível encontrar Game1.Initialize");
                    return;
                }

                harmony.Patch(initMethod,
                    postfix: new HarmonyMethod(typeof(DebugEnablerPatch), nameof(EnableDebugMenu)));
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[DebugEnabler] Erro: {ex.Message}");
            }
        }

        private static void EnableDebugMenu(object __instance)
        {
            __instance.GetType()
                .GetField("canDebug", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(null, true);
        }
    }
}