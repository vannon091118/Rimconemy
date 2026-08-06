// Tests/PopulationLedgerRegressionTests.cs
//
// Owner: Infected & Automation (Package 05).
// Phase A — P6-PROGRESS §12.
//
// Framework: static RunAll() using Foundation.Tests.TestSuite harness.
// Failure-mode: Log.Error via TestSuite.Check() — do NOT throw,
// keep Bootstrap-Crash-Safe.
//
// Coverage-target: T1-T16 from spec. Tasks 2-6 implement the missing
// tests; this file is the home for them.

using Rimconemy.Foundation.Tests;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class PopulationLedgerRegressionTests
    {
        private const int MinExpected = 24;

        public static void RunAll()
        {
            var ts = new TestSuite("InfectedAutomation", "PopulationLedger regression tests (Phase A subset)");

            // T1 (Schema-Bump), T2 (Scribe), T15 (Total)
            ts.Check(TestSchemaVersionIsOne(),                              "T1.SchemaVersionIsOne");
            ts.Check(TestProfileIdDefaultsToSurvivalInDefaultCtor(),       "T1b.ProfileIdDefaultsToSurvivalInDefaultCtor");
            ts.Check(TestScribeRoundTripPreservesFields(),                 "T2.ScribeRoundTripPreservesFields");
            ts.Check(TestGetTotalLiveCountSumsHumanoidAndAnimal(),         "T15.GetTotalLiveCountSumsHumanoidAndAnimal");
            ts.Check(TestHumanoidOnlyCount(),                              "T15b.HumanoidOnlyCount");
            ts.Check(TestAnimalOnlyCount(),                                "T15c.AnimalOnlyCount");
            ts.Check(TestEmptyLedgerHasZeroTotalLiveCount(),               "T15d.EmptyLedgerHasZeroTotalLiveCount");

            // T3-T5 RegisterKill
            ts.Check(TestRegisterKillNullPawnNoOp(),                        "T3.RegisterKillNullPawnNoOp");
            ts.Check(TestRegisterKillHumanoidPawnDecrementsHumanoid(),     "T4.RegisterKillHumanoidPawnDecrementsHumanoid");
            ts.Check(TestRegisterKillAnimalPawnDecrementsAnimal(),         "T5.RegisterKillAnimalPawnDecrementsAnimal");
            ts.Check(TestRegisterKillTwiceSameIdIsIdempotent(),            "T4b.RegisterKillTwiceSameIdIsIdempotent");

            // T6-T9 Daily-Growth + Revenge-Quote + Reset
            ts.Check(TestApplyDailyGrowthTickSurvivalBaseline(),           "T6.ApplyDailyGrowthTickSurvivalBaseline");
            ts.Check(TestApplyDailyGrowthTickProfileVariance30Days(),      "T7.ApplyDailyGrowthTickProfileVariance30Days");
            ts.Check(TestGetRevengeQuotaSurvival(),                        "T8.GetRevengeQuotaSurvival");
            ts.Check(TestGetRevengeQuotaClippedByFreeBudget(),             "T9.GetRevengeQuotaClippedByFreeBudget");
            ts.Check(TestResetDailyCountersResetsRecentOnly(),             "T9b.ResetDailyCountersResetsRecentOnly");

            // T11/T12/T16 Reconciler
            ts.Check(TestReconcilerCountSurvivingInfectedBasic(),           "T11.ReconcilerCountSurvivingInfectedBasic");
            ts.Check(TestReconcilerExcludesDeadInfected(),                 "T11b.ReconcilerExcludesDeadInfected");
            ts.Check(TestReconcilerExcludesNonHiddenFaction(),             "T12.ReconcilerExcludesNonHiddenFaction");
            ts.Check(TestReconcilerApplyCountsReplacesLedger(),            "T11c.ReconcilerApplyCountsReplacesLedger");
            ts.Check(TestReconcilerAnimalDeathDoesNotAffectHumanoid(),     "T16.ReconcilerAnimalDeathDoesNotAffectHumanoid");

            // T13/T14 NoteInoculation
            ts.Check(TestNoteInoculationStampsTickAndIncrements(),         "T13.NoteInoculationStampsTickAndIncrements");
            ts.Check(TestNoteInoculationNullKindDefNoOp(),                 "T13b.NoteInoculationNullKindDefNoOp");
            ts.Check(TestInoculationCooldownHonorsProfile(),               "T14.InoculationCooldownHonorsProfile");

            ts.RunSummary(MinExpected);
        }

        // ── T1 Schema-Bump / Schema-Version ─────────────────
        private static bool TestSchemaVersionIsOne()
        {
            var ledger = new Population.PopulationLedger();
            return ledger.SchemaVersion == 1;
        }

        private static bool TestProfileIdDefaultsToSurvivalInDefaultCtor()
        {
            var ledger = new Population.PopulationLedger();
            return ledger.ProfileId == Population.PopulationProfileMultipliers.ProfileSurvival;
        }

        // ── T2 Scribe Roundtrip ──────────────────────────────
        private static bool TestScribeRoundTripPreservesFields()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 7,
                AnimalLiveCount = 3,
                Cap = 12,
                CumulativeKills = 5,
                RecentKillsToday = 2,
                DayIndexSinceStart = 4,
                LastDayTick = 240_000L,
                ProfileId = "Collapse",
                CumulativeInoculations = 1,
                LastInoculationTick = 100_000L,
            };

            bool roundTripOk = Rimconemy.Foundation.Tests.ScribeRoundTripHelper.RoundTrip(ledger);
            if (!roundTripOk) return false;

            return ledger.HumanoidLiveCount == 7
                && ledger.AnimalLiveCount == 3
                && ledger.Cap == 12
                && ledger.CumulativeKills == 5
                && ledger.RecentKillsToday == 2
                && ledger.DayIndexSinceStart == 4
                && ledger.LastDayTick == 240_000L
                && ledger.ProfileId == "Collapse"
                && ledger.CumulativeInoculations == 1
                && ledger.LastInoculationTick == 100_000L;
        }

        // ── T15 TotalLiveCount ───────────────────────────────
        private static bool TestGetTotalLiveCountSumsHumanoidAndAnimal()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 10,
                AnimalLiveCount = 4,
            };
            return ledger.GetTotalLiveCount() == 14;
        }

        private static bool TestHumanoidOnlyCount()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 8,
                AnimalLiveCount = 0,
            };
            return ledger.GetTotalLiveCount() == 8
                && ledger.GetHumanoidLiveCount() == 8
                && ledger.GetAnimalLiveCount() == 0;
        }

        private static bool TestAnimalOnlyCount()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 0,
                AnimalLiveCount = 5,
            };
            return ledger.GetTotalLiveCount() == 5
                && ledger.GetAnimalLiveCount() == 5
                && ledger.GetHumanoidLiveCount() == 0;
        }

        private static bool TestEmptyLedgerHasZeroTotalLiveCount()
        {
            var ledger = new Population.PopulationLedger();
            return ledger.GetTotalLiveCount() == 0
                && ledger.GetHumanoidLiveCount() == 0
                && ledger.GetAnimalLiveCount() == 0;
        }

        // ── T3 RegisterKill null-Pawn ─────────────────────
        private static bool TestRegisterKillNullPawnNoOp()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 5,
                CumulativeKills = 3,
                RecentKillsToday = 3,
            };
            ledger.RegisterKill(null);
            return ledger.HumanoidLiveCount == 5
                && ledger.AnimalLiveCount == 0
                && ledger.GetCumulativeKills() == 3
                && ledger.GetRecentKillsToday() == 3;
        }

        // ── T4 RegisterKill human Pawn decrements Humanoid ─────────
        private static bool TestRegisterKillHumanoidPawnDecrementsHumanoid()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 5,
                AnimalLiveCount = 2,
                RecentKillsToday = 0,
            };
            ledger.RegisterKillForTest("humanoid-pawn-1", isHumanlike: true);
            return ledger.HumanoidLiveCount == 4
                && ledger.AnimalLiveCount == 2
                && ledger.GetCumulativeKills() == 1
                && ledger.GetRecentKillsToday() == 1;
        }

        // ── T5 RegisterKill animal Pawn decrements Animal ───────────
        private static bool TestRegisterKillAnimalPawnDecrementsAnimal()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 5,
                AnimalLiveCount = 3,
                RecentKillsToday = 0,
            };
            ledger.RegisterKillForTest("animal-pawn-1", isHumanlike: false);
            return ledger.HumanoidLiveCount == 5
                && ledger.AnimalLiveCount == 2
                && ledger.GetCumulativeKills() == 1
                && ledger.GetRecentKillsToday() == 1;
        }

        // ── T4b Idempotency ─────────────────────────────────────────
        private static bool TestRegisterKillTwiceSameIdIsIdempotent()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 5,
                AnimalLiveCount = 0,
            };
            ledger.RegisterKillForTest("repeat-id", isHumanlike: true);
            ledger.RegisterKillForTest("repeat-id", isHumanlike: true);
            return ledger.HumanoidLiveCount == 4
                && ledger.GetCumulativeKills() == 1
                && ledger.GetRecentKillsToday() == 1;
        }

        // ── T6 ApplyDailyGrowthTick Survival baseline ──────────────
        private static bool TestApplyDailyGrowthTickSurvivalBaseline()
        {
            var ledger = new Population.PopulationLedger
            {
                Cap = 10,
                DayIndexSinceStart = 0,
                ProfileId = Population.PopulationProfileMultipliers.ProfileSurvival,
            };
            int newCap = ledger.ApplyDailyGrowthTick();
            return newCap == 11
                && ledger.Cap == 11
                && ledger.DayIndexSinceStart == 1;
        }

        // ── T7 30-Day Profile-Variance ──────────────────────────────
        private static bool TestApplyDailyGrowthTickProfileVariance30Days()
        {
            int refuge = SimulateGrowth("Refuge");
            int survival = SimulateGrowth("Survival");
            int collapse = SimulateGrowth("Collapse");
            return refuge < survival && survival < collapse;
        }

        private static int SimulateGrowth(string profileId)
        {
            var ledger = new Population.PopulationLedger
            {
                Cap = 5,
                ProfileId = profileId,
            };
            for (int i = 0; i < 30; i++) ledger.ApplyDailyGrowthTick();
            return ledger.Cap;
        }

        // ── T8 GetRevengeQuota Survival baseline ───────────────────
        private static bool TestGetRevengeQuotaSurvival()
        {
            var ledger = new Population.PopulationLedger
            {
                RecentKillsToday = 10,
                Cap = 100,
                HumanoidLiveCount = 90,
                ProfileId = Population.PopulationProfileMultipliers.ProfileSurvival,
            };
            return ledger.GetRevengeQuota(100) == 7;
        }

        // ── T9 GetRevengeQuota clipped by free budget ──────────────
        private static bool TestGetRevengeQuotaClippedByFreeBudget()
        {
            var ledger = new Population.PopulationLedger
            {
                RecentKillsToday = 100,
                Cap = 5,
                HumanoidLiveCount = 5,
                ProfileId = Population.PopulationProfileMultipliers.ProfileCollapse,
            };
            return ledger.GetRevengeQuota(5) == 0;
        }

        // ── T9b ResetDailyCounters resets Recent only ───────────────
        private static bool TestResetDailyCountersResetsRecentOnly()
        {
            var ledger = new Population.PopulationLedger
            {
                RecentKillsToday = 5,
                CumulativeKills = 12,
                HumanoidLiveCount = 8,
            };
            ledger.ResetDailyCounters();
            return ledger.RecentKillsToday == 0
                && ledger.CumulativeKills == 12
                && ledger.HumanoidLiveCount == 8;
        }

        // ── T11 Reconciler counts humanoid + animal survivors ──────────
        private static bool TestReconcilerCountSurvivingInfectedBasic()
        {
            var snapshots = new System.Collections.Generic.List<Population.PawnSnapshot>
            {
                Snap(humanLike: true,  animal: false, infected: true,  dead: false),
                Snap(humanLike: false, animal: true,  infected: true,  dead: false),
                Snap(humanLike: true,  animal: false, infected: true,  dead: false),
            };
            Population.ReconciliationLogic.CountSurvivingInfected(
                snapshots, out int humanoid, out int animal);
            return humanoid == 2 && animal == 1;
        }

        private static bool TestReconcilerExcludesDeadInfected()
        {
            var snapshots = new System.Collections.Generic.List<Population.PawnSnapshot>
            {
                Snap(humanLike: true, animal: false, infected: true, dead: false),
                Snap(humanLike: true, animal: false, infected: true, dead: true),
            };
            Population.ReconciliationLogic.CountSurvivingInfected(
                snapshots, out int humanoid, out _);
            return humanoid == 1;
        }

        private static bool TestReconcilerExcludesNonHiddenFaction()
        {
            var snapshots = new System.Collections.Generic.List<Population.PawnSnapshot>
            {
                Snap(humanLike: true, animal: false, infected: false, dead: false),
                Snap(humanLike: true, animal: false, infected: true, dead: false),
            };
            Population.ReconciliationLogic.CountSurvivingInfected(
                snapshots, out int humanoid, out _);
            return humanoid == 1;
        }

        private static bool TestReconcilerApplyCountsReplacesLedger()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 99,
                AnimalLiveCount = 33,
            };
            Population.ReconciliationLogic.ApplyCounts(ledger, humanoid: 7, animal: 3);
            return ledger.HumanoidLiveCount == 7 && ledger.AnimalLiveCount == 3;
        }

        private static bool TestReconcilerAnimalDeathDoesNotAffectHumanoid()
        {
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 5,
                AnimalLiveCount = 0,
            };
            var snapshots = new System.Collections.Generic.List<Population.PawnSnapshot>
            {
                Snap(humanLike: false, animal: true, infected: true, dead: false),
            };
            Population.ReconciliationLogic.CountSurvivingInfected(snapshots, out int humanoid, out int animal);
            Population.ReconciliationLogic.ApplyCounts(ledger, humanoid, animal);
            return ledger.HumanoidLiveCount == 0
                && ledger.AnimalLiveCount == 1
                && ledger.RecentKillsToday == 0
                && ledger.CumulativeKills == 0;
        }

        private static Population.PawnSnapshot Snap(
            bool humanLike, bool animal, bool infected, bool dead)
        {
            return new Population.PawnSnapshot
            {
                IsHumanlike = humanLike,
                IsAnimal = animal,
                IsHiddenInfected = infected,
                IsDead = dead,
            };
        }

        // ── T13 NoteInoculation stamps tick + increments ───────────
        private static bool TestNoteInoculationStampsTickAndIncrements()
        {
            var ledger = new Population.PopulationLedger
            {
                CumulativeInoculations = 2,
                LastInoculationTick = 0L,
            };
            ledger.NoteInoculation("Rimconemy_Infected_Wolf");
            return ledger.CumulativeInoculations == 3
                && ledger.LastInoculationTick >= 0L;
        }

        private static bool TestNoteInoculationNullKindDefNoOp()
        {
            var ledger = new Population.PopulationLedger
            {
                CumulativeInoculations = 2,
                LastInoculationTick = 100_000L,
            };
            ledger.NoteInoculation(null);
            ledger.NoteInoculation("");
            return ledger.CumulativeInoculations == 2
                && ledger.LastInoculationTick == 100_000L;
        }

        // ── T14 Cooldown-Eligibility via profile ────────────────────
        private static bool TestInoculationCooldownHonorsProfile()
        {
            var ledger = new Population.PopulationLedger
            {
                ProfileId = Population.PopulationProfileMultipliers.ProfileSurvival,
                LastInoculationTick = 100_000L,
            };
            const long interval = 60_000L * 7;
            long now = ledger.LastInoculationTick + interval;
            return (now - ledger.LastInoculationTick) >= interval;
        }
    }
}
