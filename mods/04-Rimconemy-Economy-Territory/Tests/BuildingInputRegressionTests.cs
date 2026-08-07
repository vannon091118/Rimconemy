using Rimconemy.EconomyTerritory.Building;
using Verse;
using Rimconemy.Foundation.Tests;

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
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("EconomyTerritory", "Building input regression tests");

            _passed = 0;
            _failed = 0;

            // Physisch-vs-Wallet Trennung bleibt.
            ts.Check(BuildingInputAdapter.IsPhysicalInput("Rimconemy_ConstructionDebris"), "Building input: debris remains physical (D2/D3 preserved wallet-vs-physisch)");
            ts.Check(BuildingInputAdapter.IsPhysicalInput("Rimconemy_DistilledWater"), "Building input: distilled water remains physical");
            ts.Check(!(BuildingInputAdapter.IsCreditInput("Rimconemy_ConstructionDebris")), "Building input: debris is not credits");
            ts.Check(BuildingInputAdapter.IsCreditInput("Credits"), "Building input: Credits is an abstract wallet input");
            ts.Check(!(BuildingInputAdapter.IsCreditInput("Silver")), "Building input: Silver remains a physical vanilla item, not credits");
            ts.Check(!(BuildingInputAdapter.IsPhysicalInput("UnknownBuildingInput")), "Building input: unknown input is not physical");

            // D3-Harmo: Debris ist NICHT Wand-Stuff.
            ts.Check(Equals(0, BuildingInputAdapter.RequiredUnits("Rimconemy_ConstructionDebris", "Wall")), "Building input: debris is no longer wall stuff (D3) — RequiredUnits=0");
            ts.Check(Equals(0, BuildingInputAdapter.RequiredUnits("Rimconemy_ConstructionDebris", "Door")), "Building input: debris is no longer door stuff (D3)");

            // D3-Harmo: Tower braucht Waffen-Komponente.
            ts.Check(BuildingInputAdapter.IsPhysicalInput("Rimconemy_WeaponComponent"), "Building input: weapon component is a physical input");
            ts.Check(BuildingInputAdapter.RequiredUnits("Rimconemy_WeaponComponent", "Rimconemy_StainlessSteelTower") > 0, "Building input: tower requires weapon component (D3 Tower-Pfad grün)");
            ts.Check(BuildingInputAdapter.RequiredUnits("Rimconemy_StainlessSteel", "Rimconemy_StainlessSteelTower") > 0, "Building input: tower still requires stainless steel (existing chain)");
            ts.Check(BuildingInputAdapter.RequiredUnits("Rimconemy_MachineParts", "Rimconemy_StainlessSteelTower") > 0, "Building input: tower still requires machine parts (existing chain)");

            ts.Check(Equals(0, BuildingInputAdapter.RequiredUnits("UnknownBuildingInput", "Wall")), "Building input: unknown input requirement is zero");

            string summary = "[Rimconemy.EconomyTerritory] Building input regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }


    }
}
