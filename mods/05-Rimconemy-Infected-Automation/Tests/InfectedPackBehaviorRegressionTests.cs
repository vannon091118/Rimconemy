// Tests/InfectedPackBehaviorRegressionTests.cs
//
// Owner: Infected & Automation (Package 05).
// Phase C — Tier-Inokulation Tier-AI.
//
// Pattern: static RunAll() (like all other pacakge tests). Tests P1-P5.

using Rimconemy.InfectedAutomation.World;
using RimWorld;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class InfectedPackBehaviorRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static void RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "InfectedPackBehavior test");

            _passed = 0;
            _failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { _passed++; return; }
                _failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Error("[Rimconemy.InfectedAutomation] InfectedPackBehavior test FAILED: " + name);
            }

            Check(TestWanderingStaysWhenNoColonist(),         "P1.WanderingStaysWhenNoColonist");
            Check(TestWanderingTransitionsToTracking(),       "P2.WanderingTransitionsToTracking");
            Check(TestTrackingFallsBackToWandering(),         "P3.TrackingFallsBackAfter60Ticks");
            Check(TestTrackingFallsBackToDissipatingLongForm(), "P4.TrackingFallsBackToDissipatingIfLongExistence");
            Check(TestDissipatingReturnsToWandering(),        "P5.DissipatingReturnsToWandering");

            Log.Message(
                "[Rimconemy.InfectedAutomation] InfectedPackBehavior regression tests (Phase C subset): "
                + _passed + " passed, " + _failed + " failed."
                + (firstFailure != null ? " First failure: " + firstFailure : ""));

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
        }

        private static bool TestWanderingStaysWhenNoColonist()
        {
            return InfectedPackBehavior.ComputeNext(
                InfectedPackState.Wandering,
                colonistVisible: false,
                ticksSinceLastSight: 0,
                daysOfExistence: 0) == InfectedPackState.Wandering;
        }

        private static bool TestWanderingTransitionsToTracking()
        {
            return InfectedPackBehavior.ComputeNext(
                InfectedPackState.Wandering,
                colonistVisible: true,
                ticksSinceLastSight: 0,
                daysOfExistence: 100L) == InfectedPackState.Tracking;
        }

        private static bool TestTrackingFallsBackToWandering()
        {
            // 60 ticks later, no colonist visible, not yet a long-lived tier.
            return InfectedPackBehavior.ComputeNext(
                InfectedPackState.Tracking,
                colonistVisible: false,
                ticksSinceLastSight: 60L,
                daysOfExistence: 60_000L) == InfectedPackState.Wandering;
        }

        private static bool TestTrackingFallsBackToDissipatingLongForm()
        {
            // 5+ days of existence → Dissipating instead of Wandering.
            return InfectedPackBehavior.ComputeNext(
                InfectedPackState.Tracking,
                colonistVisible: false,
                ticksSinceLastSight: 60L,
                daysOfExistence: InfectedPackBehavior.DissipatingDurationTicks) == InfectedPackState.Dissipating;
        }

        private static bool TestDissipatingReturnsToWandering()
        {
            return InfectedPackBehavior.ComputeNext(
                InfectedPackState.Dissipating,
                colonistVisible: false,
                ticksSinceLastSight: 0,
                daysOfExistence: 0) == InfectedPackState.Wandering;
        }
    }
}
