// Source/World/InfectedPackBehavior.cs
//
// Owner: Infected & Automation (Package 05).
// Phase C — Tier-Inokulation Tier-AI.
//
// Pure static helper for tier-infected pawn AI. Modelled after the
// existing InfectedBehaviorTransition (Phase-Sprint 2 Human-State-
// Machine) but simplified for Tier-Pack-Behavior: tier-Pawns do not
// reach Assault; they wander, follow, and dissipate. Tier-Sprint 2.5
// will replace these placeholders with actual Pawn_Job assignments.

namespace Rimconemy.InfectedAutomation.World
{
    public enum InfectedPackState
    {
        Wandering = 0,
        Tracking = 1,
        Dissipating = 2,
    }

    public static class InfectedPackBehavior
    {
        /// <summary>Min Wander-Step distance (cells).</summary>
        public const float WanderMinStep = 15f;
        /// <summary>Max Wander-Step distance (cells).</summary>
        public const float WanderMaxStep = 25f;

        /// <summary>Ticks without sight before Tracking → Wandering.</summary>
        public const long TrackingLostTicks = 60L;   // ~1s
        /// <summary>Ticks in Dissipating before back to Wandering.</summary>
        public const long DissipatingDurationTicks = 60_000L * 5; // 5 days

        /// <summary>
        /// Pure state-transition. Inputs:
        ///   • current: previous state
        ///   • colonistVisible: any colonist in line-of-sight?
        ///   • ticksSinceLastSight: ticks since last colonist sight
        ///   • daysOfExistence: ticks the tier has been infected (for dissipation rule)
        /// Returns the new state. Caller is responsible for navigating
        /// the pawn; this function is the brain only.
        /// </summary>
        public static InfectedPackState ComputeNext(
            InfectedPackState current,
            bool colonistVisible,
            long ticksSinceLastSight,
            long daysOfExistence)
        {
            switch (current)
            {
                case InfectedPackState.Wandering:
                    return colonistVisible
                        ? InfectedPackState.Tracking
                        : InfectedPackState.Wandering;
                case InfectedPackState.Tracking:
                    if (!colonistVisible && ticksSinceLastSight >= TrackingLostTicks)
                    {
                        return daysOfExistence >= DissipatingDurationTicks
                            ? InfectedPackState.Dissipating
                            : InfectedPackState.Wandering;
                    }
                    return InfectedPackState.Tracking;
                case InfectedPackState.Dissipating:
                    // Dissipating terminates and returns to neutral wandering
                    // so the pack does not get stuck. Tier-Packs at this
                    // stage move 50+ tiles away from the player (caller).
                    return InfectedPackState.Wandering;
                default:
                    return InfectedPackState.Wandering;
            }
        }
    }
}
