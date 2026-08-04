using System;
using MarketModel = Rimconemy.EconomyTerritory.Market.Market;
using Rimconemy.EconomyTerritory.Market;
using Verse;

namespace Rimconemy.EconomyTerritory.Tests
{
    /// <summary>Regression checks for the per-map Market save contract.</summary>
    public static class MarketPersistenceTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            AssertTrue(typeof(MarketModel).GetInterface(nameof(IExposable)) != null,
                "Market: implements IExposable for Scribe_Deep");

            var market = new MarketModel { MapId = 42, SnapshotTick = 60000L };
            market.RegisterItem("Meal", 25L, currentStock: 0, targetStock: 10);
            AssertTrue(market.Price("Meal") > 0, "Market: registered price is readable");
            AssertTrue(market.PlaceOrder("tests", "request-1", "Meal", 1, OrderSide.Buy),
                "Market: first order is accepted");
            AssertTrue(!market.PlaceOrder("tests", "request-1", "Meal", 1, OrderSide.Buy),
                "Market: duplicate order is rejected");

            string summary = "[Rimconemy.EconomyTerritory] Market persistence tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.EconomyTerritory] MarketPersistenceTests FAILED: " + label);
            }
        }
    }
}
