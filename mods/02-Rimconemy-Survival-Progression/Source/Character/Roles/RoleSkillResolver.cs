using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Roles
{
    /// <summary>
    /// Resolves the role skills that Rimconemy presents on top of vanilla
    /// skills. The source skills remain vanilla and save-compatible; role
    /// levels are deterministic derived read models, not duplicate SkillDefs.
    /// </summary>
    public static class RoleSkillResolver
    {
        public const int MaxSkill = 20;

        public static float FarmingGrowthFactor(int plantsSkill)
        {
            return LinearFactor(plantsSkill, 1f, 1.5f);
        }

        /// <summary>Hunting is a weighted blend: shooting matters most, animal knowledge remains relevant.</summary>
        public static int HuntingLevelFromSkills(int shooting, int animals)
        {
            return WeightedLevel(shooting, animals, 0.70f, 0.30f);
        }

        /// <summary>Smithing combines practical Crafting with Artistic design.</summary>
        public static int SmithingLevelFromSkills(int crafting, int artistic)
        {
            return WeightedLevel(crafting, artistic, 0.70f, 0.30f);
        }

        public static float IntellectualExperienceFactor(int intellectual)
        {
            return LinearFactor(intellectual, 1f, 1.30f);
        }

        /// <summary>
        /// Applies the Intelligence role to Rimconemy's own progression XP.
        /// Vanilla skill XP also consumes GlobalLearningFactor through the
        /// StatPart; this helper keeps the package-owned XP ledger consistent.
        /// </summary>
        public static float ScaleExperience(float baseExperience, int intellectual)
        {
            if (baseExperience <= 0f) return 0f;
            return baseExperience * IntellectualExperienceFactor(intellectual);
        }

        public static float CookingBuffChance(int cooking)
        {
            int level = Mathf.Clamp(cooking, 0, MaxSkill);
            return Mathf.Clamp01(0.10f + level * 0.025f);
        }

        public static float CookingPenaltyChance(int cooking)
        {
            int level = Mathf.Clamp(cooking, 0, MaxSkill);
            if (level > 4) return 0f;
            return Mathf.Clamp01(0.50f - level * 0.10f);
        }

        public static int SkillOf(Pawn pawn, SkillDef skillDef)
        {
            if (pawn?.skills == null || skillDef == null) return 0;
            SkillRecord record = pawn.skills.GetSkill(skillDef);
            return record == null ? 0 : record.Level;
        }

        public static int HuntingLevel(Pawn pawn)
        {
            return HuntingLevelFromSkills(
                SkillOf(pawn, SkillDefOf.Shooting),
                SkillOf(pawn, SkillDefOf.Animals));
        }

        public static int SmithingLevel(Pawn pawn)
        {
            return SmithingLevelFromSkills(
                SkillOf(pawn, SkillDefOf.Crafting),
                SkillOf(pawn, SkillDefOf.Artistic));
        }

        private static int WeightedLevel(int first, int second, float firstWeight, float secondWeight)
        {
            float a = Mathf.Clamp(first, 0, MaxSkill);
            float b = Mathf.Clamp(second, 0, MaxSkill);
            return Mathf.Clamp(Mathf.RoundToInt(a * firstWeight + b * secondWeight), 0, MaxSkill);
        }

        private static float LinearFactor(int skill, float min, float max)
        {
            int level = Mathf.Clamp(skill, 0, MaxSkill);
            return min + (max - min) * (level / (float)MaxSkill);
        }
    }
}
