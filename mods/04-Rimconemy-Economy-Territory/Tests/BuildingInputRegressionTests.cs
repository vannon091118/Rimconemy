using Rimconemy.EconomyTerritory.Building;
using Verse;

namespace Rimconemy.EconomyTerritory.Tests
{
    /// <summary>Regression tests for the physical Building-input boundary.</summary>
    public static class BuildingInputRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            AssertTrue(BuildingInputAdapter.IsPhysicalInput("Rimconemy_ConstructionDebris"),
                "Building input: debris remains physical");
            AssertTrue(BuildingInputAdapter.IsPhysicalInput("Rimconemy_DistilledWater"),
                "Building input: distilled water remains physical");
            AssertFalse(BuildingInputAdapter.IsCreditInput("Rimconemy_ConstructionDebris"),
                "Building input: debris is not credits");
            AssertTrue(BuildingInputAdapter.IsCreditInput("Credits"),
                "Building input: Credits is an abstract wallet input");
            AssertFalse(BuildingInputAdapter.IsCreditInput("Silver"),
                "Building input: Silver remains a physical vanilla item, not credits");
            AssertFalse(BuildingInputAdapter.IsPhysicalInput("UnknownBuildingInput"),
                "Building input: unknown input is not physical");
            AssertTrue(BuildingInputAdapter.RequiredUnits("Rimconemy_ConstructionDebris", "Wall") > 0,
                "Building input: wall has deterministic debris requirement");
            AssertEqual(0, BuildingInputAdapter.RequiredUnits("UnknownBuildingInput", "Wall"),
                "Building input: unknown input requirement is zero");

            string summary = "[Rimconemy.EconomyTerritory] Building input regression tests: "
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
            else { _failed++; Log.Error("[BuildingInputRegression] " + label); }
        }

        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[BuildingInputRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
