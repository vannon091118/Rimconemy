// Tests/AnimalInfectionRegressionTests.cs
//
// Phase E — Animal-Infection via Random Encounter (T1-T8).
// spec: docs/superpowers/specs/2026-08-05-animal-infection-design.md §5
// plan: docs/superpowers/plans/2026-08-05-animal-infection.md T1-T2
//
// Owner: Infected & Automation (Package 05).
using Rimconemy.InfectedAutomation.Inoculation;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class AnimalInfectionRegressionTests
    {
        public const int ExpectedPassCount = 8;

        public static int RunAll()
        {
            int passed = 0, failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] AnimalInfection test FAILED: " + name);
            }

            Check(T1_SurvivalBaseChance(),       "T1.SurvivalBaseChance");
            Check(T2_SurvivalScaleAt100(),       "T2.SurvivalScaleAt100");
            Check(T3_SurvivalScaleAt200(),       "T3.SurvivalScaleAt200");
            Check(T4_CollapseScaleAt50(),        "T4.CollapseScaleAt50");
            Check(T5_RefugeBaseFloor(),          "T5.RefugeBaseFloor");
            Check(T6_BelowThresholdNoDecay(),    "T6.BelowThresholdNoDecay");
            Check(T7_HardCapClamp(),             "T7.HardCapClamp");
            Check(T8_CountRespectsPerDayCap(),   "T8.CountRespectsPerDayCap");

            Log.Message("[Rimconemy.InfectedAutomation] AnimalInfection regression tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        // T1: Survival 0 Horde → BaseChance = 0.05
        private static bool T1_SurvivalBaseChance()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(1L, 0, SettingProfile.Survival);
            return System.Math.Abs(c - 0.05) < 0.001;
        }

        // T2: Survival 100 Horde → 0.05 * (1 + 1.0 * 100 / 150)
        private static bool T2_SurvivalScaleAt100()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(2L, 100, SettingProfile.Survival);
            double expected = 0.05 * (1.0 + 1.0 * 100.0 / 150.0);
            return System.Math.Abs(c - expected) < 0.001;
        }

        // T3: Survival 200 Horde → 0.05 * (1 + 1.0 * 200 / 150) ≈ 0.117
        private static bool T3_SurvivalScaleAt200()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(3L, 200, SettingProfile.Survival);
            double expected = 0.05 * (1.0 + 1.0 * 200.0 / 150.0);
            return System.Math.Abs(c - expected) < 0.001;
        }

        // T4: Collapse 50 Horde → 0.15 * (1 + 1.5 * 50 / 80) ≈ 0.291
        private static bool T4_CollapseScaleAt50()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(4L, 50, SettingProfile.Collapse);
            double expected = 0.15 * (1.0 + 1.5 * 50.0 / 80.0);
            return System.Math.Abs(c - expected) < 0.001;
        }

        // T5: Refuge 0 Horde → 0.02 (Minimum floor)
        private static bool T5_RefugeBaseFloor()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(5L, 0, SettingProfile.Refuge);
            return System.Math.Abs(c - 0.02) < 0.001;
        }

        // T6: Below threshold/scaled values are not below base chance
        // (no decay; only growth above threshold).
        private static bool T6_BelowThresholdNoDecay()
        {
            double cZero = AnimalInfectionChance.ComputeChancePerDay(6L, 0, SettingProfile.Collapse);
            double cNeg = AnimalInfectionChance.ComputeChancePerDay(6L, -10, SettingProfile.Collapse);
            return cZero >= cNeg && System.Math.Abs(cZero - 0.15) < 0.001;
        }

        // T7: Hard cap clamps at 0.95 even for absurdly high horde counts.
        private static bool T7_HardCapClamp()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(7L, 10_000_000, SettingProfile.Collapse);
            return c <= AnimalInfectionChance.HardCap + 0.0001;
        }

        // T8: ComputeInfectionCount returns 0..InoculationsPerDay (per-day cap).
        private static bool T8_CountRespectsPerDayCap()
        {
            int cnt = AnimalInfectionChance.ComputeInfectionCount(8L, 100, SettingProfile.Collapse);
            int cap = PopulationProfileMultipliers.GetInoculationsPerDay("Collapse");
            return cnt >= 0 && cnt <= cap;
        }
    }
}
