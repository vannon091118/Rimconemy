using System;
using System.Collections.Generic;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// Persistent story state for save/load and determinism.
    /// Tracks the active profile, event history, cooldowns,
    /// selection seed and idempotency keys.
    ///
    /// Implements IExposable for RimWorld's Scribe save system.
    ///
    /// Specification: docs/H2-story-contract.md §5
    /// Gate G2: no duplicate event execution after save/load.
    /// </summary>
    public sealed class StoryState : IExposable
    {
        /// <summary>Current schema version for migration.</summary>
        public const int CurrentSchemaVersion = 1;

        // ── identity ──────────────────────────────────────────
        public int SchemaVersion = CurrentSchemaVersion;
        public string ProfileId;
        public int ProfileVersion;

        // ── event history ─────────────────────────────────────
        /// <summary>Last selected event ID, or null.</summary>
        public string LastEventId;

        /// <summary>Tick when the last event was selected.</summary>
        public long LastEventTick;

        /// <summary>Currently active (unresolved) event IDs.</summary>
        public List<string> ActiveEventIds;

        // ── cooldowns ─────────────────────────────────────────
        /// <summary>Tick when each event's cooldown expires.</summary>
        public Dictionary<string, long> EventCooldowns;

        // ── determinism ──────────────────────────────────────
        /// <summary>Selection seed, derived from MapID + GameTickDay.</summary>
        public int SelectionSeed;

        /// <summary>SHA-256 hash of the last SituationSnapshot used for selection.</summary>
        public string LastSnapshotHash;

        // ── idempotency ──────────────────────────────────────
        /// <summary>
        /// Set of idempotency keys that have already been executed.
        /// Format: "{EventId}:{DeterminismKey}"
        /// Prevents duplicate execution after save/load.
        /// Pruned periodically to prevent unbounded growth.
        /// </summary>
        public HashSet<string> IdempotencyKeys;

        /// <summary>Total number of events selected since game start.</summary>
        public int TotalEventsSelected;

        /// <summary>Tick when idempotency keys were last pruned.</summary>
        public long LastPruneTick;

        // ── game-over signaling (Phase B, F-V2) ──────────────
        /// <summary>
        /// True when this StoryState has flagged a game-over reason
        /// that the SOLE-OWNER (Mod 02 — ProgressionGameComponent) has
        /// not yet consumed. Mod 02 reads via a late-bound reflection
        /// bridge defined in Foundation to avoid a binary cycle.
        /// </summary>
        public bool GameOverPending;

        /// <summary>Reason string pending consumption.</summary>
        public string GameOverReasonPending;

        /// <summary>Maximum age of idempotency keys in ticks (30 days).</summary>
        private const long IdempotencyKeyMaxAgeTicks = 30 * 60000;

        // ── scribe helpers (transient, rebuilt after load) ───
        private List<string> _cooldownKeys;
        private List<long> _cooldownValues;
        private List<string> _idempotencyList;
        private List<long> _idempotencyTicks;

        // Audit-fix (Befund 3, 2026-08-04): insertion-order tracker for
        // idempotency keys. This is a FIFO list that records every key
        // in insertion order alongside the tick it was added, so
        // PruneOldKeys can remove the oldest entries regardless of
        // HashSet enumeration order.
        private List<IdempotencyEntry> _idempotencyInsertionOrder = new List<IdempotencyEntry>();

        private struct IdempotencyEntry
        {
            public string Key;
            public long Tick;
        }

        // ── helpers ──────────────────────────────────────────

        public StoryState()
        {
            ActiveEventIds = new List<string>();
            EventCooldowns = new Dictionary<string, long>();
            IdempotencyKeys = new HashSet<string>();
        }

        // ── save / load ─────────────────────────────────────

        /// <summary>
        /// Persists all story state via RimWorld's Scribe system.
        /// Schema migration: older saves are upgraded to the current
        /// schema version with safe defaults for missing fields.
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref SchemaVersion, "storyStateSchema", CurrentSchemaVersion);
            Scribe_Values.Look(ref ProfileId, "profileId", "");
            Scribe_Values.Look(ref ProfileVersion, "profileVersion", 1);
            Scribe_Values.Look(ref LastEventId, "lastEventId", (string)null);
            Scribe_Values.Look(ref LastEventTick, "lastEventTick", 0L);
            Scribe_Collections.Look(ref ActiveEventIds, "activeEventIds", LookMode.Value);
            Scribe_Values.Look(ref SelectionSeed, "selectionSeed", 0);
            Scribe_Values.Look(ref LastSnapshotHash, "lastSnapshotHash", "");
            Scribe_Values.Look(ref TotalEventsSelected, "totalEventsSelected", 0);
            Scribe_Values.Look(ref LastPruneTick, "lastPruneTick", 0L);
            // Phase B / F-V2: game-over signaling (default false / null on first save).
            Scribe_Values.Look(ref GameOverPending, "gameOverPending", false);
            Scribe_Values.Look(ref GameOverReasonPending, "gameOverReasonPending", (string)null);

            // Dictionary<string, long> → parallel lists for Scribe
            SerializeCooldownsForScribe();
            Scribe_Collections.Look(ref _cooldownKeys, "cooldownKeys", LookMode.Value);
            Scribe_Collections.Look(ref _cooldownValues, "cooldownValues", LookMode.Value);

            // HashSet<string> + insertion ticks → parallel lists for Scribe.
            // The tick list is required to preserve age-pruning semantics after load.
            SerializeIdempotencyForScribe();
            Scribe_Collections.Look(ref _idempotencyList, "idempotencyKeys", LookMode.Value);
            Scribe_Collections.Look(ref _idempotencyTicks, "idempotencyTicks", LookMode.Value);

            // ── post-load repair ─────────────────────────────
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RebuildAfterLoad();
            }
        }

        private void SerializeCooldownsForScribe()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _cooldownKeys = new List<string>(EventCooldowns?.Count ?? 0);
                _cooldownValues = new List<long>(EventCooldowns?.Count ?? 0);
                if (EventCooldowns != null)
                {
                    foreach (var kv in EventCooldowns)
                    {
                        _cooldownKeys.Add(kv.Key);
                        _cooldownValues.Add(kv.Value);
                    }
                }
            }
        }

        private void SerializeIdempotencyForScribe()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Audit-fix (Befund 3): iterate the insertion-order tracker
                // instead of the HashSet, so the serialized list preserves
                // FIFO order across save/load.
                if (_idempotencyInsertionOrder != null && _idempotencyInsertionOrder.Count > 0)
                {
                    _idempotencyList = new List<string>(_idempotencyInsertionOrder.Count);
                    _idempotencyTicks = new List<long>(_idempotencyInsertionOrder.Count);
                    foreach (var entry in _idempotencyInsertionOrder)
                    {
                        _idempotencyList.Add(entry.Key);
                        _idempotencyTicks.Add(entry.Tick);
                    }
                }
                else
                {
                    _idempotencyList = new List<string>(IdempotencyKeys?.Count ?? 0);
                    _idempotencyTicks = new List<long>(_idempotencyList.Count);
                    if (IdempotencyKeys != null)
                    {
                        foreach (var key in IdempotencyKeys)
                        {
                            _idempotencyList.Add(key);
                            _idempotencyTicks.Add(-1L); // legacy/unknown age
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Rebuilds dictionary/hashset from serialized lists and
        /// performs schema migration after loading.
        ///
        /// Audit-fix (Befund 3, 2026-08-04): also rebuilds the insertion-order
        /// tracker from the serialized idempotency list. Since the list was
        /// serialized in insertion order (see <see cref="SerializeIdempotencyForScribe"/>),
        /// we can reconstruct the FIFO order by iterating in list order.
        /// </summary>
        private void RebuildAfterLoad()
        {
            // Ensure non-null collections
            if (ActiveEventIds == null)
                ActiveEventIds = new List<string>();

            // Rebuild cooldown dictionary from parallel lists
            EventCooldowns = new Dictionary<string, long>();
            if (_cooldownKeys != null && _cooldownValues != null)
            {
                int count = Math.Min(_cooldownKeys.Count, _cooldownValues.Count);
                for (int i = 0; i < count; i++)
                {
                    if (!string.IsNullOrEmpty(_cooldownKeys[i]))
                        EventCooldowns[_cooldownKeys[i]] = _cooldownValues[i];
                }
            }

            // Rebuild idempotency set from list
            IdempotencyKeys = new HashSet<string>();
            // Audit-fix (Befund 3): rebuild insertion-order tracker from the
            // serialized list (which was saved in insertion order).
            _idempotencyInsertionOrder = new List<IdempotencyEntry>();
            if (_idempotencyList != null)
            {
                for (int i = 0; i < _idempotencyList.Count; i++)
                {
                    string key = _idempotencyList[i];
                    if (string.IsNullOrEmpty(key) || !IdempotencyKeys.Add(key))
                        continue;

                    // -1 means an old save did not persist ticks. Unknown-age
                    // keys are protected from age pruning and remain eligible
                    // for deterministic count-based pruning only.
                    long tick = (_idempotencyTicks != null && i < _idempotencyTicks.Count)
                        ? _idempotencyTicks[i]
                        : -1L;
                    _idempotencyInsertionOrder.Add(new IdempotencyEntry { Key = key, Tick = tick });
                }
            }

            // Schema migration
            if (SchemaVersion < CurrentSchemaVersion)
            {
                MigrateSchema(SchemaVersion);
            }

            // Clear transient scribe helpers
            _cooldownKeys = null;
            _cooldownValues = null;
            _idempotencyList = null;
            _idempotencyTicks = null;
        }

        private void MigrateSchema(int fromVersion)
        {
            if (fromVersion < 1)
            {
                // Version 0 → 1: initial schema, nothing to migrate
            }

            SchemaVersion = CurrentSchemaVersion;
            Log.Message($"[Rimconemy.InfectedAutomation] StoryState migrated from v{fromVersion} to v{CurrentSchemaVersion}.");
        }

        /// <summary>Returns true if the event is currently on cooldown.</summary>
        public bool IsOnCooldown(string eventId, long currentTick)
        {
            if (EventCooldowns != null && EventCooldowns.TryGetValue(eventId, out long cooldownUntil))
                return currentTick < cooldownUntil;
            return false;
        }

        /// <summary>Returns the tick when the cooldown expires, or 0 if not set.</summary>
        public long GetCooldownUntil(string eventId)
        {
            if (EventCooldowns != null && EventCooldowns.TryGetValue(eventId, out long cooldownUntil))
                return cooldownUntil;
            return 0;
        }

        /// <summary>Sets the cooldown for an event until the given tick.</summary>
        public void SetCooldown(string eventId, long cooldownUntilTick)
        {
            if (EventCooldowns == null)
                EventCooldowns = new Dictionary<string, long>();
            EventCooldowns[eventId] = cooldownUntilTick;
        }

        /// <summary>Returns true if the given idempotency key has already been executed.</summary>
        public bool HasExecuted(string idempotencyKey)
        {
            return IdempotencyKeys != null && IdempotencyKeys.Contains(idempotencyKey);
        }

        /// <summary>Marks an idempotency key as executed.</summary>
        public void MarkExecuted(string idempotencyKey, long currentTick = 0)
        {
            if (string.IsNullOrEmpty(idempotencyKey)) return;

            if (IdempotencyKeys == null)
                IdempotencyKeys = new HashSet<string>();

            if (IdempotencyKeys.Add(idempotencyKey))
            {
                // Audit-fix (Befund 3): record insertion order for reliable
                // age-based pruning.
                if (_idempotencyInsertionOrder == null)
                    _idempotencyInsertionOrder = new List<IdempotencyEntry>();
                // Omitted ticks represent legacy/save-restored state, not
                // "the current tick". Callers with live age information must
                // pass it explicitly so unknown-age keys remain safe.
                _idempotencyInsertionOrder.Add(new IdempotencyEntry
                {
                    Key = idempotencyKey,
                    Tick = currentTick > 0 ? currentTick : -1L
                });
            }
        }

        // ── commit-after-fire semantics (audit-round-3 §3) ──

        /// <summary>
        /// Commits a StorySelector result to persistent state. Called by
        /// StoryDirector AFTER <c>Find.Storyteller.TryFire</c> returned
        /// successfully (or by any caller that has otherwise guaranteed
        /// the incident will execute on the next storyteller cycle).
        ///
        /// Writes the idempotency key, LastEventId, LastEventTick,
        /// SelectionSeed, increments TotalEventsSelected, sets the cooldown,
        /// and registers the event as active. Audit-round-3 §3 (2026-08-04):
        /// previously the StorySelector wrote all of this directly inside
        /// its Step 9, which burned the idempotency key even when the
        /// subsequent incident fire failed. The result was a Letter that
        /// never appeared and an event that never re-fired. This split
        /// allows fire-or-retry semantics.
        ///
        /// Thread-safety contract: <see cref="GameComponent.GameComponentTick"/>
        /// and <see cref="GameComponent.ExposeData"/> are both invoked on
        /// RimWorld's main thread and are mutually exclusive (a save cycle
        /// pauses tick dispatch). Concurrent calls are not expected and not
        /// guarded — a partial CommitSelection interleaved with ExposeData
        /// could leave the save in a half-baked state. If that ever becomes
        /// possible (e.g. async save), wrap callers in a lock or move the
        /// commit to a Scribe-aware queue.
        /// </summary>
        /// <param name="eventId">Selected event's stable identifier.</param>
        /// <param name="idempotencyKey">Compound key (EventId:DeterminismKey).</param>
        /// <param name="currentTick">Tick at which the fire succeeded.</param>
        /// <param name="seed">Selection seed from the deterministic RNG.</param>
        /// <param name="cooldownTicks">Cooldown length in ticks.</param>
        public void CommitSelection(
            string eventId,
            string idempotencyKey,
            long currentTick,
            int seed,
            long cooldownTicks)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                // Defensive: never throw, but log so a misuse is visible.
                Log.Warning("[Rimconemy.InfectedAutomation] StoryState.CommitSelection called with empty eventId; ignoring.");
                return;
            }

            MarkExecuted(idempotencyKey, currentTick);
            LastEventId = eventId;
            LastEventTick = currentTick;
            SelectionSeed = seed;
            TotalEventsSelected++;
            SetCooldown(eventId, currentTick + cooldownTicks);

            if (ActiveEventIds == null)
                ActiveEventIds = new List<string>();
            ActiveEventIds.Add(eventId);
        }

        // ── game-over signaling (Phase B, F-V2) ──────────────

        /// <summary>
        /// Flag a game-over condition with a reason. Mod 02 (Sole-Owner)
        /// consumes it via ConsumeGameOverPending. Repeated calls overwrite
        /// the reason; the most-recent write wins. Safe to call from any
        /// background context (writes are not concurrent: GameComponents
        /// run on the main thread).
        /// </summary>
        public void MarkGameOverPending(string reason)
        {
            GameOverPending = true;
            GameOverReasonPending = reason ?? "Mod 05 signalled game over.";
        }

        /// <summary>
        /// Reader-side pull: returns whether a game-over is pending and the
        /// reason. Clears the pending flag before returning so each pending
        /// is consumed exactly once.
        /// </summary>
        public bool ConsumeGameOverPending(out string reason)
        {
            if (!GameOverPending)
            {
                reason = null;
                return false;
            }
            reason = GameOverReasonPending ?? "Mod 05 signalled game over.";
            GameOverPending = false;
            GameOverReasonPending = null;
            return true;
        }

        /// <summary>
        /// Prunes idempotency keys older than IdempotencyKeyMaxAgeTicks.
        /// Also prunes expired cooldowns. Safe to call every selection cycle.
        ///
        /// Audit-fix (Befund 3, 2026-08-04): uses an explicit insertion-order
        /// tracker (<see cref="_idempotencyInsertionOrder"/>) alongside the
        /// HashSet, so pruning by age is reliable regardless of HashSet
        /// enumeration order (.NET does not guarantee insertion order).
        /// </summary>
        public void PruneOldKeys(long currentTick)
        {
            PruneExpiredCooldowns(currentTick);

            // Only prune idempotency keys periodically (once per day)
            if (currentTick - LastPruneTick < 60000)
                return;

            LastPruneTick = currentTick;

            if (IdempotencyKeys == null || IdempotencyKeys.Count == 0)
                return;

            // Audit-fix (Befund 3): prune by age using the insertion-order
            // tracker. Keys older than 30 days are removed; anything that
            // survives the age gate also gets a count cap at 500.
            // Repair a partially missing tracker before pruning so the count
            // cap cannot silently leave untracked HashSet entries behind.
            if (_idempotencyInsertionOrder == null)
                _idempotencyInsertionOrder = new List<IdempotencyEntry>();
            if (_idempotencyInsertionOrder.Count != IdempotencyKeys.Count)
            {
                var normalized = new List<IdempotencyEntry>();
                var tracked = new HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in _idempotencyInsertionOrder)
                {
                    if (!string.IsNullOrEmpty(entry.Key)
                        && IdempotencyKeys.Contains(entry.Key)
                        && tracked.Add(entry.Key))
                        normalized.Add(entry);
                }

                var missingKeys = new List<string>();
                foreach (var key in IdempotencyKeys)
                    if (!tracked.Contains(key))
                        missingKeys.Add(key);
                missingKeys.Sort(StringComparer.Ordinal);
                foreach (var key in missingKeys)
                    normalized.Add(new IdempotencyEntry { Key = key, Tick = -1L });
                _idempotencyInsertionOrder = normalized;
            }

            long maxAge = currentTick - IdempotencyKeyMaxAgeTicks;
            if (_idempotencyInsertionOrder != null && _idempotencyInsertionOrder.Count > 0)
            {
                // Walk from oldest (front) to newest, removing expired entries.
                while (_idempotencyInsertionOrder.Count > 0)
                {
                    var oldest = _idempotencyInsertionOrder[0];
                    if (oldest.Tick < 0 || oldest.Tick > maxAge)
                    {
                        // Legacy saves do not carry insertion ticks. Do not
                        // reinterpret unknown age as tick 0, and do not let
                        // the count cap turn this age pass into blind deletion.
                        break;
                    }

                    IdempotencyKeys.Remove(oldest.Key);
                    _idempotencyInsertionOrder.RemoveAt(0);
                }
            }

            // Fallback for legacy saves whose entries have unknown age, or
            // for any state without a tracker: establish a deterministic
            // ordinal order before applying the count cap. This is not an
            // assertion of historical insertion order; it is a safe,
            // repeatable migration order for incomplete legacy state.
            if ((_idempotencyInsertionOrder == null || _idempotencyInsertionOrder.Count == 0)
                && IdempotencyKeys.Count > 0)
            {
                _idempotencyInsertionOrder = new List<IdempotencyEntry>();
                var legacyKeys = new List<string>(IdempotencyKeys);
                legacyKeys.Sort(StringComparer.Ordinal);
                foreach (var key in legacyKeys)
                    _idempotencyInsertionOrder.Add(new IdempotencyEntry { Key = key, Tick = -1L });
            }

            if (IdempotencyKeys.Count > 1000)
            {
                int toKeep = 500;
                int removeCount = IdempotencyKeys.Count - toKeep;
                for (int i = 0; i < removeCount && _idempotencyInsertionOrder.Count > 0; i++)
                {
                    var oldest = _idempotencyInsertionOrder[0];
                    _idempotencyInsertionOrder.RemoveAt(0);
                    IdempotencyKeys.Remove(oldest.Key);
                }
            }
        }

        /// <summary>Clears expired cooldowns (cooldown < currentTick).</summary>
        public void PruneExpiredCooldowns(long currentTick)
        {
            if (EventCooldowns == null) return;
            var expired = new List<string>();
            foreach (var kv in EventCooldowns)
                if (kv.Value <= currentTick)
                    expired.Add(kv.Key);
            foreach (var key in expired)
                EventCooldowns.Remove(key);
        }

        /// <summary>Builds an idempotency key from event and determinism key.</summary>
        public static string BuildIdempotencyKey(string eventId, string determinismKey)
        {
            return $"{eventId}:{determinismKey}";
        }
    }
}
