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
            TestUnmannedAutoFactor();
            TestMannedFactorFullOutput();
            TestPlannedOutpostProducesZero();

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

        // D-Harmo §31.4: Unbemannt → 30 %, bemannt → 100 %.
        private static void TestUnmannedAutoFactor()
        {
            var outpost = new Outpost("outpost-unmanned", "player", 1000L);
            outpost.ForceTransition(OutpostState.Active, "test activation", 2000L);
            outpost.UpdateEconomy(10, 1, 60000L);
            outpost.StationedPawnCount = 0;
            AssertEqual(3L, outpost.EffectiveGross,
                "Outpost: unmanned Active produces 30 % of gross (auto-faktor niedrig)");
        }

        private static void TestMannedFactorFullOutput()
        {
            var outpost = new Outpost("outpost-manned", "player", 1000L);
            outpost.ForceTransition(OutpostState.Active, "test activation", 2000L);
            outpost.UpdateEconomy(10, 1, 60000L);
            outpost.StationedPawnCount = 2;
            AssertEqual(10L, outpost.EffectiveGross,
                "Outpost: manned Active produces 100 % of gross (voller Output)");
        }

        private static void TestPlannedOutpostProducesZero()
        {
            var outpost = new Outpost("outpost-planned", "player", 1000L);
            // State is Planned (default).
            outpost.UpdateEconomy(10, 1, 2000L);
            outpost.StationedPawnCount = 5;
            AssertEqual(0L, outpost.EffectiveGross,
                "Outpost: Planned state yields zero output (gate, not auto-promote)");
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
