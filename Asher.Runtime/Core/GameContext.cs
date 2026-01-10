namespace Asher.Runtime.Core
{
    public static class GameContext
    {
        public static object? GameInstance { get; internal set; }
        public static bool HasGame => GameInstance != null;
    }

}