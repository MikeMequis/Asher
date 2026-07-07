using System;
using System.Collections.Generic;

namespace Asher.Runtime.Lifecycle
{
    /// <summary>
    /// Event bus interno para comunicação de eventos do ciclo de vida.
    /// </summary>
    internal static class LifecycleEventBus
    {
        private static readonly Dictionary<LifecycleEvent, List<Action>> _subscribers = new();

        /// <summary>
        /// Registra um handler para um evento específico
        /// </summary>
        public static void Subscribe(LifecycleEvent evt, Action handler)
        {
            if (!_subscribers.ContainsKey(evt))
                _subscribers[evt] = new List<Action>();

            _subscribers[evt].Add(handler);
        }

        /// <summary>
        /// Dispara um evento para todos os subscribers
        /// </summary>
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

        /// <summary>
        /// Retorna o número de subscribers para um evento
        /// </summary>
        public static int GetSubscriberCount(LifecycleEvent evt)
        {
            return _subscribers.ContainsKey(evt) ? _subscribers[evt].Count : 0;
        }
    }
}