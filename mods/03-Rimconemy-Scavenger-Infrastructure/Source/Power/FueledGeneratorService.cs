using Rimconemy.ScavengerInfrastructure.Storage;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Power
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// P6 — Task 10: Wasser-/Brennstoff als physischer Pfad zum Generator.
    ///
    /// Phase-6 Stub: aggregates fuel + water totals from the storage
    /// snapshot so the generator's power gate can refuse to come Online
    /// when no fuel is present. The mechanical connection between
    /// generator fuel tank and storage is owned by the User Live-Test
    /// phase.
    ///
    /// Specification: docs/P6-PROGRESS.md Task 10.
    /// </summary>
    public static class FueledGeneratorService
    {
        public struct FuelInventory
        {
            public int WoodLogs;
            public int Coal;
            public int WaterUnits;
            public bool HasAnyCombustibleFuel;
        }

        public static FuelInventory CurrentFuelInventory()
        {
            var inv = new FuelInventory();
            if (Current.Game == null) return inv;

            long tick = Find.TickManager?.TicksGame ?? 0L;
            var snapshot = StorageQuery.ReadStorage(
                StorageScope.PlayerHomeMaps, null, tick);

            if (snapshot?.Entries == null) return inv;

            foreach (var entry in snapshot.Entries)
            {
                if (entry == null) continue;
                if (entry.ResourceId == "WoodLog")
                    inv.WoodLogs += entry.TotalAmount;
                else if (entry.ResourceId == "Rimconemy_Coal"
                    || entry.ResourceId == "Coal"
                    || entry.ResourceId == "ChunkCoal")
                    inv.Coal += entry.TotalAmount;
                else if (entry.ResourceId == "Rimconemy_WaterUnit" || entry.ResourceId == "Water")
                    inv.WaterUnits += entry.TotalAmount;
            }

            inv.HasAnyCombustibleFuel = inv.WoodLogs > 0 || inv.Coal > 0;
            return inv;
        }
    }
}
