using Rimconemy.ScavengerInfrastructure.Resources;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.ScavengerInfrastructure.Tests
{
    /// <summary>
    /// Phase-3.11 (2026-08-04): Regression gate for campfire/scraps loop
    /// and Woody removal from Wall/Door.
    ///
    /// Invariants:
    ///   I1: SteelScraps resource category exists and is in ResourceCategory.All.
    ///   I2: Campfire ThingDef is loadable (defName Rimconemy_Campfire).
    ///   I3: BurnSteelScraps recipe exists (defName Rimconemy_BurnSteelScraps).
    ///   I4: Woody is NOT in Wall stuffCategories (defDatabase or patch-file check).
    ///   I5: Woody is NOT in Door stuffCategories (defDatabase or patch-file check).
    ///   I6: Stony IS in Wall stuffCategories.
    ///   I7: Stony IS in Door stuffCategories.
    ///   I8: Rimconemy_SteelScraps ThingDef has valid stackLimit.
    ///   I9: Recipe produces Steel (output product check).
    ///   I10: Campfire building has the recipe wired (recipes list contains it).
    /// </summary>
    public static class CampfireScrapsRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("ScavengerInfrastructure", "CampfireScraps regression tests");

            _passed = 0;
            _failed = 0;

            TestI1_SteelScrapsCategoryExists();
            TestI2_CampfireDefLoadable();
            TestI3_BurnRecipeExists();
            TestI4_WoodyNotInWall();
            TestI5_WoodyNotInDoor();
            TestI6_StonyInWall();
            TestI7_StonyInDoor();
            TestI8_SteelScrapsDefValid();
            TestI9_RecipeProducesSteel();
            TestI10_CampfireHasRecipeWired();

            string summary = "[Rimconemy.ScavengerInfrastructure] CampfireScraps regression tests: "
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

        /// <summary>I1: SteelScraps resource category exists.</summary>
        private static void TestI1_SteelScrapsCategoryExists()
        {
            ts.Check(!string.IsNullOrEmpty(ResourceCategory.SteelScraps), "I1.SteelScraps constant not empty");
            ts.Check(ResourceCategory.All.Contains(ResourceCategory.SteelScraps), "I1.SteelScraps is in ResourceCategory.All");
        }

        /// <summary>I2: Campfire ThingDef is loadable.</summary>
        private static void TestI2_CampfireDefLoadable()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Campfire");
            ts.Check(def != null, "I2.Rimconemy_Campfire ThingDef is loaded");
        }

        /// <summary>I3: BurnSteelScraps recipe exists.</summary>
        private static void TestI3_BurnRecipeExists()
        {
            var def = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_BurnSteelScraps");
            ts.Check(def != null, "I3.Rimconemy_BurnSteelScraps RecipeDef is loaded");
        }

        /// <summary>I4: Woody IS in Wall stuffCategories (D2 — Wood removal reverted, B1).</summary>
        private static void TestI4_WoodyNotInWall()
        {
            var wall = DefDatabase<ThingDef>.GetNamedSilentFail("Wall");
            if (wall == null || wall.stuffCategories == null)
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] CampfireScraps TEST-DEFERRED I4: Wall not loaded or no stuffCategories.");
                return;
            }
            bool hasWoody = wall.stuffCategories.Exists(c => c != null && c.defName == "Woody");
            // D2-Harmonisierung (2026-08-05): Woody-Removal aus Wall/Door/Barricade
            // wurde zurückgenommen. Holz ist wieder Stuff-Material.
            ts.Check(hasWoody, "I4.Woody IS in Wall stuffCategories (D2 revert)");
        }

        /// <summary>I5: Woody IS in Door stuffCategories (D2 — Wood removal reverted, B1).</summary>
        private static void TestI5_WoodyNotInDoor()
        {
            var door = DefDatabase<ThingDef>.GetNamedSilentFail("Door");
            if (door == null || door.stuffCategories == null)
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] CampfireScraps TEST-DEFERRED I5: Door not loaded or no stuffCategories.");
                return;
            }
            bool hasWoody = door.stuffCategories.Exists(c => c != null && c.defName == "Woody");
            ts.Check(hasWoody, "I5.Woody IS in Door stuffCategories (D2 revert)");
        }

        /// <summary>I6: Stony IS in Wall stuffCategories.</summary>
        private static void TestI6_StonyInWall()
        {
            var wall = DefDatabase<ThingDef>.GetNamedSilentFail("Wall");
            if (wall == null || wall.stuffCategories == null)
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] CampfireScraps TEST-DEFERRED I6: Wall not loaded.");
                return;
            }
            bool hasStony = wall.stuffCategories.Exists(c => c != null && c.defName == "Stony");
            ts.Check(hasStony, "I6.Stony IS in Wall stuffCategories");
        }

        /// <summary>I7: Stony IS in Door stuffCategories.</summary>
        private static void TestI7_StonyInDoor()
        {
            var door = DefDatabase<ThingDef>.GetNamedSilentFail("Door");
            if (door == null || door.stuffCategories == null)
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] CampfireScraps TEST-DEFERRED I7: Door not loaded.");
                return;
            }
            bool hasStony = door.stuffCategories.Exists(c => c != null && c.defName == "Stony");
            ts.Check(hasStony, "I7.Stony IS in Door stuffCategories");
        }

        /// <summary>I8: SteelScraps ThingDef is valid.</summary>
        private static void TestI8_SteelScrapsDefValid()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_SteelScraps");
            ts.Check(def != null, "I8.Rimconemy_SteelScraps ThingDef is loaded");
            if (def != null)
                ts.Check(def.stackLimit > 0, "I8.SteelScraps has valid stackLimit");
        }

        /// <summary>I9: BurnSteelScraps recipe produces Steel.</summary>
        private static void TestI9_RecipeProducesSteel()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_BurnSteelScraps");
            if (recipe == null)
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] CampfireScraps TEST-DEFERRED I9: Recipe not loaded.");
                return;
            }
            ts.Check(recipe.products != null && recipe.products.Count > 0, "I9.BurnSteelScraps has products");
            if (recipe.products != null && recipe.products.Count > 0)
            {
                bool producesSteel = recipe.products.Exists(p =>
                    p.thingDef != null && p.thingDef.defName == "Steel");
                ts.Check(producesSteel, "I9.BurnSteelScraps produces Steel");
            }
        }

        /// <summary>I10: Campfire building has the recipe wired in its recipes list.</summary>
        private static void TestI10_CampfireHasRecipeWired()
        {
            var campfire = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Campfire");
            if (campfire == null)
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] CampfireScraps TEST-DEFERRED I10: Campfire not loaded.");
                return;
            }
            ts.Check(campfire.recipes != null, "I10.Campfire has recipes list");
            if (campfire.recipes != null)
            {
                bool hasRecipe = campfire.recipes.Exists(r =>
                    r != null && r.defName == "Rimconemy_BurnSteelScraps");
                ts.Check(hasRecipe, "I10.Campfire recipes list contains BurnSteelScraps");
            }
        }

    }
}
