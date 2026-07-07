using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.IntroSkipper
{
    /// <summary>
    /// Skips intro sequences once the game is ready (pcManager initialized),
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
                if (game1Type == null)
                {
                    AsherLog.Warning("[IntroSkipper] Dust.Game1 not found");
                    return;
                }

                var drawStartupMethod = game1Type.GetMethod(
                    "DrawStartup",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (drawStartupMethod != null)
                {
                    harmony.Patch(
                        drawStartupMethod,
                        prefix: new HarmonyMethod(typeof(IntroSkipperPatch), nameof(DrawStartupPrefix)));
                }

                var drawMethod = game1Type.GetMethod(
                    "Draw",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (drawMethod == null)
                {
                    AsherLog.Warning("[IntroSkipper] Game1.Draw not found");
                    return;
                }

                harmony.Patch(
                    drawMethod,
                    prefix: new HarmonyMethod(typeof(IntroSkipperPatch), nameof(DrawPrefix)));
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Failed to apply patch: {ex.Message}");
            }
        }

        private static void DrawStartupPrefix()
        {
            if (_skipped)
                return;

            try
            {
                var game1Type = GetGame1Type();
                if (game1Type == null)
                    return;

                var stageField = game1Type.GetField(
                    "startUpStage",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                var timerField = game1Type.GetField(
                    "startUpTimer",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (stageField == null)
                    return;

                int currentStage = (int)stageField.GetValue(null);
                if (currentStage < 6)
                {
                    stageField.SetValue(null, 6);
                    timerField?.SetValue(null, 0f);
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] DrawStartup failed: {ex.Message}");
            }
        }

        private static void DrawPrefix()
        {
            if (_skipped)
                return;

            try
            {
                if (!IsGameReady())
                    return;

                SkipToMainMenu();
                _skipped = true;
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Failed to skip intro: {ex.Message}");
            }
        }

        private static bool IsGameReady()
        {
            var game1Type = GetGame1Type();
            if (game1Type == null)
                return false;

            var pcManagerField = game1Type.GetField(
                "pcManager",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return pcManagerField?.GetValue(null) != null;
        }

        private static void SkipToMainMenu()
        {
            var game1Type = GetGame1Type();
            if (game1Type == null)
                return;

            var gameModesType = game1Type.GetNestedType(
                "GameModes",
                BindingFlags.Public | BindingFlags.NonPublic);

            var gameModeField = game1Type.GetField(
                "gameMode",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (gameModesType == null || gameModeField == null)
            {
                AsherLog.Warning("[IntroSkipper] gameMode fields not found");
                return;
            }

            var mainMenu = Enum.Parse(gameModesType, "MainMenu");
            gameModeField.SetValue(null, mainMenu);
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
