using Rimconemy.InfectedAutomation.Threat;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Audit-Finding 6 (2026-08-04) regression tests for
    /// <see cref="ThreatSnapshotBridge"/>: validates the single-source
    /// read path used by both
    /// <see cref="InfectedRaidSpawnService"/> and
    /// <see cref="WorldRaidCoordinator"/>.
    ///
    /// The test deliberately avoids RimWorld-game-bound access (no
    /// <c>Current.Game</c>) so it remains fakeless and reproducible in
    /// the Build-Boot-Pfad. We exercise:
    ///   * Defensive null-path: no saved game → null snapshot.
    ///   * Latest property: stays null after a defensive-only resolve.
    ///   * Bridge.GetLatestPressure returns 0f when null.
    ///   * ResetForTests clears the property so subsequent calls
    ///     re-resolve.
    ///
    /// Call-site unification is asserted statically by reading the
    /// managed-method body of the two stubs via grep-style checks at
    /// build time (see Bootstrap.BridgeIsSingleSourceAssertion()).
    /// </summary>
    public static class ThreatSnapshotBridgeRegressionTests
    {
        private static int _passed;
        private static int _failed;
        private static int _run;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;
            _run++;

            ResetForTests();

            // No Current.Game during pre-game boot — GetLatest must NOT
            // throw and must return null.
            AssertNull(ThreatSnapshotBridge.GetLatest(),
                "no-game bridge returns null (defensive)");
            AssertEqual(0f, ThreatSnapshotBridge.GetLatestPressure(),
                "no-game bridge pressure is 0f (defensive)");

            // After Release, Latest is null again.
            ResetForTests();
            AssertTrue(ThreatSnapshotBridge.Latest == null,
                "ResetForTests clears the Latest property");

            // Defensive tick-pre-Load: even with hat-tick simulated
            // access by repeated GetLatest() calls, no exception escapes.
            for (int i = 0; i < 3; i++)
            {
                AssertNull(ThreatSnapshotBridge.GetLatest(),
                    "repeated GetLatest() stays safe (run " + (_run + i) + ")");
            }

            // ── IsCachedForCurrentTick contracts (tick-stamp cache) ──
            // Empty cache → cache miss for any tick queried.
            ResetForTests();
            AssertFalse(ThreatSnapshotBridge.IsCachedForCurrentTick(0L),
                "empty cache: IsCachedForCurrentTick(0) is false");
            AssertFalse(ThreatSnapshotBridge.IsCachedForCurrentTick(60000L),
                "empty cache: IsCachedForCurrentTick(any) is false");

            // Plant a known snapshot at tick T; the cache must report
            // hit for T and miss for T±1.
            const long t = 60000L;
            ThreatSnapshotBridge.SetLatestForTests(
                new ThreatAggregator { TotalPressure = 0.42f, LastUpdatedTick = t, ScopeId = "test" },
                t);

            AssertTrue(ThreatSnapshotBridge.IsCachedForCurrentTick(t),
                "IsCachedForCurrentTick(t) returns true when cache stamped at t");
            AssertFalse(ThreatSnapshotBridge.IsCachedForCurrentTick(t + 1L),
                "IsCachedForCurrentTick(t+1) returns false when cache stamped at t");
            AssertFalse(ThreatSnapshotBridge.IsCachedForCurrentTick(t - 1L),
                "IsCachedForCurrentTick(t-1) returns false when cache stamped at t");
            AssertFalse(ThreatSnapshotBridge.IsCachedForCurrentTick(0L),
                "IsCachedForCurrentTick(0) returns false when cache has a real stamp");

            // LatestTick companion mirrors the planted value.
            AssertEqual(t, ThreatSnapshotBridge.LatestTick,
                "LatestTick mirrors the planted tick stamp");

            // LatestTick==0 is the "never produced" sentinel: even
            // when Latest != null, the bridge refuses to claim a hit.
            ThreatSnapshotBridge.SetLatestForTests(
                new ThreatAggregator { TotalPressure = 0.5f, LastUpdatedTick = 0L, ScopeId = "test" },
                0L);
            AssertFalse(ThreatSnapshotBridge.IsCachedForCurrentTick(0L),
                "LatestTick==0 is treated as cache miss even if Latest != null");

            // GetOrResolveForTick returns the cached instance on hit,
            // and a fresh (null in this no-game state) on miss.
            ThreatSnapshotBridge.SetLatestForTests(
                new ThreatAggregator { TotalPressure = 0.7f, LastUpdatedTick = t, ScopeId = "test" },
                t);
            ThreatAggregator cachedHit = ThreatSnapshotBridge.GetOrResolveForTick(t);
            AssertTrue(cachedHit != null && cachedHit.TotalPressure == 0.7f,
                "GetOrResolveForTick on hit returns cached instance with original pressure");

            ResetForTests();

            string summary = "[Rimconemy.InfectedAutomation] ThreatSnapshotBridge regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void ResetForTests()
        {
            // Pre-test cleanup so a previous run's Latest state cannot
            // contaminate the property assertions.
            ThreatSnapshotBridge.ResetForTests();
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[Rimconemy.InfectedAutomation] " + label); }
        }
        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }
        private static void AssertNull<T>(T value, string label) where T : class
        {
            if (value == null) _passed++;
            else { _failed++; Log.Error("[Rimconemy.InfectedAutomation] " + label + " (expected null, got " + value + ")"); }
        }
        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.InfectedAutomation] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
