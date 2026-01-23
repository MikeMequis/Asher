using HarmonyLib;
using System;
using System.Linq;

namespace Asher.Runtime.Bootstrap
{
    public static class PatchModuleLoader
    {
        private static bool _loaded = false;

        public static void Load()
        {
            if (_loaded)
            {
                RuntimeLogger.Warning("[PatchModuleLoader] Patches já foram carregados.");
                return;
            }

            RuntimeLogger.Info("[PatchModuleLoader] Iniciando carregamento de módulos de patch...");

            Harmony harmony = new("com.asher.runtime.mods");

            var modules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    typeof(IAsherPatchModule).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface
                );

            int count = 0;
            foreach (var type in modules)
            {
                try
                {
                    var module = (IAsherPatchModule)Activator.CreateInstance(type)!;
                    RuntimeLogger.Info($"[PatchModuleLoader] Aplicando módulo: {module.Name}");
                    module.Apply(harmony);
                    count++;
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[PatchModuleLoader] Erro ao carregar {type.FullName}: {ex.Message}", ex);
                }
            }

            RuntimeLogger.Info($"[PatchModuleLoader] {count} módulos de patch aplicados.");

            LifecycleModuleLoader.Load();

            if (LifecycleModuleLoader.HasModules)
            {
                RuntimeLogger.Info("[PatchModuleLoader] Módulos de lifecycle detectados, aplicando hooks...");
                HarmonyLifecycleBootstrap.InitializeIfNeeded();
            }
            else
            {
                RuntimeLogger.Info("[PatchModuleLoader] Nenhum módulo de lifecycle detectado, hooks não serão aplicados.");
            }

            _loaded = true;
        }
    }
}