using System;
using System.Collections.Generic;
using Rimconemy.Foundation.Models;
using Verse;

namespace Rimconemy.Foundation.Events
{
    /// <summary>
    /// Owner: Foundation
    /// Append-only event log with deduplication.
    ///
    /// Events are categorized and limited to prevent log flooding.
    /// Deduplication: identical (Category, EventType, SourcePackageId, Message)
    /// events within a configurable window are counted, not repeated.
    ///
    /// Hook reason: GameComponent ensures the event log persists across
    /// sessions and survives Save/Load.
    /// </summary>
    public static class EventLog
    {
        private const int MaxEvents = 500;
        private const int DedupWindowEvents = 50;

        private static readonly List<EventRecord> _events = new List<EventRecord>();
        private static int _sequenceCounter;
        private static readonly object _lock = new object();

        /// <summary>All recorded events, newest first.</summary>
        public static IReadOnlyList<EventRecord> RecentEvents
        {
            get
            {
                lock (_lock)
                    return _events.AsReadOnly();
            }
        }

        /// <summary>Total events recorded since startup.</summary>
        public static int TotalEvents
        {
            get
            {
                lock (_lock)
                    return _sequenceCounter;
            }
        }

        /// <summary>Number of events currently stored.</summary>
        public static int StoredCount
        {
            get
            {
                lock (_lock)
                    return _events.Count;
            }
        }

        /// <summary>
        /// Records an event. Deduplicates: if the same (category, eventType,
        /// sourcePackageId, message) appears within recent events, it is
        /// counted but not stored as a separate entry.
        /// </summary>
        public static EventRecord Record(
            string category,
            string eventType,
            string sourcePackageId,
            string message,
            string detail = "")
        {
            // Defensive: during static initialization or domain reload,
            // the readonly list reference may not be ready yet.
            if (_events == null)
                return null;

            lock (_lock)
            {
                // Check for duplicates in the recent window
                int checkCount = _events.Count < DedupWindowEvents
                    ? _events.Count : DedupWindowEvents;
                for (int i = 0; i < checkCount; i++)
                {
                    var existing = _events[i];
                    if (existing.Category == category
                        && existing.EventType == eventType
                        && existing.SourcePackageId == sourcePackageId
                        && existing.Message == message)
                    {
                        // Duplicate found; don't store, just return existing
                        return existing;
                    }
                }

                int seq = ++_sequenceCounter;
                // Find.TickManager may be null during static initialization
                // or before a game is loaded. Use null-conditional access
                // instead of catching NullReferenceException.
                int tick = Find.TickManager?.TicksGame ?? 0;

                var record = new EventRecord(
                    seq,
                    tick,
                    category,
                    eventType,
                    sourcePackageId,
                    message,
                    detail);

                // Insert at front (newest first), trim if over limit
                _events.Insert(0, record);
                while (_events.Count > MaxEvents)
                    _events.RemoveAt(_events.Count - 1);

                return record;
            }
        }

        /// <summary>
        /// Restores historical records from a save without creating new events.
        /// Sequence IDs and ticks remain unchanged; the live counter advances
        /// only to the highest restored sequence.
        /// </summary>
        public static void RestoreHistorical(IEnumerable<EventRecord> historicalEvents)
        {
            if (historicalEvents == null)
                return;

            lock (_lock)
            {
                foreach (var historical in historicalEvents)
                {
                    if (historical == null)
                        continue;

                    bool alreadyPresent = false;
                    foreach (var existing in _events)
                    {
                        if (existing.SequenceId == historical.SequenceId
                            && existing.Tick == historical.Tick
                            && existing.Category == historical.Category
                            && existing.EventType == historical.EventType
                            && existing.SourcePackageId == historical.SourcePackageId
                            && existing.Message == historical.Message
                            && existing.Detail == historical.Detail)
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }

                    if (alreadyPresent)
                        continue;

                    _events.Add(historical);
                    if (historical.SequenceId > _sequenceCounter)
                        _sequenceCounter = historical.SequenceId;
                }

                _events.Sort((left, right) => right.SequenceId.CompareTo(left.SequenceId));
                while (_events.Count > MaxEvents)
                    _events.RemoveAt(_events.Count - 1);
            }
        }

        /// <summary>
        /// Replaces the in-memory history with records loaded from the current save.
        /// This prevents a second save loaded in the same process from inheriting
        /// events belonging to the previous save.
        /// </summary>
        public static void ReplaceHistorical(IEnumerable<EventRecord> historicalEvents)
        {
            lock (_lock)
            {
                _events.Clear();
                _sequenceCounter = 0;
            }

            RestoreHistorical(historicalEvents);
        }

        /// <summary>
        /// Clears all stored events. Use with caution;
        /// typically only for testing or reset.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _events.Clear();
                _sequenceCounter = 0;
            }
        }
    }
}
