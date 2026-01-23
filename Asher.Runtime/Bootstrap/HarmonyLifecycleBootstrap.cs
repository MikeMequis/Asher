using Asher.SDK.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Asher.Runtime.Bootstrap
{
    /// <summary>
    /// Aplica hooks automáticos no ciclo de vida do jogo usando Harmony.
    /// Apenas ativa se módulos de lifecycle estiverem carregados.
    /// </summary>
    public static class HarmonyLifecycleBootstrap
    {
        private static bool _initialized;
        private static readonly Harmony _lifecycleHarmony = new("com.asher.runtime.lifecycle");

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
                
                // Hooks implementados
                ApplyGameInitializeHook();
                ApplyContentLoadedHook();
                
                // Hooks futuros (descomentе quando necessário)
                // ApplyGamePausedHook();
                // ApplyGameExitingHook();

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
            var game1Type = GetGame1Type();
            if (game1Type == null) return;

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
                RuntimeLogger.Info("[LifecycleBootstrap] ✓ Hook em Game1.Initialize aplicado.");
            }
        }

        private static void ApplyContentLoadedHook()
        {
            var game1Type = GetGame1Type();
            if (game1Type == null) return;

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
                RuntimeLogger.Info("[LifecycleBootstrap] ✓ Hook em Game1.LoadContent aplicado.");
            }
        }

        /// <summary>
        /// Hook para detectar pausa do jogo.
        /// Requer análise do jogo para identificar o método correto.
        /// Possíveis alvos:
        /// - Game1.OnDeactivated()
        /// - PauseMenu.Show()
        /// - Game1.Update() quando isPaused == true
        /// </summary>
        private static void ApplyGamePausedHook()
        {
            // TODO: Implementar quando o método correto for identificado
            RuntimeLogger.Info("[LifecycleBootstrap] ⚠️ GamePaused hook não implementado ainda.");
        }

        /// <summary>
        /// Hook para detectar finalização do jogo.
        /// Possíveis alvos:
        /// - Game1.OnExiting()
        /// - Game1.Dispose()
        /// - Application.ApplicationExit
        /// </summary>
        private static void ApplyGameExitingHook()
        {
            // TODO: Implementar quando o método correto for identificado
            RuntimeLogger.Info("[LifecycleBootstrap] ⚠️ GameExiting hook não implementado ainda.");
        }

        private static Type? GetGame1Type()
        {
            var dustAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "DustAET");

            if (dustAssembly == null)
            {
                RuntimeLogger.Warning("[LifecycleBootstrap] DustAET assembly não encontrado.");
                return null;
            }

            return dustAssembly.GetType("Dust.Game1");
        }
    }
}