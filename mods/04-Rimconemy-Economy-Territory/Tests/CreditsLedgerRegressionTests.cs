using System.Collections.Generic;
using Rimconemy.EconomyTerritory.Wallet;
using Verse;

namespace Rimconemy.EconomyTerritory.Tests
{
    /// <summary>Regression tests for the durable CreditsLedger idempotency index.</summary>
    public static class CreditsLedgerRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            TestReplayAfterHistoryTrimReturnsOriginalTxId();
            TestOverflowAndUnderflowAreRejectedWithoutMutation();
            TestCountCapPrunesOldestIdempotencyKeysDeterministically();
            TestRecomputeBalanceIncludesTrimmedHistory();

            string summary = "[Rimconemy.EconomyTerritory] CreditsLedger regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void TestReplayAfterHistoryTrimReturnsOriginalTxId()
        {
            var ledger = new CreditsLedger { Balance = 0 };
            long firstId = ledger.ApplyTransaction(Transaction("first", 1));

            // Force the original record out of the 256-entry UI history.
            for (int i = 0; i < CreditsLedger.MaxHistoryRetained + 1; i++)
                ledger.ApplyTransaction(Transaction("filler-" + i, 1));

            bool historyTrimmed = ledger.Transactions.Count == CreditsLedger.MaxHistoryRetained;
            AssertTrue(historyTrimmed, "Ledger: history is capped independently");

            long replayId = ledger.ApplyTransaction(Transaction("first", 1));
            AssertEqual(firstId, replayId, "Ledger: replay returns original TxId");
            AssertEqual(CreditsLedger.MaxHistoryRetained + 2L, ledger.LastTransactionId,
                "Ledger: replay does not create a new transaction");
            AssertEqual(CreditsLedger.MaxHistoryRetained + 2L, ledger.Balance,
                "Ledger: replay leaves balance unchanged");
        }

        private static void TestOverflowAndUnderflowAreRejectedWithoutMutation()
        {
            var ledger = new CreditsLedger { Balance = CreditsLedger.MaxBalance };
            long overflowId = ledger.ApplyTransaction(Transaction("overflow", 1));
            AssertEqual(-1L, overflowId, "Ledger: overflow rejected");
            AssertEqual(0L, ledger.LastTransactionId, "Ledger: overflow leaves TxId unchanged");
            AssertEqual(CreditsLedger.MaxBalance, ledger.Balance, "Ledger: overflow leaves balance unchanged");

            ledger.Balance = 0;
            long underflowId = ledger.ApplyTransaction(Transaction("underflow", long.MinValue));
            AssertEqual(-1L, underflowId, "Ledger: long underflow rejected");
            AssertEqual(0L, ledger.LastTransactionId, "Ledger: underflow leaves TxId unchanged");
            AssertEqual(0L, ledger.Balance, "Ledger: underflow leaves balance unchanged");
        }

        private static void TestRecomputeBalanceIncludesTrimmedHistory()
        {
            var ledger = new CreditsLedger { Balance = 0 };
            for (int i = 0; i < CreditsLedger.MaxHistoryRetained + 10; i++)
                ledger.ApplyTransaction(Transaction("recompute-" + i, 1));

            ledger.Balance = -123;
            AssertEqual(CreditsLedger.MaxHistoryRetained + 10L, ledger.RecomputeBalance(),
                "Ledger: recompute includes trimmed transaction base");
        }

        private static void TestCountCapPrunesOldestIdempotencyKeysDeterministically()
        {
            var ledger = new CreditsLedger { Balance = 0 };
            for (int i = 0; i < 4100; i++)
                ledger.ApplyTransaction(Transaction("count-" + i, 1));

            // The durable index keeps the configured post-prune window and
            // does not rebuild from the 256-entry UI history.
            long oldestReplay = ledger.ApplyTransaction(Transaction("count-0", 1));
            AssertEqual(4101L, oldestReplay, "Ledger: pruned oldest key can be safely reissued");
            long retainedReplay = ledger.ApplyTransaction(Transaction("count-4099", 1));
            AssertTrue(retainedReplay > 0 && retainedReplay < 4101,
                "Ledger: newest key remains idempotent after prune");
            AssertEqual(4101L, ledger.LastTransactionId,
                "Ledger: only the deliberately pruned replay creates a transaction");
        }

        private static Transaction Transaction(string requestId, long amount)
        {
            return new Transaction
            {
                PackageId = "rimconemy.tests",
                RequestId = requestId,
                Amount = amount,
                Reason = "regression",
            };
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[CreditsLedgerRegression] " + label); }
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[CreditsLedgerRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
