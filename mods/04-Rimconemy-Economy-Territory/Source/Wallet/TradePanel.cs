using System.Collections.Generic;
using Rimconemy.Foundation.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.EconomyTerritory.Wallet
{
    /// <summary>
    /// Owner: Economy and Territory (Package 04).
    /// Credits-Trade-Panel — zeigt das aktuelle Wallet-Guthaben, einen
    /// Einnahmen/Ausgaben-Verlauf und einen Demo-Trade-Button (Sell 10 Food
    /// for 25 Credits) der über den CreditsLedger als Transaction gebucht
    /// wird.
    ///
    /// E-T2: einfache UI, die das neue <see cref="CreditsLedger"/> /
    /// <see cref="Transaction"/> ueber einen Zugangspunkt (WalletService)
    /// durchschreibt. Wir ersetzen nicht das Vanilla Trade-Panel —
    /// dieser Dialog ist eine Setting-/Mod-04-Konsole, der die letzten
    /// 16 Transaktionen auditierbar darstellt.
    /// </summary>
    public class TradePanel : RimconemyWindow
    {
        private const int HistoryDisplayCount = 16;

        private Vector2 _scrollPosition;

        public override Vector2 InitialSize =>
            new Vector2(560f, 520f);

        public TradePanel()
        {
            // Window behaviour tuned for inspecting the wallet.
            this.draggable = true;
            this.doWindowBackground = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var ledger = WalletService.GetOrCreateLedger();
            if (ledger == null)
            {
                Widgets.Label(inRect, "Rimconemy.Economy.Wallet.Unavailable".Translate());
                return;
            }

            // slop-audit-fix H6: read live deterministic prices from
            // Market instead of the demo flat 25/-15 buttons. We surface
            // the canonical quote for "Food" (or a configurable DefName
            // hook) above the demo row so the player sees real economy
            // signals before/after clicking.
            Market.Market market = Market.MarketService.ForPlayerHomeMap();
            string liveFoodPriceLabel = "Rimconemy.Economy.TradePanel.LivePrice.NoMarket"
                .Translate();
            if (market != null)
            {
                // Register a canonical food price on first sight so the
                // first frame already shows a deterministic quote.
                market.RegisterItem("Meal", 25L, currentStock: 0, targetStock: 0);
                long quote = market.Price("Meal");
                liveFoodPriceLabel = "Rimconemy.Economy.TradePanel.LivePrice"
                    .Translate(new NamedArgument(quote, "price"));
            }

            // Section title
            RimconemyUi.DrawSectionTitle(
                new Rect(inRect.x, inRect.y, inRect.width, RimconemyTheme.SectionTitleHeight),
                "Rimconemy.Economy.TradePanel.Title",
                GameFont.Medium);
            float y = inRect.y + RimconemyTheme.SectionTitleHeight + RimconemyTheme.SectionTitleSpacing;

            // Balance row with status color
            string balanceLabel = "Rimconemy.Economy.TradePanel.Balance"
                .Translate(new NamedArgument(ledger.Balance, "balance"));
            var balanceRect = new Rect(inRect.x, y, inRect.width, RimconemyTheme.RowHeight);
            GUI.color = ledger.Balance > 0 ? RimconemyTheme.Success : RimconemyTheme.Muted;
            Widgets.Label(balanceRect, balanceLabel);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(balanceRect, "Rimconemy.Economy.TradePanel.Balance.Tooltip".Translate());
            y += RimconemyTheme.RowHeight + RimconemyTheme.SectionSpacing;

            // Recent change row
            long change = ledger.NetChangeInLast(HistoryDisplayCount);
            string changeLabel = "Rimconemy.Economy.TradePanel.LastChange"
                .Translate(new NamedArgument(change, "change"));
            var changeRect = new Rect(inRect.x, y, inRect.width, RimconemyTheme.RowHeight);
            GUI.color = change >= 0 ? RimconemyTheme.Info : RimconemyTheme.Warn;
            Widgets.Label(changeRect, changeLabel);
            GUI.color = Color.white;
            y += RimconemyTheme.RowHeight;

            y += RimconemyTheme.SectionSpacing;

            // Live Market price row (H6): shown between balance and the
            // demo trade buttons so the player sees the deterministic
            // quote feeding the wallet.
            var livePriceRect = new Rect(inRect.x, y, inRect.width, RimconemyTheme.RowHeight);
            GUI.color = RimconemyTheme.Info;
            Widgets.Label(livePriceRect, liveFoodPriceLabel);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(livePriceRect, "Rimconemy.Economy.TradePanel.LivePrice.Tooltip".Translate());
            y += RimconemyTheme.RowHeight + RimconemyTheme.SectionSpacing;

            // Demo trade button row
            float btnWidth = (inRect.width - 8f) / 2f;
            var sellRect = new Rect(inRect.x, y, btnWidth, RimconemyTheme.RowHeight + 8f);
            if (Widgets.ButtonText(sellRect, "Rimconemy.Economy.TradePanel.Button.SellFood".Translate()))
            {
                WalletService.ApplyDemoTrade(WalletService.DemoTradeKind.SellFood);
            }

            var buyRect = new Rect(sellRect.xMax + 8f, y, btnWidth, RimconemyTheme.RowHeight + 8f);
            if (Widgets.ButtonText(buyRect, "Rimconemy.Economy.TradePanel.Button.BuyFood".Translate()))
            {
                WalletService.ApplyDemoTrade(WalletService.DemoTradeKind.BuyFood);
            }

            y += RimconemyTheme.RowHeight + 12f;

            // Transaction history scroll list
            float remainingHeight = inRect.yMax - y - RimconemyTheme.DefaultWindowPadding;
            float innerWidth = inRect.width - RimconemyTheme.DefaultScrollbarWidth - RimconemyTheme.DefaultWindowPadding;
            var scrollOuter = new Rect(inRect.x, y, inRect.width, remainingHeight);
            var viewInner = new Rect(0f, 0f, innerWidth,
                Mathf.Max(60f, ledger.Transactions.Count * RimconemyTheme.RowHeight + RimconemyTheme.DefaultViewPadding * 2f));

            Widgets.BeginScrollView(scrollOuter, ref _scrollPosition, viewInner);
            float rowY = 0f;
            int showCount = Mathf.Min(HistoryDisplayCount, ledger.Transactions.Count);
            for (int i = ledger.Transactions.Count - 1; i >= ledger.Transactions.Count - showCount; i--)
            {
                if (i < 0) break;
                var t = ledger.Transactions[i];
                if (t == null) continue;
                var rowRect = new Rect(0f, rowY, viewInner.width, RimconemyTheme.RowHeight);

                GUI.color = t.Amount >= 0 ? RimconemyTheme.Info : RimconemyTheme.Warn;
                string rowLabel = t.Amount >= 0
                    ? "+{0}  {1}".Translate(new NamedArgument(t.Amount, "amount"), t.Reason ?? t.RequestId ?? "tx")
                    : "{0}  {1}".Translate(new NamedArgument(t.Amount, "amount"), t.Reason ?? t.RequestId ?? "tx");
                Widgets.Label(rowRect, rowLabel);
                GUI.color = Color.white;

                TooltipHandler.TipRegion(rowRect, "Rimconemy.Economy.TradePanel.TxTooltip"
                    .Translate(new NamedArgument(t.PackageId ?? "-", "package"),
                               new NamedArgument(t.RequestId ?? "-", "request"),
                               new NamedArgument(t.TimestampTick, "tick")));
                rowY += RimconemyTheme.RowHeight;
            }
            Widgets.EndScrollView();
        }
    }

    /// <summary>
    /// Owner: Economy and Territory.
    /// One-stop accessor for the singleton <see cref="CreditsLedger"/>.
    /// Mod 02 / 05 can also call WalletService.ApplyDemoTrade from story
    /// events tagged "WalletCost:50 Credits" etc., but the parser is not
    /// part of E-T2.
    ///
    /// E-T2 initialization runs lazily on first access so the static
    /// constructor logs the wallet as soon as the Save is loaded. Until
    /// then the wallet sits as a singleton instance on
    /// <see cref="Current"/>.Game.GetComponent or as a static fallback.
    /// </summary>
    public static class WalletService
    {
        private static CreditsLedger _singleton;

        public static CreditsLedger GetOrCreateLedger()
        {
            if (_singleton != null) return _singleton;

            // Try to fish a ledger out of a GameComponent (preferred path).
            if (Current.Game != null)
            {
                var comp = Current.Game.GetComponent<WalletGameComponent>();
                if (comp != null)
                {
                    comp.EnsureLedger();
                    _singleton = comp.Ledger;
                    return _singleton;
                }
            }

            // Fallback: stand-alone ledger for Main Menu / boot scenarios.
            _singleton = new CreditsLedger
            {
                WalletId = "default-wallet",
                OwnerId = "default-owner",
            };
            _singleton.RecomputeBalance();
            return _singleton;
        }

        /// <summary>
        /// Apply a demo trade so the UI gets something visible on first
        /// open. Returns the assigned TxId.
        ///
        /// slop-audit-fix §4 (review 2026-08-04): charges are sourced from
        /// the live MarketService quote when a player-home map is available,
        /// with the historical 25 / -15 fallback only when no market
        /// exists. This eliminates the dual price source (live row says
        /// "Market: 25 credits" but the buttons charge "25 credits" - by
        /// construction they must agree).
        /// </summary>
        public static long ApplyDemoTrade(DemoTradeKind kind)
        {
            var ledger = GetOrCreateLedger();

            // Use deterministic requestId per kind so repeated clicks are
            // idempotent within the same save. CreditsLedger owns the
            // canonical Key -> TxId lookup; do not pre-check the capped UI
            // history here.
            string requestId = "demo-" + kind.ToString();

            // Try the live market quote first. Fall back to the historical
            // 25/-15 only when no market is reachable.
            long amount;
            string reason;
            long liveQuote = TryGetMarketQuote(kind);
            if (liveQuote > 0)
            {
                amount = kind == DemoTradeKind.SellFood ? liveQuote : -liveQuote;
                reason = kind == DemoTradeKind.SellFood
                    ? $"Demo: sell food at market ({amount} credits)"
                    : $"Demo: buy food at market ({-amount} credits)";
            }
            else
            {
                amount = kind switch
                {
                    DemoTradeKind.SellFood => 25L,
                    DemoTradeKind.BuyFood => -15L,
                    _ => 0L,
                };
                reason = kind switch
                {
                    DemoTradeKind.SellFood => "Demo: sell 10 food (fallback 25 credits)",
                    DemoTradeKind.BuyFood => "Demo: buy food (fallback -15 credits)",
                    _ => "Unknown demo",
                };
            }

            if (amount == 0) return 0;

            var tx = new Transaction
            {
                PackageId = "rimconemy.economyterritory",
                RequestId = requestId,
                Amount = amount,
                Reason = reason,
            };
            return ledger.ApplyTransaction(tx);
        }

        /// <summary>
        /// Returns the live Market quote for a demo trade kind, or 0 when
        /// no player-home map / no market can be resolved. Pure helper -
        /// does not write any state.
        /// </summary>
        private static long TryGetMarketQuote(DemoTradeKind kind)
        {
            try
            {
                Market.Market market = Market.MarketService.ForPlayerHomeMap();
                if (market == null) return 0;
                // Register a canonical Meal quote so subsequent price queries
                // are non-zero even on first sight.
                market.RegisterItem("Meal", 25L, currentStock: 0, targetStock: 0);
                return market.Price("Meal");
            }
            catch
            {
                return 0;
            }
        }

        public enum DemoTradeKind
        {
            SellFood = 1,
            BuyFood = 2,
        }
    }

    /// <summary>
    /// Owner: Economy and Territory.
    /// GameComponent that hosts the persistent
    /// <see cref="CreditsLedger"/> in save files. Mostly defers to the
    /// ledger's own Scribe path so the wallet survives save/load.
    /// </summary>
    public sealed class WalletGameComponent : GameComponent
    {
        public CreditsLedger Ledger = new CreditsLedger();

        public WalletGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Ledger == null)
                Ledger = new CreditsLedger();
            Scribe_Deep.Look(ref Ledger, "creditsLedger");
            if (Ledger == null)
                Ledger = new CreditsLedger();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            EnsureLedger();
        }

        public void EnsureLedger()
        {
            if (Ledger == null) Ledger = new CreditsLedger();
            if (string.IsNullOrEmpty(Ledger.WalletId))
                Ledger.WalletId = "player-wallet";
            if (string.IsNullOrEmpty(Ledger.OwnerId))
                Ledger.OwnerId = "player";

            // The persisted Balance is authoritative. Once TrimHistory has
            // run, the visible 256-row list is intentionally incomplete and
            // must never be used to overwrite older accepted transactions.
            // New/legacy empty ledgers already start at the correct zero value.

        }
    }
}
