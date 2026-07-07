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
        public static bool HasModules => _modules.Count > 0;

        public static void Load()
        {
            if (_loaded)
                return;

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

            foreach (var type in moduleTypes)
            {
                try
                {
                    var module = (IAsherLifecycleModule)Activator.CreateInstance(type)!;
                    _modules.Add(module);
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[Lifecycle] Failed to load {type.FullName}: {ex.Message}", ex);
                }
            }

            if (_modules.Count > 0)
            {
                LifecycleEventBus.Subscribe(LifecycleEvent.GameInitialized, OnGameInitialized);
                LifecycleEventBus.Subscribe(LifecycleEvent.ContentLoaded, OnContentLoaded);
                LifecycleEventBus.Subscribe(LifecycleEvent.GamePaused, OnGamePaused);
                LifecycleEventBus.Subscribe(LifecycleEvent.GameExiting, OnGameExiting);
            }

            if (_modules.Count > 0)
                RuntimeLogger.Info($"[Lifecycle] {_modules.Count} modules registered.");

            _loaded = true;
        }

        private static void OnGameInitialized()
        {
            foreach (var module in _modules)
            {
                try { module.OnGameInitialized(); }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[Lifecycle] {module.Name}.OnGameInitialized failed", ex);
                }
            }
        }

        private static void OnContentLoaded()
        {
            foreach (var module in _modules)
            {
                try { module.OnContentLoaded(); }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[Lifecycle] {module.Name}.OnContentLoaded failed", ex);
                }
            }
        }

        private static void OnGamePaused()
        {
            foreach (var module in _modules)
            {
                try { module.OnGamePaused(); }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[Lifecycle] {module.Name}.OnGamePaused failed", ex);
                }
            }
        }

        private static void OnGameExiting()
        {
            foreach (var module in _modules)
            {
                try { module.OnGameExiting(); }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[Lifecycle] {module.Name}.OnGameExiting failed", ex);
                }
            }
        }
    }
}
