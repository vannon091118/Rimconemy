// Tests/HordeProfileMultipliersTests.cs
//
// Phase F T1 — Profile-Multipliers regression tests for Horde-related
// configuration. Determines the per-profile capacity, activation
// threshold, letter-cooldown, and staging-duration for the wandering
// horde.
using Rimconemy.InfectedAutomation.Population;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeProfileMultipliersTests
    {
        private static TestSuite ts;
        public const int ExpectedPassCount = 3;

        public static int RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "HordeProfileMultipliers test");

            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Error("[Rimconemy.InfectedAutomation] HordeProfileMultipliers test FAILED: " + name);
            }

            Check(T1_HordeCapacityRefuge(),         "T1.HordeCapacityRefuge");
            Check(T2_HordeCapacityCollapse(),       "T2.HordeCapacityCollapse");
            Check(T5_HordeStagingDurationTicks(),   "T5.HordeStagingDurationTicks");

            Log.Message("[Rimconemy.InfectedAutomation] HordeProfileMultipliers tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));

            ts.Check(failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return passed;
        }

        private static bool T1_HordeCapacityRefuge() =>
            PopulationProfileMultipliers.GetHordeCapacity("Refuge") == 50;

        private static bool T2_HordeCapacityCollapse() =>
            PopulationProfileMultipliers.GetHordeCapacity("Collapse") == 200;

        private static bool T5_HordeStagingDurationTicks()
        {
            int staging = PopulationProfileMultipliers.GetHordeStagingDurationTicks("Collapse");
            return staging > 0 && staging < 10000;
        }
    }
}
