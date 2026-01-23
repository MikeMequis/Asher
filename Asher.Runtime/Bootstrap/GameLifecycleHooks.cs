using Asher.Runtime.Lifecycle;

namespace Asher.Runtime.Bootstrap
{
    /// <summary>
    /// Métodos de hook que são injetados no jogo via Harmony.
    /// Cada método deve ser public static para ser usado como patch.
    /// </summary>
    public static class GameLifecycleHooks
    {
        /// <summary>
        /// Hook aplicado em: Game1.Initialize() (postfix)
        /// </summary>
        public static void OnGameInitialized()
        {
            RuntimeLogger.Info("[Lifecycle] Game1.Initialize concluído");
            LifecycleEventBus.Publish(LifecycleEvent.GameInitialized);
        }

        /// <summary>
        /// Hook aplicado em: Game1.LoadContent() (postfix)
        /// </summary>
        public static void OnContentLoaded()
        {
            RuntimeLogger.Info("[Lifecycle] Game1.LoadContent concluído");
            LifecycleEventBus.Publish(LifecycleEvent.ContentLoaded);
        }

        /// <summary>
        /// Hook para Game1.OnDeactivated() ou método de pausa.
        /// TODO: Implementar quando o método correto for identificado via dnSpy
        /// </summary>
        public static void OnGamePaused()
        {
            RuntimeLogger.Info("[Lifecycle] Jogo pausado");
            LifecycleEventBus.Publish(LifecycleEvent.GamePaused);
        }

        /// <summary>
        /// Hook para Game1.OnActivated() ou método de resume.
        /// TODO: Implementar quando o método correto for identificado
        /// </summary>
        public static void OnGameResumed()
        {
            RuntimeLogger.Info("[Lifecycle] Jogo retomado");
            LifecycleEventBus.Publish(LifecycleEvent.GameResumed);
        }

        /// <summary>
        /// Hook para Game1.OnExiting() ou Game1.Dispose().
        /// TODO: Implementar quando o método correto for identificado
        /// </summary>
        public static void OnGameExiting()
        {
            RuntimeLogger.Info("[Lifecycle] Jogo finalizando");
            LifecycleEventBus.Publish(LifecycleEvent.GameExiting);
        }
    }
}