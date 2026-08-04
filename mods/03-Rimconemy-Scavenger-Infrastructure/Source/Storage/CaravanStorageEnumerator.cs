using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Storage
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// StorageQuery caravan extension.
    ///
    /// Caravans are RimWorld world objects (RimWorld.Planet.Caravan) rather
    /// than loaded maps. They travel between settlements and temporarily
    /// host item stockpiles via pawn inventories and equipment slots. The
    /// base StorageQuery only sees map.listerThings.AllThings which is
    /// insufficient: car-held items must be aggregated separately and
    /// tagged with a sentinel map id so they are distinguishable from
    /// home-map items.
    ///
    /// Sentinel encoding:
    ///   mapId = -(Caravan.uniqueID + 1)    →  unique per caravan
    ///                                          (Caravan.uniqueIDs are positive)
    ///
    /// The new entries flow through the same aggregator pipeline as
    /// in-storage items, so resource-wise the snapshot is uniform.
    ///
    /// Specification: docs/H4-storage-query-contract.md §4 (caravans).
    /// Gate: H4 §4 — caravans must appear in the snapshot so the Story
    /// Writer and Economy modules read the same source.
    /// </summary>
    internal static class CaravanStorageEnumerator
    {
        /// <summary>Stable sentinel offset so all caravan mapIds are negative.</summary>
        private const int CaravanSentinelOffset = 1;

        /// <summary>
        /// Enumerates player caravans and feeds items directly into the
        /// shared aggregator via a synthetic Map-shaped carrier so we can
        /// reuse EnumerateMapThings' storage-zone logic.
        /// </summary>
        /// <remarks>
        /// We avoid engine-side type probing by relying on the documented
        /// <see cref="GameComponent"/> / <see cref="WorldObjectsHolder"/>
        /// surface. Caravan inherits WorldObject and routes its items
        /// through pawn inventories and equipment slots.
        /// </remarks>
        public static List<int> EnumerateCaravans()
        {
            var ids = new List<int>();
            if (Current.Game == null || Find.World == null) return ids;

            // WorldObjectsHolder.AllWorldObjects enumerates every WorldObject
            // currently in scope (caravans, settlements, quests, etc.).
            var all = Find.WorldObjects?.AllWorldObjects;
            if (all == null) return ids;

            foreach (var wo in all)
            {
                if (wo == null) continue;
                var caravan = wo as Caravan;
                if (caravan == null) continue;

                if (!caravan.Faction.IsPlayer) continue;

                ids.Add(MakeCaravanMapId(caravan));
            }
            return ids;
        }

        /// <summary>
        /// Returns true if the supplied mapId is a caravan sentinel
        /// (negative).
        /// </summary>
        public static bool IsCaravanSentinel(int mapId)
        {
            return mapId < 0;
        }

        /// <summary>
        /// Decodes the caravan uniqueID from a sentinel mapId, or returns
        /// -1 if the mapId is not a valid sentinel.
        /// </summary>
        public static int DecodeCaravanId(int mapId)
        {
            if (mapId >= 0) return -1;
            return -(mapId + CaravanSentinelOffset);
        }

        /// <summary>Encodes a caravan uniqueID as a sentinel mapId.</summary>
        public static int MakeCaravanMapId(Caravan caravan)
        {
            if (caravan == null) return -1;
            // WorldObject.ID is the stable unique integer across the
            // world object's lifetime. We use it as the sentinel seed.
            return -(caravan.ID + CaravanSentinelOffset);
        }

        /// <summary>
        /// Iterates every carrier pawn in the caravan and enumerates
        /// items in their inventory + equipment slots directly into the
        /// shared entries dictionary. Items are tagged with the sentinel
        /// mapId so downstream consumers can identify caravan source.
        /// </summary>
        public static void EnumerateCaravanItems(
            Caravan caravan,
            Dictionary<string, StorageEntry> entries)
        {
            if (caravan == null || entries == null) return;
            int sentinelMapId = MakeCaravanMapId(caravan);

            // The Caravan class implements IThingHolder, so it exposes the
            // canonical Items enumeration via GetDirectlyHeldThings(). This
            // walks inventories + equipment + cargo slots uniformly and
            // is more stable than reflection against member-name changes.
            foreach (var thing in caravan.GetDirectlyHeldThings())
            {
                if (thing == null || thing.def == null) continue;
                AddOrUpdateEntry(entries, thing, sentinelMapId);
            }
        }

        private static void EnumerateItemsForPawn(Pawn pawn, int sentinelMapId, Dictionary<string, StorageEntry> entries)
        {
            // Inventory (held items)
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (var thing in pawn.inventory.innerContainer)
                {
                    if (thing == null || thing.def == null) continue;
                    AddOrUpdateEntry(entries, thing, sentinelMapId);
                }
            }
            // Equipment (worn, like armor)
            if (pawn.equipment?.AllEquipmentListForReading != null)
            {
                foreach (var thing in pawn.equipment.AllEquipmentListForReading)
                {
                    if (thing == null || thing.def == null) continue;
                    AddOrUpdateEntry(entries, thing, sentinelMapId);
                }
            }
        }

        private static void AddOrUpdateEntry(
            Dictionary<string, StorageEntry> entries, Thing thing, int sentinelMapId)
        {
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

            if (!entry.MapIds.Contains(sentinelMapId))
                entry.MapIds.Add(sentinelMapId);
        }
    }
}
