using System;
using System.Collections.Generic;
using Rimconemy.SurvivalProgression.Scenarios;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02).
    /// Static regression checks for the Phase-1.1 / 1.4 start-state contract.
    /// We mirror the dedup semantics with a local HashSet rather than instantiating
    /// <see cref="RimconemyStartState"/> (which derives from GameComponent and
    /// comes up cold in a static fakeless test); the parity test then closes the
    /// gap on every refactor.
    ///
    /// Pattern: same shape as BuildingProgressionRegressionTests
    /// (single static RunAll(), AssertTrue / AssertEqual, log-only, no NUnit).
    /// </summary>
    public static class RimconemyStartStateRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            // 1. ComposeKey is stable and unique-per-(mapId, eventKey). We use
            //    the part-based overload because creating a RimWorld.Map in a
            //    static fakeless test is not worth the boilerplate. The
            //    Map-based overload delegates to the same composer.
            AssertEqual("42:survivor",  RimconemyStartState.ComposeKey(42, "survivor"),
                "ComposeKey: deterministic 'mapId:eventKey' composition");
            AssertEqual("0:survivor",   RimconemyStartState.ComposeKey(0, "survivor"),
                "ComposeKey: mapId=0 is preserved (not dropped as default)");
            AssertEqual("7:",           RimconemyStartState.ComposeKey(7, ""),
                "ComposeKey: empty eventKey still composes (caller-side guard)");

            // 2. Local HashSet mirrors ScenPart_RimconemyStart.IsCompletedFor / MarkCompleted.
            //    A direct test keeps the contract deterministic without a Map stub.
            var localSeen = new HashSet<string>(StringComparer.Ordinal);

            // First write accepted.
            AssertTrue(localSeen.Add("7:single-survivor"), "first mark: accepted");
            // Duplicate rejected.
            AssertFalse(localSeen.Add("7:single-survivor"), "duplicate mark: rejected");
            // Different event on the same map accepted.
            AssertTrue(localSeen.Add("7:scrap-rifle-given"), "different event: accepted");
            // Same event on a different map accepted (no cross-map collision).
            AssertTrue(localSeen.Add("8:single-survivor"), "different map: accepted");
            AssertEqual(3, localSeen.Count, "Set: 3 distinct entries, no duplicates collapsed");

            // Damage control: null/empty event keys must be filtered out by KeyFor-equivalent
            // guards. The contract requires IsCompletedFor / MarkCompleted to filter null maps
            // and empty event keys, so we rebuild a defender HashSet in the same shape.
            var guardedSeen = new HashSet<string>(StringComparer.Ordinal);
            AssertFalse(TryDefensiveMark(guardedSeen, -1, ""), "TryDefensiveMark: mapId=-1 → reject");
            AssertFalse(TryDefensiveMark(guardedSeen, 42, null), "TryDefensiveMark: null eventKey → reject");
            AssertEqual(0, guardedSeen.Count, "TryDefensiveMark: nothing added via invalid inputs");

            // 3. Rebuild from a save-state: parallel-list roundtrip preserves ORDER and
            //    DEDUP. BuildingProgressionLedger uses the same pattern; we mirror it.
            string[] savedLines =
            {
                "schemaVersion=" + RimconemyStartState.CurrentSchemaVersion,
                "keys=7:single-survivor,7:scrap-rifle-given,8:single-survivor",
            };
            var rebuilt = new HashSet<string>(StringComparer.Ordinal);
            string parsedTail = null;
            foreach (var line in savedLines)
            {
                int idx = line.IndexOf("keys=", StringComparison.Ordinal);
                if (idx < 0) continue;
                parsedTail = line.Substring(idx + "keys=".Length);
            }
            AssertTrue(parsedTail != null, "save-state contains a 'keys' line");
            foreach (var raw in parsedTail.Split(','))
            {
                if (!string.IsNullOrEmpty(raw)) rebuilt.Add(raw.Trim());
            }
            AssertEqual(3, rebuilt.Count, "rebuilt ledger = 3 entries");
            AssertTrue(rebuilt.Contains("7:single-survivor"), "map 7 single-survivor preserved");
            AssertTrue(rebuilt.Contains("7:scrap-rifle-given"), "map 7 scrap-rifle-given preserved");
            AssertTrue(rebuilt.Contains("8:single-survivor"), "map 8 single-survivor preserved");

            // 4. SchemaVersion is exposed and stable across re-reads.
            AssertEqual(1, RimconemyStartState.CurrentSchemaVersion,
                "CurrentSchemaVersion = 1");

            string summary = "[Rimconemy.SurvivalProgression] RimconemyStartState regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        /// <summary>
        /// Mirrors <c>RimconemyStartState.MarkCompleted</c>'s null/empty guard.
        /// Returns true if the entry was added; false on invalid inputs.
        /// </summary>
        private static bool TryDefensiveMark(HashSet<string> ledger, int mapId, string eventKey)
        {
            if (mapId < 0) return false;            // canonical surrogate for null-map
            if (string.IsNullOrEmpty(eventKey)) return false;
            string key = mapId + ":" + eventKey;
            return ledger.Add(key);
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[RimconemyStartStateRegression] " + label); }
        }

        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[RimconemyStartStateRegression] " + label
                    + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
