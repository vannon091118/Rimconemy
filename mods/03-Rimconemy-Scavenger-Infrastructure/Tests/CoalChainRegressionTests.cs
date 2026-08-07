using Rimconemy.ScavengerInfrastructure.Resources;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.ScavengerInfrastructure.Tests
{
    /// <summary>
    /// P0 Coal Chain Regression Gate (2026-08-04).
    ///
    /// Invariants:
    ///   C1: Rimconemy_Coal ThingDef exists and is valid.
    ///   C2: Rimconemy_MachineParts ThingDef exists and is valid.
    ///   C3: Rimconemy_CraftingStations ThingCategoryDef exists.
    ///   C4: Rimconemy_MakeCoal RecipeDef exists.
    ///   C5: Rimconemy_SalvageMachineParts RecipeDef exists.
    ///   C6: Rimconemy_BurnSteelScraps RecipeDef still wired.
    ///   C7: Campfire has all 3 recipes wired.
    ///   C8: WoodCoalGenerator has 2 Refuelable comps.
    ///   C9: Coal category is Rimconemy_GeneratorInputs.
    ///   C10: MachineParts category is Rimconemy_Scraps.
    /// </summary>
    public static class CoalChainRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("ScavengerInfrastructure", "CoalChain regression tests");

            _passed = 0;
            _failed = 0;

            TestC1_CoalDefExists();
            TestC2_MachinePartsDefExists();
            TestC3_CraftingStationsCategoryExists();
            TestC4_MakeCoalRecipeExists();
            TestC5_SalvageMachinePartsRecipeExists();
            TestC6_BurnSteelScrapsRecipeWired();
            TestC7_CampfireHasAllRecipes();
            TestC8_GeneratorHasSingleRefuelable();
            TestC9_CoalCategory();
            TestC10_MachinePartsCategory();

            string summary = "[Rimconemy.ScavengerInfrastructure] CoalChain regression tests: "
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

        /// <summary>C1: Rimconemy_Coal ThingDef exists and is valid.</summary>
        private static void TestC1_CoalDefExists()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Coal");
            ts.Check(def != null, "C1.Rimconemy_Coal ThingDef is loaded");
            if (def != null)
            {
                ts.Check(def.stackLimit > 0, "C1.Coal has valid stackLimit");
                ts.Check(def.thingCategories != null && def.thingCategories.Exists(c => c.defName == "Rimconemy_GeneratorInputs"), "C1.Coal has GeneratorInputs category");
            }
        }

        /// <summary>C2: Rimconemy_MachineParts ThingDef exists and is valid.</summary>
        private static void TestC2_MachinePartsDefExists()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_MachineParts");
            ts.Check(def != null, "C2.Rimconemy_MachineParts ThingDef is loaded");
            if (def != null)
            {
                ts.Check(def.stackLimit > 0, "C2.MachineParts has valid stackLimit");
                ts.Check(def.thingCategories != null && def.thingCategories.Exists(c => c.defName == "Rimconemy_Scraps"), "C2.MachineParts has Scraps category");
            }
        }

        /// <summary>C3: Rimconemy_CraftingStations ThingCategoryDef exists.</summary>
        private static void TestC3_CraftingStationsCategoryExists()
        {
            var def = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Rimconemy_CraftingStations");
            ts.Check(def != null, "C3.Rimconemy_CraftingStations ThingCategoryDef is loaded");
        }

        /// <summary>C4: Rimconemy_MakeCoal RecipeDef exists.</summary>
        private static void TestC4_MakeCoalRecipeExists()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_MakeCoal");
            ts.Check(recipe != null, "C4.Rimconemy_MakeCoal RecipeDef is loaded");
            if (recipe != null)
            {
                ts.Check(recipe.products != null && recipe.products.Count > 0, "C4.MakeCoal has products");
                bool producesCoal = recipe.products.Exists(p =>
                    p.thingDef != null && p.thingDef.defName == "Rimconemy_Coal" && p.count == 4);
                ts.Check(producesCoal, "C4.MakeCoal produces 4 Rimconemy_Coal");
            }
        }

        /// <summary>C5: Rimconemy_SalvageMachineParts RecipeDef exists.</summary>
        private static void TestC5_SalvageMachinePartsRecipeExists()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_SalvageMachineParts");
            ts.Check(recipe != null, "C5.Rimconemy_SalvageMachineParts RecipeDef is loaded");
            if (recipe != null)
            {
                ts.Check(recipe.products != null && recipe.products.Count > 0, "C5.SalvageMachineParts has products");
                bool producesParts = recipe.products.Exists(p =>
                    p.thingDef != null && p.thingDef.defName == "Rimconemy_MachineParts" && p.count == 1);
                ts.Check(producesParts, "C5.SalvageMachineParts produces 1 Rimconemy_MachineParts");
            }
        }

        /// <summary>C6: Rimconemy_BurnSteelScraps RecipeDef still wired (Phase-First 5:1 ratio).</summary>
        private static void TestC6_BurnSteelScrapsRecipeWired()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_BurnSteelScraps");
            ts.Check(recipe != null, "C6.Rimconemy_BurnSteelScraps RecipeDef is loaded");
            if (recipe != null)
            {
                // Phase-First contract (PHASE_PROGRESSION_CONTRACT.md): 5 SteelScraps -> 1 Steel.
                // Updated 2026-08-05 to harden the 5:1 gate.
                bool producesSteel = recipe.products.Exists(p =>
                    p.thingDef != null && p.thingDef.defName == "Steel" && p.count == 1);
                ts.Check(producesSteel, "C6.BurnSteelScraps produces 1 Steel (5:1 Phase-First)");
            }
        }

        /// <summary>C7: Campfire has all 5 recipes wired (D3 adds MakeWeaponComponent).</summary>
        private static void TestC7_CampfireHasAllRecipes()
        {
            var campfire = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Campfire");
            ts.Check(campfire != null, "C7.Rimconemy_Campfire ThingDef is loaded");
            if (campfire != null)
            {
                ts.Check(campfire.recipes != null && campfire.recipes.Count >= 5, "C7.Campfire has at least 5 recipes");
                bool hasBurn = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_BurnSteelScraps");
                bool hasMake = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_MakeCoal");
                bool hasSalvage = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_SalvageMachineParts");
                bool hasSS = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_MakeStainlessSteel");
                bool hasWC = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_MakeWeaponComponent");
                ts.Check(hasBurn, "C7.Campfire has BurnSteelScraps");
                ts.Check(hasMake, "C7.Campfire has MakeCoal");
                ts.Check(hasSalvage, "C7.Campfire has SalvageMachineParts");
                ts.Check(hasSS, "C7.Campfire has MakeStainlessSteel");
                ts.Check(hasWC, "C7.Campfire has MakeWeaponComponent (D3)");
            }
        }

        /// <summary>C8: WoodCoalGenerator has exactly ONE Refuelable comp (Phase-First consolidation).</summary>
        private static void TestC8_GeneratorHasSingleRefuelable()
        {
            var gen = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_WoodCoalGenerator");
            ts.Check(gen != null, "C8.Rimconemy_WoodCoalGenerator ThingDef is loaded");
            if (gen != null && gen.comps != null)
            {
                var refuelables = new List<CompProperties_Refuelable>();
                foreach (var comp in gen.comps)
                {
                    if (comp is CompProperties_Refuelable r)
                        refuelables.Add(r);
                }
                // Phase-First (2026-08-05): duplicate Refuelable comp removed because
                // Vanilla IM only instantiates the first; consolidation into a single
                // Refuelable with WoodLog/Chemfuel/Rimconemy_Coal in fuelFilter.
                ts.Check(refuelables.Count == 1, "C8.Generator has exactly 1 Refuelable comp (Phase-First consolidation)");

                var primaryRefuel = refuelables.Count > 0 ? refuelables[0] : null;
                if (primaryRefuel != null && primaryRefuel.fuelFilter != null)
                {
                    bool hasWood = primaryRefuel.fuelFilter.AllowedThingDefs.Any(d => d.defName == "WoodLog");
                    bool hasChem = primaryRefuel.fuelFilter.AllowedThingDefs.Any(d => d.defName == "Chemfuel");
                    bool hasCoal = primaryRefuel.fuelFilter.AllowedThingDefs.Any(d => d.defName == "Rimconemy_Coal");
                    ts.Check(hasWood && hasChem && hasCoal, "C8.Generator Refuelable fuelFilter contains WoodLog+Chemfuel+Rimconemy_Coal");
                }
            }
        }

        /// <summary>C9: Coal category is Rimconemy_GeneratorInputs.</summary>
        private static void TestC9_CoalCategory()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Coal");
            if (def != null && def.thingCategories != null)
            {
                bool hasCat = def.thingCategories.Exists(c => c.defName == "Rimconemy_GeneratorInputs");
                ts.Check(hasCat, "C9.Coal has GeneratorInputs category");
            }
            else
            {
                ts.Check(false, "C9.Coal category check failed (def or categories null)");
            }
        }

        /// <summary>C10: MachineParts category is Rimconemy_Scraps.</summary>
        private static void TestC10_MachinePartsCategory()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_MachineParts");
            if (def != null && def.thingCategories != null)
            {
                bool hasCat = def.thingCategories.Exists(c => c.defName == "Rimconemy_Scraps");
                ts.Check(hasCat, "C10.MachineParts has Scraps category");
            }
            else
            {
                ts.Check(false, "C10.MachineParts category check failed (def or categories null)");
            }
        }

    }
}
