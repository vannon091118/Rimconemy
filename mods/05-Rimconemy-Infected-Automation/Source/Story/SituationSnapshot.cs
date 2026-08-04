using System;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// Aggregated, immutable read-model of the current situation.
    /// Contains no Pawn/Thing/Map object references — only
    /// serializable values, hashes and IDs.
    ///
    /// Foundation reads this snapshot for dashboard display.
    /// Other packages supply data through the service bus.
    ///
    /// Specification: docs/tasks.md §4 (SituationSnapshot)
    /// </summary>
    public sealed class SituationSnapshot
    {
        /// <summary>Schema version for save migration.</summary>
        public int SchemaVersion = 1;

        /// <summary>Game time in ticks when this snapshot was taken.</summary>
        public long GameTick;

        /// <summary>Days elapsed (GameTick / <see cref="Rimconemy.Foundation.TimeConstants.TicksPerDay"/>).</summary>
        public float GameDays => GameTick / Rimconemy.Foundation.TimeConstants.TicksPerDay;

        // ── survivors ────────────────────────────────────────
        /// <summary>Number of directly controlled player colonists.</summary>
        public int SurvivorCount;

        /// <summary>Average health percentage of all survivors (0-1).</summary>
        public float AverageSurvivorHealth;

        /// <summary>True if a game-over condition is active.</summary>
        public bool GameOverPending;

        // ── threat ───────────────────────────────────────────
        /// <summary>Current threat pressure from ThreatAggregator (0-1).</summary>
        public float ThreatPressure;

        /// <summary>Threat trend (-1 to +1): rising or falling.</summary>
        public float ThreatTrend;

        // ── ideology ─────────────────────────────────────────
        /// <summary>Current ideology tension (0-1).</summary>
        public float IdeologyTension;

        /// <summary>Number of active setting rules.</summary>
        public int ActiveSettingRuleCount;

        // ── storage ──────────────────────────────────────────
        /// <summary>SHA-256 hash of the last StorageSnapshot.ContentHash.</summary>
        public string StorageHash;

        /// <summary>True if any resource is below the profile scarcity threshold.</summary>
        public bool AnyResourceCritical;

        /// <summary>Comma-separated list of critical resource IDs.</summary>
        public List<string> CriticalResourceIds;

        /// <summary>Currently active event family IDs (e.g., "SupplyCrisis").</summary>
        public List<string> ActiveEventFamilies;

        // ── determinism anchors (Phase 1) ────────────────────
        /// <summary>
        /// Canonical map uniqueID for deterministic seed computation.
        /// -1 means no player home map available (main menu / no map loaded).
        /// Stable across save/load because RimWorld persists map.uniqueID.
        /// </summary>
        public int MapID;

        /// <summary>
        /// Stable, deterministic colonist ThingID used to resolve the
        /// "{PawnId}" placeholder in event DeterminismKeyTemplates.
        /// Chosen by StoryDirector as colonists[dayIndex] with sorted
        /// ThingID ordering. Varies per in-game day, reproducible across
        /// save/load at the same tick. Null when no colonists exist.
        /// Phase 1: deterministic placeholder; Phase 2 may replace with
        /// "target pawn chosen by event-specified rule".
        /// </summary>
        public string DeterministicTargetPawnId;

        /// <summary>
        /// FNV-1a fingerprint of the colonist roster (sorted ThingIDs
        /// joined with "|" then hashed). Combined with
        /// DeterministicTargetPawnId inside DeterministicRng.BuildSeed
        /// so that pawn-anchored determinism keys remain stable across
        /// save→load even when the colony lost or gained a colonist
        /// between sessions.
        /// Empty string when no colonists exist.
        /// </summary>
        public string PawnRosterFingerprint;

        // ── progress ─────────────────────────────────────────
        /// <summary>Completed research capability IDs.</summary>
        public List<string> CompletedResearchIds;

        /// <summary>Days since game start.</summary>
        public float DaysSinceStart;

        /// <summary>Days since last turn point.</summary>
        public float DaysSinceLastTurnPoint;

        // ── active events ────────────────────────────────────
        /// <summary>Currently active event IDs (not yet resolved).</summary>
        public List<string> ActiveEventIds;

        /// <summary>Last execution tick per event ID (for cooldown).</summary>
        public Dictionary<string, long> LastEventTicks;

        // ── helpers ──────────────────────────────────────────

        /// <summary>Returns true if the given event family is currently active.</summary>
        public bool HasActiveEventOfFamily(string family)
        {
            if (ActiveEventFamilies == null) return false;
            return ActiveEventFamilies.Contains(family);
        }

        /// <summary>Returns the tick when the given event was last fired, or 0.</summary>
        public long GetLastEventTick(string eventId)
        {
            if (LastEventTicks != null && LastEventTicks.TryGetValue(eventId, out long tick))
                return tick;
            return 0;
        }

        /// <summary>Days since the given event was last fired.</summary>
        public float DaysSinceEvent(string eventId)
        {
            long lastTick = GetLastEventTick(eventId);
            if (lastTick == 0) return float.MaxValue;
            return (GameTick - lastTick) / Rimconemy.Foundation.TimeConstants.TicksPerDay;
        }
    }
}
