using System;

namespace Asher.Runtime.Core
{
    public static class GameContext
    {
        private static object? _gameInstance;
        private static Type? _gameType;

        public static object? GameInstance
        {
            get => _gameInstance;
            internal set
            {
                _gameInstance = value;
                _gameType = value?.GetType();
                RuntimeLogger.Info($"[GameContext] Game instance definida: {_gameType?.FullName ?? "null"}");
            }
        }

        public static Type? GameType => _gameType;

        public static bool HasGame => _gameInstance != null;

        public static T? GetGameField<T>(string fieldName)
        {
            if (_gameInstance == null || _gameType == null)
                return default;

            try
            {
                var field = _gameType.GetField(fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (field != null)
                    return (T?)field.GetValue(_gameInstance);

                var property = _gameType.GetProperty(fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                return property != null ? (T?)property.GetValue(_gameInstance) : default;
            }
            catch
            {
                return default;
            }
        }
    }
}