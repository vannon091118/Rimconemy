// Tests/AnimalInfectionLedgerFieldsTests.cs
//
// Phase E T3 — PopulationLedger LastAnimalInfectionTick + AnimalInfectionCountToday
// plus RegisterAnimalInfection(c, tick) + ResetAtDayBucket.

using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class AnimalInfectionLedgerFieldsTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0;
            string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] AnimalInfectionLedgerFields test FAILED: " + name);
            }

            Check(T1_DefaultZeroes(),       "T9.LedgerDefaultZeroes");
            Check(T2_RegisterIncrements(), "T10.LedgerRegisterIncrements");
            Check(T3_CumulativeAdd(),      "T11.LedgerCumulativeAdd");
            Check(T4_ZeroCountNoop(),      "T12.LedgerZeroCountNoop");
            Check(T5_DayBucketReset(),     "T13.LedgerDayBucketReset");

            Log.Message("[Rimconemy.InfectedAutomation] AnimalInfectionLedgerFields tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        private static bool T1_DefaultZeroes()
        {
            var l = new PopulationLedger();
            return l.LastAnimalInfectionTick == 0L
                && l.AnimalInfectionCountToday == 0;
        }

        private static bool T2_RegisterIncrements()
        {
            var l = new PopulationLedger();
            l.RegisterAnimalInfection(3, 60_000L);
            return l.AnimalInfectionCountToday == 3
                && l.LastAnimalInfectionTick == 60_000L;
        }

        private static bool T3_CumulativeAdd()
        {
            var l = new PopulationLedger();
            l.RegisterAnimalInfection(2, 60_000L);
            l.RegisterAnimalInfection(2, 120_000L);
            return l.AnimalInfectionCountToday == 5
                && l.LastAnimalInfectionTick == 120_000L;
        }

        private static bool T4_ZeroCountNoop()
        {
            var l = new PopulationLedger();
            l.RegisterAnimalInfection(0, 0L);
            return l.AnimalInfectionCountToday == 0
                && l.LastAnimalInfectionTick == 0L;
        }

        private static bool T5_DayBucketReset()
        {
            var l = new PopulationLedger();
            l.RegisterAnimalInfection(4, 60_000L);
            l.ResetAnimalInfectionDailyCounters();
            return l.AnimalInfectionCountToday == 0
                && l.LastAnimalInfectionTick == 60_000L; // NOT reset
        }
    }
}
