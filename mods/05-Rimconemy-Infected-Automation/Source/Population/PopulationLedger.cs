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

        // ── Lese-API ────────────────────────────────────────
        public int GetHumanoidLiveCount() => HumanoidLiveCount;
        public int GetAnimalLiveCount() => AnimalLiveCount;
        public int GetTotalLiveCount() => HumanoidLiveCount + AnimalLiveCount;
        public int GetCap() => Cap;
        public int GetCumulativeKills() => CumulativeKills;
        public int GetRecentKillsToday() => RecentKillsToday;
        public long GetLastInoculationTick() => LastInoculationTick;
        public int GetCumulativeInoculations() => CumulativeInoculations;

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
                Rimconemy.Foundation.Save.MigrationRegistry.Clear();
                if (SchemaVersion < CurrentSchemaVersion)
                    MigrateIfNeeded();
            }
        }
    }
}
