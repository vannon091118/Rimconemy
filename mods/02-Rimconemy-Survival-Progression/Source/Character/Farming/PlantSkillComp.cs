using System.Collections.Generic;
using HarmonyLib;
using Rimconemy.SurvivalProgression.Character.Roles;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimconemy.SurvivalProgression.Character.Farming
{
    public sealed class CompProperties_PlanterSkill : CompProperties
    {
        public CompProperties_PlanterSkill()
        {
            compClass = typeof(CompPlanterSkill);
        }
    }

    public sealed class CompPlanterSkill : ThingComp
    {
        private int planterSkill;

        public int PlanterSkill => planterSkill;

        public void SetPlanterSkill(int skill)
        {
            planterSkill = UnityEngine.Mathf.Clamp(skill, 0, RoleSkillResolver.MaxSkill);
        }

        public float GrowthFactor => planterSkill <= 0 ? 1f : RoleSkillResolver.FarmingGrowthFactor(planterSkill);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref planterSkill, "planterPlantsSkill", 0);
        }

        public override string CompInspectStringExtra()
        {
            if (planterSkill <= 0) return null;
            return "Rimconemy.Farming.PlantedBy".Translate(planterSkill, GrowthFactor.ToStringPercent());
        }
    }

    // MakeNewToils is protected in RimWorld 1.6; use Harmony's string
    // resolver instead of taking a compile-time reference to the protected member.
    [HarmonyPatch(typeof(JobDriver_PlantSow), "MakeNewToils")]
    public static class PlantSowSkillPatch
    {
        [HarmonyPostfix]
        public static void AddPlanterSkill(ref IEnumerable<Toil> __result, JobDriver_PlantSow __instance)
        {
            if (__result == null || __instance == null) return;

            // JobDriver.Map and JobDriver.TargetA are protected in 1.6. Read
            // the same state through the public actor/job surface and wrap the
            // iterator without eagerly evaluating MakeNewToils().
            Pawn planter = __instance.GetActor();
            Job job = planter?.CurJob;
            Map map = planter?.Map;
            IntVec3 cell = job == null ? IntVec3.Invalid : job.GetTarget(TargetIndex.A).Cell;
            if (planter == null || map == null || !cell.InBounds(map)) return;

            __result = AttachRecordAction(__result, map, cell, planter);
        }

        private static IEnumerable<Toil> AttachRecordAction(IEnumerable<Toil> source, Map map, IntVec3 cell, Pawn planter)
        {
            // The vanilla toil sequence is an implementation detail. Attach
            // an idempotent probe to every toil instead of assuming the last
            // toil is the sow action; the first finish after the plant exists
            // records the planter, and later probes become no-ops.
            foreach (Toil toil in source)
            {
                if (toil != null)
                    toil.AddFinishAction(() => RecordPlanterSkill(map, cell, planter));
                yield return toil;
            }
        }

        private static void RecordPlanterSkill(Map map, IntVec3 cell, Pawn planter)
        {
            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            if (things == null) return;
            for (int i = 0; i < things.Count; i++)
            {
                Plant plant = things[i] as Plant;
                if (plant == null) continue;
                CompPlanterSkill comp = plant.GetComp<CompPlanterSkill>();
                if (comp == null) return;
                comp.SetPlanterSkill(RoleSkillResolver.SkillOf(planter, SkillDefOf.Plants));
                return;
            }
        }
    }

    [HarmonyPatch(typeof(Plant), "get_GrowthRate")]
    public static class PlantGrowthSkillPatch
    {
        [HarmonyPostfix]
        public static void ApplyPlanterSkill(Plant __instance, ref float __result)
        {
            if (__instance == null || __result <= 0f) return;
            CompPlanterSkill comp = __instance.GetComp<CompPlanterSkill>();
            if (comp == null || comp.PlanterSkill <= 0) return;
            __result *= comp.GrowthFactor;
        }
    }
}
