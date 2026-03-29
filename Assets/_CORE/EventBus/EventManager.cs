using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveGame.Core
{
    public sealed class EventManager : MonoBehaviour
    {
        private readonly Dictionary<Type, Delegate> subscriptions = new Dictionary<Type, Delegate>();

        public void Initialize()
        {
        }

        public void Subscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                return;
            }

            var eventType = typeof(TEvent);
            subscriptions.TryGetValue(eventType, out var existing);
            subscriptions[eventType] = Delegate.Combine(existing, listener);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                return;
            }

            var eventType = typeof(TEvent);
            if (!subscriptions.TryGetValue(eventType, out var existing))
            {
                return;
            }

            var updated = Delegate.Remove(existing, listener);
            if (updated == null)
            {
                subscriptions.Remove(eventType);
                return;
            }

            subscriptions[eventType] = updated;
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            var eventType = typeof(TEvent);
            if (!subscriptions.TryGetValue(eventType, out var existing))
            {
                return;
            }

            if (existing is Action<TEvent> callback)
            {
                callback.Invoke(eventData);
            }
        }

        public void ClearAllSubscriptions()
        {
            subscriptions.Clear();
        }
    }
}
