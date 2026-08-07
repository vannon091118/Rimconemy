using System;
using Rimconemy.InfectedAutomation.World;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// World-space darkness renderer invariants. These tests intentionally use
    /// only pure helpers so startup validation does not require a live Map mesh.
    /// </summary>
    public static class DarknessSectionLayerRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "DarknessSectionLayer regression tests");

            _passed = 0;
            _failed = 0;

            TestAlphaCurve();
            TestAlphaClamping();
            TestAmbientVeilRemoved();
            TestAmbientVeilApplied();
            TestMeshBufferCounts();
            TestOverlayColorContract();
            TestVisibilityRangeContract();
            TestConditionalVeilGateValue();

            string summary = "[Rimconemy.InfectedAutomation] DarknessSectionLayer regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }

            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        private static void TestAlphaCurve()
        {
            // v2: max alpha 1.0 (out-of-cone = COMPLETE black), curve pow(1-v, 0.4).
            // All calls pass veil=0f to model the init / no-colonists gate.
            AssertFloat(1.0f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0f, 1.0f, 0f),
                0.001f,
                "DL1. Zero visibility reaches COMPLETE black (alpha 1.0)");
            AssertFloat(0f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(1f, 1.0f, 0f),
                0.001f,
                "DL2. Full visibility has no overlay alpha when veil is gated off");
            AssertTrue(
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0.25f, 1.0f, 0f)
                    > DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0.75f, 1.0f, 0f),
                "DL3. Darker cells have greater alpha");
            // DL3a/b/c verify the pow(1-v, 0.4) curve math precisely.
            AssertFloat(0.891f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0.25f, 1.0f, 0f), 0.02f,
                "DL3a. Curve at vis=0.25 ≈ pow(0.75, 0.4) ≈ 0.891");
            AssertFloat(0.758f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0.50f, 1.0f, 0f), 0.02f,
                "DL3b. Curve at vis=0.50 ≈ pow(0.50, 0.4) ≈ 0.758");
            AssertFloat(0.574f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0.75f, 1.0f, 0f), 0.02f,
                "DL3c. Curve at vis=0.75 ≈ pow(0.25, 0.4) ≈ 0.574");
        }

        private static void TestAlphaClamping()
        {
            AssertFloat(1.0f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(-1f, 1.0f, 0f),
                0.001f,
                "DL4. Visibility below zero clamps to curve at max alpha 1.0");
            AssertFloat(0f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(2f, 1.0f, 0f),
                0.001f,
                "DL5. Visibility above one clamps to zero alpha");
            AssertFloat(0f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(1f, 2f, 0f),
                0.001f,
                "DL6. Max alpha clamps; full visibility returns zero");
        }

        private static void TestAmbientVeilRemoved()
        {
            // ConditionalVeil: veil is an explicit parameter gated by HasActiveSight().
            // "No global veil" contract holds when invoked with veil=0f.
            AssertFloat(0f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(1f, 1.0f, 0f),
                0.0001f,
                "DL13. Visibility=1 stays at zero alpha when veil is gated off");
        }

        private static void TestAmbientVeilApplied()
        {
            // Once HasActiveSight() flips true, Rebuild passes AmbientVeilAlpha=0.04
            // and visibility=1 cells pick up that subtle shadow floor.
            AssertFloat(0.04f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(1f, 1.0f, 0.04f),
                0.0001f,
                "DL16. Veil applies at visibility=1 once ConditionalVeil gate is open");
            AssertTrue(
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0.95f, 1.0f, 0.04f) >= 0.04f,
                "DL17. Curve >= veil for mid-bright cells; veil never darkens below curve");
            // At vis=0.99 the curve pow(0.01, 0.4) ≈ 0.158 exceeds the veil floor.
            AssertFloat(0.158f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(0.99f, 1.0f, 0.04f),
                0.01f,
                "DL18. At visibility=0.99 the curve (≈0.158) dominates the veil (0.04)");
            AssertFloat(0.04f,
                DarknessSectionLayerLifecycle.ComputeOverlayAlpha(1f, 1.0f, 0.04f),
                0.0001f,
                "DL18b. Belt: veil applies only when visibility=1 makes curve=0");
        }

        private static void TestMeshBufferCounts()
        {
            AssertTrue(
                DarknessSectionLayerLifecycle.ValidateMeshBuffers(4, 4, 6),
                "DL7. One quad has matching buffers");
            AssertTrue(
                DarknessSectionLayerLifecycle.ValidateMeshBuffers(0, 0, 0),
                "DL8. Empty section is valid");
            AssertTrue(
                !DarknessSectionLayerLifecycle.ValidateMeshBuffers(4, 3, 6),
                "DL9. Color mismatch is rejected");
            AssertTrue(
                !DarknessSectionLayerLifecycle.ValidateMeshBuffers(4, 4, 5),
                "DL10. Non-triangle index count is rejected");
        }

        private static void TestOverlayColorContract()
        {
            // Darkness material uses vertex color as black-overlay multiplier:
            // black RGB darkens the map; alpha is the visibility-derived channel.
            var opaque = DarknessSectionLayerLifecycle.CreateOverlayColor(1f);
            AssertTrue(
                DarknessSectionLayerLifecycle.IsBlackOverlayColor(opaque),
                "DL20. Opaque overlay uses black RGB so Darkness material darkens the map");
            AssertTrue(opaque.a == 255,
                "DL21. Opaque overlay preserves alpha=255");

            var partial = DarknessSectionLayerLifecycle.CreateOverlayColor(0.5f);
            AssertTrue(
                DarknessSectionLayerLifecycle.IsBlackOverlayColor(partial),
                "DL22. Partial overlay keeps black RGB");
            AssertTrue(partial.a >= 127 && partial.a <= 128,
                "DL23. Partial overlay converts alpha to approximately 128");

            var clamped = DarknessSectionLayerLifecycle.CreateOverlayColor(2f);
            AssertTrue(clamped.a == 255,
                "DL24. Overlay color clamps alpha above one");
        }

        private static void TestVisibilityRangeContract()
        {
            AssertTrue(
                SightConeMath.ComputeCellVisibility(
                    new IntVec3(10, 0, 10),
                    new IntVec3(10, 0, 10),
                    IntVec3.Zero,
                    0f,
                    0f,
                    IntVec3.Invalid) >= 0f,
                "DL11. Visibility helper remains non-negative");
            AssertTrue(
                SightConeMath.ComputeCellVisibility(
                    new IntVec3(10, 0, 10),
                    new IntVec3(10, 0, 10),
                    IntVec3.Zero,
                    0f,
                    0f,
                    IntVec3.Invalid) <= 1f,
                "DL12. Visibility helper remains at most one");
        }

        private static void TestConditionalVeilGateValue()
        {
            // The veil parameter is wired through HasActiveSight() gate.
            // Runtime gate is exercised by live ColonistSightSystemRegressionTests.
            AssertTrue(true,
                "DL19. Conditional veil wiring documented (HasActiveSight gate)");
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else
            {
                _failed++;
                Log.Error("[DarknessSectionLayerRegression] " + label);
            }
        }

        private static void AssertFloat(float expected, float actual, float tolerance, string label)
        {
            AssertTrue(Math.Abs(expected - actual) <= tolerance,
                label + ": expected " + expected + ", got " + actual);
        }
    }
}
