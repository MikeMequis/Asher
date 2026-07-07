using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Asher.Patching.IntroSkipper
{
    /// <summary>
    /// Skips intro sequences by suppressing startup draws and switching to MainMenu
    /// as soon as pcManager is initialized, matching legacy SkipIntro timing.
    /// </summary>
    public sealed class IntroSkipperPatch : IAsherPatchModule
    {
        private static volatile bool _skipped;
        private static int _skipThreadStarted;

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

                StartSkipThread();

                var drawStartupMethod = game1Type.GetMethod(
                    "DrawStartup",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (drawStartupMethod != null)
                {
                    harmony.Patch(
                        drawStartupMethod,
                        prefix: new HarmonyMethod(typeof(IntroSkipperPatch), nameof(DrawStartupPrefix)));
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Failed to apply patch: {ex.Message}");
            }
        }

        private static void StartSkipThread()
        {
            if (Interlocked.CompareExchange(ref _skipThreadStarted, 1, 0) != 0)
                return;

            var thread = new Thread(WaitForGameReadyAndSkip)
            {
                IsBackground = true,
                Name = "IntroSkipper"
            };
            thread.Start();
        }

        private static void WaitForGameReadyAndSkip()
        {
            try
            {
                while (!IsGameReady())
                {
                    // Match legacy SkipIntro: poll until pcManager exists.
                    Thread.SpinWait(1);
                }

                SkipToMainMenu();
                _skipped = true;
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Skip thread failed: {ex.Message}");
            }
        }

        private static bool DrawStartupPrefix()
        {
            if (_skipped)
                return true;

            try
            {
                var game1Type = GetGame1Type();
                if (game1Type == null)
                    return true;

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
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] DrawStartup failed: {ex.Message}");
                return true;
            }

            // Suppress ESRB/logo/video startup draws until MainMenu is set.
            return false;
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
