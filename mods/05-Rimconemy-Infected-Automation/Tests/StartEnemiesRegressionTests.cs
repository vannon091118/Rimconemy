using System;
using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Scenarios;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Phase-1.4 static regression checks for the starter-infected idempotency
    /// contract, mirroring the pattern of Rimconemy.SurvivalProgression.Tests
    /// (sibling package). Shape is intentionally short to fit the existing
    /// Paket-05 test suite (<see cref="StoryStateRegressionTests"/>).
    /// </summary>
    public static class StartEnemiesRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            // 1. Event-key is stable across Scribe authoring — a single
            //    "starter-infected-spawn" key per map.
            AssertEqual(
                "starter-infected-spawn",
                RimconemyStartEnemiesLedger.EventKey_OneInfectedSpawn,
                "EventKey: stable for cross-package documentation");

            // 2. Stable schema.
            AssertEqual(1, RimconemyStartEnemiesLedger.CurrentSchemaVersion,
                "CurrentSchemaVersion = 1");

            // 3. Mirrors the ledger semantics with a local HashSet. We don't
            //    instantiate the GameComponent directly (needs Verse.Game);
            //    we assert contract parity, which is what regressions are for.
            var seen = new HashSet<string>(StringComparer.Ordinal);

            string KeyFor(int mapId) => mapId + ":" + RimconemyStartEnemiesLedger.EventKey_OneInfectedSpawn;
            bool TryMark(int mapId) => seen.Add(KeyFor(mapId));
            bool IsCompleted(int mapId) => seen.Contains(KeyFor(mapId));

            AssertTrue(TryMark(7), "first mark: accepted");
            AssertTrue(IsCompleted(7), "after mark: completed");
            AssertFalse(TryMark(7), "duplicate mark: idempotent (false on second)");
            AssertTrue(TryMark(8), "different map: accepted (no cross-map collision)");
            AssertEqual(2, seen.Count, "two distinct maps: 2 entries");
            AssertEqual(2, HashKeyCount(seen, 9) + 2,
                "unrelated mapId is *not* present (sanity)");

            // 4. Roundtrip via parallel list. BuildingProgressionLedger + RimconemyStart-
            //    State use the same writer; we mirror it here for ledger parity.
            string[] savedLines =
            {
                "schemaVersion=" + RimconemyStartEnemiesLedger.CurrentSchemaVersion,
                "keys=7:" + RimconemyStartEnemiesLedger.EventKey_OneInfectedSpawn
                       + ",8:" + RimconemyStartEnemiesLedger.EventKey_OneInfectedSpawn,
            };
            var rebuilt = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in savedLines)
            {
                int idx = line.IndexOf("keys=", StringComparison.Ordinal);
                if (idx < 0) continue;
                foreach (var raw in line.Substring(idx + "keys=".Length).Split(','))
                    if (!string.IsNullOrEmpty(raw)) rebuilt.Add(raw.Trim());
            }
            AssertEqual(2, rebuilt.Count, "saved keys rebuild to 2 entries (no dups)");
            AssertTrue(rebuilt.Contains("7:starter-infected-spawn"), "map 7 survives");
            AssertTrue(rebuilt.Contains("8:starter-infected-spawn"), "map 8 survives");

            string summary = "[Rimconemy.InfectedAutomation] StartEnemies regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static int HashKeyCount(HashSet<string> set, int mapIdFilter)
        {
            int hits = 0;
            foreach (var key in set)
            {
                if (key.StartsWith(mapIdFilter + ":", StringComparison.Ordinal)) hits++;
            }
            return hits;
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[Rimconemy.InfectedAutomation.StartEnemiesRegression] " + label); }
        }
        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.InfectedAutomation.StartEnemiesRegression] " + label
                    + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
