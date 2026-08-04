using Rimconemy.SurvivalProgression.Progression;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>Regression tests for post-output Building XP awards.</summary>
    public static class BuildingProgressionRegressionTests
    {
        private static int _passed;
        private static int _failed;
        private static int _run;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;
            _run++;

            string key = "building-regression-run-" + _run;
            BuildingXpAward award;
            AssertEqual("Building", BuildingProgressionAdapter.BuildingWorkTypeId,
                "Building XP: stable work type");
            AssertTrue(BuildingProgressionAdapter.TryCreateAward(key, "pawn-1", 12, out award),
                "Building XP: valid output creates award");
            AssertEqual(12, award.Amount, "Building XP: award amount preserved");
            AssertFalse(BuildingProgressionAdapter.TryCreateAward(key, "pawn-1", 12, out award),
                "Building XP: duplicate output is rejected");
            AssertFalse(BuildingProgressionAdapter.TryCreateAward("", "pawn-1", 1, out award),
                "Building XP: empty idempotency key is rejected");
            AssertFalse(BuildingProgressionAdapter.TryCreateAward(key + "-zero", "pawn-1", 0, out award),
                "Building XP: non-positive amount is rejected");

            string summary = "[Rimconemy.SurvivalProgression] Building progression regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[BuildingProgressionRegression] " + label); }
        }

        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[BuildingProgressionRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
