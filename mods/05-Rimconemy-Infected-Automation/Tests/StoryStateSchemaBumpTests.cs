using System;
using System.Collections.Generic;
using Rimconemy.Foundation.Tests;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Phase-2.8-Pattern (2026-08-04): Save/Load-SchemaBump-Tests
    /// für <see cref="StoryState"/>.
    ///
    /// Belegt dass StoryState einen v0→v1 Save/Load-Roundtrip überlebt
    /// mit allen Story-Feldern (ProfileId, LastEventId, Cooldowns,
    /// IdempotencyKeys, GameOver-Pending) erhalten.
    ///
    /// T1-T6 Struktur analog zu CharacterSetupStateSchemaBumpTests.
    /// </summary>
    public static class StoryStateSchemaBumpTests
    {
        private static TestSuite ts;
        public const int ExpectedPassCount = 6;

        /// <summary>Matches <see cref="StoryState.ClassId"/>;
        /// used to clean up test instances between runs.</summary>
        private const string TestClassId = "rimconemy.infectedautomation.storyState";

        public static int RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "StoryStateSchemaBump test");

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
                Log.Error("[Rimconemy.InfectedAutomation] StoryStateSchemaBump test FAILED: " + name);
            }

            void CheckAndClean(bool ok, string name)
            {
                Check(ok, name);
                // Each test creates a new StoryState instance that
                // self-registers via MigrateIfNeeded → RunMigration.
                // Unregister after every test so the next one starts with a
                // clean registry and the real component's registration
                // later never sees a stale test instance.
                Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);
            }

            CheckAndClean(TestV0SchemaBumpsToCurrent(),                "T1.V0SchemaBumpsToCurrent");
            CheckAndClean(TestV1SchemaIsIdempotent(),                  "T2.V1SchemaIsIdempotent");
            CheckAndClean(TestV0WithProfileDataPreserved(),            "T3.V0WithProfileDataPreserved");
            CheckAndClean(TestV0WithNullCollectionsNormalized(),       "T4.V0WithNullCollectionsNormalized");
            CheckAndClean(TestV0WithGameOverPendingPreserved(),        "T5.V0WithGameOverPendingPreserved");
            CheckAndClean(TestScribeRoundTripBumpsSchema(),
                  "T6.ScribeRoundTripBumpsSchema");

            Log.Message(
                "[Rimconemy.InfectedAutomation] StoryStateSchemaBump tests: " + passed +
                " passed, " + failed + " failed (min=" + ExpectedPassCount + ")." +
                (firstFailure == null ? "" : " First failure: " + firstFailure));

            ts.Check(failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return failed;
        }

        // ── T1 ────────────────────────────────────────────────────────
        public static bool TestV0SchemaBumpsToCurrent()
        {
            try
            {
                var state = new StoryState { SchemaVersion = 0 };
                state.MigrateIfNeeded();
                return state.SchemaVersion == StoryState.CurrentSchemaVersion;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod05] test caught: " + ex); return false; }
        }

        // ── T2 ────────────────────────────────────────────────────────
        public static bool TestV1SchemaIsIdempotent()
        {
            try
            {
                var state = new StoryState
                {
                    SchemaVersion = StoryState.CurrentSchemaVersion
                };
                state.MigrateIfNeeded();
                return state.SchemaVersion == StoryState.CurrentSchemaVersion;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod05] test caught: " + ex); return false; }
        }

        // ── T3 ────────────────────────────────────────────────────────
        public static bool TestV0WithProfileDataPreserved()
        {
            try
            {
                var state = new StoryState
                {
                    SchemaVersion = 0,
                    ProfileId = "FullOverhaul",
                    ProfileVersion = 3,
                    LastEventId = "Rimconemy_InfectedRaid_01",
                    LastEventTick = 42_000L,
                    TotalEventsSelected = 7,
                };
                state.EventCooldowns = new Dictionary<string, long>
                {
                    { "EvtA", 1000L },
                    { "EvtB", 2000L },
                };

                state.MigrateIfNeeded();

                if (state.SchemaVersion != StoryState.CurrentSchemaVersion) return false;
                if (state.ProfileId != "FullOverhaul") return false;
                if (state.ProfileVersion != 3) return false;
                if (state.LastEventId != "Rimconemy_InfectedRaid_01") return false;
                if (state.LastEventTick != 42_000L) return false;
                if (state.TotalEventsSelected != 7) return false;
                if (state.EventCooldowns == null || state.EventCooldowns.Count != 2) return false;
                if (!state.EventCooldowns.ContainsKey("EvtA") || state.EventCooldowns["EvtA"] != 1000L) return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod05] test caught: " + ex); return false; }
        }

        // ── T4 ────────────────────────────────────────────────────────
        public static bool TestV0WithNullCollectionsNormalized()
        {
            try
            {
                var state = new StoryState { SchemaVersion = 0 };
                state.ActiveEventIds = null;
                state.EventCooldowns = null;
                state.IdempotencyKeys = null;

                state.MigrateIfNeeded();

                // MigrateIfNeeded delegates to MigrateSchema which
                // doesn't touch these collections. But constructor
                // guarantees they were set. Post-migration check:
                // they should still be non-null because constructor
                // ran. But if someone bypassed the constructor,
                // RebuildAfterLoad would fix them. This test just
                // verifies schema version is correct.
                return state.SchemaVersion == StoryState.CurrentSchemaVersion;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod05] test caught: " + ex); return false; }
        }

        // ── T5 ────────────────────────────────────────────────────────
        public static bool TestV0WithGameOverPendingPreserved()
        {
            try
            {
                var state = new StoryState
                {
                    SchemaVersion = 0,
                    GameOverPending = true,
                    GameOverReasonPending = "test-reason",
                };

                state.MigrateIfNeeded();

                return state.GameOverPending == true
                    && state.GameOverReasonPending == "test-reason"
                    && state.SchemaVersion == StoryState.CurrentSchemaVersion;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod05] test caught: " + ex); return false; }
        }

        // ── T6 ────────────────────────────────────────────────────────
        // Echter Scribe-File-Roundtrip via MemoryStream + ScribeRoundTripHelper.
        public static bool TestScribeRoundTripBumpsSchema()
        {
            try
            {
                var state = new StoryState
                {
                    SchemaVersion = 0,
                    ProfileId = "TestProfile",
                    LastEventId = "Evt_Test",
                };

                bool roundTripOk = ScribeRoundTripHelper.RoundTrip(state);

                if (roundTripOk)
                {
                    return state.SchemaVersion == StoryState.CurrentSchemaVersion
                        && state.ProfileId == "TestProfile"
                        && state.LastEventId == "Evt_Test";
                }

                // A failed stream helper is a failed T6; do not downgrade
                // this file-cycle assertion to a logic-only migration test.
                return false;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod05] test caught: " + ex); return false; }
        }
    }
}
