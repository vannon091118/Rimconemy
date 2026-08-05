// Tests/PopulationLedgerRegressionTests.cs
//
// Owner: Infected & Automation (Package 05).
// Phase A — P6-PROGRESS §12.
//
// Framework: static RunAll(). Pattern matches the package's existing
// regression suites (StoryStateRegressionTests etc.). Failure-mode: log
// + counter, do NOT throw — keep Bootstrap-Crash-Safe.
//
// Coverage-target: T1-T16 from spec. Tasks 2-6 implement the missing
// tests; this file is the home for them. Tests added by later tasks
// wired into the same RunAll() switch.

using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class PopulationLedgerRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static void RunAll()
        {
            _passed = 0;
            _failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { _passed++; return; }
                _failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] PopulationLedger test FAILED: " + name);
            }

            // T1 (Schema-Bump), T2 (Scribe), T15 (Total) kommen aus Task 2
            Check(TestSchemaVersionIsOne(),                              "T1.SchemaVersionIsOne");
            Check(TestProfileIdDefaultsToSurvivalInDefaultCtor(),       "T1b.ProfileIdDefaultsToSurvivalInDefaultCtor");
            Check(TestScribeRoundTripPreservesFields(),                 "T2.ScribeRoundTripPreservesFields");
            Check(TestGetTotalLiveCountSumsHumanoidAndAnimal(),         "T15.GetTotalLiveCountSumsHumanoidAndAnimal");
            Check(TestHumanoidOnlyCount(),                              "T15b.HumanoidOnlyCount");
            Check(TestAnimalOnlyCount(),                                "T15c.AnimalOnlyCount");
            Check(TestEmptyLedgerHasZeroTotalLiveCount(),               "T15d.EmptyLedgerHasZeroTotalLiveCount");

            Log.Message(
                "[Rimconemy.InfectedAutomation] PopulationLedger regression tests (Phase A subset): "
                + _passed + " passed, " + _failed + " failed."
                + (firstFailure != null ? " First failure: " + firstFailure : ""));
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
            // The ScribeRoundTripHelper.RoundTrip() mutates the instance
            // (save → load). It writes all 10 fields through the same
            // Scribe_Values.Look pipeline that ExposeData uses, then loads
            // them back. If preserved, all values are equal afterwards.
            var ledger = new Population.PopulationLedger
            {
                HumanoidLiveCount = 7,
                AnimalLiveCount = 3,
                Cap = 12,
                CumulativeKills = 5,
                RecentKillsToday = 2,
                DayIndexSinceStart = 4,
                LastDayTick = 240_000L,
                ProfileId = "Collapse",  // != Default ("Survival") so Scribe-Persistenz wirklich getestet
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
    }
}
