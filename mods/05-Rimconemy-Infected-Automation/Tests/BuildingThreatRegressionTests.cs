using Rimconemy.InfectedAutomation.Building;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>Regression tests for the Building contribution to threat read models.</summary>
    public static class BuildingThreatRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            AssertNear(0.475f, BuildingThreatAdapter.ComputePressure(2, 1, 0.5f), 0.0001f,
                "Building threat: weighted pressure is deterministic");
            AssertEqual(0f, BuildingThreatAdapter.ComputePressure(0, 0, -1f),
                "Building threat: lower bound is clamped");
            AssertEqual(1f, BuildingThreatAdapter.ComputePressure(100, 100, 10f),
                "Building threat: upper bound is clamped");
            string key = BuildingThreatAdapter.BuildDeterminismKey(120L, "ABC", "DEF");
            AssertEqual("120|ABC|DEF", key, "Building threat: determinism key is canonical");

            string summary = "[Rimconemy.InfectedAutomation] Building threat regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void AssertNear(float expected, float actual, float tolerance, string label)
        {
            if (System.Math.Abs(expected - actual) <= tolerance) _passed++;
            else
            {
                _failed++;
                Log.Error("[BuildingThreatRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[BuildingThreatRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
