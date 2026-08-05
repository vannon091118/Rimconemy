// Source/Horde/HordeChunkCleanupService.cs
//
// Phase F — Reveal-Radius-Sync. Tile-distance ≤ HordeRevealRadiusTiles
// triggers MaterializeTile; outside triggers CleanupTile.
//
// Spec §3.7.

using System;
using Rimconemy.Foundation.Maps;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeChunkCleanupService
    {
        public static void SyncRevealRadius(HordeManifest manifest, int homeMapTile, long currentTick, Map homeMap)
        {
            if (manifest == null || homeMap == null) return;

            if (Find.World == null) return;  // defensive: main menu / no Map

            for (int i = manifest.TileRecords.Count - 1; i >= 0; i--)
            {
                int tile = manifest.TileRecords[i].Tile;
                int dist = TileDistance(tile, homeMapTile);
                if (dist <= HordeManifest.HordeRevealRadiusTiles)
                {
                    if (!manifest.IsTileMaterialized(tile))
                        HordeMaterializationService.MaterializeTile(manifest, tile, homeMap);
                }
                else
                {
                    if (manifest.IsTileMaterialized(tile))
                        HordeMaterializationService.CleanupTile(manifest, tile, homeMap, currentTick);
                    var record = manifest.TileRecords[i];
                    record.LastSeenAtTick = currentTick;
                    manifest.TileRecords[i] = record;
                }
            }

            HordeMaterializationService.StaleStampGC(manifest, currentTick);
        }

        /// <summary>Chebyshev distance. Cheap, deterministic.</summary>
        private static int TileDistance(int tileA, int tileB)
        {
            if (tileA == tileB) return 0;
            int aX = tileA % 10000;
            int aZ = tileA / 10000;
            int bX = tileB % 10000;
            int bZ = tileB / 10000;
            return Math.Max(Math.Abs(aX - bX), Math.Abs(aZ - bZ));
        }
    }
}
