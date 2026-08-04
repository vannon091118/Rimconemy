using System;
using System.Collections.Generic;
using Verse;

namespace Rimconemy.SurvivalProgression.Scenarios
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02).
    /// Package-02 read/write boundary for Phase 1.1 / Phase 1.4 scenario-start events.
    /// Tracks per-map completion of:
    ///   - Phase 1.1: <see cref="ScenPart_RimconemyStart"/> (survivor single + weapon + scraps)
    ///   - Phase 1.4: <see cref="ScenPart_RimconemyStartEnemies"/> (one hostile pawn) — Package 05 reads
    ///
    /// Pattern: deterministic, idempotent, Save/Load-aware. The companion set is a HashSet
    /// (runtime) with parallel parallel-lists (save), matching the
    /// <see cref="Rimconemy.SurvivalProgression.Progression.BuildingProgressionLedger"/>
    /// contract. Deduplication is exact-match on `(mapId:key)` — reload-safe and replayproof.
    ///
    /// Phase 1.1 / Phase 1.4 call sites:
    ///   <c>if (RimconemyStartState.IsCompletedFor(map, "single-survivor")) return;</c>
    ///   <c>RimconemyStartState.MarkCompleted(map, "single-survivor");</c>
    ///
    /// Cross-package access (Phase 1.4): Package 05 reads via
    /// <see cref="Rimconemy.Foundation.CrossPackage.CrossPackageState"/>
    /// capability-gated late-bound reflection. Direct cross-reference into Package 02
    /// from Package 05 is forbidden by INTERFACE_CONTRACT §0 / §9.
    /// </summary>
    public sealed class RimconemyStartState : GameComponent
    {
        public const int CurrentSchemaVersion = 1;

        // Private constants for Save/Load tags: stable across renames.
        public const string Tag_MapsCompleted = "rimconemyStartState_mapsCompleted";
        public const string Tag_KeysForSave   = "rimconemyStartState_keysForSave";
        public const string Tag_SchemaVersion = "rimconemyStartState_schemaVersion";

        public int SchemaVersion = CurrentSchemaVersion;

        // Runtime dedup set. The strings correspond to (mapId:eventKey).
        private HashSet<string> _completed = new HashSet<string>(StringComparer.Ordinal);
        private List<string> _keysForSave;

        public RimconemyStartState(Game game) { }

        /// <summary>
        /// Stable composite key for one scenario-start event on one map.
        /// Deterministic across saves (map unique id) and across runs (no RNG).
        /// </summary>
        public static string KeyFor(Map map, string eventKey)
        {
            if (map == null || string.IsNullOrEmpty(eventKey))
                return null;
            return ComposeKey(map.uniqueID, eventKey);
        }

        /// <summary>
        /// Test-/serialisation-friendly overload: encodes a composite key from raw
        /// parts without a Map handle. Both call paths produce identical strings.
        /// </summary>
        public static string ComposeKey(int mapId, string eventKey)
        {
            return mapId + ":" + (eventKey ?? "");
        }

        public bool IsCompletedFor(Map map, string eventKey)
        {
            var key = KeyFor(map, eventKey);
            if (key == null) return false;
            return _completed.Contains(key);
        }

        /// <summary>
        /// Mark a scenario-start event as completed. Idempotent: returns false if the key
        /// was already present. The first writer wins — duplicate calls do not duplicate state.
        /// </summary>
        public bool MarkCompleted(Map map, string eventKey)
        {
            var key = KeyFor(map, eventKey);
            if (key == null) return false;
            if (!_completed.Add(key)) return false;
            // Defensive: keep in sync with save-side. Save forces refresh on next ExposeData.
            return true;
        }

        public int CompletedCount => _completed?.Count ?? 0;

        /// <summary>
        /// Returns the active component when a game exists; otherwise a standing-instance
        /// for tests / pre-load callers. Following the BuildingProgressionAdapter.StandaloneLedger
        /// pattern from <see cref="Rimconemy.SurvivalProgression.Progression.BuildingProgressionAdapter"/>.
        /// </summary>
        public static RimconemyStartState Resolve()
        {
            if (Current.Game != null)
            {
                var comp = Current.Game.GetComponent<RimconemyStartState>();
                if (comp != null) return comp;
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SchemaVersion, Tag_SchemaVersion, CurrentSchemaVersion);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _keysForSave = new List<string>(_completed ?? new HashSet<string>());
            }

            Scribe_Collections.Look(ref _keysForSave, Tag_KeysForSave, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _completed = _keysForSave != null
                    ? new HashSet<string>(_keysForSave, StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal);
                _keysForSave = null;
                SchemaVersion = CurrentSchemaVersion;
            }

            if (_completed == null)
                _completed = new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
