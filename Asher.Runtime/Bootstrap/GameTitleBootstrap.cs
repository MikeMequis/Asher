using HarmonyLib;
using System;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    public static class GameTitleBootstrap
    {
        private const string GameWindowTitle = "Dust - An Elysian Tail (Asher)";

        public static void Apply(Assembly gameAssembly)
        {
            var harmony = new Harmony("com.asher.runtime.title");
            var game1Type = gameAssembly.GetType("Dust.Game1");
            if (game1Type == null)
            {
                RuntimeLogger.Warning("[GameTitle] Tipo Dust.Game1 não encontrado.");
                return;
            }

            var initializeMethod = game1Type.GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (initializeMethod == null)
            {
                RuntimeLogger.Warning("[GameTitle] Método Game1.Initialize não encontrado.");
                return;
            }

            harmony.Patch(
                initializeMethod,
                postfix: new HarmonyMethod(typeof(GameTitleBootstrap), nameof(SetWindowTitle)));

        }

        private static void SetWindowTitle(object __instance)
        {
            try
            {
                var window = __instance.GetType().GetProperty(
                    "Window",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(__instance);

                if (window == null)
                    return;

                var titleProperty = window.GetType().GetProperty("Title");
                titleProperty?.SetValue(window, GameWindowTitle);
            }
            catch (Exception ex)
            {
                RuntimeLogger.Warning($"[GameTitle] Falha ao definir título: {ex.Message}");
            }
        }
    }
}
