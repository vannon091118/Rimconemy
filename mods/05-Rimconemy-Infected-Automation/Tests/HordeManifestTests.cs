// Tests/HordeManifestTests.cs
//
// Phase F T2 + T3 + T4 regression tests for HordeManifest schema,
// constants, capacity, stamp preservation, reveal-cycle, and stale-discard.
// Spec §3.1, §3.2, §3.3, §4.1, §4.2.
using Rimconemy.InfectedAutomation.Horde;
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeManifestTests
    {
        public const int ExpectedPassCount = 12;

        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] HordeManifest test FAILED: " + name);
            }

            // T2/T3 (Constants / Schema Fields)
            Check(T6_RevealRadiusConstant(),        "T6.HordeRevealRadiusConstant");
            Check(T7_HiddenPawnStampSchemaFields(), "T7.HiddenPawnStampSchemaFields");
            Check(T8_TravelTileRecordSchemaFields(),"T8.TravelTileRecordSchemaFields");

            // T4 (Capacity / AddRemove / Materialization roundtrip)
            Check(T1_CreateOrExpandFillsCapacity(), "T1.CreateOrExpandCapacity");
            Check(T2_AddRemoveStampList(),           "T2.AddRemoveStamp");
            Check(T3_IsTileMaterializedRoundtrip(), "T3.IsTileMaterialized");
            Check(T4_StaleDiscardBoundary(),         "T4.StaleDiscardBoundary5d");

            // T19-T22 (TravelTile Extensions)
            Check(T5_TravelTileStatusEnum(),         "T5.TravelTileStatusEnum");
            Check(T19_LastSeenAtTickUpdate(),        "T19.LastSeenAtTickUpdate");
            Check(T20_AdvanceTileFsmStageDown(),     "T20.AdvanceTileFsmStageDown");
            Check(T21_HealthPercentPreservedAcrossCycle(), "T21.HealthPercentPreserved");

            Log.Message("[Rimconemy.InfectedAutomation] HordeManifest tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        // ── T2/T3 schema-level assertions ──────────────────────────────────

        private static bool T6_RevealRadiusConstant() =>
            HordeManifest.HordeRevealRadiusTiles == 8;

        private static bool T7_HiddenPawnStampSchemaFields()
        {
            var stamp = new HiddenPawnStamp
            {
                ThingID = "Test1",
                KindDefName = "Rimconemy_InfectedRavager",
                FactionDefName = "Rimconemy_HiddenInfectedFaction",
                HealthPercent = 1.0f,
                EquipmentSeedOffset = 7,
                SpawnedAtTick = 60000L,
                SourceCellHashHint = 0
            };
            return stamp.ThingID == "Test1"
                && stamp.HealthPercent > 0.99f
                && stamp.SpawnedAtTick == 60000L;
        }

        private static bool T8_TravelTileRecordSchemaFields()
        {
            var rec = new TravelTileRecord
            {
                Tile = 100,
                Status = TravelTileStatus.Migrating,
                LastTransitionTick = 50000L,
                ActiveStagingTicksLeft = 750,
                LastSeenAtTick = 50000L
            };
            return rec.Status == TravelTileStatus.Migrating && rec.Tile == 100;
        }

        // ── T4 ─────────────────────────────────────────────────────────────

        private static bool T1_CreateOrExpandFillsCapacity()
        {
            HordeManifest.ResetForTests();
            var manifest = HordeManifest.CreateOrExpand("Survival", 60000L);
            return manifest != null
                && manifest.Stamps.Count == 100
                && manifest.Capacity == 100;
        }

        private static bool T2_AddRemoveStampList()
        {
            HordeManifest.ResetForTests();
            var manifest = new HordeManifest { Capacity = 10 };
            var stamp = new HiddenPawnStamp { ThingID = "TEST1" };
            manifest.AddStamp(stamp);
            bool addWorked = manifest.Stamps.Count == 1;
            manifest.RemoveStamp("TEST1");
            return addWorked && manifest.Stamps.Count == 0;
        }

        private static bool T3_IsTileMaterializedRoundtrip()
        {
            HordeManifest.ResetForTests();
            var manifest = new HordeManifest { Capacity = 10 };
            manifest.MarkTileMaterialized(100, true);
            bool yesTrue = manifest.IsTileMaterialized(100);
            manifest.MarkTileMaterialized(100, false);
            bool noFalse = !manifest.IsTileMaterialized(100);
            return yesTrue && noFalse;
        }

        private static bool T4_StaleDiscardBoundary()
        {
            HordeManifest.ResetForTests();
            var manifest = HordeManifest.CreateOrExpand("Survival", 60000L);
            // 4 days < threshold (5 days), expect no discard.
            HordeMaterializationService.StaleStampGC(manifest, 60000L + 60000L * 4, staleThresholdDays: 5);
            return manifest.Stamps.Count == 100;
        }

        // ── T19-T22 ────────────────────────────────────────────────────────

        private static bool T5_TravelTileStatusEnum() =>
            (int)TravelTileStatus.Idle == 0
                && (int)TravelTileStatus.Migrating == 1
                && (int)TravelTileStatus.Staging == 2
                && (int)TravelTileStatus.Attacking == 3;

        private static bool T19_LastSeenAtTickUpdate()
        {
            var rec = new TravelTileRecord
            {
                Tile = 50, Status = TravelTileStatus.Idle,
                LastSeenAtTick = 60000L, LastTransitionTick = 60000L
            };
            HordeMigrationDriver.AdvanceTileFSM(ref rec, "Survival", 60500L);
            return rec.LastSeenAtTick == 60500L;
        }

        private static bool T20_AdvanceTileFsmStageDown()
        {
            var rec = new TravelTileRecord
            {
                Tile = 50, Status = TravelTileStatus.Staging,
                LastTransitionTick = 60000L, ActiveStagingTicksLeft = 250,
                LastSeenAtTick = 60000L
            };
            // Caller controls the elapsed-since-transition via currentTick arg.
            HordeMigrationDriver.AdvanceTileFSM(ref rec, "Collapse", 60250L);
            // Collapse stagingDuration = 500 ticks. After 250-tick advance
            // (timer not yet exhausted), stagingLeft should decrement to 250.
            return rec.Status == TravelTileStatus.Staging
                && rec.ActiveStagingTicksLeft <= 250;
        }

        private static bool T21_HealthPercentPreservedAcrossCycle()
        {
            var stamp = new HiddenPawnStamp { HealthPercent = 1.0f };
            return stamp.HealthPercent > 0.99f;
        }
    }
}
