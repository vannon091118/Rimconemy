using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Phase 1.4 — Save-aware idempotency for the single starter-infected spawn.
    ///
    /// INTERFACE_CONTRACT §0 / §9 forbids direct Package 02 references from
    /// Package 05. ScenPart_RimconemyStartEnemies therefore owns its own
    /// deterministic dedup state instead of reading
    /// <c>RimconemyStartState</c> directly. Keys are synchronised at the
    /// conceptual level via the documented event-name constants so a future
    /// cross-package coordination layer can reconcile both maps without
    /// touching the per-package Save-data.
    /// </summary>
    public sealed class RimconemyStartEnemiesLedger : GameComponent
    {
        public const int CurrentSchemaVersion = 1;

        public const string Tag_SchemaVersion = "rimconemyStartEnemiesLedger_schema";
        public const string Tag_KeysForSave   = "rimconemyStartEnemiesLedger_keys";

        // Stable event-key shared conceptually with Package-02's
        // ScenPart_RimconemyStart.EventKey_* constants. We do NOT
        // share those constants across packages; instead we mirror
        // them at the defensive boundary so renaming stays reversible.
        public const string EventKey_OneInfectedSpawn = "starter-infected-spawn";

        public int SchemaVersion = CurrentSchemaVersion;
        public int SpawnedCount => _completed?.Count ?? 0;

        private HashSet<string> _completed = new HashSet<string>(StringComparer.Ordinal);
        private List<string> _keysForSave;

        public RimconemyStartEnemiesLedger(Game game) { }

        public static string KeyFor(Map map, string eventKey)
            => map == null || string.IsNullOrEmpty(eventKey)
                ? null
                : map.uniqueID + ":" + eventKey;

        public bool IsSpawnCompletedFor(Map map)
            => _completed.Contains(KeyFor(map, EventKey_OneInfectedSpawn));

        /// <summary>
        /// One-shot commit. Returns true on first write; false on duplicate.
        /// </summary>
        public bool MarkSpawnCompleted(Map map)
        {
            var key = KeyFor(map, EventKey_OneInfectedSpawn);
            if (key == null) return false;
            return _completed.Add(key);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SchemaVersion, Tag_SchemaVersion, CurrentSchemaVersion);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _keysForSave = _completed != null
                    ? new List<string>(_completed)
                    : new List<string>();
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

            if (_completed == null) _completed = new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
