// Source/Population/PopulationLedger.cs
//
// Owner: Infected & Automation (Package 05).
// Phase A — P6-PROGRESS §12 Daten-SSOT.
//
// SSOT for infected population data. GameComponent because it spans
// all maps (player-home + temporary). ISchemaMigratable because the
// schema will evolve across phases B/C/D and we need a controlled
// migration path (MigrationRegistry.Step walkers).
//
// All persisted fields are public so RimWorld's Scribe can read them by
// ref. **The non-persisted dead-pawn HashSet (added in Task 3) clears
// its tracking set on every load to avoid kill-count inflation across
// the load boundary — documented in Phase A spec §3.**

using System.Collections.Generic;
using Rimconemy.Foundation.Save;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Population
{
    public sealed class PopulationLedger : GameComponent, ISchemaMigratable
    {
        // ── ISchemaMigratable contract ───────────────────────
        public const int CurrentSchemaVersion = 1;

        int ISchemaMigratable.CurrentSchemaVersion => CurrentSchemaVersion;

        public string ClassId => "rimconemy.infectedautomation.population";

        /// <summary>
        /// Public mutable view of the persisted schema version. Tests and
        /// loaders can read and write. Default-ctor seeds it to the current
        /// version; Scribe-load overwrites it from the save's stamp and
        /// MigrateIfNeeded bumps it forward.
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Lazily-built list of migration steps. Phase A has only one
        /// v0→v1 step (initial schema, all fields carry safe defaults).
        /// Future phases will append their steps here as
        /// <c>new SchemaStep(prev, current, description, apply)</c>.
        /// </summary>
        private List<SchemaStep> _cachedSteps;
        public IList<SchemaStep> Steps
        {
            get
            {
                if (_cachedSteps != null) return _cachedSteps;
                _cachedSteps = new List<SchemaStep>
                {
                    new SchemaStep(0, 1,
                        "Initial population-ledger schema applied (no data loss; all fields carry safe defaults).",
                        () => { /* no-op: every persisted field is already default-safe */ }),
                };
                return _cachedSteps;
            }
        }

        // ── Persisted state ──────────────────────────────────
        // Humanoid layer
        public int HumanoidLiveCount;
        public int CumulativeKills;
        public int RecentKillsToday;
        public int Cap;
        public int DayIndexSinceStart;
        public long LastDayTick;
        public string ProfileId;
        // Animal layer (Phase C service consumes LastInoculationTick;
        // Phase A only stores the data slots).
        public int AnimalLiveCount;
        public int CumulativeInoculations;
        public long LastInoculationTick;

        // ── Non-persisted state (Task 3+) ────────────────────
        /// <summary>
        /// Tracks recently-killed pawn IDs so RegisterKill is idempotent
        /// across multiple callers (CombatResolve, NightInfectedWorker,
        /// InfectedRaidWorker). Cleared when the GameComponent is loaded
        /// — see ExposeData() LoadingVars branch. This is intentional:
        /// killing a pawn that survives a save/load cycle would otherwise
        /// inflate the cumulative counter.
        /// </summary>
        private readonly HashSet<string> _killedIds = new HashSet<string>();

        // ── Constructors ────────────────────────────────────
        public PopulationLedger(Game game) : this()
        {
            // Production constructor — RimWorld will call this when
            // registering the GameComponent. ProfileId is set in the
            // default ctor; if a future ProfileOverride arrives via
            // GameComponent args, it can be set there.
        }

        public PopulationLedger()
        {
            ProfileId = PopulationProfileMultipliers.ProfileSurvival;
            SchemaVersion = CurrentSchemaVersion;
        }

        // ── Static accessor ─────────────────────────────────
        public static PopulationLedger Get()
        {
            if (Current.Game != null)
            {
                var existing = Current.Game.GetComponent<PopulationLedger>();
                if (existing != null) return existing;
            }
            return new PopulationLedger();
        }

        /// <summary>
        /// No persisted state to wipe; placeholder for future test hooks.
        /// </summary>
        public static void ResetForTests()
        {
            // Intentionally empty. The class holds no static mutable
            // state, but the method exists so future test hooks (e.g. an
            // internal dictionary that becomes static) have a single reset
            // point.
        }

        // ── Lese-API ──────────────────────────────────────
        public int GetHumanoidLiveCount() => HumanoidLiveCount;
        public int GetAnimalLiveCount() => AnimalLiveCount;
        public int GetTotalLiveCount() => HumanoidLiveCount + AnimalLiveCount;
        public int GetCap() => Cap;
        public int GetCumulativeKills() => CumulativeKills;
        public int GetRecentKillsToday() => RecentKillsToday;
        public long GetLastInoculationTick() => LastInoculationTick;
        public int GetCumulativeInoculations() => CumulativeInoculations;

        // ── Write-API: Kill-Tracking (Task 3) ────────────────
        /// <summary>
        /// Increment the cumulative kill counter and decrement the matching
        /// LiveCount (Humanoid or Animal). Idempotent per <c>pawn.ThingID</c>
        /// so multiple callers (CombatResolve, RaidWorker, NightWorker) can
        /// fire on the same death without double-counting.
        ///
        /// Rule of thumb for callers: pass the dying pawn once from the
        /// winning side; the ledger handles dedup. If you are unsure whether
        /// you already counted this pawn, just call <c>RegisterKill</c> — the
        /// idempotency set guarantees correctness.
        ///
        /// Edge cases:
        ///   pawn == null → no-op + Warning
        ///   pawn already counted → no-op
        ///   pawn.RaceProps null → no-op (defensive)
        /// </summary>
        public void RegisterKill(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] PopulationLedger.RegisterKill(null); ignored.");
                return;
            }

            string id = pawn.ThingID ?? "<no-id>";
            bool isHumanlike = pawn.RaceProps != null && pawn.RaceProps.Humanlike;
            ApplyKill(id, isHumanlike);
        }

        /// <summary>
        /// Internal Test-Hook: same kill-registration logic as the
        /// production <see cref="RegisterKill(Pawn)"/> but with the
        /// pawn-properties passed in directly. Enables unit tests without
        /// a real <c>Pawn</c> instance.
        /// </summary>
        internal void RegisterKillForTest(string thingId, bool isHumanlike)
        {
            if (string.IsNullOrEmpty(thingId))
            {
                Log.Warning("[Rimconemy.InfectedAutomation] PopulationLedger.RegisterKillForTest(<empty>); ignored.");
                return;
            }
            ApplyKill(thingId, isHumanlike);
        }

        /// <summary>
        /// Core kill-routing: idempotency + counter increment + LiveCount
        /// decrement. Centralised here so test- and production-paths share
        /// the same logic.
        /// </summary>
        private void ApplyKill(string thingId, bool isHumanlike)
        {
            if (!_killedIds.Add(thingId))
            {
                // re-entry; idempotent.
                return;
            }
            CumulativeKills += 1;
            RecentKillsToday += 1;
            if (isHumanlike)
            {
                HumanoidLiveCount = System.Math.Max(0, HumanoidLiveCount - 1);
            }
            else
            {
                AnimalLiveCount = System.Math.Max(0, AnimalLiveCount - 1);
            }
        }

        // ── Write-API: Daily-Growth + Revenge-Quote (Task 4) ──
        /// <summary>
        /// Apply the profile-driven daily growth multiplier to <c>Cap</c>,
        /// floored. Increments <c>DayIndexSinceStart</c>. Returns the new
        /// Cap. This is the StoryDirector's primary daily escalation hook.
        ///
        /// Overflow guard (spec §7): <c>Cap</c> is floored at
        /// <c>int.MaxValue / 1000</c> so a Collapse-profile, multi-year
        /// run cannot overflow into a Log.Error inside a GameComponent.
        /// Once sat, the multiplier becomes a no-op for the field.
        /// </summary>
        public int ApplyDailyGrowthTick()
        {
            float m = PopulationProfileMultipliers.GetDailyGrowth(ProfileId);
            int newCap = (int)System.Math.Floor((double)Cap * (double)m);
            const int CapCeiling = int.MaxValue / 1000;
            if (newCap > CapCeiling) newCap = CapCeiling;
            if (newCap < Cap) newCap = Cap;  // never shrink Cap (defensive)
            Cap = newCap;
            DayIndexSinceStart += 1;
            return Cap;
        }

        /// <summary>
        /// Resets <c>RecentKillsToday</c> to zero. Called at the start of
        /// each Day-Tick so the Revenge-Quote metric reflects the current
        /// day's kills only.
        /// </summary>
        public void ResetDailyCounters()
        {
            RecentKillsToday = 0;
        }

        /// <summary>
        /// Returns the night-spawn quota driven by today's kill count.
        /// Clipped by the free budget (Cap minus current total live).
        ///
        /// The original-spec "always &lt; kills" property falls out
        /// naturally because <c>maxCap</c> is the free budget — the caller
        /// can never receive a Revenge-Quote greater than what would fit in
        /// the cap. Combined with the profile multiplier (0.4 / 0.7 /
        /// 0.9) the result always stays &lt; RecentKillsToday.
        /// </summary>
        public int GetRevengeQuota(int maxCap)
        {
            if (maxCap <= 0) return 0;
            float ratio = PopulationProfileMultipliers.GetRevengeRatio(ProfileId);
            int raw = (int)System.Math.Floor((double)RecentKillsToday * (double)ratio);
            int freeBudget = System.Math.Max(0, maxCap - GetTotalLiveCount());
            return System.Math.Min(freeBudget, raw);
        }

        // ── Write-API: NoteInoculation (Task 6) ─────────────────
        /// <summary>
        /// Note a successful animal-inoculation event from Phase C
        /// <c>RandomInoculationService</c>. Stamps the supplied
        /// <paramref name="animalKindDefName"/> for diagnostics and
        /// records the current tick as the last-inoculation timestamp
        /// so the cooldown gate (driven by
        /// <c>PopulationProfileMultipliers.GetInoculationMinInterval</c>)
        /// stays deterministic.
        ///
        /// Per spec, this method does NOT spawn a pawn — Phase C's
        /// service owns the actual conversion. Phase A only persists
        /// the diagnostic slot.
        /// </summary>
        public void NoteInoculation(string animalKindDefName)
        {
            if (string.IsNullOrEmpty(animalKindDefName))
            {
                Log.Warning("[Rimconemy.InfectedAutomation] PopulationLedger.NoteInoculation(<empty>); ignored.");
                return;
            }
            CumulativeInoculations += 1;
            LastInoculationTick = Find.TickManager?.TicksGame ?? 0L;
        }

        /// <summary>
        /// Returns true when the cooldown gate has elapsed for the
        /// current Profile. Pure function over the last-stamp + the
        /// profile-driven interval. Phase C's RandomInoculationService
        /// uses this to decide whether to attempt a new inoculation.
        /// </summary>
        public bool IsInoculationCooldownElapsed()
        {
            long interval = PopulationProfileMultipliers.GetInoculationMinInterval(ProfileId);
            if (interval <= 0L) return true;  // safety, never on an unconfigured profile
            long now = Find.TickManager?.TicksGame ?? 0L;
            if (LastInoculationTick == 0L) return true;  // never inoculated yet
            return (now - LastInoculationTick) >= interval;
        }

        // ── Migration ───────────────────────────────────────
        public void MigrateIfNeeded()
        {
            int oldVersion = SchemaVersion;
            this.RunMigration();

            int newVersion = SchemaVersion;
            if (newVersion > oldVersion)
            {
                Log.Message("[Rimconemy.InfectedAutomation] PopulationLedger: schema "
                    + oldVersion + " → " + CurrentSchemaVersion
                    + " applied. Migration detail: initial schema, no data loss.");
            }
        }

        // ── Scribe ───────────────────────────────────────────
        public override void ExposeData()
        {
            base.ExposeData();

            // Schema-Version-Stamp
            int schemaVersion = SchemaVersion;
            Scribe_Values.Look(ref schemaVersion, "rimconemyILedgerSchemaVersion", CurrentSchemaVersion);
            SchemaVersion = schemaVersion;

            // Humanoid-Layer
            Scribe_Values.Look(ref HumanoidLiveCount, "rimconemyILedgerHumanoidLiveCount", 0);
            Scribe_Values.Look(ref CumulativeKills, "rimconemyILedgerKills", 0);
            Scribe_Values.Look(ref RecentKillsToday, "rimconemyILedgerKillsToday", 0);
            Scribe_Values.Look(ref Cap, "rimconemyILedgerCap", 5);
            Scribe_Values.Look(ref DayIndexSinceStart, "rimconemyILedgerDayIndex", 0);
            Scribe_Values.Look(ref LastDayTick, "rimconemyILedgerLastDayTick", 0L);
            Scribe_Values.Look(ref ProfileId, "rimconemyILedgerProfileId",
                PopulationProfileMultipliers.ProfileSurvival);
            // Animal-Layer
            Scribe_Values.Look(ref AnimalLiveCount, "rimconemyILedgerAnimalLiveCount", 0);
            Scribe_Values.Look(ref CumulativeInoculations, "rimconemyILedgerInocCount", 0);
            Scribe_Values.Look(ref LastInoculationTick, "rimconemyILedgerLastInocTick", 0L);

            // After Scribe finished loading, run migration.
            // Foundation reference pattern: clear the shared
            // MigrationRegistry so stale entries from a previous game do
            // not leak across the load boundary.
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Reset the non-persisted kill-idempotency set so a pawn
                // that survived a save can be counted again if it dies
                // after the load. CumulativeKills is persisted, so the
                // total remains stable across reload.
                _killedIds.Clear();
                Rimconemy.Foundation.Save.MigrationRegistry.Clear();
                if (SchemaVersion < CurrentSchemaVersion)
                    MigrateIfNeeded();
            }
        }
    }
}
