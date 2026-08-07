using System.Collections.Generic;
using Rimconemy.SurvivalProgression.Progression;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>Red-first gate for durable Building XP output keys.</summary>
    public static class BuildingProgressionPersistenceRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("SurvivalProgression", "Building progression persistence tests");

            _passed = 0;
            _failed = 0;

            var ledger = new BuildingProgressionLedger();
            AssertTrue(ledger.TryAward("build|output-1", "pawn-1", 12, 60000L, out var first),
                "Building XP: first output is accepted");
            AssertFalse(ledger.TryAward("build|output-1", "pawn-1", 12, 60001L, out var replay),
                "Building XP: replay after simulated reload is rejected");
            AssertEqual(first.Amount, replay.Amount, "Building XP: replay exposes original award");
            AssertEqual(1, ledger.AwardCount, "Building XP: durable ledger stores one output");

            string summary = "[Rimconemy.SurvivalProgression] Building progression persistence tests: "
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

        private static void AssertTrue(bool value, string label)
        {
            if (value) _passed++;
            else { _failed++; Log.Error("[Rimconemy.SurvivalProgression] " + label); }
        }

        private static void AssertFalse(bool value, string label) { AssertTrue(!value, label); }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.SurvivalProgression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
