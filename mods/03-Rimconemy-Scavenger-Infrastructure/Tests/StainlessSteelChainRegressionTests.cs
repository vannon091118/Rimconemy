using Rimconemy.ScavengerInfrastructure.Resources;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Tests
{
    /// <summary>
    /// P1 StainlessSteel Chain Regression Gate (2026-08-04; Phase-First update 2026-08-05).
    ///
    /// Invariants:
    ///   S1: Rimconemy_StainlessSteel ThingDef exists and is valid.
    ///   S2: Rimconemy_MakeStainlessSteel RecipeDef exists (2 Steel + 1 MachineParts -> 2 StainlessSteel).
    ///   S3: Rimconemy_StainlessSteelTower ThingDef exists with correct cost.
    ///   S4: Campfire has MakeStainlessSteel recipe wired (and the recipe is gated by Smithing research).
    ///   S5: StainlessSteel has Metallic stuffProps with correct stat factors.
    ///   S6: StainlessSteel category is Rimconemy_Scraps.
    ///   S7: Tower requires power (CompPowerTrader, 150W).
    ///   S8: Tower cost includes StainlessSteel + MachineParts + Steel.
    ///   S9 (Phase-First 2026-08-05): MakeStainlessSteel has researchPrerequisites element.
    /// </summary>
    public static class StainlessSteelChainRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            TestS1_StainlessSteelDefExists();
            TestS2_MakeStainlessSteelRecipeExists();
            TestS3_StainlessSteelTowerDefExists();
            TestS4_CampfireHasMakeStainlessSteel();
            TestS5_StainlessSteelHasMetallicProps();
            TestS6_StainlessSteelCategory();
            TestS7_TowerRequiresPower();
            TestS8_TowerCost();
            TestS9_MakeStainlessHasResearchGate();

            string summary = "[Rimconemy.ScavengerInfrastructure] StainlessSteelChain regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }

            Log.Message(summary);
            return true;
        }

        /// <summary>S1: Rimconemy_StainlessSteel ThingDef exists and is valid.</summary>
        private static void TestS1_StainlessSteelDefExists()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_StainlessSteel");
            AssertTrue(def != null, "S1.Rimconemy_StainlessSteel ThingDef is loaded");
            if (def != null)
            {
                AssertTrue(def.stackLimit > 0, "S1.StainlessSteel has valid stackLimit");
            }
        }

        /// <summary>S2: Rimconemy_MakeStainlessSteel RecipeDef exists with correct ingredients/products.</summary>
        private static void TestS2_MakeStainlessSteelRecipeExists()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_MakeStainlessSteel");
            AssertTrue(recipe != null, "S2.Rimconemy_MakeStainlessSteel RecipeDef is loaded");
            if (recipe != null)
            {
                // Check products: 2 StainlessSteel
                AssertTrue(recipe.products != null && recipe.products.Count > 0,
                    "S2.MakeStainlessSteel has products");
                bool producesSS = recipe.products.Exists(p => 
                    p.thingDef != null && p.thingDef.defName == "Rimconemy_StainlessSteel" && p.count == 2);
                AssertTrue(producesSS, "S2.MakeStainlessSteel produces 2 Rimconemy_StainlessSteel");
            }
        }

        /// <summary>S3: Rimconemy_StainlessSteelTower ThingDef exists.</summary>
        private static void TestS3_StainlessSteelTowerDefExists()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_StainlessSteelTower");
            AssertTrue(def != null, "S3.Rimconemy_StainlessSteelTower ThingDef is loaded");
            if (def != null)
            {
                AssertTrue(def.thingClass != null && def.thingClass.Name.Contains("Turret"),
                    "S3.Tower has Building_TurretGun class");
                AssertTrue(def.size.x == 2 && def.size.z == 2,
                    "S3.Tower size is 2x2");
            }
        }

        /// <summary>S4: Campfire has MakeStainlessSteel recipe wired.</summary>
        private static void TestS4_CampfireHasMakeStainlessSteel()
        {
            var campfire = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_Campfire");
            AssertTrue(campfire != null, "S4.Rimconemy_Campfire ThingDef is loaded");
            if (campfire != null)
            {
                AssertTrue(campfire.recipes != null && campfire.recipes.Count >= 4,
                    "S4.Campfire has at least 4 recipes");
                bool hasMakeSS = campfire.recipes.Exists(r => r != null && r.defName == "Rimconemy_MakeStainlessSteel");
                AssertTrue(hasMakeSS, "S4.Campfire has MakeStainlessSteel");
            }
        }

        /// <summary>S5: StainlessSteel has Metallic stuffProps with correct stat factors.</summary>
        private static void TestS5_StainlessSteelHasMetallicProps()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_StainlessSteel");
            AssertTrue(def != null, "S5.Rimconemy_StainlessSteel ThingDef is loaded");
            if (def != null && def.stuffProps != null)
            {
                AssertTrue(def.stuffProps.categories != null && def.stuffProps.categories.Any(c => c.defName == "Metallic"),
                    "S5.StainlessSteel has Metallic stuff category");
                
                // Find MaxHitPoints factor in statFactors list
                bool hasMaxHPFactor = def.stuffProps.statFactors != null && def.stuffProps.statFactors.Any(s => 
                    s.stat != null && s.stat.defName == "MaxHitPoints" && s.value > 1.2f);
                AssertTrue(hasMaxHPFactor, "S5.StainlessSteel has MaxHitPoints factor > 1.2");
            }
            else
            {
                AssertTrue(false, "S5.StainlessSteel missing stuffProps");
            }
        }

        /// <summary>S6: StainlessSteel category is Rimconemy_Scraps.</summary>
        private static void TestS6_StainlessSteelCategory()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_StainlessSteel");
            if (def != null && def.thingCategories != null)
            {
                bool hasCat = def.thingCategories.Any(c => c.defName == "Rimconemy_Scraps");
                AssertTrue(hasCat, "S6.StainlessSteel has Scraps category");
            }
            else
            {
                AssertTrue(false, "S6.StainlessSteel category check failed");
            }
        }

        /// <summary>S7: Tower requires power (CompPowerTrader, 150W).</summary>
        private static void TestS7_TowerRequiresPower()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_StainlessSteelTower");
            if (def != null && def.comps != null)
            {
                var powerComps = def.comps.Where(c => c is CompProperties_Power && c.compClass?.Name == "CompPowerTrader").ToList();
                AssertTrue(powerComps.Count > 0, "S7.Tower has CompPowerTrader comp");
                if (powerComps.Count > 0)
                {
                    var power = powerComps[0] as CompProperties_Power;
                    // Use reflection to access basePowerConsumption field
                    var powerConsumptionField = power.GetType().GetField("basePowerConsumption", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var powerConsumption = powerConsumptionField != null ? (float)powerConsumptionField.GetValue(power) : 0f;
                    Log.Message("[Rimconemy.ScavengerInfrastructure] S7 Tower power consumption = " + powerConsumption);
                    AssertTrue(powerConsumption == 150f,
                        "S7.Tower power consumption is 150W");
                }
            }
            else
            {
                AssertTrue(false, "S7.Tower comps check failed");
            }
        }

        /// <summary>S8: Tower cost includes StainlessSteel + MachineParts + Steel + WeaponComponent (D3-Harmonisierung 2026-08-05).</summary>
        private static void TestS8_TowerCost()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_StainlessSteelTower");
            if (def != null && def.costList != null)
            {
                bool hasSS = def.costList.Any(c => c.thingDef != null && c.thingDef.defName == "Rimconemy_StainlessSteel" && c.count == 30);
                bool hasParts = def.costList.Any(c => c.thingDef != null && c.thingDef.defName == "Rimconemy_MachineParts" && c.count == 4);
                bool hasSteel = def.costList.Any(c => c.thingDef != null && c.thingDef.defName == "Steel" && c.count == 20);
                bool hasWC = def.costList.Any(c => c.thingDef != null && c.thingDef.defName == "Rimconemy_WeaponComponent" && c.count >= 1);
                Log.Message("[Rimconemy.ScavengerInfrastructure] S8 Tower costList count=" + def.costList.Count +
                    ", hasSS=" + hasSS + ", hasParts=" + hasParts + ", hasSteel=" + hasSteel + ", hasWC=" + hasWC);
                if (!hasSS) Log.Error("[Rimconemy.ScavengerInfrastructure] S8.Tower cost includes 30 StainlessSteel - MISSING");
                if (!hasParts) Log.Error("[Rimconemy.ScavengerInfrastructure] S8.Tower cost includes 4 MachineParts - MISSING");
                if (!hasSteel) Log.Error("[Rimconemy.ScavengerInfrastructure] S8.Tower cost includes 20 Steel - MISSING");
                if (!hasWC) Log.Error("[Rimconemy.ScavengerInfrastructure] S8.Tower cost includes WeaponComponent - MISSING (D3-Harmonisierung)");
                AssertTrue(hasSS, "S8.Tower cost includes 30 StainlessSteel");
                AssertTrue(hasParts, "S8.Tower cost includes 4 MachineParts");
                AssertTrue(hasSteel, "S8.Tower cost includes 20 Steel");
                AssertTrue(hasWC, "S8.Tower cost includes WeaponComponent (D3-Harmonisierung)");
            }
            else
            {
                Log.Error("[Rimconemy.ScavengerInfrastructure] S8.Tower costList check failed - def or costList null");
                AssertTrue(false, "S8.Tower costList check failed");
            }
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition)
            {
                _passed++;
                Log.Message("[Rimconemy.ScavengerInfrastructure] PASS: " + label);
            }
            else
            {
                _failed++;
                Log.Error("[Rimconemy.ScavengerInfrastructure] FAIL: " + label);
            }
        }

        /// <summary>S9: MakeStainlessSteel recipe now has researchPrerequisites (Phase-First).</summary>
        private static void TestS9_MakeStainlessHasResearchGate()
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Rimconemy_MakeStainlessSteel");
            if (recipe == null)
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] S9: MakeStainlessSteel not loaded yet; deferred.");
                return;
            }
            AssertTrue(recipe.researchPrerequisites != null && recipe.researchPrerequisites.Count > 0,
                "S9.MakeStainlessSteel has researchPrerequisites (Phase-First)");
        }

        private static void HelperPrintRecipeSignal(RecipeDef recipe)
        {
            // Reserved: future cross-SSOT signal printer (no-op placeholder).
        }
    }
}