using System;
using Rimconemy.Foundation.Profile;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Regression checks for the TryEmitDetection dedup gate. Simulates the
    /// Foundation static-cctor re-entry where PackageRegistry.Register
    /// (rimconemy.survivalprogression) wakes ProfileDetector's type initializer
    /// mid-flight, then continues and re-runs DetectProfile via
    /// NotifyPackageRegistryChanged. Without the dedup, both runs emitted
    /// duplicate "Profile detected" lines; with the gate, only the first run
    /// produces a log line.
    /// </summary>
    public static class ProfileDetectorDedupTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("Foundation", "Profile detector dedup tests");

            _passed = 0;
            _failed = 0;

            // Test 1: Reset before exercising the gate so the dedup token is
            // cleared and a known good baseline is established.
            ProfileDetector.ResetForReload();

            // Test 2: First call after reset must emit (token is null).
            bool firstEmitted = ProfileDetector.TryEmitDetection(out string firstSummary);
            AssertTrue(firstEmitted,
                "TryEmitDetection: first call after ResetForReload emits");
            AssertTrue(!string.IsNullOrEmpty(firstSummary),
                "TryEmitDetection: out summary is populated on first call");

            // Test 3: Second call with no state change must dedup — the same
            // canonical state produces the same string, so TryEmitDetection
            // returns false. This is the literal cctor re-entry scenario.
            bool secondEmitted = ProfileDetector.TryEmitDetection(out string secondSummary);
            AssertTrue(!secondEmitted,
                "TryEmitDetection: same-state re-entry does not re-emit (cctor race)");
            AssertTrue(string.Equals(firstSummary, secondSummary, StringComparison.Ordinal),
                "TryEmitDetection: out summary is stable across same-state calls");

            // Test 4: A third back-to-back call STILL dedups (asserts the dedup
            // is a stable invariant and not a "first two only" coincidence).
            bool thirdEmitted = ProfileDetector.TryEmitDetection(out string thirdSummary);
            AssertTrue(!thirdEmitted,
                "TryEmitDetection: a third same-state call still dedups");
            AssertTrue(string.Equals(firstSummary, thirdSummary, StringComparison.Ordinal),
                "TryEmitDetection: out summary stable across N same-state calls");

            // Test 5: After ResetForReload the dedup token is cleared, so the
            // next TryEmitDetection emits again — exactly the save/load path.
            ProfileDetector.ResetForReload();
            bool fourthEmitted = ProfileDetector.TryEmitDetection(out string fourthSummary);
            AssertTrue(fourthEmitted,
                "TryEmitDetection: post-ResetForReload call emits a fresh summary");
            AssertTrue(string.Equals(firstSummary, fourthSummary, StringComparison.Ordinal),
                "TryEmitDetection: post-reset summary matches the original content");

            // Final report. Format must match the runtime_test.sh required
            // summaries regex "Profile detector dedup tests: [0-9]+ passed, 0 failed".
            string summary = "[Rimconemy.Foundation] Profile detector dedup tests: "
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

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.Foundation] ProfileDetectorDedupTests FAILED: " + label);
            }
        }
    }
}
