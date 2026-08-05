// Tests/PopulationProfileMultipliersRegressionTests.cs
//
// Owner: Infected & Automation (Package 05).
// Phase A — P6-PROGRESS §12.
//
// Test-Pattern: static RunAll() mit Inline-Assertions, keine externen
// Test-Frameworks. Più mit Log.Message/Log.Warning statt Log.Error, damit
// ein Fail keinen Bootstrap-Crash auslöst.

using System.Linq;
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class PopulationProfileMultipliersRegressionTests
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
                Log.Warning("[Rimconemy.InfectedAutomation] PopulationProfileMultipliers test FAILED: " + name);
            }

            Check(TestSupportedProfilesContainAllThree(),               "T1.SupportedProfilesContainAllThree");
            Check(TestDailyGrowthMonotonicProfileVariance(),           "T2.DailyGrowthMonotonicVariance");
            Check(TestRevengeRatioMonotonicProfileVariance(),          "T3.RevengeRatioMonotonicVariance");
            Check(TestHordeThresholdMonotonicReverse(),                "T4.HordeThresholdMonotonicReverse");
            Check(TestInoculationsPerDayMonotonicProfileVariance(),   "T5.InoculationsPerDayMonotonicVariance");
            Check(TestInoculationMinIntervalMonotonicReverse(),        "T6.InoculationMinIntervalMonotonicReverse");
            Check(TestSurvivalBaselineMatchesSpec(),                   "T7.SurvivalBaselineMatchesSpec");
            Check(TestUnknownProfileFallsBackToSurvival(),             "T8.UnknownProfileFallsBackToSurvival");
            Check(TestNullProfileFallsBackToSurvival(),                "T9.NullProfileFallsBackToSurvival");

            Log.Message(
                "[Rimconemy.InfectedAutomation] PopulationProfileMultipliers regression tests: "
                + _passed + " passed, " + _failed + " failed."
                + (firstFailure != null ? " First failure: " + firstFailure : ""));
        }

        private static bool TestSupportedProfilesContainAllThree()
        {
            var supported = PopulationProfileMultipliers.SupportedProfiles.ToList();
            return supported.Contains(PopulationProfileMultipliers.ProfileRefuge)
                && supported.Contains(PopulationProfileMultipliers.ProfileSurvival)
                && supported.Contains(PopulationProfileMultipliers.ProfileCollapse)
                && supported.Count == 3;
        }

        private static bool TestDailyGrowthMonotonicProfileVariance()
        {
            float refuge = PopulationProfileMultipliers.GetDailyGrowth(PopulationProfileMultipliers.ProfileRefuge);
            float survival = PopulationProfileMultipliers.GetDailyGrowth(PopulationProfileMultipliers.ProfileSurvival);
            float collapse = PopulationProfileMultipliers.GetDailyGrowth(PopulationProfileMultipliers.ProfileCollapse);
            return refuge < survival && survival < collapse;
        }

        private static bool TestRevengeRatioMonotonicProfileVariance()
        {
            float refuge = PopulationProfileMultipliers.GetRevengeRatio(PopulationProfileMultipliers.ProfileRefuge);
            float survival = PopulationProfileMultipliers.GetRevengeRatio(PopulationProfileMultipliers.ProfileSurvival);
            float collapse = PopulationProfileMultipliers.GetRevengeRatio(PopulationProfileMultipliers.ProfileCollapse);
            return refuge < survival && survival < collapse;
        }

        private static bool TestHordeThresholdMonotonicReverse()
        {
            int refuge = PopulationProfileMultipliers.GetHordeThreshold(PopulationProfileMultipliers.ProfileRefuge);
            int survival = PopulationProfileMultipliers.GetHordeThreshold(PopulationProfileMultipliers.ProfileSurvival);
            int collapse = PopulationProfileMultipliers.GetHordeThreshold(PopulationProfileMultipliers.ProfileCollapse);
            // Monotonic reverse: Refuge > Survival > Collapse (easier difficulty triggers Horde earlier)
            return refuge > survival && survival > collapse;
        }

        private static bool TestInoculationsPerDayMonotonicProfileVariance()
        {
            int refuge = PopulationProfileMultipliers.GetInoculationsPerDay(PopulationProfileMultipliers.ProfileRefuge);
            int survival = PopulationProfileMultipliers.GetInoculationsPerDay(PopulationProfileMultipliers.ProfileSurvival);
            int collapse = PopulationProfileMultipliers.GetInoculationsPerDay(PopulationProfileMultipliers.ProfileCollapse);
            return refuge < survival && survival < collapse;
        }

        private static bool TestInoculationMinIntervalMonotonicReverse()
        {
            long refuge = PopulationProfileMultipliers.GetInoculationMinInterval(PopulationProfileMultipliers.ProfileRefuge);
            long survival = PopulationProfileMultipliers.GetInoculationMinInterval(PopulationProfileMultipliers.ProfileSurvival);
            long collapse = PopulationProfileMultipliers.GetInoculationMinInterval(PopulationProfileMultipliers.ProfileCollapse);
            // Monotonic reverse: harder profile has SHORTER interval → more frequent.
            return refuge > survival && survival > collapse;
        }

        private static bool TestSurvivalBaselineMatchesSpec()
        {
            // Survival is the documented User-Spec default.
            return FloatsApproximately(PopulationProfileMultipliers.GetDailyGrowth(PopulationProfileMultipliers.ProfileSurvival), 1.15f)
                && FloatsApproximately(PopulationProfileMultipliers.GetRevengeRatio(PopulationProfileMultipliers.ProfileSurvival), 0.7f)
                && PopulationProfileMultipliers.GetHordeThreshold(PopulationProfileMultipliers.ProfileSurvival) == 150
                && PopulationProfileMultipliers.GetInoculationsPerDay(PopulationProfileMultipliers.ProfileSurvival) == 1
                && PopulationProfileMultipliers.GetInoculationMinInterval(PopulationProfileMultipliers.ProfileSurvival) == 60_000L * 7;
        }

        private static bool TestUnknownProfileFallsBackToSurvival()
        {
            // Spec §Fehlerbehandlung: Unknown profile → Survival-default + Log.Warning.
            return FloatsApproximately(
                PopulationProfileMultipliers.GetDailyGrowth("BogusProfile"),
                PopulationProfileMultipliers.GetDailyGrowth(PopulationProfileMultipliers.ProfileSurvival))
                && PopulationProfileMultipliers.GetHordeThreshold("BogusProfile")
                    == PopulationProfileMultipliers.GetHordeThreshold(PopulationProfileMultipliers.ProfileSurvival);
        }

        private static bool TestNullProfileFallsBackToSurvival()
        {
            return FloatsApproximately(
                PopulationProfileMultipliers.GetDailyGrowth(null),
                PopulationProfileMultipliers.GetDailyGrowth(PopulationProfileMultipliers.ProfileSurvival))
                && PopulationProfileMultipliers.GetInoculationsPerDay(null)
                    == PopulationProfileMultipliers.GetInoculationsPerDay(PopulationProfileMultipliers.ProfileSurvival);
        }

        private static bool FloatsApproximately(float a, float b)
        {
            // |a - b| < 0.0001f — matches RimWorld Mathf.Approximately spec
            return System.Math.Abs(a - b) < 0.0001f;
        }
    }
}
