using System.Collections.Generic;
using HarmonyLib;
using Rimconemy.SurvivalProgression.Character.Roles;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimconemy.SurvivalProgression.Character.Cooking
{
    public sealed class CompProperties_CookSkill : CompProperties
    {
        public CompProperties_CookSkill()
        {
            compClass = typeof(CompCookSkill);
        }
    }

    public sealed class CompCookSkill : ThingComp
    {
        private int cookSkill;
        public int CookSkill => cookSkill;

        public void SetCookSkill(int skill)
        {
            cookSkill = UnityEngine.Mathf.Clamp(skill, 0, RoleSkillResolver.MaxSkill);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref cookSkill, "cookSkill", 0);
        }
    }

    public static class CookingOutcomeResolver
    {
        public const string BuffDefName = "Rimconemy_CookingBuff";
        public const string PenaltyDefName = "Rimconemy_CookingPenalty";
        public const string LowCookingTraitDefName = "Rimconemy_Trait_CookingUnsteady";
        public const int LowCookingTraitThreshold = 2;

        /// <summary>
        /// Low cooking is represented once at setup time by a dedicated
        /// cooking trait; meal outcomes then remain the dynamic layer.
        /// Never reuse a generic work trait or touch vanilla traits.
        /// </summary>
        public static void ApplyStartingRoleTrait(Pawn pawn)
        {
            if (pawn?.skills == null || pawn.story?.traits == null) return;
            int cooking = RoleSkillResolver.SkillOf(pawn, SkillDefOf.Cooking);
            if (cooking > LowCookingTraitThreshold) return;

            TraitDef traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(LowCookingTraitDefName);
            if (traitDef == null || pawn.story.traits.HasTrait(traitDef)) return;
            pawn.story.traits.GainTrait(new Trait(traitDef));
        }

        public static void ApplyMealOutcome(Pawn eater, Thing meal)
        {
            if (meal == null) return;
            CompCookSkill comp = meal.TryGetComp<CompCookSkill>();
            if (comp == null) return;
            ApplyMealOutcome(eater, comp.CookSkill);
        }

        public static void ApplyMealOutcome(Pawn eater, int cookSkill)
        {
            if (eater?.health == null || cookSkill < 0) return;

            int skill = UnityEngine.Mathf.Clamp(cookSkill, 0, RoleSkillResolver.MaxSkill);
            if (skill <= 4 && Rand.Chance(RoleSkillResolver.CookingPenaltyChance(skill)))
            {
                AddFreshHediff(eater, PenaltyDefName);
                return;
            }

            if (Rand.Chance(RoleSkillResolver.CookingBuffChance(skill)))
                AddFreshHediff(eater, BuffDefName);
        }

        private static void AddFreshHediff(Pawn pawn, string defName)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (def == null) return;
            Hediff previous = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (previous != null) pawn.health.RemoveHediff(previous);
            pawn.health.AddHediff(HediffMaker.MakeHediff(def, pawn));
        }
    }

    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class RecipeCookSkillPatch
    {
        [HarmonyPostfix]
        public static void RecordCookSkill(
            ref IEnumerable<Thing> __result,
            RecipeDef recipeDef,
            Pawn worker)
        {
            if (__result == null || worker?.skills == null || recipeDef == null
                || recipeDef.workSkill != SkillDefOf.Cooking)
                return;

            SkillRecord cooking = worker.skills.GetSkill(SkillDefOf.Cooking);
            if (cooking != null)
                __result = TagProducts(__result, cooking.Level);
        }

        private static IEnumerable<Thing> TagProducts(IEnumerable<Thing> products, int cookingSkill)
        {
            // Materialise once. A lazy wrapper can be enumerated by more than
            // one downstream recipe consumer; an eager list preserves the exact
            // product set and tags each meal exactly once.
            var tagged = new List<Thing>();
            foreach (Thing product in products)
            {
                ThingWithComps meal = product as ThingWithComps;
                if (meal?.def?.ingestible != null)
                {
                    CompCookSkill comp = meal.GetComp<CompCookSkill>();
                    if (comp != null) comp.SetCookSkill(cookingSkill);
                }
                tagged.Add(product);
            }
            return tagged;
        }
    }

    [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.FinalizeIngest))]
    public static class MealOutcomePatch
    {
        [HarmonyPostfix]
        public static void ApplyOutcome(ref Toil __result, Pawn ingester, TargetIndex ingestibleInd)
        {
            if (__result == null || ingester == null || ingester.CurJob == null) return;

            // Capture the cook skill while the meal still exists. FinalizeIngest
            // may destroy the last stack before its finish action runs, so the
            // outcome must not depend on looking the meal up again afterwards.
            Thing meal = ingester.CurJob.GetTarget(ingestibleInd).Thing;
            CompCookSkill comp = meal?.TryGetComp<CompCookSkill>();
            if (comp == null) return;
            int cookSkill = comp.CookSkill;
            __result.AddFinishAction(() => CookingOutcomeResolver.ApplyMealOutcome(ingester, cookSkill));
        }
    }
}
