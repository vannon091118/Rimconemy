using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Setting Rule: Transparency (H3 §3).
    ///
    /// Computes the active Transparency thought for a pawn:
    ///   - Stage 0 = InformedDecision (positive), if last decision explained.
    ///   - Stage N = UnexplainedDecision, where N = consecutive-unexplained
    ///     count (clamped to ThoughtDef stage range).
    ///
    /// Worker is invoked per pawn each day by the vanilla thought evaluator.
    /// Reads <see cref="TransparencyTracker"/> via the singleton accessor;
    /// never throws when Current.Game == null (returns Inactive).
    ///
    /// Specification: docs/H3-ideology-influence-matrix.md §3.
    /// </summary>
    public class ThoughtWorker_Transparency : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Apply common guards first so we never feed the tracker a wrong pawn.
            if (pawn == null || pawn.Dead) return ThoughtState.Inactive;
            if (!pawn.IsColonist || !pawn.IsFreeNonSlaveColonist) return ThoughtState.Inactive;
            if (!pawn.ageTracker.Adult) return ThoughtState.Inactive;

            var tracker = TransparencyTracker.Get();
            if (tracker == null) return ThoughtState.Inactive;
            tracker.Aggregate(out int total, out int explained, out float trust);
            if (total == 0) return ThoughtState.Inactive; // no decisions yet

            int unexplainedCount = tracker.GetConsecutiveUnexplained(pawn);

            // Informed: if the LAST decision was explained, this pawn sees
            // a +2 buff for one day. We detect this via the consecutive
            // counter being zero.
            if (unexplainedCount == 0 && explained > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            }

            // Unexplained: stage from consecutive count, clamped to range.
            if (ThoughtDefs_Transparency.UnexplainedDecision == null) return ThoughtState.Inactive;
            int stageCount = ThoughtDefs_Transparency.StageCount;
            int stage = System.Math.Min(unexplainedCount, stageCount - 1);
            return ThoughtState.ActiveAtStage(stage);
        }
    }
}
