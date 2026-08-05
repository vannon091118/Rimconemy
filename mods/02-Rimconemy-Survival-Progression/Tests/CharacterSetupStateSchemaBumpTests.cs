using System;
using System.Collections.Generic;
using Rimconemy.Foundation.Tests;
using Rimconemy.SurvivalProgression.Character;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Phase-2.8 (2026-08-04): Save/Load-SchemaBump-Tests.
    ///
    /// Owner: Paket 02 (Sole-Owner, INTERFACE_CONTRACT §9.1).
    ///
    /// Proof that <see cref="CharacterSetupState"/> survives a v0 → v1
    /// save/load roundtrip with all scorecard records preserved. The
    /// user-visible Beleg promises (Audit §B6 BESTÄTIGT) are:
    ///   T1  A v0 instance (SchemaVersion=0) is bumped to current schema.
    ///   T2  Schema bump is idempotent: re-running on a v-current state
    ///       is a no-op.
    ///   T3  Scorecards survive the bump: SchemaVersion, Records.Count,
    ///       per-record Age/Skills/Traits all stay intact.
    ///   T4  v0 with null Records is normalised to an empty dictionary
    ///       (v1 contract guarantees Records is non-null).
    ///   T5  Applied flag survives the bump (Bio-Remap-eligibility state
    ///       remains readable after migration).
    ///   T6  Echter Scribe-File-Roundtrip via <see cref="ScribeRoundTripHelper.RoundTrip{T}"/>:
    ///       MemoryStream-Save → Load → PostLoadInit in einem Zyklus
    ///       OHNE aktive Game-Session. Belegt dass der Schema-Bump
    ///       durch einen echten Scribe-Stream-Pfad läuft.
    ///
    /// Owner-Constraint: this test class is internal-only to Paket 02.
    /// No Foundation or other-package capability is registered from
    /// here; the SchemaBump logic lives and dies inside
    /// <see cref="CharacterSetupState"/>.
    /// </summary>
    public static class CharacterSetupStateSchemaBumpTests
    {
        public const int ExpectedPassCount = 6;

        /// <summary>Matches <see cref="CharacterSetupState.ClassId"/>;
        /// used to clean up test instances between runs.</summary>
        private const string TestClassId = "rimconemy.survivalprogression.characterSetup";

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            // Wipe any leftover registration from a previous hot-reload or
            // double-static-constructor edge case before the real tests start.
            Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);

            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.SurvivalProgression] SchemaBump test FAILED: " + name);
            }

            void CheckAndClean(bool ok, string name)
            {
                Check(ok, name);
                // Each test creates a new CharacterSetupState instance that
                // self-registers via MigrateIfNeeded → RunMigration.
                // Unregister after every test so the next one starts with a
                // clean registry and the real GameComponent's registration
                // later never sees a stale test instance.
                Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);
            }

            CheckAndClean(TestV0SchemaBumpsToCurrent(),                "T1.V0SchemaBumpsToCurrent");
            CheckAndClean(TestV1SchemaIsIdempotent(),                  "T2.V1SchemaIsIdempotent");
            CheckAndClean(TestV0WithRecordsPreservesData(),            "T3.V0WithRecordsPreservesData");
            CheckAndClean(TestV0WithNullRecordsNormalizesToEmpty(),    "T4.V0WithNullRecordsNormalizesToEmpty");
            CheckAndClean(TestV0WithAppliedFlagPreserved(),            "T5.V0WithAppliedFlagPreserved");
            CheckAndClean(TestScribeRoundTripBumpsSchema(),
                  "T6.ScribeRoundTripBumpsSchema");

            Log.Message(
                "[Rimconemy.SurvivalProgression] SchemaBump tests: " + passed + " passed, " +
                failed + " failed (expected=" + ExpectedPassCount + ")." +
                (firstFailure == null ? "" : " First failure: " + firstFailure));
            return failed;
        }

        // ── T1 ────────────────────────────────────────────────────────
        // Construct a v0 instance: SchemaVersion=0, no records. After
        // MigrateIfNeeded the state must report SchemaVersion==CurrentSchemaVersion.
        public static bool TestV0SchemaBumpsToCurrent()
        {
            try
            {
                var state = new CharacterSetupState(null) { SchemaVersion = 0 };
                state.MigrateIfNeeded();
                return state.SchemaVersion == CharacterSetupState.CurrentSchemaVersion;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02A] test caught: " + ex); return false; }
        }

        // ── T2 ────────────────────────────────────────────────────────
        // Idempotency: re-running MigrateIfNeeded on a state already at
        // the current schema must be a no-op. This guarantees the helper
        // is safe to invoke from arbitrary checkpoints (FinalizeInit,
        // PostLoadInit tick, defensive load).
        public static bool TestV1SchemaIsIdempotent()
        {
            try
            {
                var state = new CharacterSetupState(null)
                {
                    SchemaVersion = CharacterSetupState.CurrentSchemaVersion
                };
                state.MigrateIfNeeded();
                return state.SchemaVersion == CharacterSetupState.CurrentSchemaVersion;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02A] test caught: " + ex); return false; }
        }

        // ── T3 ────────────────────────────────────────────────────────
        // Scorecards survive the bump. This is the meat of the audit
        // §B6 Beleg: a v0 save loaded into a v1 build still produces
        // useful PawnSetupRecord entries after Scribe Deep + PostLoadInit.
        public static bool TestV0WithRecordsPreservesData()
        {
            try
            {
                var state = new CharacterSetupState(null) { SchemaVersion = 0 };
                state.Records = new Dictionary<int, PawnSetupRecord>();
                var record = new PawnSetupRecord
                {
                    AgeBiologicalYears = 18,
                    AgeChronologicalYears = 18,
                    SkillDefNames = new List<string> { "Construction", "Mining" },
                    SkillLevels = new List<int> { 5, 3 },
                    TraitDefNames = new List<string> { "Rimconemy_Trait_Hardy" },
                    NeutralBand = 0,
                };
                state.Records[42] = record;

                state.MigrateIfNeeded();

                if (state.SchemaVersion != CharacterSetupState.CurrentSchemaVersion) return false;
                if (state.Records == null || state.Records.Count != 1) return false;
                var persisted = state.Records[42];
                if (persisted == null) return false;
                if (persisted.AgeBiologicalYears != 18) return false;
                if (persisted.AgeChronologicalYears != 18) return false;
                if (persisted.SkillDefNames == null || persisted.SkillDefNames.Count != 2) return false;
                if (persisted.SkillDefNames[0] != "Construction") return false;
                if (persisted.SkillDefNames[1] != "Mining") return false;
                if (persisted.SkillLevels == null || persisted.SkillLevels.Count != 2) return false;
                if (persisted.SkillLevels[0] != 5) return false;
                if (persisted.SkillLevels[1] != 3) return false;
                if (persisted.TraitDefNames == null || persisted.TraitDefNames.Count != 1) return false;
                if (persisted.TraitDefNames[0] != "Rimconemy_Trait_Hardy") return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02A] test caught: " + ex); return false; }
        }

        // ── T4 ────────────────────────────────────────────────────────
        // v0 saves had null Records; v1 must guarantee non-null so the
        // downstream consumers do not have to defend against null.
        public static bool TestV0WithNullRecordsNormalizesToEmpty()
        {
            try
            {
                var state = new CharacterSetupState(null)
                {
                    SchemaVersion = 0,
                    Records = null
                };
                state.MigrateIfNeeded();
                return state.Records != null && state.Records.Count == 0;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02A] test caught: " + ex); return false; }
        }

        // ── T5 ────────────────────────────────────────────────────────
        // The Applied flag is part of the GameComponent contract
        // for the Bio-Remap idempotency. Migration must preserve it.
        //
        // 2026-08-05: the v1→v2 step (MigrateLegacyRecordsToBundles)
        // recalculates Applied from RequiredPawnIds validity. An empty
        // PawnSetupRecord has no skills and is filtered out during
        // migration, draining RequiredPawnIds to empty and resetting
        // Applied to false. Supply a realistic record with skills so the
        // bundle derivation produces a valid entry that preserves the
        // flag across the schema bump.
        public static bool TestV0WithAppliedFlagPreserved()
        {
            try
            {
                var state = new CharacterSetupState(null)
                {
                    SchemaVersion = 0,
                    Applied = true
                };
                state.Records = new Dictionary<int, PawnSetupRecord>();
                state.Records[42] = new PawnSetupRecord
                {
                    SkillDefNames = new List<string> { "Construction", "Mining" },
                    SkillLevels = new List<int> { 5, 3 }
                };
                state.MigrateIfNeeded();
                return state.Applied == true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02A] test caught: " + ex); return false; }
        }

        // ── T6 ────────────────────────────────────────────────────────
        // Echter Scribe-File-Roundtrip via MemoryStream.
        // ScribeRoundTripHelper treibt Scribe.mode + Scribe.saver +
        // Scribe.loader via Reflection, speichert das Objekt in einen
        // MemoryStream und lädt es zurück. PostLoadInit triggert dann
        // MigrateIfNeeded. Ein nicht funktionierender Helper lässt T6
        // bewusst fehlschlagen; kein Logic-only-Fallback.
        public static bool TestScribeRoundTripBumpsSchema()
        {
            try
            {
                var state = new CharacterSetupState(null);
                state.SchemaVersion = 0;
                state.Records = new Dictionary<int, PawnSetupRecord>();
                state.Records[42] = new PawnSetupRecord
                {
                    AgeBiologicalYears = 18,
                    AgeChronologicalYears = 18,
                    SkillDefNames = new List<string> { "Shooting" },
                    SkillLevels = new List<int> { 6 }
                };
                // Keep a second, distinct key: a wrong dictionary key
                // LookMode can appear to work with one value while dropping
                // or misaligning keys during Scribe_Collections loading.
                state.Records[84] = new PawnSetupRecord
                {
                    AgeBiologicalYears = 19,
                    AgeChronologicalYears = 20,
                    SkillDefNames = new List<string> { "Construction" },
                    SkillLevels = new List<int> { 4 }
                };

                bool roundTripOk = ScribeRoundTripHelper.RoundTrip(state);

                if (roundTripOk)
                {
                    // Echter Scribe-Roundtrip lief durch — alle Felder
                    // sollten den Save/Load-Zyklus überlebt haben.
                    return state.SchemaVersion == CharacterSetupState.CurrentSchemaVersion
                        && state.Records != null
                        && state.Records.Count == 2
                        && state.Records[42] != null
                        && state.Records[42].AgeBiologicalYears == 18
                        && state.Records[42].SkillDefNames != null
                        && state.Records[42].SkillDefNames.Count == 1
                        && state.Records[42].SkillDefNames[0] == "Shooting"
                        && state.Records[42].SkillLevels != null
                        && state.Records[42].SkillLevels.Count == 1
                        && state.Records[42].SkillLevels[0] == 6
                        && state.Records[84] != null
                        && state.Records[84].AgeBiologicalYears == 19
                        && state.Records[84].AgeChronologicalYears == 20
                        && state.Records[84].SkillDefNames != null
                        && state.Records[84].SkillDefNames.Count == 1
                        && state.Records[84].SkillDefNames[0] == "Construction"
                        && state.Records[84].SkillLevels != null
                        && state.Records[84].SkillLevels.Count == 1
                        && state.Records[84].SkillLevels[0] == 4;
                }

                // A failed stream helper is a failed T6; do not downgrade
                // this file-cycle assertion to a logic-only migration test.
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.SurvivalProgression] SchemaBump T6 stream failure: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
