using Rimconemy.ScavengerInfrastructure.Resources;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

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
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            TestC1_CoalDefExists();
            TestC2_MachinePartsDefExists();
            TestC3_CraftingStationsCategoryExists();
            TestC4_MakeCoalRecipeExists();
            TestC5_SalvageMachinePartsRecipeExists();
            TestC6_BurnSteelScrapsRecipeWired();
            TestC7_CampfireHasAllRecipes();
            TestC8_GeneratorHasTwoRefuelables();
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
            return true;
        }

        /// <summary>C1: Rimconemy_Coal ThingDef exists and is valid.</summary>
        private static void TestC1_CoalDefExists()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Coal");
            AssertTrue(def != null, "C1.Rimconemy_Coal ThingDef is loaded");
            if (def != null)
            {
                AssertTrue(def.stackLimit > 0, "C1.Coal has valid stackLimit");
                AssertTrue(def.thingCategories != null && def.thingCategories.Exists(c => c.defName == "Rimconemy_GeneratorInputs"),
                    "C1.Coal has GeneratorInputs category");
            }
        }

        /// <summary>C2: Rimconemy_MachineParts ThingDef exists and is valid.</summary>
        private static void TestC2_MachinePartsDefExists()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_MachineParts");
            AssertTrue(def != null, "C2.Rimconemy_MachineParts ThingDef is loaded");
            if (def != null)
            {
                AssertTrue(def.stackLimit > 0, "C2.MachineParts has valid stackLimit");
                AssertTrue(def.thingCategories != null && def.thingCategories.Exists(c => c.defName == "Rimconemy_Scraps"),
                    "C2.MachineParts has Scraps category");
            }
        }

        /// <summary>C3: Rimconemy_CraftingStations ThingCategoryDef exists.</summary>
        private static void TestC3_CraftingStationsCategoryExists()
        {
            var def = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Rimconemy_CraftingStations");
            AssertTrue(def != null, "C3.Rimconemy_CraftingStations ThingCategoryDef is loaded");
        }

        /// <summary>C4: Rimconemy_MakeCoal RecipeDef exists.</summary>
        private static void TestC4_MakeCoalRecipeExists()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_MakeCoal");
            AssertTrue(recipe != null, "C4.Rimconemy_MakeCoal RecipeDef is loaded");
            if (recipe != null)
            {
                AssertTrue(recipe.products != null && recipe.products.Count > 0,
                    "C4.MakeCoal has products");
                bool producesCoal = recipe.products.Exists(p => 
                    p.thingDef != null && p.thingDef.defName == "Rimconemy_Coal" && p.count == 4);
                AssertTrue(producesCoal, "C4.MakeCoal produces 4 Rimconemy_Coal");
            }
        }

        /// <summary>C5: Rimconemy_SalvageMachineParts RecipeDef exists.</summary>
        private static void TestC5_SalvageMachinePartsRecipeExists()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_SalvageMachineParts");
            AssertTrue(recipe != null, "C5.Rimconemy_SalvageMachineParts RecipeDef is loaded");
            if (recipe != null)
            {
                AssertTrue(recipe.products != null && recipe.products.Count > 0,
                    "C5.SalvageMachineParts has products");
                bool producesParts = recipe.products.Exists(p => 
                    p.thingDef != null && p.thingDef.defName == "Rimconemy_MachineParts" && p.count == 1);
                AssertTrue(producesParts, "C5.SalvageMachineParts produces 1 Rimconemy_MachineParts");
            }
        }

        /// <summary>C6: Rimconemy_BurnSteelScraps RecipeDef still wired.</summary>
        private static void TestC6_BurnSteelScrapsRecipeWired()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_BurnSteelScraps");
            AssertTrue(recipe != null, "C6.Rimconemy_BurnSteelScraps RecipeDef is loaded");
            if (recipe != null)
            {
                bool producesSteel = recipe.products.Exists(p => 
                    p.thingDef != null && p.thingDef.defName == "Steel" && p.count == 2);
                AssertTrue(producesSteel, "C6.BurnSteelScraps produces 2 Steel");
            }
        }

        /// <summary>C7: Campfire has all 3 recipes wired.</summary>
        private static void TestC7_CampfireHasAllRecipes()
        {
            var campfire = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Campfire");
            AssertTrue(campfire != null, "C7.Rimconemy_Campfire ThingDef is loaded");
            if (campfire != null)
            {
                AssertTrue(campfire.recipes != null && campfire.recipes.Count == 3,
                    "C7.Campfire has exactly 3 recipes");
                bool hasBurn = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_BurnSteelScraps");
                bool hasMake = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_MakeCoal");
                bool hasSalvage = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_SalvageMachineParts");
                AssertTrue(hasBurn, "C7.Campfire has BurnSteelScraps");
                AssertTrue(hasMake, "C7.Campfire has MakeCoal");
                AssertTrue(hasSalvage, "C7.Campfire has SalvageMachineParts");
            }
        }

        /// <summary>C8: WoodCoalGenerator has Refuelable comps for fuels.</summary>
        private static void TestC8_GeneratorHasTwoRefuelables()
        {
            var gen = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_WoodCoalGenerator");
            AssertTrue(gen != null, "C8.Rimconemy_WoodCoalGenerator ThingDef is loaded");
            if (gen != null && gen.comps != null)
            {
                var refuelables = new List<CompProperties_Refuelable>();
                foreach (var comp in gen.comps)
                {
                    if (comp is CompProperties_Refuelable r)
                        refuelables.Add(r);
                }
                AssertTrue(refuelables.Count == 2,
                    "C8.Generator has exactly 2 Refuelable comps");

                // Check for WoodLog/Chemfuel Refuelable and its documented rate.
                var woodRefuel = refuelables.Find(r => r.fuelFilter != null
                    && r.fuelFilter.AllowedThingDefs.Any(d => d.defName == "WoodLog")
                    && r.fuelFilter.AllowedThingDefs.Any(d => d.defName == "Chemfuel"));
                AssertTrue(woodRefuel != null, "C8.Generator has WoodLog/Chemfuel Refuelable");
                if (woodRefuel != null)
                    AssertTrue(System.Math.Abs(woodRefuel.fuelConsumptionRate - 1.0f) < 0.001f,
                        "C8.WoodLog/Chemfuel Refuelable has rate 1.0");

                // Check for Coal Refuelable and its documented efficiency.
                var coalRefuel = refuelables.Find(r => r.fuelFilter != null
                    && r.fuelFilter.AllowedThingDefs.Any(d => d.defName == "Rimconemy_Coal"));
                AssertTrue(coalRefuel != null, "C8.Generator has Coal Refuelable");
                if (coalRefuel != null)
                    AssertTrue(System.Math.Abs(coalRefuel.fuelConsumptionRate - 0.67f) < 0.001f,
                        "C8.Coal Refuelable has rate 0.67");
            }
        }

        /// <summary>C9: Coal category is Rimconemy_GeneratorInputs.</summary>
        private static void TestC9_CoalCategory()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Coal");
            if (def != null && def.thingCategories != null)
            {
                bool hasCat = def.thingCategories.Exists(c => c.defName == "Rimconemy_GeneratorInputs");
                AssertTrue(hasCat, "C9.Coal has GeneratorInputs category");
            }
            else
            {
                AssertTrue(false, "C9.Coal category check failed (def or categories null)");
            }
        }

        /// <summary>C10: MachineParts category is Rimconemy_Scraps.</summary>
        private static void TestC10_MachinePartsCategory()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_MachineParts");
            if (def != null && def.thingCategories != null)
            {
                bool hasCat = def.thingCategories.Exists(c => c.defName == "Rimconemy_Scraps");
                AssertTrue(hasCat, "C10.MachineParts has Scraps category");
            }
            else
            {
                AssertTrue(false, "C10.MachineParts category check failed (def or categories null)");
            }
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[CoalChainRegression] " + label); }
        }
    }
}