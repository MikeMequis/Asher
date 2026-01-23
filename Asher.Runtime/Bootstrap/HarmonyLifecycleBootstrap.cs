using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    public static class HarmonyLifecycleBootstrap
    {
        private static bool _initialized;
        private static readonly Harmony _lifecycleHarmony = new Harmony("com.asher.runtime.lifecycle");

        public static void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            if (!RequiresLifecycleHooks())
            {
                RuntimeLogger.Info("[LifecycleBootstrap] Nenhum módulo requer lifecycle hooks, pulando inicialização.");
                return;
            }

            try
            {
                RuntimeLogger.Info("[LifecycleBootstrap] Aplicando lifecycle hooks...");
                ApplyGameInitializeHook();
                ApplyContentLoadedHook();

                _initialized = true;
                RuntimeLogger.Info("[LifecycleBootstrap] Lifecycle hooks aplicados com sucesso.");
            }
            catch (Exception ex)
            {
                RuntimeLogger.Error("[LifecycleBootstrap] Falha ao aplicar lifecycle hooks", ex);
                throw;
            }
        }

        private static bool RequiresLifecycleHooks()
        {
            var hasLifecycleModules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Any(t => typeof(IAsherLifecycleModule).IsAssignableFrom(t) &&
                         !t.IsAbstract &&
                         !t.IsInterface);

            return hasLifecycleModules;
        }

        private static void ApplyGameInitializeHook()
        {
            var dustAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET");

            if (dustAssembly == null)
            {
                RuntimeLogger.Warning("[LifecycleBootstrap] DustAET não encontrado, hooks não serão aplicados.");
                return;
            }

            var game1Type = dustAssembly.GetType("Dust.Game1");
            if (game1Type == null)
                return;

            var initMethod = game1Type.GetMethod("Initialize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (initMethod != null)
            {
                _lifecycleHarmony.Patch(
                    initMethod,
                    postfix: new HarmonyMethod(
                        typeof(GameLifecycleHooks),
                        nameof(GameLifecycleHooks.OnGameInitialized)
                    )
                );
                RuntimeLogger.Info("[LifecycleBootstrap] Hook em Game1.Initialize aplicado.");
            }
        }

        private static void ApplyContentLoadedHook()
        {
            var dustAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET");

            if (dustAssembly == null)
                return;

            var game1Type = dustAssembly.GetType("Dust.Game1");
            if (game1Type == null)
                return;

            var loadContentMethod = game1Type.GetMethod("LoadContent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (loadContentMethod != null)
            {
                _lifecycleHarmony.Patch(
                    loadContentMethod,
                    postfix: new HarmonyMethod(
                        typeof(GameLifecycleHooks),
                        nameof(GameLifecycleHooks.OnContentLoaded)
                    )
                );
                RuntimeLogger.Info("[LifecycleBootstrap] Hook em Game1.LoadContent aplicado.");
            }
        }
    }
}