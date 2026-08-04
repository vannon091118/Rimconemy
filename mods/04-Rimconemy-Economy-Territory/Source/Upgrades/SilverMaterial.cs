using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.EconomyTerritory.Upgrades
{
    /// <summary>
    /// Owner: Economy and Territory (Package 04).
    /// Silber (Silver) als eigenes Upgrade-Material fuer das Setting
    /// Rimconemy. Wir kapseln Silber NICHT als Credits, weil Credits ein
    /// reiner Wallet-Stand sind und Silber ein physisches Item ist, das die
    /// Werkbank verbraucht.
    ///
    /// E-T4: neben dem Wallet (Credits) fuehren wir eine Silber-Bilanz
    /// pro Spieler-Fraktion. Die Bilanz laesst sich parallel zur
    /// CreditsLedger persistieren.
    /// </summary>
    public sealed class SilverLedger : IExposable
    {
        public const string LogMarker = "v1";
        public const long MaxBalance = 100_000L;
        public const int MaxHistoryRetained = 64;

        public string OwnerId;
        public long Balance;
        public long LastUpdatedTick;
        public List<SilverTransaction> Transactions = new List<SilverTransaction>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref OwnerId, "silverOwnerId", "");
            Scribe_Values.Look(ref Balance, "silverBalance", 0L);
            Scribe_Values.Look(ref LastUpdatedTick, "silverLastTick", 0L);
            Scribe_Collections.Look(ref Transactions, "silverTransactions", LookMode.Deep);
            if (Transactions == null) Transactions = new List<SilverTransaction>();
        }

        /// <summary>
        /// Add or remove silver by a signed amount. Returns the running
        /// balance after the operation.
        /// </summary>
        public long ApplyDelta(long amount, string reason)
        {
            long newBalance = Balance + amount;
            if (newBalance < 0) newBalance = 0;
            if (newBalance > MaxBalance) newBalance = MaxBalance;
            Balance = newBalance;

            Transactions.Add(new SilverTransaction
            {
                Amount = amount,
                Reason = reason ?? "Silver change",
                TimestampTick = LastUpdatedTick,
            });
            if (Transactions.Count > MaxHistoryRetained)
                Transactions.RemoveAt(0);

            return Balance;
        }

        /// <summary>
        /// Drain silver for an upgrade. Returns true if the wallet had
        /// enough; false if the request was rejected.
        /// </summary>
        public bool TrySpend(long amount, string upgradeId)
        {
            if (amount <= 0) return true;
            if (Balance < amount) return false;
            ApplyDelta(-amount, $"Silver upgrade: {upgradeId}");
            return true;
        }
    }

    /// <summary>
    /// Single silver-movement entry. Persisted deep.
    /// </summary>
    public sealed class SilverTransaction : IExposable
    {
        public long Amount;
        public string Reason;
        public long TimestampTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Amount, "amount", 0L);
            Scribe_Values.Look(ref Reason, "reason", "");
            Scribe_Values.Look(ref TimestampTick, "timestampTick", 0L);
        }
    }

    /// <summary>
    /// Static accessor + GameComponent hosting the player's
    /// <see cref="SilverLedger"/>. Mirrors <c>WalletService</c>.
    /// </summary>
    public static class SilverService
    {
        private static SilverLedger _standAlone;

        public static SilverLedger GetOrCreateLedger()
        {
            if (Current.Game == null) return StandAlone();

            var comp = Current.Game.GetComponent<SilverGameComponent>();
            if (comp != null)
            {
                comp.EnsureLedger();
                return comp.Ledger;
            }
            return StandAlone();
        }

        private static SilverLedger StandAlone()
        {
            if (_standAlone == null)
                _standAlone = new SilverLedger { OwnerId = "default-owner" };
            return _standAlone;
        }

        /// <summary>
        /// Stamp a tiny starter amount once per save so the player sees the
        /// upgrade material exist. Idempotent via owner-key match.
        /// </summary>
        public static bool StampStarterAmountIfAbsent(long amount, string reason)
        {
            var ledger = GetOrCreateLedger();
            if (ledger.Balance >= amount) return false;
            if (ledger.Transactions.Count > 0) return false;
            ledger.ApplyDelta(amount, reason ?? "Starter silver");
            return true;
        }
    }

    /// <summary>
    /// GameComponent persisting the player's silver-stock in save games.
    /// </summary>
    public sealed class SilverGameComponent : GameComponent
    {
        public SilverLedger Ledger = new SilverLedger();

        public SilverGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Ledger == null) Ledger = new SilverLedger();
            Scribe_Deep.Look(ref Ledger, "silverLedger");
            if (Ledger == null) Ledger = new SilverLedger();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            EnsureLedger();
        }

        public void EnsureLedger()
        {
            if (Ledger == null) Ledger = new SilverLedger();
            if (string.IsNullOrEmpty(Ledger.OwnerId))
                Ledger.OwnerId = "player";
        }
    }
}
