// Tests/HordeMigrationDriverTests.cs
//
// Phase F T5 — HordeMigrationDriver Tick-Loop + FSM regression tests.
// Spec §5 Tick-Loop, §7.2.
using Rimconemy.InfectedAutomation.Horde;
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeMigrationDriverTests
    {
        public const int ExpectedPassCount = 10;

        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] HordeMigrationDriver test FAILED: " + name);
            }

            Check(T9_TileFsmIdleToMigrating(),       "T9.FsmIdleToMigrating");
            Check(T10_MigratingToStagingWithTimer(), "T10.FsmMigratingToStaging");
            Check(T11_LeaderTileMatchesHordeUpdate(),"T11.LeaderTileDeterministic");
            Check(T12_StaticDespawnMethodExists(),    "T12.DespawnHelperExists");
            Check(T13_MultiTileInWindow(),            "T13.MultiTileAdvance");
            Check(T14_TileDeterminismSameTick(),     "T14.TileDeterminismSameTick");
            Check(T15_ProfileStagingDurationDiffers(), "T15.ProfileStagingDiffers");
            Check(T16_IdempotentFiringDedup(),        "T16.IdempotentFire");
            Check(T17_HordeCapacitySurvival(),        "T17.HordeCapacitySurvival");
            Check(T18_AttackingTransitionsBackToIdle(), "T18.AttackBackToIdle");

            Log.Message("[Rimconemy.InfectedAutomation] HordeMigrationDriver tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        private static TravelTileRecord MakeRec(int tile, TravelTileStatus status, long tick, int stagingLeft)
            => new TravelTileRecord { Tile = tile, Status = status, LastTransitionTick = tick, ActiveStagingTicksLeft = stagingLeft, LastSeenAtTick = tick };

        private static bool T9_TileFsmIdleToMigrating()
        {
            var rec = MakeRec(50, TravelTileStatus.Idle, 60000L, 0);
            HordeMigrationDriver.AdvanceTileFSM(ref rec, "Survival", 60000L);
            return rec.Status == TravelTileStatus.Migrating;
        }

        private static bool T10_MigratingToStagingWithTimer()
        {
            var rec = MakeRec(50, TravelTileStatus.Migrating, 60000L, 0);
            HordeMigrationDriver.AdvanceTileFSM(ref rec, "Survival", 60000L);
            // Survival stagingDuration = 750 ticks (3 * 250).
            return rec.Status == TravelTileStatus.Staging && rec.ActiveStagingTicksLeft == 750;
        }

        private static bool T11_LeaderTileMatchesHordeUpdate()
        {
            int home = 100;
            int tile0 = HordeUpdateLogic.ComputeHordeTile(home, 0L);
            int tile1250 = HordeUpdateLogic.ComputeHordeTile(home, 1250L);
            return tile0 == home + 5 && tile1250 == home;
        }

        private static bool T12_StaticDespawnMethodExists()
        {
            return typeof(HordeMigrationDriver).GetMethod("DespawnWorldObjects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) != null;
        }

        private static bool T13_MultiTileInWindow()
        {
            var rec1 = MakeRec(48, TravelTileStatus.Idle, 60000L, 0);
            var rec2 = MakeRec(49, TravelTileStatus.Idle, 60000L, 0);
            HordeMigrationDriver.AdvanceTileFSM(ref rec1, "Survival", 60000L);
            HordeMigrationDriver.AdvanceTileFSM(ref rec2, "Survival", 60000L);
            return rec1.Status == TravelTileStatus.Migrating && rec2.Status == TravelTileStatus.Migrating;
        }

        private static bool T14_TileDeterminismSameTick()
        {
            var rec1 = MakeRec(50, TravelTileStatus.Idle, 60000L, 0);
            var rec2 = MakeRec(50, TravelTileStatus.Idle, 60000L, 0);
            HordeMigrationDriver.AdvanceTileFSM(ref rec1, "Survival", 60500L);
            HordeMigrationDriver.AdvanceTileFSM(ref rec2, "Survival", 60500L);
            return rec1.Status == rec2.Status && rec1.Tile == rec2.Tile;
        }

        private static bool T15_ProfileStagingDurationDiffers()
        {
            int collapseStaging = PopulationProfileMultipliers.GetHordeStagingDurationTicks("Collapse");
            int refugeStaging = PopulationProfileMultipliers.GetHordeStagingDurationTicks("Refuge");
            return refugeStaging > collapseStaging;
        }

        private static bool T16_IdempotentFiringDedup()
        {
            // Two consecutive calls without time-elapsed between them don't advance further.
            var rec = MakeRec(50, TravelTileStatus.Migrating, 60000L, 750);
            HordeMigrationDriver.AdvanceTileFSM(ref rec, "Survival", 60000L);
            // First advance: Migrating → Staging, timer = 750.
            // With same CurrentTick, no elapsed → timer unchanged.
            TravelTileRecord before = rec;
            HordeMigrationDriver.AdvanceTileFSM(ref rec, "Survival", 60000L);
            // Staging with elapsed=0 (< 750) → timer unchanged.
            return rec.ActiveStagingTicksLeft == before.ActiveStagingTicksLeft;
        }

        private static bool T17_HordeCapacitySurvival() =>
            PopulationProfileMultipliers.GetHordeCapacity("Survival") == 100;

        private static bool T18_AttackingTransitionsBackToIdle()
        {
            var rec = MakeRec(50, TravelTileStatus.Attacking, 60000L, 0);
            HordeMigrationDriver.AdvanceTileFSM(ref rec, "Survival", 60250L);
            return rec.Status == TravelTileStatus.Idle;
        }
    }
}
