using Asher.SDK.Logging;
using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Patching.IntroSkipper
{
    /// <summary>
    /// Skips startup splashes and intro video, then jumps straight to the main menu
    /// "Press A" screen. Menu music is deferred until initLoaded so Music::Play works.
    /// </summary>
    public sealed class IntroSkipperPatch : IAsherPatchModule
    {
        private static volatile bool _mainMenuApplied;
        private static volatile bool _skipApplied;

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
                    AsherLog.Warning("[IntroSkipper] Não foi possível encontrar Dust.Game1");
                    return;
                }

                var drawStartupMethod = game1Type.GetMethod(
                    "DrawStartup",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                var updateMethod = game1Type.GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (drawStartupMethod == null)
                {
                    AsherLog.Warning("[IntroSkipper] Não foi possível encontrar Game1.DrawStartup");
                    return;
                }

                harmony.Patch(
                    drawStartupMethod,
                    prefix: new HarmonyMethod(typeof(IntroSkipperPatch), nameof(DrawStartupPrefix)));

                if (updateMethod != null)
                {
                    harmony.Patch(
                        updateMethod,
                        postfix: new HarmonyMethod(typeof(IntroSkipperPatch), nameof(UpdatePostfix)));
                }
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Erro ao aplicar patch: {ex.Message}");
            }
        }

        private static void UpdatePostfix()
        {
            TryApplyIntroSkip();
        }

        /// <summary>
        /// Block the entire startup draw path (logos, splash audio, intro video).
        /// </summary>
        private static bool DrawStartupPrefix(object __instance)
        {
            TryApplyIntroSkip();

            if (!_mainMenuApplied)
                ClearToBlack(__instance);

            return false;
        }

        private static void TryApplyIntroSkip()
        {
            if (_skipApplied || !IsPcManagerReady())
                return;

            try
            {
                var game1Type = GetGame1Type();
                if (game1Type == null)
                    return;

                if (!_mainMenuApplied)
                {
                    StopStartupVideo(game1Type);
                    SetGameModeMainMenu(game1Type);
                    _mainMenuApplied = true;
                }

                // SkipToStartPage calls Music::Play("beauty"), which only works after
                // LoadInitContent finishes and initLoaded is set.
                if (!IsInitLoaded())
                    return;

                var menu = game1Type.GetField(
                        "menu",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(null);

                if (menu == null)
                    return;

                menu.GetType()
                    .GetMethod("SkipToStartPage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.Invoke(menu, null);

                _skipApplied = true;
            }
            catch (Exception ex)
            {
                AsherLog.Error($"[IntroSkipper] Erro ao aplicar skip: {ex.Message}");
            }
        }

        private static void SetGameModeMainMenu(Type game1Type)
        {
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

        private static void StopStartupVideo(Type game1Type)
        {
            var videoPlayer = game1Type.GetField(
                    "videoPlayer",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(null);

            if (videoPlayer == null)
                return;

            videoPlayer.GetType()
                .GetMethod("Stop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(videoPlayer, null);
        }

        private static void ClearToBlack(object gameInstance)
        {
            try
            {
                var gameType = gameInstance.GetType().BaseType;
                var graphicsDevice = gameType?.GetMethod(
                        "get_GraphicsDevice",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.Invoke(gameInstance, null);

                if (graphicsDevice == null)
                    return;

                var colorType = graphicsDevice.GetType().Assembly
                    .GetType("Microsoft.Xna.Framework.Color");

                var black = colorType?.GetProperty(
                        "Black",
                        BindingFlags.Static | BindingFlags.Public)
                    ?.GetValue(null);

                if (black == null || colorType == null)
                    return;

                graphicsDevice.GetType()
                    .GetMethod(
                        "Clear",
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        new[] { colorType },
                        null)
                    ?.Invoke(graphicsDevice, new[] { black });
            }
            catch
            {
                // Non-fatal; a blank frame is acceptable while loading finishes.
            }
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

        private static bool IsInitLoaded()
        {
            var game1Type = GetGame1Type();
            if (game1Type == null)
                return false;

            var initLoadedField = game1Type.GetField(
                "initLoaded",
                BindingFlags.Static | BindingFlags.NonPublic);

            return initLoadedField != null && (bool)initLoadedField.GetValue(null);
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
