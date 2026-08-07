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
            ts.Check(ledger.TryAward("build|output-1", "pawn-1", 12, 60000L, out var first), "Building XP: first output is accepted");
            ts.Check(!(ledger.TryAward("build|output-1", "pawn-1", 12, 60001L, out var replay)), "Building XP: replay after simulated reload is rejected");
            ts.Check(Equals(first.Amount, replay.Amount), "Building XP: replay exposes original award");
            ts.Check(Equals(1, ledger.AwardCount), "Building XP: durable ledger stores one output");

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


    }
}
