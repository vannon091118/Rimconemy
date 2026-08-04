using System.Collections.Generic;
using Rimconemy.EconomyTerritory.Outposts;
using Verse;

namespace Rimconemy.EconomyTerritory.Tests
{
    /// <summary>Red-first gates for Outpost investment and state persistence contracts.</summary>
    public static class OutpostInvestmentRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            TestInvestmentReservesPhysicalInput();
            TestInvestmentReplayDoesNotDuplicate();
            TestOutpostStateUsesAbsoluteTicks();

            string summary = "[Rimconemy.EconomyTerritory] Outpost investment regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void TestInvestmentReservesPhysicalInput()
        {
            var transfers = new Transfers.PhysicalTransferService();
            transfers.SetAvailable("Rimconemy_ConstructionDebris", 80);
            var outpost = new Outpost("outpost-test", "player", 60000L);
            OutpostInvestmentResult result = outpost.TryReserveInvestment(
                transfers, "rimconemy.tests", "invest-1", "Rimconemy_ConstructionDebris", 30, 60000L);

            AssertEqual(OutpostInvestmentStatus.Reserved, result.Status,
                "Outpost: physical investment is reserved");
            AssertEqual(80, transfers.GetAvailable("Rimconemy_ConstructionDebris"),
                "Outpost: reserve does not consume physical stock");
            AssertEqual(30, transfers.GetReserved("Rimconemy_ConstructionDebris"),
                "Outpost: investment reservation is visible");
        }

        private static void TestInvestmentReplayDoesNotDuplicate()
        {
            var transfers = new Transfers.PhysicalTransferService();
            transfers.SetAvailable("Rimconemy_ConstructionDebris", 80);
            var outpost = new Outpost("outpost-test", "player", 60000L);
            OutpostInvestmentResult first = outpost.TryReserveInvestment(
                transfers, "rimconemy.tests", "invest-2", "Rimconemy_ConstructionDebris", 30, 60000L);
            OutpostInvestmentResult replay = outpost.TryReserveInvestment(
                transfers, "rimconemy.tests", "invest-2", "Rimconemy_ConstructionDebris", 30, 60000L);

            AssertEqual(first.TransferId, replay.TransferId, "Outpost: investment replay is idempotent");
            AssertEqual(30, transfers.GetReserved("Rimconemy_ConstructionDebris"),
                "Outpost: investment replay does not double-reserve");
        }

        private static void TestOutpostStateUsesAbsoluteTicks()
        {
            var outpost = new Outpost("outpost-ticks", "player", 1000L);
            outpost.ForceTransition(OutpostState.Active, "test activation", 2000L);
            outpost.UpdateEconomy(10, 1, 60000L);
            outpost.Tick(60000L);
            AssertEqual(60000L, outpost.LastUpdatedTick,
                "Outpost: state evaluation records absolute world tick");
            AssertEqual(9L, outpost.CurrentNet, "Outpost: net economy is deterministic");
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[OutpostInvestmentRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
