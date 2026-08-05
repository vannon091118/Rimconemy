using System;
using System.Collections.Generic;

namespace Rimconemy.Foundation.Bridge
{
    /// <summary>
    /// Event bridge for loose-coupled event publishing/subscribing across packages.
    /// Allows packages to communicate without direct DLL references.
    /// </summary>
    public delegate void EventCallback();
    
    public static class EventBridge
    {
        private static readonly Dictionary<string, List<EventCallback>> subscribers = 
            new Dictionary<string, List<EventCallback>>();
        
        /// <summary>
        /// Subscribe to an event.
        /// </summary>
        public static void Subscribe(string eventKey, EventCallback callback)
        {
            if (!subscribers.ContainsKey(eventKey))
                subscribers[eventKey] = new List<EventCallback>();
                
            subscribers[eventKey].Add(callback);
        }
        
        /// <summary>
        /// Unsubscribe from an event.
        /// </summary>
        public static void Unsubscribe(string eventKey, EventCallback callback)
        {
            if (subscribers.TryGetValue(eventKey, out var callbacks))
                callbacks.Remove(callback);
        }
        
        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        public static void Publish(string eventKey)
        {
            if (subscribers.TryGetValue(eventKey, out var callbacks))
            {
                // Copy to avoid issues if callbacks modify the list
                var callbacksCopy = new List<EventCallback>(callbacks);
                foreach (var callback in callbacksCopy)
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Verse.Log.Error($"EventBridge callback failed for {eventKey}: {e}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Clear all subscribers (for testing).
        /// </summary>
        public static void Clear()
        {
            subscribers.Clear();
        }
    }
}