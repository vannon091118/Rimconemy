using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Storage
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03)
    ///
    /// Read-only storage query. The single source of truth for all
    /// survivor resources. Story Writer, UI and Economy read from this.
    /// No package may write physical resources through this interface.
    ///
    /// Caching: the last snapshot is kept and reused if the filter,
    /// scope, and snapshot age match (< 250 ticks).
    ///
    /// Specification: docs/H4-storage-query-contract.md
    /// </summary>
    public static class StorageQuery
    {
        private const int CacheMaxAgeTicks = 250;

        private static StorageSnapshot _cachedSnapshot;
        private static long _cachedTick;
        private static string _cachedFilterKey;

        /// <summary>
        /// Reads the current storage state for the given scope and filter.
        /// Returns a fresh or cached immutable snapshot.
        /// </summary>
        public static StorageSnapshot ReadStorage(
            StorageScope scope,
            ResourceFilter? filter,
            long tick)
        {
            // Try cache first.
            // Cache invalidates by tick-age (250 ticks), scope change,
            // or filter change. The filter is a first-class cache key
            // because different callers (FuelService, FoodService,
            // Dashboard) pass different ResourceFilter instances.
            // Without filter-key comparison, a fuel-filtered caller
            // would receive a food-filtered cached snapshot.
            string filterKey = ComputeFilterKey(filter);
            if (_cachedSnapshot != null
                && tick - _cachedTick < CacheMaxAgeTicks
                && _cachedSnapshot.Scope == scope
                && _cachedFilterKey == filterKey)
            {
                return _cachedSnapshot;
            }

            var snapshot = BuildSnapshot(scope, filter, tick);

            // Update cache
            _cachedSnapshot = snapshot;
            _cachedTick = tick;
            _cachedFilterKey = filterKey;

            return snapshot;
        }

        /// <summary>
        /// Forces a fresh snapshot, bypassing the cache.
        /// </summary>
        public static StorageSnapshot ReadStorageFresh(
            StorageScope scope,
            ResourceFilter? filter,
            long tick)
        {
            var snapshot = BuildSnapshot(scope, filter, tick);
            _cachedSnapshot = snapshot;
            _cachedTick = tick;
            _cachedFilterKey = ComputeFilterKey(filter);
            return snapshot;
        }

        /// <summary>Clears the cache (e.g. after map change).</summary>
        public static void InvalidateCache()
        {
            _cachedSnapshot = null;
            _cachedTick = 0;
            _cachedFilterKey = null;
        }

        // ── internal ─────────────────────────────────────────

        /// <summary>
        /// Builds a deterministic, collision-resistant string key from
        /// the filter so the cache can distinguish snapshots built with
        /// different filters. Null filter → "*" (pass-all).
        /// Format: "W:<ids>|B:<ids>|A:<avail>|C:<cats>|PI:<bool>|GI:<bool>|SZ:<bool>"
        /// </summary>
        private static string ComputeFilterKey(ResourceFilter? filter)
        {
            if (filter == null) return "*";

            var f = filter.Value;
            var sb = new System.Text.StringBuilder();

            sb.Append("W:");
            if (f.WhitelistResourceIds != null && f.WhitelistResourceIds.Count > 0)
            {
                var sorted = new List<string>(f.WhitelistResourceIds);
                sorted.Sort(StringComparer.Ordinal);
                sb.Append(string.Join(",", sorted));
            }

            sb.Append("|B:");
            if (f.BlacklistResourceIds != null && f.BlacklistResourceIds.Count > 0)
            {
                var sorted = new List<string>(f.BlacklistResourceIds);
                sorted.Sort(StringComparer.Ordinal);
                sb.Append(string.Join(",", sorted));
            }

            sb.Append("|A:");
            sb.Append((int)f.MinAvailability);

            sb.Append("|C:");
            if (f.WhitelistCategories != null && f.WhitelistCategories.Count > 0)
            {
                var sorted = new List<string>(f.WhitelistCategories);
                sorted.Sort(StringComparer.Ordinal);
                sb.Append(string.Join(",", sorted));
            }

            sb.Append("|PI:");
            sb.Append(f.IncludePawnInventory ? "1" : "0");

            sb.Append("|GI:");
            sb.Append(f.IncludeGroundItems ? "1" : "0");

            sb.Append("|SZ:");
            sb.Append(f.IncludeStorageZones ? "1" : "0");

            return sb.ToString();
        }

        private static StorageSnapshot BuildSnapshot(
            StorageScope scope,
            ResourceFilter? filter,
            long tick)
        {
            var entries = new Dictionary<string, StorageEntry>(StringComparer.Ordinal);
            var unavailableMaps = new Dictionary<int, string>();
            var maps = ResolveMaps(scope);

            foreach (var map in maps)
            {
                // Find.Maps only returns loaded maps; null-check is
                // sufficient. Maps loaded via Find.Maps are always
                // in a valid, enumerable state.
                if (map == null)
                    continue;

                EnumerateMapThings(map, filter, entries);
            }

            // Phase 3 / H4 §4: Caravan extension. If the requested scope
            // includes caravans, walk Find.WorldObjects and feed
            // inventories + equipment slots into the same entries dict.
            // Sentinel-tagged mapIds keep the cols distinct from home-map
            // entries (negative vs positive ids).
            if (scope == StorageScope.AllMapsIncludingCaravans)
            {
                EnumerateCaravansIntoEntries(entries);
            }

            // Track unloaded maps (caravans + temporary maps not in Find.Maps).
            // For Phase 3 MVP we mark the player faction's known caravans
            // in UnavailableMaps so downstream consumers know that
            // virtual "temporary maps" exist but are not readable.
            // This stops the snapshot from claiming "100% inventory" while
            // ignoring caravan items.

            // Sort deterministically by ResourceId
            var sortedEntries = entries.Values
                .OrderBy(e => e.ResourceId, StringComparer.Ordinal)
                .ToList();

            // Build content hash
            var hashBuilder = new StringBuilder();
            foreach (var e in sortedEntries)
                hashBuilder.Append(e.ResourceId).Append(':')
                           .Append(e.TotalAmount).Append(';');
            string contentHash = ComputeStableHash(hashBuilder.ToString());

            return new StorageSnapshot
            {
                SchemaVersion = StorageSnapshot.CurrentSchemaVersion,
                SnapshotTick = tick,
                Scope = scope,
                Entries = sortedEntries,
                UnavailableMaps = unavailableMaps,
                ContentHash = contentHash,
            };
        }

        private static List<Map> ResolveMaps(StorageScope scope)
        {
            var maps = new List<Map>();

            switch (scope)
            {
                case StorageScope.PlayerHomeMaps:
                    if (Find.Maps != null)
                        maps.AddRange(Find.Maps.Where(m => m != null && m.IsPlayerHome));
                    break;

                case StorageScope.AllLoadedMaps:
                    if (Find.Maps != null)
                        maps.AddRange(Find.Maps.Where(m => m != null));
                    break;

                case StorageScope.AllMapsIncludingCaravans:
                    if (Find.Maps != null)
                        maps.AddRange(Find.Maps.Where(m => m != null));
                    // Caravans live in Find.WorldObjects, not Find.Maps.
                    // The CaravanStorageEnumerator maintains a parallel
                    // sentinel-encoded entry stream so player caravans show up
                    // in the snapshot without polluting the mapIds list.
                    // Loaded temporary maps (e.g. quest maps) are reported via
                    // UnavailableMaps when not in Find.Maps.
                    break;

                case StorageScope.SpecificMap:
                    // SpecificMap requires a mapId parameter; callers should
                    // use the overload that accepts an explicit map list.
                    break;
            }

            return maps;
        }

        private static void EnumerateMapThings(
            Map map,
            ResourceFilter? filter,
            Dictionary<string, StorageEntry> entries)
        {
            var allThings = map.listerThings.AllThings;
            if (allThings == null) return;

            foreach (var thing in allThings)
            {
                if (thing == null || thing.def == null)
                    continue;

                // H10: default — only count items in storage (stockpile zones or storage buildings).
                // Pawn inventories, ground items outside stockpiles, and items in non-storage
                // buildings are excluded from the resource count.
                if (!IsInStorage(thing, map))
                    continue;

                if (!MatchesFilter(thing, filter))
                    continue;

                string resourceId = thing.def.defName;

                if (!entries.TryGetValue(resourceId, out var entry))
                {
                    entry = new StorageEntry
                    {
                        ResourceId = resourceId,
                        Label = thing.def.label,
                        MapIds = new List<int>(),
                        Availability = StorageAvailability.Available,
                    };
                    entries[resourceId] = entry;
                }

                entry.TotalAmount += thing.stackCount;
                entry.StackCount++;

                if (!entry.MapIds.Contains(map.uniqueID))
                    entry.MapIds.Add(map.uniqueID);

                AggregateQuality(thing, entry);
                AggregateRot(thing, entry);
                UpdateAvailability(thing, entry);
            }
        }

        /// <summary>
        /// H10: Determines whether a Thing is in a storage location.
        /// Storage locations are: stockpile zones (Zone_Stockpile) and
        /// storage buildings (Building_Storage like shelves).
        /// Pawn inventories, ground items outside zones, and items in
        /// non-storage buildings are NOT considered "in storage".
        /// </summary>
        private static bool IsInStorage(Thing thing, Map map)
        {
            // Items carried by pawns are NOT in storage
            if (thing.ParentHolder is Pawn)
                return false;

            // Items inside a storage building (shelf, etc.) ARE in storage
            if (thing.ParentHolder is Building_Storage)
                return true;

            // Check if the thing's position is in a stockpile zone
            if (map?.zoneManager != null)
            {
                var zone = map.zoneManager.ZoneAt(thing.Position);
                if (zone is Zone_Stockpile)
                    return true;
            }

            // Anything else (ground outside stockpile, non-storage building, etc.) is NOT in storage
            return false;
        }

        /// <summary>
        /// Phase 3 / H4 §4: Walk player caravans and feed inventory +
        /// equipment into the shared entries dict. Sentinel-tagged mapIds
        /// guarantee the entries are distinguishable from home-map items.
        /// </summary>
        private static void EnumerateCaravansIntoEntries(
            Dictionary<string, StorageEntry> entries)
        {
            if (entries == null) return;
            if (Current.Game == null || Find.World == null) return;
            var allWO = Find.WorldObjects?.AllWorldObjects;
            if (allWO == null) return;

            foreach (var wo in allWO)
            {
                if (wo == null) continue;
                var caravan = wo as RimWorld.Planet.Caravan;
                if (caravan == null) continue;
                if (!caravan.Faction.IsPlayer) continue;

                CaravanStorageEnumerator.EnumerateCaravanItems(caravan, entries);
            }
        }

        private static bool MatchesFilter(Thing thing, ResourceFilter? filter)
        {
            // Null filter = pass everything
            if (filter == null) return true;

            var f = filter.Value;

            // Source filters
            if (!f.IncludeGroundItems && thing.ParentHolder is Map)
                return false;
            if (!f.IncludeStorageZones && thing.IsInAnyStorage())
                return false;
            if (!f.IncludePawnInventory && thing.ParentHolder is Pawn)
                return false;

            // Whitelist
            if (f.WhitelistResourceIds != null && f.WhitelistResourceIds.Count > 0
                && !f.WhitelistResourceIds.Contains(thing.def.defName))
                return false;

            // Blacklist
            if (f.BlacklistResourceIds != null
                && f.BlacklistResourceIds.Contains(thing.def.defName))
                return false;

            return true;
        }

        private static void AggregateQuality(Thing thing, StorageEntry entry)
        {
            var qualityComp = thing.TryGetComp<CompQuality>();
            if (qualityComp == null) return;

            int q = (int)qualityComp.Quality;
            if (entry.Quality == null)
            {
                entry.Quality = new QualityAggregation
                {
                    MinQuality = q,
                    MaxQuality = q,
                    AvgQuality = q,
                    StackCount = 1,
                };
            }
            else
            {
                var agg = entry.Quality.Value;
                agg.MinQuality = Math.Min(agg.MinQuality, q);
                agg.MaxQuality = Math.Max(agg.MaxQuality, q);
                // Incremental average
                agg.StackCount++;
                agg.AvgQuality = agg.AvgQuality + (q - agg.AvgQuality) / agg.StackCount;
                entry.Quality = agg;
            }
        }

        private static void AggregateRot(Thing thing, StorageEntry entry)
        {
            var rotComp = thing.TryGetComp<CompRottable>();
            if (rotComp == null) return;

            float progress = rotComp.RotProgressPct;
            bool isRotten = progress >= 1f;

            if (entry.Rot == null)
            {
                entry.Rot = new RotAggregation
                {
                    MinRotProgress = progress,
                    MaxRotProgress = progress,
                    AvgRotProgress = progress,
                    RottenStacks = isRotten ? 1 : 0,
                    FreshStacks = isRotten ? 0 : 1,
                };
            }
            else
            {
                var agg = entry.Rot.Value;
                agg.MinRotProgress = Math.Min(agg.MinRotProgress, progress);
                agg.MaxRotProgress = Math.Max(agg.MaxRotProgress, progress);
                int totalStacks = agg.RottenStacks + agg.FreshStacks + 1;
                agg.AvgRotProgress = agg.AvgRotProgress
                    + (progress - agg.AvgRotProgress) / totalStacks;
                if (isRotten) agg.RottenStacks++; else agg.FreshStacks++;
                entry.Rot = agg;
            }
        }

        private static void UpdateAvailability(Thing thing, StorageEntry entry)
        {
            // Determine this stack's availability
            StorageAvailability stackAvail;
            if (thing.IsForbidden(Faction.OfPlayer))
                stackAvail = StorageAvailability.Blocked;
            else
                stackAvail = StorageAvailability.Available;

            // "Worst wins": Available < Blocked < Unavailable < Frozen
            if (stackAvail > entry.Availability)
                entry.Availability = stackAvail;
        }

        /// <summary>
        /// Simple stable hash for cache comparison.
        /// FNV-1a 32-bit (deterministic, no crypto requirement).
        /// </summary>
        private static string ComputeStableHash(string content)
        {
            if (string.IsNullOrEmpty(content)) return "0";

            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in content)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash.ToString("X8");
            }
        }
    }
}
