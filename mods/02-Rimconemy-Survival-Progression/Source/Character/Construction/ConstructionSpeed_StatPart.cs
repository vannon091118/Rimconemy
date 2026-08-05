using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Construction
{
    /// <summary>
    /// Owner: Survival & Progression (Package 02).
    /// DECISIONS §30 — Two-Layer Construction-Speed-Curve (Interpretation B).
    ///
    /// Layer A — Skill-Kurve: skill 1 ≈ 0,5×, skill 20 ≈ 2,5× (linear).
    /// Layer B — separates Effizienz-Modell: konstanter +50 %-Bonus
    ///          (`BuilderEfficiencyMultiplier`), nicht an Skill gekoppelt.
    ///
    /// Beide Layer sind reine Multiplikatoren; Vanilla `WorkSpeedGlobal`,
    /// Trait- und Health-Boni multiplizieren weiterhin ohne Kollision.
    ///
    /// Wiring: Das StatPart wird via XML an `StatDef.ConstructionSpeed.parts`
    /// angehängt (siehe `Patches/StatDef_ConstructionSpeed_RimconemyParts.xml`).
    /// Die Reihenfolge ist die vanilla-Multiplikations-Reihenfolge:
    ///   base × Layer A × Layer B × WorkSpeedGlobal × ...
    /// </summary>
    public class ConstructionSpeed_StatPart : StatPart
    {
        // Owner-tunable Layer-B Effizienz. Default 1.5 = +50 %.
        public const float DefaultBuilderEfficiencyMultiplier = 1.5f;

        public override void TransformValue(StatRequest req, ref float val)
        {
            try
            {
                if (req == null || req.Thing == null) return;
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null || pawn.skills == null) return;
                SkillRecord construction = pawn.skills.GetSkill(SkillDefOf.Construction);
                if (construction == null) return;

                float layerA = SkillCurveFactor(construction.Level);
                float layerB = DefaultBuilderEfficiencyMultiplier;
                val *= layerA * layerB;
            }
            catch (System.Exception ex)
            {
                // Defensive: ein defekter StatPart darf die Stat-Pipeline
                // nicht reißen; vanilla Inspector erfasst diesen Log-Eintrag.
                Log.Warning($"[Rimconemy.SurvivalProgression] ConstructionSpeed_StatPart.TransformValue: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            try
            {
                if (req == null || req.Thing == null) return null;
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null || pawn.skills == null) return null;
                SkillRecord construction = pawn.skills.GetSkill(SkillDefOf.Construction);
                if (construction == null) return null;
                float layerA = SkillCurveFactor(construction.Level);
                float layerB = DefaultBuilderEfficiencyMultiplier;
                return $"Rimconemy (Skill ×{layerA:0.00}, Efficiency ×{layerB:0.00})";
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lineare Interpolation: skill 1 → 0,5x, skill 20 → 2,5x.
        /// Außerhalb des Bereichs wird klemmend gehalten (kein &lt; 0,5 und
        /// kein &gt; 2,5). Skill 0 (sehr junger Survivor ohne Skillpunkte)
        /// wird als 0,5x bewertet, um Hardcap-Bugs zu vermeiden.
        /// </summary>
        public static float SkillCurveFactor(int skillLevel)
        {
            const float minFactor = 0.5f;
            const float maxFactor = 2.5f;
            const int maxSkill = 20;
            if (skillLevel <= 1) return minFactor;
            if (skillLevel >= maxSkill) return maxFactor;
            float t = (float)(skillLevel - 1) / (maxSkill - 1);
            return minFactor + t * (maxFactor - minFactor);
        }
    }
}
