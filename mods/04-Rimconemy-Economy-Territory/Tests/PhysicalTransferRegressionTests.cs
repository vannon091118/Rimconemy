using System.Collections.Generic;
using Rimconemy.EconomyTerritory.Transfers;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.EconomyTerritory.Tests
{
    /// <summary>Red-first gates for the physical Reserve/Execute/Cancel contract.</summary>
    public static class PhysicalTransferRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("EconomyTerritory", "Physical transfer regression tests");

            _passed = 0;
            _failed = 0;

            TestReserveDoesNotConsume();
            TestExecuteConsumesExactlyOnce();
            TestCancelConsumesNothing();
            TestReplayReturnsOriginalResult();
            TestMissingStockBlocksWithoutPhantomConsumption();

            string summary = "[Rimconemy.EconomyTerritory] Physical transfer regression tests: "
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

        private static void TestReserveDoesNotConsume()
        {
            var service = new PhysicalTransferService();
            service.SetAvailable("Rimconemy_ConstructionDebris", 100);
            TransferResult result = service.ReservePhysicalTransfer(Request("reserve", 25));
            ts.Check(Equals(TransferStatus.Reserved, result.Status), "Transfer: reserve succeeds");
            ts.Check(Equals(100, service.GetAvailable("Rimconemy_ConstructionDebris")), "Transfer: reserve does not consume physical stock");
            ts.Check(Equals(25, service.GetReserved("Rimconemy_ConstructionDebris")), "Transfer: reserve records locked amount");
        }

        private static void TestExecuteConsumesExactlyOnce()
        {
            var service = new PhysicalTransferService();
            service.SetAvailable("Steel", 100);
            TransferRequest request = Request("execute", 40, "Steel");
            TransferResult reserved = service.ReservePhysicalTransfer(request);
            TransferResult executed = service.ExecutePhysicalTransfer(reserved.TransferId);
            TransferResult replay = service.ExecutePhysicalTransfer(reserved.TransferId);
            ts.Check(Equals(TransferStatus.Reserved, reserved.Status), "Transfer: execute setup reserves");
            ts.Check(Equals(TransferStatus.Executed, executed.Status), "Transfer: execute commits");
            ts.Check(Equals(TransferStatus.Executed, replay.Status), "Transfer: execute replay is stable");
            ts.Check(Equals(60, service.GetAvailable("Steel")), "Transfer: execute consumes exact amount once");
            ts.Check(Equals(0, service.GetReserved("Steel")), "Transfer: execute releases reservation");
        }

        private static void TestCancelConsumesNothing()
        {
            var service = new PhysicalTransferService();
            service.SetAvailable("WoodLog", 50);
            TransferRequest request = Request("cancel", 10, "WoodLog");
            TransferResult reserved = service.ReservePhysicalTransfer(request);
            TransferResult cancelled = service.CancelPhysicalTransfer(reserved.TransferId);
            ts.Check(Equals(TransferStatus.Cancelled, cancelled.Status), "Transfer: cancel commits");
            ts.Check(Equals(50, service.GetAvailable("WoodLog")), "Transfer: cancel consumes nothing");
            ts.Check(Equals(0, service.GetReserved("WoodLog")), "Transfer: cancel releases reservation");
        }

        private static void TestReplayReturnsOriginalResult()
        {
            var service = new PhysicalTransferService();
            service.SetAvailable("Chemfuel", 20);
            TransferRequest request = Request("replay", 5, "Chemfuel");
            TransferResult first = service.ReservePhysicalTransfer(request);
            TransferResult second = service.ReservePhysicalTransfer(request);
            ts.Check(Equals(first.TransferId, second.TransferId), "Transfer: reserve replay returns same transfer");
            ts.Check(Equals(first.Status, second.Status), "Transfer: reserve replay returns same status");
            ts.Check(Equals(5, service.GetReserved("Chemfuel")), "Transfer: replay does not double-lock stock");
        }

        private static void TestMissingStockBlocksWithoutPhantomConsumption()
        {
            var service = new PhysicalTransferService();
            service.SetAvailable("Steel", 2);
            TransferResult result = service.ReservePhysicalTransfer(Request("blocked", 3, "Steel"));
            ts.Check(Equals(TransferStatus.Blocked, result.Status), "Transfer: insufficient stock is blocked");
            ts.Check(Equals(2, service.GetAvailable("Steel")), "Transfer: blocked request leaves stock unchanged");
            ts.Check(Equals(0, service.GetReserved("Steel")), "Transfer: blocked request creates no reservation");
        }

        private static TransferRequest Request(string requestId, int amount, string resourceId = "Rimconemy_ConstructionDebris")
        {
            return new TransferRequest
            {
                PackageId = "rimconemy.tests",
                RequestId = requestId,
                IdempotencyKey = "tests|" + requestId,
                ResourceId = resourceId,
                Amount = amount,
                CurrentTick = 60000L,
            };
        }

    }
}
