using Asher.Runtime.Lifecycle;
using System;

namespace Asher.Runtime.Bootstrap
{
    public static class GameLifecycleHooks
    {
        public static void OnGameInitialized()
        {
            RuntimeLogger.Info("[Lifecycle] Game1.Initialize concluído");

            LifecycleEventBus.Publish(LifecycleEvent.GameInitialized);
        }

        public static void OnContentLoaded()
        {
            RuntimeLogger.Info("[Lifecycle] Game1.LoadContent concluído");
            LifecycleEventBus.Publish(LifecycleEvent.ContentLoaded);
        }

        // Futuro: OnUpdate, OnDraw, etc.
    }
}