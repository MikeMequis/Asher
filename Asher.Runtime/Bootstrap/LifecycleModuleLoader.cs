using Asher.Runtime.Lifecycle;
using Asher.SDK.Patching;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Asher.Runtime.Bootstrap
{
    public static class LifecycleModuleLoader
    {
        private static readonly List<IAsherLifecycleModule> _modules = new List<IAsherLifecycleModule>();
        private static bool _loaded = false;

        public static void Load()
        {
            if (_loaded)
            {
                RuntimeLogger.Warning("[LifecycleModuleLoader] Módulos de lifecycle já foram carregados.");
                return;
            }

            RuntimeLogger.Info("[LifecycleModuleLoader] Iniciando carregamento de módulos de lifecycle...");

            var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    typeof(IAsherLifecycleModule).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface
                );

            int count = 0;
            foreach (var type in moduleTypes)
            {
                try
                {
                    var module = (IAsherLifecycleModule)Activator.CreateInstance(type)!;
                    _modules.Add(module);
                    RuntimeLogger.Info($"[LifecycleModuleLoader] Módulo registrado: {module.Name}");
                    count++;
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[LifecycleModuleLoader] Erro ao carregar {type.FullName}: {ex.Message}", ex);
                }
            }

            if (count > 0)
            {
                LifecycleEventBus.Subscribe(LifecycleEvent.GameInitialized, OnGameInitialized);
                LifecycleEventBus.Subscribe(LifecycleEvent.ContentLoaded, OnContentLoaded);
                LifecycleEventBus.Subscribe(LifecycleEvent.GamePaused, OnGamePaused);
                LifecycleEventBus.Subscribe(LifecycleEvent.GameExiting, OnGameExiting);
            }

            RuntimeLogger.Info($"[LifecycleModuleLoader] {count} módulos de lifecycle carregados.");
            _loaded = true;
        }

        private static void OnGameInitialized()
        {
            RuntimeLogger.Info("[LifecycleModuleLoader] Notificando módulos: GameInitialized");
            foreach (var module in _modules)
            {
                try
                {
                    module.OnGameInitialized();
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[LifecycleModuleLoader] Erro em {module.Name}.OnGameInitialized", ex);
                }
            }
        }

        private static void OnContentLoaded()
        {
            RuntimeLogger.Info("[LifecycleModuleLoader] Notificando módulos: ContentLoaded");
            foreach (var module in _modules)
            {
                try
                {
                    module.OnContentLoaded();
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[LifecycleModuleLoader] Erro em {module.Name}.OnContentLoaded", ex);
                }
            }
        }

        private static void OnGamePaused()
        {
            RuntimeLogger.Info("[LifecycleModuleLoader] Notificando módulos: GamePaused");
            foreach (var module in _modules)
            {
                try
                {
                    module.OnGamePaused();
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[LifecycleModuleLoader] Erro em {module.Name}.OnGamePaused", ex);
                }
            }
        }

        private static void OnGameExiting()
        {
            RuntimeLogger.Info("[LifecycleModuleLoader] Notificando módulos: GameExiting");
            foreach (var module in _modules)
            {
                try
                {
                    module.OnGameExiting();
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[LifecycleModuleLoader] Erro em {module.Name}.OnGameExiting", ex);
                }
            }
        }

        public static bool HasModules => _modules.Count > 0;
    }
}