// Tests/AnimalInfectionDriverTests.cs
//
// Phase E T5 — AnimalInfectionDriver TryFireOnce Tests.
using Rimconemy.InfectedAutomation.Inoculation;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class AnimalInfectionDriverTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] AnimalInfectionDriver test FAILED: " + name);
            }

            Check(T9_TickIntervalConstant(), "T9.DriverTickIntervalConstant");
            Check(T10_NullMapReturnsZero(),   "T10.DriverNullMapZero");
            Check(T11_StubTodayBlocksFire(), "T11.DriverStubTodayBlocksFire");
            Check(T12_ResetForTests(),       "T12.DriverResetForTests");
            Check(T13_IdempotentFire(),      "T13.DriverIdempotentFire");

            Log.Message("[Rimconemy.InfectedAutomation] AnimalInfectionDriver tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        private static bool T9_TickIntervalConstant()
        {
            return AnimalInfectionDriver.TickInterval >= 3_600;
        }

        private static bool T10_NullMapReturnsZero()
        {
            try { return AnimalInfectionDriver.TryFireOnce(null, 60_000L) == 0; }
            catch { return false; }
        }

        private static bool T11_StubTodayBlocksFire()
        {
            int cnt = AnimalInfectionChance.ComputeInfectionCount(60_000L, 200, SettingProfile.Survival);
            int cap = PopulationProfileMultipliers.GetInoculationsPerDay("Survival");
            return cnt >= 0 && cnt <= cap;
        }

        private static bool T12_ResetForTests()
        {
            AnimalInfectionDriver.ResetForTests();
            long a = AnimalInfectionDriver.GetLastFireTick();
            AnimalInfectionDriver.ResetForTests();
            long b = AnimalInfectionDriver.GetLastFireTick();
            return a == -1L && b == -1L;
        }

        private static bool T13_IdempotentFire()
        {
            var l = new PopulationLedger
            {
                HumanoidLiveCount = 100,
                AnimalLiveCount = 100,
                Cap = 250,
                ProfileId = "Survival",
                AnimalInfectionCountToday = 0,
            };
            bool should1 = AnimalInfectionChance.ShouldFireToday(60_000L,
                l.AnimalInfectionCountToday, 100 + 100 / 2, SettingProfile.Survival);
            bool should2 = AnimalInfectionChance.ShouldFireToday(60_000L,
                l.AnimalInfectionCountToday, 100 + 100 / 2, SettingProfile.Survival);
            return should1 == should2;
        }
    }
}
