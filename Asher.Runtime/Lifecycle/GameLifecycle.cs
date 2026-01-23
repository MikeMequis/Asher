using System;
using System.Collections.Generic;

namespace Asher.Runtime.Lifecycle
{
    public static class GameLifecycle
    {
        private static GameLifecycleState _currentState = GameLifecycleState.None;

        public static GameLifecycleState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState == value)
                    return;

                var previous = _currentState;
                _currentState = value;

                RuntimeLogger.Info($"[Lifecycle] {previous} -> {value}");
                OnStateChanged?.Invoke(previous, value);
            }
        }

        public static event Action<GameLifecycleState, GameLifecycleState>? OnStateChanged;

        internal static void SetState(GameLifecycleState state)
        {
            CurrentState = state;
        }
    }

    internal static class LifecycleEventBus
    {
        private static readonly Dictionary<LifecycleEvent, List<Action>> _subscribers = new();

        public static void Subscribe(LifecycleEvent evt, Action handler)
        {
            if (!_subscribers.ContainsKey(evt))
                _subscribers[evt] = new List<Action>();

            _subscribers[evt].Add(handler);
        }

        public static void Publish(LifecycleEvent evt)
        {
            if (!_subscribers.ContainsKey(evt))
                return;

            foreach (var handler in _subscribers[evt])
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    RuntimeLogger.Error($"[LifecycleEventBus] Erro ao executar handler para {evt}", ex);
                }
            }
        }
    }
}