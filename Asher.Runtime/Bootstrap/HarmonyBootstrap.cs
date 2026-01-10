using HarmonyLib;
using System;

namespace Asher.Runtime.Bootstrap
{
    internal static class HarmonyBootstrap
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                RuntimeLogger.Warning("Harmony já inicializado, ignorando chamada duplicada.");
                return;
            }

            try
            {
                var harmony = new Harmony("com.asher.runtime");
                harmony.PatchAll();

                _initialized = true;
                RuntimeLogger.Info("Harmony inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                RuntimeLogger.Fatal("Falha ao inicializar Harmony", ex);
                throw;
            }
        }
    }
}
