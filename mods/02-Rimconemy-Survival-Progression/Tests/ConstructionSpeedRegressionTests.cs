using System;
using Rimconemy.SurvivalProgression.Character.Construction;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Regression tests for the retired construction-speed layer.
    /// Construction skill now changes finished-building durability
    /// (<see cref="BuilderDurability"/>); it must not change build speed.
    /// The durability curve itself is covered by RoleMechanicsRegressionTests.
    /// </summary>
    public static class ConstructionSpeedRegressionTests
    {
        public static int RunAll()
        {
            int failures = 0;
            int probes = 0;

            // The retired stat part must stay inert: no value change, no explanation.
            // (The shim is [Obsolete] by design; this test verifies it does nothing.)
#pragma warning disable CS0618
            var part = new ConstructionSpeed_StatPart();
#pragma warning restore CS0618
            float value = 100f;
            part.TransformValue(default, ref value);
            probes++;
            Check(ref failures, Math.Abs(value - 100f) < 0.0001f, "Retired stat part leaves value unchanged");
            probes++;
            Check(ref failures, part.ExplanationPart(default) == null, "Retired stat part offers no explanation");

            int passed = probes - failures;
            Log.Message(string.Format(
                "[ConstructionSpeed] ConstructionSpeed regression tests: {0} passed, {1} failed",
                passed, failures));

            return failures;
        }

        private static void Check(ref int failures, bool condition, string label)
        {
            if (condition) return;
            failures++;
            Log.Error("[Construction] FAIL: " + label);
        }
    }
}
