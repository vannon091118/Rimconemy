using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimconemy.EconomyTerritory.Wallet
{
    /// <summary>
    /// Owner: Economy and Territory (Package 04).
    /// Credits ledger snapshot. Credits are pure wallet data, not items.
    /// Idempotency: every state mutation uses (PackageId, RequestId) and
    /// produces a TransactionId; repetitions return the same result.
    /// SPIKE: API-TRADE-01 (1.6 TradeSession/MarketValue alignment unverified).
    ///
    /// E-T1: extends the historical snapshot with a real transaction list
    /// (Transaction class) and net-balance computation. The double-entry
    /// backend stores every change, so the wallet is auditable.
    ///
    /// Audit-fix (2026-08-04):
    ///   - Idempotency keys are tracked in a SEPARATE HashSet<string> that
    ///     is NOT trimmed by the UI-history window. This prevents the
    ///     "history trimmed → idempotency lost → duplicate transaction" bug
    ///     (Befund 1).
    ///   - Transactions that would exceed MaxBalance or underflow below 0
    ///     are REJECTED with a result code instead of silently clamping the
    ///     balance while recording the original amount (Befund 2).
    ///   - RecomputeBalance() now sums only the recorded (possibly partial)
    ///     ActualAmount so balance and history never diverge.
    /// </summary>
    public sealed class CreditsLedger : IExposable
    {
        public const string CurrencyId = "credits";
        public const string LogMarker = "v1";
        public const long MaxBalance = 1_000_000_000;
        public const int MaxHistoryRetained = 256;

        public string WalletId;
        public string OwnerId;
        public long Balance;
        public long LockedBalance;
        public long LastTransactionId;
        public long LastUpdatedTick;

        // E-T1: real transaction history. Each entry is one tx record.
        // Persisted as Deep (look_mode=LookMode.Deep) so we round-trip
        // safely on save/load.
        public List<Transaction> Transactions = new List<Transaction>();

        // Audit-fix (Befund 1): the idempotency index owns the replay result
        // directly. It is independent from the capped UI history, so a replay
        // never needs to search Transactions for its original TxId.
        private Dictionary<string, long> _idempotencyTxIds = new Dictionary<string, long>();
        private List<string> _idempotencyInsertionOrder = new List<string>();
        private List<string> _idempotencyList; // Scribe helper
        private List<long> _idempotencyTxIdList; // Scribe helper

        // Running amount of transactions that have fallen out of the retained
        // UI history. This keeps RecomputeBalance() correct after trimming.
        private long _trimmedTransactionAmount;
        private bool _historyWasTrimmed;
        // False on legacy saves that predate the trim metadata. Such a save
        // retains its persisted Balance, but cannot safely reconstruct a full
        // balance from a possibly truncated transaction list.
        private bool _historyCompletenessKnown;

        public CreditsLedger()
        {
            _historyCompletenessKnown = true;
        }

        private const int IdempotencyKeyMaxCount = 4096;
        private const int IdempotencyKeyRetainedAfterPrune = 3072;

        public void ExposeData()
        {
            Scribe_Values.Look(ref WalletId, "walletId", "");
            Scribe_Values.Look(ref OwnerId, "ownerId", "");
            Scribe_Values.Look(ref Balance, "balance", 0L);
            Scribe_Values.Look(ref LockedBalance, "lockedBalance", 0L);
            Scribe_Values.Look(ref LastTransactionId, "lastTransactionId", 0L);
            Scribe_Values.Look(ref LastUpdatedTick, "lastUpdatedTick", 0L);
            Scribe_Values.Look(ref _trimmedTransactionAmount, "trimmedTransactionAmount", 0L);
            Scribe_Values.Look(ref _historyWasTrimmed, "historyWasTrimmed", false);
            Scribe_Values.Look(ref _historyCompletenessKnown, "historyCompletenessKnown", false);
            Scribe_Collections.Look(ref Transactions, "transactions", LookMode.Deep);

            // Audit-fix (Befund 1): persist the idempotency key and its
            // original TxId in insertion order. This remains independent of
            // TrimHistory's 256-entry UI window.
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _idempotencyList = new List<string>(_idempotencyInsertionOrder);
                _idempotencyTxIdList = new List<long>(_idempotencyInsertionOrder.Count);
                foreach (var key in _idempotencyInsertionOrder)
                {
                    long txId;
                    _idempotencyTxIds.TryGetValue(key, out txId);
                    _idempotencyTxIdList.Add(txId);
                }
            }
            Scribe_Collections.Look(ref _idempotencyList, "idempotencyKeys", LookMode.Value);
            Scribe_Collections.Look(ref _idempotencyTxIdList, "idempotencyTxIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _idempotencyTxIds = new Dictionary<string, long>();
                _idempotencyInsertionOrder = new List<string>();
                if (_idempotencyList != null)
                {
                    for (int i = 0; i < _idempotencyList.Count; i++)
                    {
                        string key = _idempotencyList[i];
                        if (string.IsNullOrEmpty(key) || _idempotencyTxIds.ContainsKey(key))
                            continue;

                        long txId = 0;
                        bool hasPersistedTxId = _idempotencyTxIdList != null
                            && i < _idempotencyTxIdList.Count;
                        if (hasPersistedTxId)
                            txId = _idempotencyTxIdList[i];
                        else
                        {
                            // Legacy save: recover the TxId when the retained
                            // history still contains it. If it was already
                            // trimmed, keep a tombstone (0) so replay is
                            // rejected rather than applied a second time.
                            var historical = Transactions.FirstOrDefault(t =>
                                t != null && BuildIdempotencyKey(t.PackageId, t.RequestId) == key);
                            txId = historical?.TxId ?? 0;
                        }

                        _idempotencyTxIds[key] = txId;
                        _idempotencyInsertionOrder.Add(key);
                    }
                }
                else if (Transactions != null)
                {
                    // Very old saves did not serialize an idempotency index.
                    // Rebuild what can still be identified from retained
                    // history; trimmed records cannot be recovered and are
                    // intentionally not guessed.
                    foreach (var historical in Transactions)
                    {
                        if (historical == null) continue;
                        string key = BuildIdempotencyKey(historical.PackageId, historical.RequestId);
                        if (string.IsNullOrEmpty(key) || _idempotencyTxIds.ContainsKey(key))
                            continue;
                        _idempotencyTxIds[key] = historical.TxId;
                        _idempotencyInsertionOrder.Add(key);
                    }
                }
                _idempotencyList = null;
                _idempotencyTxIdList = null;
            }

            if (Transactions == null)
                Transactions = new List<Transaction>();
            if (_idempotencyTxIds == null)
                _idempotencyTxIds = new Dictionary<string, long>();
            if (_idempotencyInsertionOrder == null)
                _idempotencyInsertionOrder = new List<string>();
        }

        /// <summary>
        /// Apply a transaction. Returns the assigned TransactionId or
        /// returns a previously-issued id when the (PackageId, RequestId)
        /// pair has already been applied (idempotency).
        ///
        /// Audit-fix (Befund 2): if the transaction would cause the balance
        /// to exceed MaxBalance or underflow below 0, the transaction is
        /// REJECTED and returns -1 instead of silently clamping. Callers
        /// must check for negative return values.
        /// </summary>
        /// <returns>
        /// Positive TransactionId on success, previously-issued id on
        /// idempotent replay, or -1 if the transaction was rejected
        /// (balance would exceed limits).
        /// </returns>
        public long ApplyTransaction(Transaction tx)
        {
            if (tx == null) return LastTransactionId;

            // Audit-fix (Befund 1): replay directly from the durable
            // Key→TxId index. The capped Transactions list is not involved.
            string ik = BuildIdempotencyKey(tx.PackageId, tx.RequestId);
            if (!string.IsNullOrEmpty(ik) && _idempotencyTxIds.TryGetValue(ik, out long originalTxId))
            {
                if (originalTxId > 0)
                    return originalTxId;

                // A legacy save can contain the key after its transaction was
                // trimmed, but cannot recover the historical TxId. Rejecting
                // is safe; creating a new transaction would violate idempotency.
                Log.Warning($"[CreditsLedger] Transaction replay rejected: historical TxId unavailable for key={ik}.");
                return -1;
            }

            // Audit-fix (Befund 2): reject on underflow/overflow instead of clamping.
            // Check the operands before adding so a long overflow cannot wrap
            // into an apparently valid balance.
            if ((tx.Amount > 0 && Balance > MaxBalance - tx.Amount)
                || (tx.Amount < 0
                    && (tx.Amount == long.MinValue || Balance < -tx.Amount)))
            {
                Log.Warning($"[CreditsLedger] Transaction rejected: balance={Balance} amount={tx.Amount} would exceed limits (max={MaxBalance}). (PackageId={tx.PackageId}, RequestId={tx.RequestId}, Reason={tx.Reason})");
                return -1;
            }

            long newBalance = Balance + tx.Amount;
            if (newBalance < 0 || newBalance > MaxBalance)
            {
                Log.Warning($"[CreditsLedger] Transaction rejected: balance={Balance} amount={tx.Amount} would produce {newBalance} (max={MaxBalance}). (PackageId={tx.PackageId}, RequestId={tx.RequestId}, Reason={tx.Reason})");
                return -1;
            }

            LastTransactionId++;
            tx.TxId = LastTransactionId;
            tx.TimestampTick = tx.TimestampTick > 0 ? tx.TimestampTick : LastUpdatedTick;
            tx.ActualAmount = tx.Amount; // Audit-fix: record the actual applied delta
            Balance = newBalance;

            Transactions.Add(tx);

            // Do not promote a legacy save with unknown/truncated history to
            // "complete" merely because a new transaction was accepted.
            // New instances already start complete in the constructor.

            // Audit-fix (Befund 1): record the TxId in the durable index and
            // retain explicit insertion order for deterministic pruning.
            if (!string.IsNullOrEmpty(ik) && !_idempotencyTxIds.ContainsKey(ik))
            {
                _idempotencyTxIds[ik] = tx.TxId;
                _idempotencyInsertionOrder.Add(ik);
                PruneIdempotencyKeys();
            }

            TrimHistory();

            return tx.TxId;
        }

        /// <summary>
        /// Reverse an existing transaction by posting a paired counter-tx
        /// with sign-flipped amount. The original record stays in history;
        /// the reverse tx is a new row so audit-trails are linear.
        /// Returns the reverse-tx TxId, or -1 if the original tx is missing
        /// or the reversal would violate balance limits.
        /// </summary>
        public long ReverseTransaction(long txId, string reason)
        {
            if (txId <= 0) return -1;
            Transaction original = null;
            foreach (var t in Transactions)
            {
                if (t != null && t.TxId == txId)
                {
                    original = t;
                    break;
                }
            }
            if (original == null) return -1;

            var reverse = new Transaction
            {
                PackageId = original.PackageId,
                RequestId = "reverse-" + original.TxId,
                Reason = string.IsNullOrEmpty(reason) ? "Storno" : reason,
                Amount = -original.Amount,
                TimestampTick = LastUpdatedTick,
            };
            return ApplyTransaction(reverse);
        }

        /// <summary>
        /// Recompute Balance from the transaction history.
        /// Use only when the persisted balance gets out of sync (defensive).
        /// Audit-fix (Befund 2): uses ActualAmount so balance and history
        /// never diverge. Falls back to Amount for pre-fix saves where
        /// ActualAmount was not persisted (default 0).
        /// </summary>
        public long RecomputeBalance()
        {
            if (!_historyCompletenessKnown)
            {
                // Legacy save: the persisted balance is the only complete
                // source of truth available after an unknown history trim.
                return Balance;
            }

            long sum = _historyWasTrimmed ? _trimmedTransactionAmount : 0;
            for (int i = 0; i < Transactions.Count; i++)
            {
                var t = Transactions[i];
                if (t != null)
                    sum += t.ActualAmount != 0 ? t.ActualAmount : t.Amount;
            }
            if (sum < 0) sum = 0;
            if (sum > MaxBalance) sum = MaxBalance;
            Balance = sum;
            return Balance;
        }

        /// <summary>Net change from the past N transactions.</summary>
        public long NetChangeInLast(int count)
        {
            if (Transactions == null || Transactions.Count == 0 || count <= 0) return 0;
            int start = Mathf.Max(0, Transactions.Count - count);
            long sum = 0;
            for (int i = start; i < Transactions.Count; i++)
            {
                var t = Transactions[i];
                if (t != null)
                    sum += t.ActualAmount != 0 ? t.ActualAmount : t.Amount;
            }
            return sum;
        }

        private void TrimHistory()
        {
            // Defensive: keep only the most-recent N entries. The original
            // entry stays because we never rewrite history, so an entry
            // "drops" out only when the window slides.
            if (Transactions.Count > MaxHistoryRetained)
            {
                int drop = Transactions.Count - MaxHistoryRetained;
                for (int i = 0; i < drop; i++)
                {
                    var removed = Transactions[i];
                    if (removed != null)
                        _trimmedTransactionAmount += removed.ActualAmount != 0
                            ? removed.ActualAmount : removed.Amount;
                }
                _historyWasTrimmed = true;
                Transactions.RemoveRange(0, drop);
            }
        }

        /// <summary>Prune idempotency keys when they exceed the cap.</summary>
        private void PruneIdempotencyKeys()
        {
            if (_idempotencyTxIds.Count <= IdempotencyKeyMaxCount)
                return;

            int removeCount = _idempotencyTxIds.Count - IdempotencyKeyRetainedAfterPrune;
            for (int i = 0; i < removeCount && _idempotencyInsertionOrder.Count > 0; i++)
            {
                string oldest = _idempotencyInsertionOrder[0];
                _idempotencyInsertionOrder.RemoveAt(0);
                _idempotencyTxIds.Remove(oldest);
            }
        }

        private static string BuildIdempotencyKey(string packageId, string requestId)
        {
            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(requestId))
                return null;
            return packageId + "|" + requestId;
        }
    }

    /// <summary>
    /// One entry in the credits ledger. Persisted deep via Scribe.
    /// Sign convention: positive Amount = income, negative = cost.
    /// </summary>
    public sealed class Transaction : IExposable
    {
        public long TxId;
        public string PackageId;
        public string RequestId;
        public string Reason;
        public long Amount;
        public long TimestampTick;

        /// <summary>
        /// Audit-fix (Befund 2): the amount that was actually applied to the
        /// balance. Identical to <see cref="Amount"/> for accepted transactions;
        /// exists so <see cref="CreditsLedger.RecomputeBalance"/> can sum
        /// recorded deltas without diverging from the persisted Balance field.
        /// </summary>
        public long ActualAmount;

        public void ExposeData()
        {
            Scribe_Values.Look(ref TxId, "txId", 0L);
            Scribe_Values.Look(ref PackageId, "packageId", "");
            Scribe_Values.Look(ref RequestId, "requestId", "");
            Scribe_Values.Look(ref Reason, "reason", "");
            Scribe_Values.Look(ref Amount, "amount", 0L);
            Scribe_Values.Look(ref TimestampTick, "timestampTick", 0L);
            // Audit-fix (Befund 2): persist ActualAmount. Default 0 means
            // pre-fix saves will compute RecomputeBalance as before (sum of
            // Amount), which is safe because pre-fix saves never had clamping.
            Scribe_Values.Look(ref ActualAmount, "actualAmount", 0L);
        }
    }
}
