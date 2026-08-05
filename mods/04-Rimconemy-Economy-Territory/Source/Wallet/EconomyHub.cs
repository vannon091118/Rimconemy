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
            RimconemyUi.DrawFeatureStatus(
                new Rect(inRect.x, y, width, RimconemyTheme.RowHeight * 2f + 6f),
                "PARTIAL · Wallet/Markt-Daten",
                "OPEN · physische Transfers und vollständige Weltkartenlogistik sind noch nicht aktiv.",
                StatusLevel.Warn);
            y += RimconemyTheme.RowHeight * 2f + 6f + RimconemyTheme.SectionSpacing;
            _selectedTab = RimconemyUi.DrawTabs(new Rect(inRect.x, y, width, 30f),
                new List<string> { "Wallet", "Märkte", "Outposts" }, _selectedTab);
            y += 38f;

            var outer = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            float contentHeight = _selectedTab == 0
                ? WalletContentHeight(ledger)
                : _selectedTab == 1
                    ? MarketContentHeight()
                    : OutpostContentHeight();
            var view = new Rect(0f, 0f, width, Mathf.Max(520f, contentHeight));
            Widgets.BeginScrollView(outer, ref _scrollPosition, view);
            if (_selectedTab == 0) DrawWallet(0f, 0f, width, ledger);
            else if (_selectedTab == 1) DrawMarket(0f, 0f, width);
            else DrawOutposts(0f, 0f, width);
            Widgets.EndScrollView();
            RimconemyUi.ResetTextFontAndColor();
        }

        private static float WalletContentHeight(CreditsLedger ledger)
        {
            int transactionCount = 0;
            if (ledger?.Transactions != null)
            {
                for (int i = ledger.Transactions.Count - 1; i >= 0 && transactionCount < 16; i--)
                    if (ledger.Transactions[i] != null) transactionCount++;
            }
            // DrawWallet starts transaction content at y=142 after the
            // title, cards, and transaction-section header. Each row consumes
            // 24px; the empty-state fallback consumes 36px.
            return 142f + (transactionCount > 0 ? transactionCount * 24f : 36f);
        }

        private static float MarketContentHeight()
        {
            var market = MarketService.ForPlayerHomeMap();
            int count = market?.AllPrices()?.Count ?? 0;
            // DrawMarket starts its first price at y=32 after the title.
            // Each price consumes 58px (row, stock bar, and gaps); the empty
            // state consumes 50px.
            return 32f + (count > 0 ? count * 58f : 50f);
        }

        private static float OutpostContentHeight()
        {
            var ledger = OutpostService.GetOrCreateLedger();
            // DrawOutposts starts its first row at y=32 after the section
            // title. An empty state consumes 50px; each normal row advances
            // 24px (22px badge + 2px trailing gap), a blocked countdown
            // adds 38px, and a repair button adds 28px for Disconnected/Ruined.
            // Trailing "+ Settlement planen" button adds 28px.
            if (ledger?.Outposts == null || ledger.Outposts.Count == 0)
                return 32f + 54f + 28f; // empty state (50px badge + 4px gap) + trailing plan button

            float height = 32f;
            foreach (var pair in ledger.Outposts)
            {
                if (pair.Value == null) continue;
                height += 24f;
                if (pair.Value.State == OutpostState.Blocked) height += 38f;
                if (pair.Value.State == OutpostState.Disconnected || pair.Value.State == OutpostState.Ruined)
                    height += 28f; // repair button row
            }
            height += 28f; // trailing "+ Settlement planen" button
            return height;
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
            y += 74f;

            // ── Interaktive Handels-Aktionen ──────────────────────
            float btnW = (width - 8f) / 2f;
            if (Widgets.ButtonText(new Rect(x, y, btnW, 26f), "+25 Cr (Nahrung verkaufen)"))
            {
                WalletService.ApplyDemoTrade(WalletService.DemoTradeKind.SellFood);
            }
            if (Widgets.ButtonText(new Rect(x + btnW + 8f, y, btnW, 26f), "-15 Cr (Nahrung kaufen)"))
            {
                WalletService.ApplyDemoTrade(WalletService.DemoTradeKind.BuyFood);
            }
            y += 34f;

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
                // E1 Caller: "Settlement planen" Button. Ohne aktiven Eintrag:
                // legt einen neuen Outpost im Planned-State an und ruft
                // TryReserveInvestment mit dem Default-Gründungs-CostSet auf.
                if (Widgets.ButtonText(new Rect(x, y + 54f, width, 28f),
                    "+ Settlement planen (Wood/Steel/Credits)"))
                {
                    TryPlanSettlementFromHub(ledger);
                }
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
                    pair.Key + "  [" + outpost.State + "]  Netto " + outpost.CurrentNet
                    + " (Stationiert: " + outpost.StationedPawnCount + ")", level);
                y += 24f;
                if (outpost.State == OutpostState.Blocked)
                {
                    long remaining = outpost.DisconnectDeadlineTick - (Find.TickManager?.TicksGame ?? 0L);
                    RimconemyUi.DrawCountdown(new Rect(x, y, width, 30f), remaining, Outpost.DefaultBlockedTimeoutTicks, "Frist");
                    y += 38f;
                }
                // E1: Reparatur-Invest-Pfad für disconnected/ruined Outposts.
                if (outpost.State == OutpostState.Disconnected || outpost.State == OutpostState.Ruined)
                {
                    if (Widgets.ButtonText(new Rect(x, y, width, 24f), "Reparieren: 60 Cr"))
                    {
                        TryRepairInvestmentFromHub(outpost, ledger);
                    }
                    y += 28f;
                }
            }
            // E1 Caller für die Plan-Phase: Liste + Plan-Button.
            if (Widgets.ButtonText(new Rect(x, y, width, 28f),
                "+ Settlement planen (Wood/Steel/Credits)"))
            {
                TryPlanSettlementFromHub(ledger);
            }
        }

        // D-Harmo §31.4 Caller: legt einen planned Outpost an und ruft
        // TryReserveInvestment mit dem D3-konformen CostSet auf
        // (Wood + Steel + Credits; KEIN Wand-Debris, KEIN StuffMaterial).
        private static void TryPlanSettlementFromHub(OutpostLedger ledger)
        {
            try
            {
                long now = Find.TickManager?.TicksGame ?? 0L;
                string newId = "settlement-" + (ledger?.Outposts?.Count ?? 0) + "-" + now;
                var op = OutpostNetwork.Register(newId, "player");
                if (op == null) return;

                // E2: Konsumiert Holz + Stahl physisch über PhysicalTransferService.
                var transfers = new Rimconemy.EconomyTerritory.Transfers.PhysicalTransferService();
                transfers.SetAvailable("WoodLog", 200);
                transfers.SetAvailable("Steel", 50);
                transfers.SetAvailable("Credits", 1000);

                op.TryReserveInvestment(transfers, "rimconemy.economyterritory", "plan-" + newId,
                    "WoodLog", 20, now);
                op.TryReserveInvestment(transfers, "rimconemy.economyterritory", "plan2-" + newId,
                    "Steel", 10, now + 1L);

                // E3: Standortgebundene Produktion — kleiner positiver Grundumsatz,
                // damit NetPerTick > 0 erreichbar ist sobald bemannt.
                op.UpdateEconomy(grossPerTick: 8, defenseCostPerTick: 1, currentTick: now);
                op.StateEnteredTick = now;
                op.LastSeenActiveTick = now;
                // Kein ForceTransition: Planned bleibt Planned bis Ticks evaluieren.
                Log.Message("[Rimconemy.EconomyTerritory] Caller: Settlement " + newId + " planned (Wood 20 + Steel 10 reserved).");
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.EconomyTerritory] TryPlanSettlementFromHub: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // E1 Caller für Reparatur: Wallet-Service nutzt Credits, danach Tick-Repair.
        private static void TryRepairInvestmentFromHub(Outpost outpost, OutpostLedger ledger)
        {
            try
            {
                long now = Find.TickManager?.TicksGame ?? 0L;
                long balance = WalletService.GetBalance();
                if (balance < 60L)
                {
                    Log.Message("[Rimconemy.EconomyTerritory] Repair skipped: insufficient credits (" + balance + "/60).");
                    return;
                }
                // Wallet abbuchen.
                WalletService.ApplyManualDebit(60L, "outpost-repair:" + outpost.OutpostId);
                // Reparatur triggern.
                bool ok = outpost.TryRepair(60L, now);
                Log.Message("[Rimconemy.EconomyTerritory] Repair caller: outpost=" + outpost.OutpostId + " ok=" + ok);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.EconomyTerritory] TryRepairInvestmentFromHub: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
