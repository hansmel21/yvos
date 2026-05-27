// ============================================================
//  EventBus.cs
//  Generic static publish/subscribe message bus.
//  All systems communicate through events — no direct refs.
//
//  Usage:
//    EventBus.Subscribe<YearStartedEvent>(OnYearStarted);
//    EventBus.Publish(new YearStartedEvent { worldYear = 851 });
//    EventBus.Unsubscribe<YearStartedEvent>(OnYearStarted);
// ============================================================
using System;
using System.Collections.Generic;

namespace YVOS.Core
{
    /// <summary>Marker interface. All game events implement this.</summary>
    public interface IEvent { }

    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers
            = new Dictionary<Type, List<Delegate>>();

        // -----------------------------------------------------------------
        //  Public API
        // -----------------------------------------------------------------

        public static void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        /// <summary>
        /// Publish an event. Iterates a snapshot so handlers may
        /// subscribe/unsubscribe without breaking iteration.
        /// </summary>
        public static void Publish<T>(T evt) where T : IEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0)
                return;

            var snapshot = new List<Delegate>(list);
            foreach (var handler in snapshot)
                (handler as Action<T>)?.Invoke(evt);
        }

        /// <summary>
        /// Remove all subscriptions. Call on scene unload to prevent
        /// stale references.
        /// </summary>
        public static void ClearAll() => _handlers.Clear();
    }
}
