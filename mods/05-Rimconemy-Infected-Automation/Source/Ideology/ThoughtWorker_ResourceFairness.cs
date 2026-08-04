using System.Collections.Generic;
using Rimconemy.Foundation.Colonials;
using Rimconemy.Foundation.Registry;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05)
    /// Setting Rule: ResourceFairness (H3 §1)
    ///
    /// Checks whether food and medicine are fairly distributed among
    /// adult player colonists. Runs every 60,000 ticks (1 day).
    ///
    /// Migration note (S-T4 / I-T4): moved from Package 02 here because
    /// the Setting-Rule semantics belong to the Ideology domain which
    /// Package 05 owns (X4 decision). Mod 02 keeps capability-awareness
    /// so a downstream consumer that explicitly toggles the rule off
    /// still sees the gate fire. Package 02 had a transitional gate;
    /// gate stays here so the Ideology domain owns it.
    ///
    /// Fair: all colonists have similar access to critical resources.
    /// Unfair: some colonists have significantly less than the average.
    ///
    /// Specification: docs/H3-ideology-influence-matrix.md §1
    /// </summary>
    public class ThoughtWorker_ResourceFairness : ThoughtWorker
    {
        // Check once per day
        private const int CheckIntervalTicks = 60000;

        // Thresholds
        private const float FairThreshold = 0.10f;    // within 10% of average = fair
        private const float UnfairThreshold = 0.30f;  // >30% below average = unfair

        // Mood stages (match stage indices in ThoughtDefs).
        // FairDistribution has 1 stage (0: +3 mood).
        // UnfairDistribution has 2 stages (0: -5, 1: -8 cumulative).
        // MVP: cumulative stage 1 is not yet evaluated - requires
        // tracking unfair-thought duration across multiple days.
        private const int FairStage = 0;
        private const int UnfairStage = 0;

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // F-V5 / X4: Ideology-Domain resolution. We do not require a
            // capability gate here anymore because the Ideology domain IS
            // Package 05's domain. The gate stays at the StoryDirector level
            // at the call site (Mod 02) so the cross-package READ can be
            // observed and gated there. Mod 02 tests ensure no other package
            // can suppress this ThoughtWorker's evaluation.

            // Only adult player colonists
            if (pawn == null || !pawn.IsColonist || pawn.Dead)
                return ThoughtState.Inactive;

            if (!pawn.IsFreeNonSlaveColonist)
                return ThoughtState.Inactive;

            // Not applicable to children (Biotech)
            if (!pawn.ageTracker.Adult)
                return ThoughtState.Inactive;

            // F-V1: delegate colonist enumeration to ColonialReader (Foundation).
            var allColonists = ColonialReader.GetActiveColonists();
            if (allColonists.Count < 2)
                return ThoughtState.Inactive;  // single colonist = no fairness issue

            // Calculate per-colonist resource value (food + meds weighted)
            var resourceValues = new Dictionary<int, float>();
            float totalValue = 0f;

            foreach (var colonist in allColonists)
            {
                float value = CalculateResourceValue(colonist);
                resourceValues[colonist.thingIDNumber] = value;
                totalValue += value;
            }

            float averageValue = totalValue / allColonists.Count;

            // If nobody has anything, distribution is trivially "fair"
            if (averageValue < 0.01f)
                return ThoughtState.ActiveAtStage(FairStage);

            float pawnValue = resourceValues.TryGetValue(pawn.thingIDNumber, out float v) ? v : 0f;
            float deviation = (pawnValue - averageValue) / averageValue;

            // Fair: within threshold
            if (deviation >= -FairThreshold)
            {
                return ThoughtState.ActiveAtStage(FairStage);
            }

            // Unfair: significantly below average
            if (deviation <= -UnfairThreshold)
            {
                return ThoughtState.ActiveAtStage(UnfairStage);
            }

            // In the gray zone between fair and unfair - no thought
            return ThoughtState.Inactive;
        }

        /// <summary>
        /// Calculates a weighted resource value for a colonist.
        /// Counts food (raw + meals) and medicine in the pawn's inventory.
        /// Food: 1 point per unit. Medicine: 3 points per unit.
        /// </summary>
        private static float CalculateResourceValue(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
                return 0f;

            float value = 0f;
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing == null || thing.def == null)
                    continue;

                if (thing.def.IsNutritionGivingIngestible)
                    value += thing.stackCount * 1.0f;
                else if (thing.def.IsMedicine)
                    value += thing.stackCount * 3.0f;
            }
            return value;
        }
    }
}
