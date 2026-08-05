using Rimconemy.SurvivalProgression.Character.Roles;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character
{
    public sealed class IntellectualLearning_StatPart : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null) return;
            int skill = RoleSkillResolver.SkillOf(pawn, SkillDefOf.Intellectual);
            val *= RoleSkillResolver.IntellectualExperienceFactor(skill);
        }

        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null) return null;
            int skill = RoleSkillResolver.SkillOf(pawn, SkillDefOf.Intellectual);
            return "Rimconemy.Intellectual.LearningFactor".Translate(skill, RoleSkillResolver.IntellectualExperienceFactor(skill).ToStringPercent());
        }
    }
}
