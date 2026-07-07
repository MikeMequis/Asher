using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.IntroSkipper
{
    /// <summary>
    /// Skips intro sequences by forcing MainMenu mode after initialization,
    /// matching the legacy SkipIntro mod behavior.
    /// </summary>
    public sealed class IntroSkipperPatch : IAsherPatchModule
    {
        private static bool _skipped;

        public static bool Enabled { get; set; }

        public string Name => "Intro Skipper";

        public void Apply(Harmony harmony)
        {
            if (!Enabled)
                return;

            try
            {
                var game1Type = GetGame1Type();
                var initializeMethod = game1Type?.GetMethod(
                    "Initialize",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (initializeMethod == null)
                {
                    AsherLog.Warning("[IntroSkipper] Game1.Initialize not found");
                    return;
                }

                harmony.Patch(
                    initializeMethod,
                    postfix: new HarmonyMethod(typeof(IntroSkipperPatch), nameof(InitializePostfix)));
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Failed to apply patch: {ex.Message}");
            }
        }

        private static void InitializePostfix(object __instance)
        {
            if (_skipped)
                return;

            try
            {
                SkipIntro(__instance);
                _skipped = true;
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Failed to skip intro: {ex.Message}");
            }
        }

        private static void SkipIntro(object gameInstance)
        {
            var game1Type = GetGame1Type();
            if (game1Type == null)
                return;

            var gameModesType = game1Type.GetNestedType(
                "GameModes",
                BindingFlags.Public | BindingFlags.NonPublic);

            var gameModeField = game1Type.GetField(
                "gameMode",
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (gameModesType == null || gameModeField == null)
            {
                AsherLog.Warning("[IntroSkipper] gameMode fields not found");
                return;
            }

            var mainMenu = Enum.Parse(gameModesType, "MainMenu");
            gameModeField.SetValue(gameModeField.IsStatic ? null : gameInstance, mainMenu);

            var stageField = game1Type.GetField(
                "startUpStage",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var timerField = game1Type.GetField(
                "startUpTimer",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (stageField != null)
                stageField.SetValue(null, 6);

            if (timerField != null)
                timerField.SetValue(null, 0f);
        }

        private static Type GetGame1Type()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET")
                ?.GetType("Dust.Game1");
        }
    }
}
