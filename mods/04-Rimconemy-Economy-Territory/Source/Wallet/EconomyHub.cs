using System.Collections.Generic;
using Rimconemy.Foundation.UI;
using Rimconemy.EconomyTerritory.Market;
using Rimconemy.EconomyTerritory.Outposts;
using UnityEngine;
using Verse;

namespace Rimconemy.EconomyTerritory.Wallet
{
    /// <summary>
    /// P2 economy surface. It reads existing wallet/market/outpost state and
    /// deliberately keeps mutations in WalletService/CreditsLedger.
    /// </summary>
    public sealed class EconomyHub : RimconemyMainTabWindow
    {
        private int _selectedTab;
        private Vector2 _scrollPosition;

        public override Vector2 InitialSize => new Vector2(680f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            var ledger = WalletService.GetOrCreateLedger();
            if (ledger == null)
            {
                RimconemyUi.DrawEmptyState(inRect, "Rimconemy.Economy.Unavailable");
                return;
            }

            float width = inRect.width - RimconemyTheme.DefaultScrollbarWidth;
            float y = inRect.y;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, width, 28f), "Rimconemy · Wirtschaft");
            y += 32f;
            RimconemyUi.DrawStatusBadge(new Rect(inRect.x, y, width, 24f),
                "Wallet: " + ledger.Balance + " Cr  ·  " + (ledger.Balance > 0 ? "OK" : "Leer"),
                ledger.Balance > 0 ? StatusLevel.Success : StatusLevel.Warn);
            y += 32f;
            _selectedTab = RimconemyUi.DrawTabs(new Rect(inRect.x, y, width, 30f),
                new List<string> { "Wallet", "Märkte", "Outposts" }, _selectedTab);
            y += 38f;

            var outer = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            var view = new Rect(0f, 0f, width, 520f);
            Widgets.BeginScrollView(outer, ref _scrollPosition, view);
            if (_selectedTab == 0) DrawWallet(0f, 0f, width, ledger);
            else if (_selectedTab == 1) DrawMarket(0f, 0f, width);
            else DrawOutposts(0f, 0f, width);
            Widgets.EndScrollView();
            RimconemyUi.ResetTextFontAndColor();
        }

        private static void DrawWallet(float x, float y, float width, CreditsLedger ledger)
        {
            RimconemyUi.DrawSectionTitle(new Rect(x, y, width, 26f), "Rimconemy.Economy.Wallet", GameFont.Medium);
            y += 32f;
            long change = ledger.NetChangeInLast(16);
            RimconemyUi.DrawStatCard(new Rect(x, y, width / 2f - 4f, 66f), "Cr", "Kontostand", ledger.Balance.ToString(), -1f,
                ledger.Balance > 0 ? StatusLevel.Success : StatusLevel.Warn);
            RimconemyUi.DrawStatCard(new Rect(x + width / 2f + 4f, y, width / 2f - 4f, 66f), "Δ", "Letzte 16", change.ToString(), -1f,
                change >= 0 ? StatusLevel.Info : StatusLevel.Warn);
            y += 80f;
            RimconemyUi.DrawSectionTitle(new Rect(x, y, width, 26f), "Rimconemy.Economy.Transactions", GameFont.Medium);
            y += 30f;
            int shown = 0;
            if (ledger.Transactions != null)
            {
                for (int i = ledger.Transactions.Count - 1; i >= 0 && shown < 16; i--)
                {
                    var tx = ledger.Transactions[i];
                    if (tx == null) continue;
                    long actualAmount = tx.ActualAmount != 0 ? tx.ActualAmount : tx.Amount;
                    string text = (actualAmount >= 0 ? "+" : "") + actualAmount + "  " + (tx.Reason ?? tx.RequestId ?? "tx");
                    RimconemyUi.DrawStatusBadge(new Rect(x, y, width, 22f), text,
                        actualAmount >= 0 ? StatusLevel.Info : StatusLevel.Warn);
                    y += 24f;
                    shown++;
                }
            }
            if (shown == 0) RimconemyUi.DrawEmptyState(new Rect(x, y, width, 36f), "Rimconemy.Economy.NoTransactions");
        }

        private static void DrawMarket(float x, float y, float width)
        {
            var market = MarketService.ForPlayerHomeMap();
            RimconemyUi.DrawSectionTitle(new Rect(x, y, width, 26f), "Rimconemy.Economy.Markets", GameFont.Medium);
            y += 32f;
            if (market == null)
            {
                RimconemyUi.DrawEmptyState(new Rect(x, y, width, 50f), "Rimconemy.Economy.NoMarket");
                return;
            }
            var prices = market.AllPrices();
            if (prices == null || prices.Count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(x, y, width, 50f), "Rimconemy.Economy.NoPrices");
                return;
            }
            foreach (var price in prices)
            {
                RimconemyUi.DrawRow(new Rect(x, y, width, 22f), price.ThingDefName,
                    "" + market.Price(price.ThingDefName) + " Cr  · Basis " + price.BasePrice + " Cr");
                y += 24f;
                RimconemyUi.DrawNeedBar(new Rect(x, y, width, 10f),
                    price.TargetStock <= 0 ? 1f : Mathf.Clamp01(price.CurrentStock / (float)price.TargetStock),
                    price.CurrentStock >= price.TargetStock ? RimconemyTheme.Success : RimconemyTheme.Warn,
                    "Bestand " + price.CurrentStock + "/" + price.TargetStock);
                y += 24f;
            }
        }

        private static void DrawOutposts(float x, float y, float width)
        {
            RimconemyUi.DrawSectionTitle(new Rect(x, y, width, 26f), "Rimconemy.Economy.Outposts", GameFont.Medium);
            y += 32f;
            var ledger = OutpostService.GetOrCreateLedger();
            if (ledger?.Outposts == null || ledger.Outposts.Count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(x, y, width, 50f), "Rimconemy.Economy.NoOutposts");
                return;
            }
            foreach (var pair in ledger.Outposts)
            {
                var outpost = pair.Value;
                if (outpost == null) continue;
                StatusLevel level = outpost.State == OutpostState.Active ? StatusLevel.Success
                    : outpost.State == OutpostState.Blocked || outpost.State == OutpostState.Disconnected ? StatusLevel.Warn
                    : outpost.State == OutpostState.Ruined ? StatusLevel.Error : StatusLevel.Info;
                RimconemyUi.DrawStatusBadge(new Rect(x, y, width, 22f),
                    pair.Key + "  [" + outpost.State + "]  Netto " + outpost.CurrentNet, level);
                y += 24f;
                if (outpost.State == OutpostState.Blocked)
                {
                    long remaining = outpost.DisconnectDeadlineTick - (Find.TickManager?.TicksGame ?? 0L);
                    RimconemyUi.DrawCountdown(new Rect(x, y, width, 30f), remaining, Outpost.DefaultBlockedTimeoutTicks, "Frist");
                    y += 38f;
                }
            }
        }
    }
}
