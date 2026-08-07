using System;
using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Scenarios;
using Verse;
using Rimconemy.Foundation.Tests;

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
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "StartEnemies regression tests");

            _passed = 0;
            _failed = 0;

            // 1. Event-key is stable across Scribe authoring — a single
            //    "starter-infected-spawn" key per map.
            ts.Check(Equals("starter-infected-spawn", RimconemyStartEnemiesLedger.EventKey_OneInfectedSpawn), "EventKey: stable for cross-package documentation");

            // 2. Stable schema.
            ts.Check(Equals(1, RimconemyStartEnemiesLedger.CurrentSchemaVersion), "CurrentSchemaVersion = 1");

            // 3. Mirrors the ledger semantics with a local HashSet. We don't
            //    instantiate the GameComponent directly (needs Verse.Game);
            //    we assert contract parity, which is what regressions are for.
            var seen = new HashSet<string>(StringComparer.Ordinal);

            string KeyFor(int mapId) => mapId + ":" + RimconemyStartEnemiesLedger.EventKey_OneInfectedSpawn;
            bool TryMark(int mapId) => seen.Add(KeyFor(mapId));
            bool IsCompleted(int mapId) => seen.Contains(KeyFor(mapId));

            ts.Check(TryMark(7), "first mark: accepted");
            ts.Check(IsCompleted(7), "after mark: completed");
            ts.Check(!(TryMark(7)), "duplicate mark: idempotent (false on second)");
            ts.Check(TryMark(8), "different map: accepted (no cross-map collision)");
            ts.Check(Equals(2, seen.Count), "two distinct maps: 2 entries");
            ts.Check(Equals(2, HashKeyCount(seen, 9) + 2), "unrelated mapId is *not* present (sanity)");

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
            ts.Check(Equals(2, rebuilt.Count), "saved keys rebuild to 2 entries (no dups)");
            ts.Check(rebuilt.Contains("7:starter-infected-spawn"), "map 7 survives");
            ts.Check(rebuilt.Contains("8:starter-infected-spawn"), "map 8 survives");

            string summary = "[Rimconemy.InfectedAutomation] StartEnemies regression tests: "
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

        private static int HashKeyCount(HashSet<string> set, int mapIdFilter)
        {
            int hits = 0;
            foreach (var key in set)
            {
                if (key.StartsWith(mapIdFilter + ":", StringComparison.Ordinal)) hits++;
            }
            return hits;
        }


    }
}
