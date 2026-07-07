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
    /// Pula a intro combinando o skip de estágios no DrawStartup (Asher original)
    /// com a troca para MainMenu após pcManager inicializar (legacy SkipIntro).
    /// </summary>
    public sealed class IntroSkipperPatch : IAsherPatchModule
    {
        private static volatile bool _mainMenuApplied;
        private static int _skipThreadStarted;

        public static bool Enabled { get; set; }

        public string Name => "Intro Skipper";

        public void Apply(Harmony harmony)
        {
            if (!Enabled)
                return;

            try
            {
                StartLegacySkipThread();

                var game1Type = GetGame1Type();
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
                    prefix: new HarmonyMethod(typeof(IntroSkipperPatch), nameof(DrawStartupPrefix)));
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Erro ao aplicar patch: {ex.Message}");
            }
        }

        private static void StartLegacySkipThread()
        {
            if (Interlocked.CompareExchange(ref _skipThreadStarted, 1, 0) != 0)
                return;

            var thread = new Thread(LegacySkipProc)
            {
                IsBackground = true,
                Name = "IntroSkipper"
            };
            thread.Start();
        }

        private static void LegacySkipProc()
        {
            try
            {
                while (!IsPcManagerReady())
                {
                    // Legacy SkipIntro: poll until pcManager exists.
                }

                SetMainMenu();
                _mainMenuApplied = true;
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Skip thread failed: {ex.Message}");
            }
        }

        private static bool DrawStartupPrefix()
        {
            if (_mainMenuApplied)
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

                if (stageField == null)
                    return true;

                int currentStage = (int)stageField.GetValue(null);

                if (currentStage < 6)
                {
                    stageField.SetValue(null, 6);
                    if (timerField != null)
                        timerField.SetValue(null, 0f);
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Erro durante DrawStartup: {ex.Message}");
            }

            return true;
        }

        private static bool IsPcManagerReady()
        {
            var game1Type = GetGame1Type();
            if (game1Type == null)
                return false;

            var pcManagerField = game1Type.GetField(
                "pcManager",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return pcManagerField != null && pcManagerField.GetValue(null) != null;
        }

        private static void SetMainMenu()
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
                return;

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
