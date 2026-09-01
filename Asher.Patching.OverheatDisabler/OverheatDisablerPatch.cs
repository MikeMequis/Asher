using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.OverheatDisabler
{
    /// <summary>
    /// Prevents Dust Storm from overheating by capping overHeating before SpinBlade runs.
    /// </summary>
    public sealed class OverheatDisablerPatch : IAsherPatchModule
    {
        public static bool Enabled { get; set; }

        public string Name => "Dust Storm Overheat Disabler";

        public void Apply(Harmony harmony)
        {
            if (!Enabled)
                return;

            try
            {
                var characterType = GetCharacterType();
                var spinBladeMethod = characterType?.GetMethod(
                    "SpinBlade",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (spinBladeMethod == null)
                {
                    AsherLog.Warning("[OverheatDisabler] Não foi possível encontrar Character.SpinBlade");
                    return;
                }

                harmony.Patch(
                    spinBladeMethod,
                    prefix: new HarmonyMethod(typeof(OverheatDisablerPatch), nameof(SpinBladePrefix)));
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[OverheatDisabler] Erro ao aplicar patch: {ex.Message}");
            }
        }

        private static void SpinBladePrefix()
        {
            try
            {
                var game1Type = GetGame1Type();
                if (game1Type == null)
                    return;

                var stats = game1Type.GetField(
                        "stats",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(null);

                if (stats == null)
                    return;

                var statsType = stats.GetType();
                var isSpinningField = statsType.GetField("isSpinning");
                var overHeatingField = statsType.GetField("overHeating");

                if (isSpinningField == null || overHeatingField == null)
                    return;

                bool isSpinning = (bool)isSpinningField.GetValue(stats)!;
                float overHeating = (float)overHeatingField.GetValue(stats)!;
                float frameTime = (float)game1Type.GetField(
                        "FrameTime",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
                    .GetValue(null)!;

                if (isSpinning && overHeating + frameTime > 5f)
                    overHeatingField.SetValue(stats, 5f - frameTime);
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[OverheatDisabler] Erro durante execução: {ex.Message}");
            }
        }

        private static Type? GetGame1Type()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET")
                ?.GetType("Dust.Game1");
        }

        private static Type? GetCharacterType()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET")
                ?.GetType("Dust.CharClasses.Character");
        }

        public IEnumerable<Type> GetPatchTypes() => Array.Empty<Type>();
    }
}
