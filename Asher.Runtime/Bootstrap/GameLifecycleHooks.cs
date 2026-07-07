using Asher.Runtime.Lifecycle;

namespace Asher.Runtime.Bootstrap
{
    /// <summary>
    /// Métodos de hook que são injetados no jogo via Harmony.
    /// Cada método deve ser public static para ser usado como patch.
    /// </summary>
    public static class GameLifecycleHooks
    {
        public static void OnGameInitialized()
        {
            LifecycleEventBus.Publish(LifecycleEvent.GameInitialized);
        }

        public static void OnContentLoaded()
        {
            LifecycleEventBus.Publish(LifecycleEvent.ContentLoaded);
        }

        public static void OnGamePaused()
        {
            LifecycleEventBus.Publish(LifecycleEvent.GamePaused);
        }

        public static void OnGameResumed()
        {
            LifecycleEventBus.Publish(LifecycleEvent.GameResumed);
        }

        public static void OnGameExiting()
        {
            LifecycleEventBus.Publish(LifecycleEvent.GameExiting);
        }
    }
}
