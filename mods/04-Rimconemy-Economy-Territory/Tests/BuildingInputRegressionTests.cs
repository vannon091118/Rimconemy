using Rimconemy.EconomyTerritory.Building;
using Verse;

namespace Rimconemy.EconomyTerritory.Tests
{
    /// <summary>
    /// Regression tests for the physical Building-input boundary.
    /// D2/D3-Harmonisierung 2026-08-05:
    ///   - `Rimconemy_ConstructionDebris` ist NICHT mehr Wand-Stuff.
    ///     RequiredUnits(Debris, "Wall") == 0.
    ///   - `Rimconemy_WeaponComponent` ist der Tower-Input (DECISIONS §29).
    ///     RequiredUnits(WeaponComponent, "Rimconemy_StainlessSteelTower") > 0.
    ///   - Debris bleibt physisch (wallet-vs-physisch Trennung §6).
    /// </summary>
    public static class BuildingInputRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            // Physisch-vs-Wallet Trennung bleibt.
            AssertTrue(BuildingInputAdapter.IsPhysicalInput("Rimconemy_ConstructionDebris"),
                "Building input: debris remains physical (D2/D3 preserved wallet-vs-physisch)");
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

            // D3-Harmo: Debris ist NICHT Wand-Stuff.
            AssertEqual(0, BuildingInputAdapter.RequiredUnits("Rimconemy_ConstructionDebris", "Wall"),
                "Building input: debris is no longer wall stuff (D3) — RequiredUnits=0");
            AssertEqual(0, BuildingInputAdapter.RequiredUnits("Rimconemy_ConstructionDebris", "Door"),
                "Building input: debris is no longer door stuff (D3)");

            // D3-Harmo: Tower braucht Waffen-Komponente.
            AssertTrue(BuildingInputAdapter.IsPhysicalInput("Rimconemy_WeaponComponent"),
                "Building input: weapon component is a physical input");
            AssertTrue(BuildingInputAdapter.RequiredUnits("Rimconemy_WeaponComponent", "Rimconemy_StainlessSteelTower") > 0,
                "Building input: tower requires weapon component (D3 Tower-Pfad grün)");
            AssertTrue(BuildingInputAdapter.RequiredUnits("Rimconemy_StainlessSteel", "Rimconemy_StainlessSteelTower") > 0,
                "Building input: tower still requires stainless steel (existing chain)");
            AssertTrue(BuildingInputAdapter.RequiredUnits("Rimconemy_MachineParts", "Rimconemy_StainlessSteelTower") > 0,
                "Building input: tower still requires machine parts (existing chain)");

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
