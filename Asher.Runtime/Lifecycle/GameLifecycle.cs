namespace Asher.Runtime.Lifecycle
{
    public static class GameLifecycle
    {
        public static GameLifecycleState State { get; private set; } = GameLifecycleState.None;

        internal static void SetState(GameLifecycleState state)
        {
            State = state;
            RuntimeLogger.Info($"[Lifecycle] State -> {state}");
        }
    }
}
