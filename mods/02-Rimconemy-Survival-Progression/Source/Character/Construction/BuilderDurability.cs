using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Construction
{
    /// <summary>
    /// Construction skill is no longer a second speed/efficiency layer.
    /// The builder's skill is recorded on the finished building and increases
    /// its actual MaxHitPoints: level 1 = 1.00x, level 20 = 1.50x.
    /// </summary>
    public static class BuilderDurability
    {
        public const int MaxSkill = 20;
        public const float MaxBonus = 0.50f;

        public static float HitPointFactor(int constructionSkill)
        {
            int level = Mathf.Clamp(constructionSkill, 1, MaxSkill);
            return 1f + (level - 1) * (MaxBonus / (MaxSkill - 1));
        }

        public static void ApplyToBuilding(Building building, Pawn builder)
        {
            if (building == null || builder?.skills == null) return;
            SkillRecord skill = builder.skills.GetSkill(SkillDefOf.Construction);
            if (skill == null) return;

            CompBuilderDurability comp = building.GetComp<CompBuilderDurability>();
            if (comp == null) return;
            comp.SetBuilderSkill(skill.Level);

            // The durability factor raises MaxHitPoints; top the building up
            // to its new maximum so it reads as undamaged.
            int maxHitPoints = building.MaxHitPoints;
            if (maxHitPoints > 0)
                building.HitPoints = maxHitPoints;
        }
    }

    public sealed class CompProperties_BuilderDurability : CompProperties
    {
        public CompProperties_BuilderDurability()
        {
            compClass = typeof(CompBuilderDurability);
        }
    }

    /// <summary>Persistent construction provenance and MaxHitPoints modifier.</summary>
    public sealed class CompBuilderDurability : ThingComp
    {
        private int builderSkill;

        public int BuilderSkill => builderSkill;

        public void SetBuilderSkill(int skill)
        {
            builderSkill = Mathf.Clamp(skill, 1, BuilderDurability.MaxSkill);
        }

        public override float GetStatFactor(StatDef stat)
        {
            if (stat == StatDefOf.MaxHitPoints && builderSkill > 0)
                return BuilderDurability.HitPointFactor(builderSkill);
            return 1f;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref builderSkill, "builderConstructionSkill", 0);
        }

        public override string CompInspectStringExtra()
        {
            if (builderSkill <= 0) return null;
            return "Rimconemy.BuilderDurability.Inspect".Translate(builderSkill, (BuilderDurability.HitPointFactor(builderSkill) - 1f).ToStringPercent());
        }
    }
}
