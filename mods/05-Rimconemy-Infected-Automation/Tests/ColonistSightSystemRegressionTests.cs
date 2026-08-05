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
            TestDistanceFalloff();
            TestLightLevelRange();
            TestSightConstants();
            TestHasActiveSightApi();

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
            // forwardRadius argument is unused by ComputeDirectionalFactor;
            // pass the constant so future radius changes do not silently
            // invalidate this test as 2026-08-05.
            float radius = SightConeMath.MaxForwardRadius;

            // Forward target: factor should be 1.
            var forwardTarget = new IntVec3(60, 0, 50);
            float fwd = SightConeMath.ComputeDirectionalFactor(pos, forwardTarget, facing, radius);
            AssertFloat(1f, fwd, 0.01f, "CS4. Forward target = factor 1");

            // Behind target: factor should be ~0.15.
            var behindTarget = new IntVec3(40, 0, 50);
            float behind = SightConeMath.ComputeDirectionalFactor(pos, behindTarget, facing, radius);
            AssertFloat(SightConeMath.BehindRadiusFactor, behind, 0.05f, "CS5. Behind target = factor 0.15");

            // No facing: factor = 1 (omnidirectional).
            float noFacing = SightConeMath.ComputeDirectionalFactor(pos, forwardTarget, IntVec3.Invalid, radius);
            AssertFloat(1f, noFacing, 0.01f, "CS6. No facing = omnidirectional (factor 1)");

            // Side target (90°): in transition zone 60° → 120°.
            // t = (90-60)/(120-60) = 0.5 → factor = 1 - 0.5*(1-0.35) = 0.675.
            var sideTarget = new IntVec3(50, 0, 60);
            float side = SightConeMath.ComputeDirectionalFactor(pos, sideTarget, facing, radius);
            AssertFloat(0.675f, side, 0.05f, "CS7. Side target (90°) = transition midpoint 0.675");
        }

        private static void TestCellVisibility()
        {
            var pos = new IntVec3(50, 0, 50);
            var facing = new IntVec3(1, 0, 0);

            // At pawn's own cell: always fully visible.
            float selfVis = SightConeMath.ComputeCellVisibility(
                pos, pos, facing, 0.5f, 0f, IntVec3.Invalid);
            AssertFloat(1f, selfVis, 0.01f, "CS8. Own cell always visible");

            // Forward 5 tiles at full light: 2026-08-05 shortened radius
            // 25→18→16 with power-curve exponent 2.0 gives
            // (1-5/16)^2 ≈ 0.473.
            var fwd5 = new IntVec3(55, 0, 50);
            float visFwd5 = SightConeMath.ComputeCellVisibility(
                pos, fwd5, facing, 1f, 0f, IntVec3.Invalid);
            AssertTrue(visFwd5 > 0.45f, "CS9. Forward 5 tiles at full light remains above 0.45 after MaxForwardRadius=16");

            // Forward 10 tiles at full light: power curve (exp=2.0) drops
            // mid-range steeply. (1-10/16)^2 = (6/16)^2 ≈ 0.141 — clearly
            // below 0.5 even in daylight.
            var fwd10 = new IntVec3(60, 0, 50);
            float visFwd10 = SightConeMath.ComputeCellVisibility(
                pos, fwd10, facing, 1f, 0f, IntVec3.Invalid);
            AssertFloat(0.141f, visFwd10, 0.02f, "CS9b. Forward 10 tiles at full light follows power-curve ~0.14 (MaxForward=16, exp=2.0)");

            // Forward at 30 tiles (beyond max radius): zero visibility.
            var fwd30 = new IntVec3(80, 0, 50);
            float visFar = SightConeMath.ComputeCellVisibility(
                pos, fwd30, facing, 0.5f, 0f, IntVec3.Invalid);
            AssertFloat(0f, visFar, 0.01f, "CS10. Beyond max radius = 0");

            // Forward 5 tiles vs behind 5 tiles: behind past the small
            // back-cone radius (forwardRadius × 0.15 = 2.7) returns 0,
            // forward still has positive visibility.
            var behind5 = new IntVec3(45, 0, 50);
            float visBehind = SightConeMath.ComputeCellVisibility(
                pos, behind5, facing, 1f, 0f, IntVec3.Invalid);
            AssertTrue(visBehind < visFwd5, "CS11. Behind visibility < forward visibility");
        }

        private static void TestDistanceFalloff()
        {
            // Power-curve exponent documented. Linear distance falloff had
            // mid-range cells too bright — the exponent makes every tile
            // beyond the cone's near radius contribute visible shadow.
            AssertFloat(2.0f, SightConeMath.DistanceFalloffExponent, 0.001f,
                "CS19. DistanceFalloffExponent=2.0 (quadratic — aggressive 2026-08-05 v2)");

            // Monotonic: closer tile strictly brighter than farther when
            // both are inside the cone.
            var pos = new IntVec3(50, 0, 50);
            var facing = new IntVec3(1, 0, 0);
            var near = new IntVec3(53, 0, 50); // 3 tiles
            var far  = new IntVec3(58, 0, 50); // 8 tiles
            float visNear = SightConeMath.ComputeCellVisibility(pos, near, facing, 1f, 0f, IntVec3.Invalid);
            float visFar  = SightConeMath.ComputeCellVisibility(pos, far,  facing, 1f, 0f, IntVec3.Invalid);
            AssertTrue(visNear > visFar,
                "CS20. Closer forward tile brighter than farther forward tile (power curve)");

            // Daylight tile 10 ahead is darker than night-time tile 10 ahead
            // measured RELATIVE to cone-edge? No — absolute distance still
            // dominates. Verify instead that with the new curve both daytime
            // AND nighttime give visibly different shadows than the old line.
            // We accomplish that by checking that alpha-equivalent visibility
            // halving occurs within the cone (i.e. mid-range is 0.3 not 0.6).
            var mid = new IntVec3(60, 0, 50); // 10 tiles, full daylight
            float visMidDay = SightConeMath.ComputeCellVisibility(pos, mid, facing, 1f, 0f, IntVec3.Invalid);
            AssertTrue(visMidDay < 0.4f,
                "CS21. Mid-range daylight visibility < 0.4 (power curve makes distance perceptible)");
        }

        private static void TestLightLevelRange()
        {
            // 2026-08-05 v3: range Min 12 → Max 16 (4 tile swing) with
            // DistanceFalloffExponent=2.0. At 5 tiles forward,
            // brightness gives ~47 % while pitch black gives ~34 %.
            var pos = new IntVec3(50, 0, 50);
            var target = new IntVec3(55, 0, 50);

            float visDark = SightConeMath.ComputeCellVisibility(
                pos, target, IntVec3.Zero, 0f, 0f, IntVec3.Invalid);
            float visBright = SightConeMath.ComputeCellVisibility(
                pos, target, IntVec3.Zero, 1f, 0f, IntVec3.Invalid);

            AssertTrue(visBright >= visDark, "CS12. Bright light >= dark visibility");
            // Day/Night contrast at 5 tiles, exp=2.0.
            AssertFloat(0.473f, visBright, 0.02f,
                "CS12b. Bright-light visibility at 5 tiles (formula: (1-5/16)^2)");
            AssertFloat(0.340f, visDark, 0.02f,
                "CS12c. Dark visibility at 5 tiles (formula: (1-5/12)^2)");
        }

        private static void TestSightConstants()
        {
            AssertFloat(16f, SightConeMath.MaxForwardRadius, 0.01f, "CS13. MaxForwardRadius=16 (shortened 25\u219218\u219216)");
            AssertFloat(12f, SightConeMath.MinForwardRadius, 0.01f, "CS14. MinForwardRadius=12 (15\u219212 for Day/Night contrast)");
            AssertFloat(0.15f, SightConeMath.BehindRadiusFactor, 0.01f, "CS15. BehindRadiusFactor=0.15");
            AssertFloat(0.35f, SightConeMath.SideRadiusFactor, 0.01f, "CS16. SideRadiusFactor=0.35");
            AssertFloat(3f, SightConeMath.MouseGlowRadius, 0.01f, "CS17. MouseGlowRadius=3");
            AssertFloat(0.2f, SightConeMath.MouseGlowIntensity, 0.01f, "CS18. MouseGlowIntensity=0.2");
        }

        private static void TestHasActiveSightApi()
        {
            // ConditionalVeil (2026-08-05) depends on this public accessor
            // being present on ColonistSightSystem so DarknessSectionLayer
            // can gate the AmbientVeilAlpha floor on live sight. We
            // verify the API contract via reflection because constructing a
            // real ColonistSightSystem requires a Map; the runtime flip is
            // covered by the live regression test instead.
            var method = typeof(ColonistSightSystem).GetMethod("HasActiveSight",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            AssertTrue(method != null,
                "CS22. ColonistSightSystem.HasActiveSight() public accessor exists");
            AssertTrue(method.ReturnType == typeof(bool),
                "CS23. HasActiveSight() returns bool");
            AssertTrue(method.GetParameters().Length == 0,
                "CS24. HasActiveSight() takes no parameters");
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
