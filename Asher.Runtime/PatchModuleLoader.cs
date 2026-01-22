using HarmonyLib;
using System;
using System.Linq;

namespace Asher.Runtime
{
    public static class PatchModuleLoader
    {
        public static void Load()
        {
            RuntimeLogger.Info("[PatchModuleLoader] Iniciando carregamento de módulos de patch...");

            var harmony = new Harmony("com.asher.runtime.mods");

            var modules =
                AppDomain.CurrentDomain.GetAssemblies()
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
                    RuntimeLogger.Error($"[PatchModuleLoader] Erro ao carregar {type.FullName}: {ex.Message}");
                }
            }

            RuntimeLogger.Info($"[PatchModuleLoader] {count} módulos de patch aplicados.");
        }
    }
}