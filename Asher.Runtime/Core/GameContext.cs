using System;
using System.Reflection;

namespace Asher.Runtime.Core
{
    /// <summary>
    /// Contexto global do jogo em runtime.
    /// NOTA: Atualmente não utilizado - preparado para funcionalidades futuras.
    /// 
    /// Casos de uso futuros:
    /// - Acesso a Game1 instance de qualquer mod
    /// - Leitura de campos privados do jogo
    /// - Injeção de dependências em mods
    /// </summary>
    public static class GameContext
    {
        private static object? _gameInstance;
        private static Type? _gameType;

        /// <summary>
        /// Tipo da classe Game1 (Dust.Game1)
        /// </summary>
        public static Type? GameType => _gameType;

        /// <summary>
        /// Indica se a instância do jogo foi capturada
        /// </summary>
        public static bool HasGame => _gameInstance != null;

        /// <summary>
        /// Instância atual do Game1.
        /// Define automaticamente o tipo quando atribuído.
        /// </summary>
        public static object? GameInstance
        {
            get => _gameInstance;
            internal set
            {
                _gameInstance = value;
                _gameType = value?.GetType();
                RuntimeLogger.Info($"[GameContext] Game instance captured: {_gameType?.FullName ?? "null"}");
            }
        }

        /// <summary>
        /// Obtém um campo ou propriedade do game instance via Reflection.
        /// Útil para acessar membros privados do jogo.
        /// </summary>
        /// <typeparam name="T">Tipo esperado do campo/propriedade</typeparam>
        /// <param name="memberName">Nome do campo ou propriedade</param>
        /// <returns>Valor do membro, ou default(T) se não encontrado</returns>
        public static T? GetGameMember<T>(string memberName)
        {
            if (_gameInstance == null || _gameType == null)
            {
                RuntimeLogger.Warning($"[GameContext] Tentativa de acessar '{memberName}' mas GameInstance é null");
                return default;
            }

            try
            {
                // Tenta acessar como campo
                var field = _gameType.GetField(memberName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (field != null)
                    return (T?)field.GetValue(_gameInstance);

                // Tenta acessar como propriedade
                var property = _gameType.GetProperty(memberName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (property != null)
                    return (T?)property.GetValue(_gameInstance);

                RuntimeLogger.Warning($"[GameContext] Membro '{memberName}' não encontrado em {_gameType.Name}");
                return default;
            }
            catch (Exception ex)
            {
                RuntimeLogger.Error($"[GameContext] Erro ao acessar '{memberName}'", ex);
                return default;
            }
        }

        /// <summary>
        /// Define o valor de um campo ou propriedade do game instance.
        /// </summary>
        public static bool SetGameMember<T>(string memberName, T value)
        {
            if (_gameInstance == null || _gameType == null)
            {
                RuntimeLogger.Warning($"[GameContext] Tentativa de modificar '{memberName}' mas GameInstance é null");
                return false;
            }

            try
            {
                // Tenta como campo
                var field = _gameType.GetField(memberName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (field != null)
                {
                    field.SetValue(_gameInstance, value);
                    return true;
                }

                // Tenta como propriedade
                var property = _gameType.GetProperty(memberName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (property != null && property.CanWrite)
                {
                    property.SetValue(_gameInstance, value);
                    return true;
                }

                RuntimeLogger.Warning($"[GameContext] Membro '{memberName}' não encontrado ou não é writable");
                return false;
            }
            catch (Exception ex)
            {
                RuntimeLogger.Error($"[GameContext] Erro ao modificar '{memberName}'", ex);
                return false;
            }
        }
    }
}