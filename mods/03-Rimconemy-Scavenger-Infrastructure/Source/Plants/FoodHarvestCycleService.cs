using System.Collections.Generic;
using Rimconemy.ScavengerInfrastructure.Storage;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Plants
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// P6 — Task 9: Nahrung/Hanf getrennt.
    ///
    /// Phase-6 Stub: reads the storage snapshot and returns a per-pipeline
    /// counter so the UI can show "Food harvested" vs "Hemp harvested"
    /// independently. The actual harvest cycle (workgiver, growth tick,
    /// rot accumulation) is a User Live-Test concern. Stub does not
    /// modify world state.
    ///
    /// Specification: docs/P6-PROGRESS.md Task 9.
    /// </summary>
    public static class FoodHarvestCycleService
    {
        public struct HarvestTotals
        {
            public int FoodTotal;
            public int HempTotal;
            public int StrawTotal;
            public int RottenTotal;
        }

        public static HarvestTotals ReadTotals()
        {
            var totals = new HarvestTotals();
            if (Current.Game == null) return totals;

            long tick = Find.TickManager?.TicksGame ?? 0L;
            var snapshot = StorageQuery.ReadStorage(
                StorageScope.PlayerHomeMaps, null, tick);

            if (snapshot?.Entries == null) return totals;

            foreach (var entry in snapshot.Entries)
            {
                if (entry == null) continue;

                if (IsFood(entry.ResourceId))
                    totals.FoodTotal += entry.TotalAmount;
                else if (IsHemp(entry.ResourceId))
                    totals.HempTotal += entry.TotalAmount;
                else if (IsStraw(entry.ResourceId))
                    totals.StrawTotal += entry.TotalAmount;

                if (entry.Rot != null)
                {
                    totals.RottenTotal += entry.Rot.Value.RottenStacks;
                }
            }
            return totals;
        }

        private static bool IsFood(string resourceId)
        {
            // Vanilla food resources we care about for the food pipeline.
            return resourceId == "MealSimple" || resourceId == "MealFine"
                || resourceId == "MealSurvivalPack" || resourceId == "Pemmican"
                || resourceId == "RawFood";
        }

        private static bool IsHemp(string resourceId)
        {
            return resourceId == "Rimconemy_Hemp" || resourceId == "HempCloth";
        }

        private static bool IsStraw(string resourceId)
        {
            return resourceId == "Rimconemy_Straw" || resourceId == "Plant_Straw";
        }
    }
}
