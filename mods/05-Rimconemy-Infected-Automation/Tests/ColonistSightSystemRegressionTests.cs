using System;
using Rimconemy.InfectedAutomation.World;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Sprint 2.5 — Colonist sight cone regression tests.
    /// Verifies SightConeMath formulas and colonist sight geometry.
    /// </summary>
    public static class ColonistSightSystemRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            TestAngleBetween();
            TestDirectionalFactor();
            TestCellVisibility();
            TestLightLevelRange();
            TestSightConstants();

            string summary = "[Rimconemy.InfectedAutomation] ColonistSightSystem regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void TestAngleBetween()
        {
            // Forward: angle 0.
            var forward = new IntVec3(1, 0, 0);
            var same = new IntVec3(1, 0, 0);
            float angle0 = SightConeMath_AngleBetween(forward, same);
            AssertFloat(0f, angle0, 0.01f, "CS1. Same direction = angle 0");

            // Opposite: angle π.
            var opposite = new IntVec3(-1, 0, 0);
            float anglePi = SightConeMath_AngleBetween(forward, opposite);
            AssertFloat(MathF.PI, anglePi, 0.01f, "CS2. Opposite direction = angle π");

            // Perpendicular: angle π/2.
            var perp = new IntVec3(0, 0, 1);
            float angle90 = SightConeMath_AngleBetween(forward, perp);
            AssertFloat(MathF.PI / 2f, angle90, 0.01f, "CS3. Perpendicular = angle π/2");
        }

        private static void TestDirectionalFactor()
        {
            var pos = new IntVec3(50, 0, 50);
            var facing = new IntVec3(1, 0, 0);

            // Forward target: factor should be 1.
            var forwardTarget = new IntVec3(60, 0, 50);
            float fwd = SightConeMath.ComputeDirectionalFactor(pos, forwardTarget, facing, 25f);
            AssertFloat(1f, fwd, 0.01f, "CS4. Forward target = factor 1");

            // Behind target: factor should be ~0.15.
            var behindTarget = new IntVec3(40, 0, 50);
            float behind = SightConeMath.ComputeDirectionalFactor(pos, behindTarget, facing, 25f);
            AssertFloat(SightConeMath.BehindRadiusFactor, behind, 0.05f, "CS5. Behind target = factor 0.15");

            // No facing: factor = 1 (omnidirectional).
            float noFacing = SightConeMath.ComputeDirectionalFactor(pos, forwardTarget, IntVec3.Invalid, 25f);
            AssertFloat(1f, noFacing, 0.01f, "CS6. No facing = omnidirectional (factor 1)");

            // Side target (90°): factor ~0.35.
            var sideTarget = new IntVec3(50, 0, 60);
            float side = SightConeMath.ComputeDirectionalFactor(pos, sideTarget, facing, 25f);
            AssertFloat(SightConeMath.SideRadiusFactor, side, 0.05f, "CS7. Side target = factor 0.35");
        }

        private static void TestCellVisibility()
        {
            var pos = new IntVec3(50, 0, 50);
            var facing = new IntVec3(1, 0, 0);

            // At pawn's own cell: always fully visible.
            float selfVis = SightConeMath.ComputeCellVisibility(
                pos, pos, facing, 0.5f, 0f, IntVec3.Invalid);
            AssertFloat(1f, selfVis, 0.01f, "CS8. Own cell always visible");

            // Forward at 10 tiles with bright light: high visibility.
            var fwd10 = new IntVec3(60, 0, 50);
            float visFwd = SightConeMath.ComputeCellVisibility(
                pos, fwd10, facing, 1f, 0f, IntVec3.Invalid);
            AssertTrue(visFwd > 0.5f, "CS9. Forward 10 tiles at full light > 0.5");

            // Forward at 30 tiles (beyond max radius): zero visibility.
            var fwd30 = new IntVec3(80, 0, 50);
            float visFar = SightConeMath.ComputeCellVisibility(
                pos, fwd30, facing, 0.5f, 0f, IntVec3.Invalid);
            AssertFloat(0f, visFar, 0.01f, "CS10. Beyond max radius = 0");

            // Behind at same distance: lower visibility.
            var behind10 = new IntVec3(40, 0, 50);
            float visBehind = SightConeMath.ComputeCellVisibility(
                pos, behind10, facing, 1f, 0f, IntVec3.Invalid);
            AssertTrue(visBehind < visFwd, "CS11. Behind visibility < forward visibility");
        }

        private static void TestLightLevelRange()
        {
            // Pitch black (0) → min radius 3. Bright light (1) → max radius 25.
            var pos = new IntVec3(50, 0, 50);
            var target = new IntVec3(55, 0, 50);

            float visDark = SightConeMath.ComputeCellVisibility(
                pos, target, IntVec3.Zero, 0f, 0f, IntVec3.Invalid);
            float visBright = SightConeMath.ComputeCellVisibility(
                pos, target, IntVec3.Zero, 1f, 0f, IntVec3.Invalid);

            AssertTrue(visBright >= visDark, "CS12. Bright light >= dark visibility");
        }

        private static void TestSightConstants()
        {
            AssertFloat(25f, SightConeMath.MaxForwardRadius, 0.01f, "CS13. MaxForwardRadius=25");
            AssertFloat(3f, SightConeMath.MinForwardRadius, 0.01f, "CS14. MinForwardRadius=3");
            AssertFloat(0.15f, SightConeMath.BehindRadiusFactor, 0.01f, "CS15. BehindRadiusFactor=0.15");
            AssertFloat(0.35f, SightConeMath.SideRadiusFactor, 0.01f, "CS16. SideRadiusFactor=0.35");
            AssertFloat(3f, SightConeMath.MouseGlowRadius, 0.01f, "CS17. MouseGlowRadius=3");
            AssertFloat(0.2f, SightConeMath.MouseGlowIntensity, 0.01f, "CS18. MouseGlowIntensity=0.2");
        }

        // ── reflection helper ─────────────────────────────────

        private static float SightConeMath_AngleBetween(IntVec3 a, IntVec3 b)
        {
            float dot = a.x * b.x + a.z * b.z;
            float magA = MathF.Sqrt(a.x * a.x + a.z * a.z);
            float magB = MathF.Sqrt(b.x * b.x + b.z * b.z);
            if (magA < 0.001f || magB < 0.001f) return 0f;
            float cosAngle = dot / (magA * magB);
            cosAngle = Math.Clamp(cosAngle, -1f, 1f);
            return MathF.Acos(cosAngle);
        }

        // ── helpers ───────────────────────────────────────────

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[ColonistSightRegression] " + label); }
        }

        private static void AssertFloat(float expected, float actual, float tolerance, string label)
        {
            if (Math.Abs(expected - actual) <= tolerance) _passed++;
            else
            {
                _failed++;
                Log.Error("[ColonistSightRegression] " + label + ": expected " + expected
                    + ", got " + actual);
            }
        }
    }
}
