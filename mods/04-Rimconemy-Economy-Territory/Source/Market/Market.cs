using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.EconomyTerritory.Market
{
    /// <summary>
    /// Owner: Economy and Territory (Package 04).
    /// Per-map market snapshot. Deterministic price formula, dedup reserve
    /// commands by (PackageId, RequestId). Never global; never aliases
    /// Vanilla's MarketValue. The visible price may diverge from MarketValue
    /// without "patching" it - we publish a local snapshot instead.
    /// SPIKE: API-TRADE-01.
    /// </summary>
    public sealed class Market : IExposable
    {
        public const string LogMarker = "v1";
        public const float MinPriceFactor = 0.20f;
        public const float MaxPriceFactor = 4.50f;
        public const int MaxEntriesPerMap = 64;

        private readonly Dictionary<string, ItemPrice> _prices;
        private readonly Dictionary<string, OrderRecord> _orders;

        // Scribe helpers. The runtime dictionaries remain the canonical lookup
        // structures; parallel primitive lists are only the save envelope.
        private List<string> _priceKeys;
        private List<long> _priceBasePrices;
        private List<int> _priceCurrentStocks;
        private List<int> _priceTargetStocks;
        private List<float> _priceDemandFactors;
        private List<int> _priceVersions;
        private List<string> _orderPackageIds;
        private List<string> _orderRequestIds;
        private List<string> _orderThingDefNames;
        private List<int> _orderAmounts;
        private List<int> _orderSides;
        private List<long> _orderPlacedTicks;

        public int MapId;
        public long SnapshotTick;
        public string CatalogHash;

        public Market()
        {
            _prices = new Dictionary<string, ItemPrice>(System.StringComparer.Ordinal);
            _orders = new Dictionary<string, OrderRecord>(System.StringComparer.Ordinal);
        }

        // ── prices ─────────────────────────────────────────────

        /// <summary>
        /// Compute the deterministic current price for a thing defname.
        /// Formula:
        ///   price = base * (1 + scarcity) * (1 - demandBuffer)
        /// With:
        ///   scarcity = max(0, (target - current) / target)
        ///   demandBuffer = clamp01(observedDemand / observedSupply)
        /// Result is clamped to [MinPriceFactor * base, MaxPriceFactor * base].
        /// </summary>
        public long Price(string thingDefName)
        {
            if (string.IsNullOrEmpty(thingDefName)) return 0;
            if (_prices.TryGetValue(thingDefName, out var item))
                return ComputePrice(item);
            return 0;
        }

        /// <summary>
        /// Register or update an item's price entry. basePrice comes from
        /// Mod 03's Market Catalogue or the requester.
        /// </summary>
        public ItemPrice RegisterItem(string thingDefName, long basePrice, int currentStock = 0, int targetStock = 0)
        {
            if (basePrice < 1) basePrice = 1;
            var entry = new ItemPrice
            {
                ThingDefName = thingDefName,
                BasePrice = basePrice,
                CurrentStock = Mathf.Max(0, currentStock),
                TargetStock = Mathf.Max(0, targetStock),
                Version = 1,
            };
            _prices[thingDefName] = entry;

            // Hash degrade defence: cap entries per map.
            if (_prices.Count > MaxEntriesPerMap)
            {
                string worstKey = null;
                int worstStock = int.MinValue;
                foreach (var kvp in _prices)
                {
                    if (kvp.Value.CurrentStock > worstStock)
                    {
                        worstStock = kvp.Value.CurrentStock;
                        worstKey = kvp.Key;
                    }
                }
                if (worstKey != null) _prices.Remove(worstKey);
            }

            return entry;
        }

        public List<ItemPrice> AllPrices()
        {
            var list = new List<ItemPrice>(_prices.Values);
            list.Sort((a, b) => string.Compare(a.ThingDefName, b.ThingDefName, System.StringComparison.Ordinal));
            return list;
        }

        // ── orders ─────────────────────────────────────────────

        /// <summary>
        /// Idempotently place or update an order. Returns true when the
        /// order was newly created, false when an existing (PackageId,
        /// RequestId) pair was reused.
        /// </summary>
        public bool PlaceOrder(string packageId, string requestId, string thingDefName, int amount, OrderSide side)
        {
            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(requestId)) return false;
            string key = packageId + "|" + requestId;
            if (_orders.ContainsKey(key)) return false;

            _orders[key] = new OrderRecord
            {
                PackageId = packageId,
                RequestId = requestId,
                ThingDefName = thingDefName ?? "",
                Amount = Mathf.Abs(amount),
                Side = side,
                PlacedTick = SnapshotTick,
            };
            return true;
        }

        public List<OrderRecord> AllOrders()
        {
            var list = new List<OrderRecord>(_orders.Values);
            list.Sort((a, b) => string.Compare(a.RequestId, b.RequestId, System.StringComparison.Ordinal));
            return list;
        }

        /// <summary>
        /// Persists the per-map market snapshot. Scribe cannot deep-save this
        /// object unless it implements IExposable; dictionaries are converted
        /// to deterministic parallel/list envelopes and rebuilt after load.
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref MapId, "mapId", 0);
            Scribe_Values.Look(ref SnapshotTick, "snapshotTick", 0L);
            Scribe_Values.Look(ref CatalogHash, "catalogHash", "");

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _priceKeys = new List<string>();
                _priceBasePrices = new List<long>();
                _priceCurrentStocks = new List<int>();
                _priceTargetStocks = new List<int>();
                _priceDemandFactors = new List<float>();
                _priceVersions = new List<int>();

                foreach (var price in AllPrices())
                {
                    _priceKeys.Add(price.ThingDefName);
                    _priceBasePrices.Add(price.BasePrice);
                    _priceCurrentStocks.Add(price.CurrentStock);
                    _priceTargetStocks.Add(price.TargetStock);
                    _priceDemandFactors.Add(price.DemandFactor);
                    _priceVersions.Add(price.Version);
                }

                _orderPackageIds = new List<string>();
                _orderRequestIds = new List<string>();
                _orderThingDefNames = new List<string>();
                _orderAmounts = new List<int>();
                _orderSides = new List<int>();
                _orderPlacedTicks = new List<long>();

                foreach (var order in AllOrders())
                {
                    _orderPackageIds.Add(order.PackageId);
                    _orderRequestIds.Add(order.RequestId);
                    _orderThingDefNames.Add(order.ThingDefName);
                    _orderAmounts.Add(order.Amount);
                    _orderSides.Add((int)order.Side);
                    _orderPlacedTicks.Add(order.PlacedTick);
                }
            }

            Scribe_Collections.Look(ref _priceKeys, "priceKeys", LookMode.Value);
            Scribe_Collections.Look(ref _priceBasePrices, "priceBasePrices", LookMode.Value);
            Scribe_Collections.Look(ref _priceCurrentStocks, "priceCurrentStocks", LookMode.Value);
            Scribe_Collections.Look(ref _priceTargetStocks, "priceTargetStocks", LookMode.Value);
            Scribe_Collections.Look(ref _priceDemandFactors, "priceDemandFactors", LookMode.Value);
            Scribe_Collections.Look(ref _priceVersions, "priceVersions", LookMode.Value);
            Scribe_Collections.Look(ref _orderPackageIds, "orderPackageIds", LookMode.Value);
            Scribe_Collections.Look(ref _orderRequestIds, "orderRequestIds", LookMode.Value);
            Scribe_Collections.Look(ref _orderThingDefNames, "orderThingDefNames", LookMode.Value);
            Scribe_Collections.Look(ref _orderAmounts, "orderAmounts", LookMode.Value);
            Scribe_Collections.Look(ref _orderSides, "orderSides", LookMode.Value);
            Scribe_Collections.Look(ref _orderPlacedTicks, "orderPlacedTicks", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _prices.Clear();
                _orders.Clear();

                int priceCount = _priceKeys?.Count ?? 0;
                for (int i = 0; i < priceCount; i++)
                {
                    string key = _priceKeys[i];
                    if (string.IsNullOrEmpty(key)) continue;
                    _prices[key] = new ItemPrice
                    {
                        ThingDefName = key,
                        BasePrice = ValueAt(_priceBasePrices, i, 1L),
                        CurrentStock = ValueAt(_priceCurrentStocks, i, 0),
                        TargetStock = ValueAt(_priceTargetStocks, i, 0),
                        DemandFactor = ValueAt(_priceDemandFactors, i, 0f),
                        Version = ValueAt(_priceVersions, i, 1),
                    };
                }

                int orderCount = _orderPackageIds?.Count ?? 0;
                for (int i = 0; i < orderCount; i++)
                {
                    string packageId = _orderPackageIds[i];
                    string requestId = ValueAt(_orderRequestIds, i, "");
                    if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(requestId))
                        continue;

                    _orders[packageId + "|" + requestId] = new OrderRecord
                    {
                        PackageId = packageId,
                        RequestId = requestId,
                        ThingDefName = ValueAt(_orderThingDefNames, i, ""),
                        Amount = ValueAt(_orderAmounts, i, 0),
                        Side = (OrderSide)ValueAt(_orderSides, i, (int)OrderSide.Buy),
                        PlacedTick = ValueAt(_orderPlacedTicks, i, 0L),
                    };
                }

                _priceKeys = null;
                _priceBasePrices = null;
                _priceCurrentStocks = null;
                _priceTargetStocks = null;
                _priceDemandFactors = null;
                _priceVersions = null;
                _orderPackageIds = null;
                _orderRequestIds = null;
                _orderThingDefNames = null;
                _orderAmounts = null;
                _orderSides = null;
                _orderPlacedTicks = null;
            }
        }

        private static T ValueAt<T>(List<T> values, int index, T fallback)
        {
            return values != null && index >= 0 && index < values.Count
                ? values[index]
                : fallback;
        }

        // ── helper ─────────────────────────────────────────────

        private static long ComputePrice(ItemPrice item)
        {
            if (item.BasePrice <= 0) return 0;

            float basePrice = item.BasePrice;
            float scarcity = 0f;
            if (item.TargetStock > 0)
                scarcity = Mathf.Max(0f, (item.TargetStock - item.CurrentStock) / (float)item.TargetStock);
            float demandBuffer = Mathf.Clamp01(item.DemandFactor);

            float factor = (1f + scarcity) * (1f - demandBuffer);
            factor = Mathf.Clamp(factor, MinPriceFactor, MaxPriceFactor);
            return (long)Mathf.Round(basePrice * factor);
        }
    }

    public struct ItemPrice
    {
        public string ThingDefName;
        public long BasePrice;
        public int CurrentStock;
        public int TargetStock;
        public float DemandFactor;
        public int Version;
    }

    public enum OrderSide
    {
        Buy = 0,
        Sell = 1,
    }

    public struct OrderRecord : IExposable
    {
        public string PackageId;
        public string RequestId;
        public string ThingDefName;
        public int Amount;
        public OrderSide Side;
        public long PlacedTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref PackageId, "packageId", "");
            Scribe_Values.Look(ref RequestId, "requestId", "");
            Scribe_Values.Look(ref ThingDefName, "thingDefName", "");
            Scribe_Values.Look(ref Amount, "amount", 0);
            Scribe_Values.Look(ref Side, "side", OrderSide.Buy);
            Scribe_Values.Look(ref PlacedTick, "placedTick", 0L);
        }
    }

    /// <summary>
    /// Owner: Economy and Territory.
    /// Global accessor convenience for single-map scenarios. Multi-map
    /// games should resolve their per-map Market from a service locator.
    /// </summary>
    public static class MarketService
    {
        public static Market ForPlayerHomeMap()
        {
            var map = Find.AnyPlayerHomeMap;
            if (map == null) return null;
            return ForMap(map);
        }

        /// <summary>
        /// Returns the per-map market instance. Caches via
        /// MapComponentMarketExtension (per-map) so re-entry is idempotent.
        /// </summary>
        public static Market ForMap(Map map)
        {
            if (map == null) return null;

            var comp = map.GetComponent<MapMarketComponent>();
            if (comp == null)
            {
                // We do not auto-inject components. Returns a stand-alone
                // Market bound to the map id so callers can reference it.
                return new Market { MapId = map.uniqueID };
            }
            return comp.GetOrCreateMarket();
        }
    }

    /// <summary>
    /// Per-Map GameComponent. Holds one <see cref="Market"/> snapshot.
    /// </summary>
    public sealed class MapMarketComponent : MapComponent
    {
        public Market Market;

        public MapMarketComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode != LoadSaveMode.LoadingVars && Market == null)
                Market = new Market { MapId = map.uniqueID };
            if (Market != null)
                Market.MapId = map.uniqueID;
            // Market implements IExposable and owns a primitive/list save
            // envelope, so the deep object is safe for map save/load.
            Scribe_Deep.Look(ref Market, "marketSnapshot");
            if (Market == null)
                Market = new Market { MapId = map.uniqueID };
        }

        public Market GetOrCreateMarket()
        {
            if (Market == null)
                Market = new Market { MapId = map.uniqueID };
            return Market;
        }
    }

    /// <summary>
    /// Backward-compatibility stub for callers still referring to the
    /// historical <c>MarketStub</c>. E-T3 forwarded to the new
    /// <see cref="Market"/> + <see cref="MarketService"/> APIs.
    /// </summary>
    public static class MarketStub
    {
        public const string LogMarker = "v1";
        public static readonly List<string> TrackOrderIds = new List<string>();

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Log.Message(
                    "[Rimconemy.EconomyTerritory] Market stub deprecated; " +
                    "use Market + MarketService for live deterministic prices.");
            }
        }
    }
}
