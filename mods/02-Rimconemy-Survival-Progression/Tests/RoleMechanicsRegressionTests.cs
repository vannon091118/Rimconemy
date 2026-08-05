using System;
using Rimconemy.SurvivalProgression.Character.Construction;
using Rimconemy.SurvivalProgression.Character.Cooking;
using Rimconemy.SurvivalProgression.Character.Farming;
using Rimconemy.SurvivalProgression.Character.Roles;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    public static class RoleMechanicsRegressionTests
    {
        public static int RunAll()
        {
            int failures = 0;
            Check(ref failures, Math.Abs(BuilderDurability.HitPointFactor(1) - 1f) < 0.001f, "Builder HP skill 1 = 1.00");
            Check(ref failures, Math.Abs(BuilderDurability.HitPointFactor(20) - 1.5f) < 0.001f, "Builder HP skill 20 = 1.50");
            Check(ref failures, Math.Abs(BuilderDurability.HitPointFactor(10) - 1.2368422f) < 0.001f, "Builder HP curve is linear at skill 10");
            Check(ref failures, RoleSkillResolver.FarmingGrowthFactor(20) <= 1.5f, "Farming growth capped at 1.50");
            Check(ref failures, RoleSkillResolver.HuntingLevelFromSkills(20, 0) == 14, "Hunting weighted from Shooting");
            Check(ref failures, RoleSkillResolver.HuntingLevelFromSkills(0, 20) == 6, "Hunting weighted from Animals");
            Check(ref failures, RoleSkillResolver.SmithingLevelFromSkills(20, 20) == 20, "Smithing merges Crafting and Artistic");
            Check(ref failures, Math.Abs(RoleSkillResolver.IntellectualExperienceFactor(20) - 1.3f) < 0.001f, "Intellectual learning capped at 1.30");
            Check(ref failures, Math.Abs(RoleSkillResolver.ScaleExperience(10f, 20) - 13f) < 0.001f, "Intellectual scales package XP");
            Check(ref failures, RoleSkillResolver.CookingPenaltyChance(1) > RoleSkillResolver.CookingPenaltyChance(4), "Low Cooking penalty declines with skill");
            Check(ref failures, RoleSkillResolver.CookingBuffChance(20) > RoleSkillResolver.CookingBuffChance(1), "Cooking buff chance rises with skill");
            Check(ref failures, HasComp<ThingDef>("Wall", typeof(CompProperties_BuilderDurability)), "Building comp inherited by Wall");
            Check(ref failures, HasComp<ThingDef>("Plant_Potato", typeof(CompProperties_PlanterSkill)), "Planter comp inherited by crop");
            // D2-Harmonisierung: CompProperties_CookSkill wird via XML-Patch
            // an MealCookedIngredientless (Parent aller gekochten Mahlzeiten)
            // UND individuell an MealSimple/MealFine/MealLavish/MealSurvivalPack
            // gehängt (RimWorld merged comps nur, wenn das Kind KEINEN eigenen
            // <comps>-Block definiert — andernfalls überschreibt der Kind-Block).
            Check(ref failures, HasComp<ThingDef>("MealSimple", typeof(CompProperties_CookSkill)), "Cook comp inherited by MealSimple");
            Check(ref failures, HasComp<ThingDef>("MealFine", typeof(CompProperties_CookSkill)), "Cook comp inherited by MealFine");
            Log.Message("[Rimconemy.SurvivalProgression] Role mechanics regression tests: " + (15 - failures) + " passed, " + failures + " failed");
            return failures;
        }

        private static bool HasComp<T>(string defName, Type compType) where T : Def
        {
            T def = DefDatabase<T>.GetNamedSilentFail(defName);
            if (def is ThingDef thingDef)
            {
                return thingDef.comps != null && thingDef.comps.Exists(comp => comp != null && comp.GetType() == compType);
            }
            return false;
        }

        private static void Check(ref int failures, bool condition, string label)
        {
            if (condition) return;
            failures++;
            Log.Error("[Rimconemy.SurvivalProgression] RoleMechanics FAIL: " + label);
        }
    }
}
