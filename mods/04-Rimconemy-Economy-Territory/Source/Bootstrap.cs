using Verse;

namespace Rimconemy.EconomyTerritory
{
    /// <summary>
    /// Owner: Economy &amp; Territory.
    /// Standalone startup marker for Package 04 scaffold.
    ///
    /// Hook reason: StaticConstructorOnStartup binds before any map loads.
    /// Wallet, markets, transactions, outposts, proxies and territory counts
    /// are exposed as data, not as WalletCounter subclassing; the Wallet is
    /// a World-/Game-side record, never a Thing/Item, and credits never
    /// alias silver. API-TRADE-01 / API-WORLD-01 remain UNVERIFIED.
    ///
    /// No Foundation, Scavenger, Survival or Infected compile references.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            Log.Message("[Rimconemy.EconomyTerritory] Standalone bootstrap starting...");
            Log.Message("[Rimconemy.EconomyTerritory] Wallet, Markets, Outposts and Territory stubs registered as data records (currency=credits, never silver).");
            Log.Message("[Rimconemy.EconomyTerritory] WalletLedger is Game-/World-side, not ThingDef. Routes count absolute ticks, not local counters.");

            var _walletId = Wallet.CreditsLedger.LogMarker;
            var _marketCount = Market.MarketStub.LogMarker;
            var _outposts = Outposts.OutpostStub.LogMarker;
            var _nodes = Territory.TerritoryNode.LogMarker;
            Log.Message($"[Rimconemy.EconomyTerritory] Domain stubs ready: wallet={_walletId}, market={_marketCount}, outposts={_outposts}, territory={_nodes}");
            Tests.CreditsLedgerRegressionTests.RunAll();
            Tests.MarketPersistenceTests.RunAll();
            Tests.BuildingInputRegressionTests.RunAll();
            Tests.PhysicalTransferRegressionTests.RunAll();
            Tests.OutpostInvestmentRegressionTests.RunAll();
            Log.Message("[Rimconemy.EconomyTerritory] Physical transfer and outpost investment contracts are gated for Milestone B.");
        }
    }
}
