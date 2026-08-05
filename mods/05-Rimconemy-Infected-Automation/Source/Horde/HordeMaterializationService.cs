// Source/Horde/HordeMaterializationService.cs
//
// Phase F — Pawn Materialization on Reveal-Radius Entry. Maintains
// Stamp↔state roundtrip (the Save/Load-determinism hot path). Actual
// Pawn spawning grows in Phase G+ via the Reveal-Listener hook.
//
// Spec §3.5, §3.7, §6.

using System;
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeMaterializationService
    {
        public static void MaterializeTile(HordeManifest manifest, int tile, Map homeMap)
        {
            if (manifest == null || homeMap == null) return;
            manifest.MarkTileMaterialized(tile, true);
        }

        public static void CleanupTile(HordeManifest manifest, int tile, Map homeMap, long currentTick)
        {
            if (manifest == null || homeMap == null) return;
            // Bitmap-only for now: MaterializeTile spawns no pawns yet (Phase
            // G+), so there is nothing to clean beyond the materialization flag.
            manifest.MarkTileMaterialized(tile, false);
        }

        /// <summary>
        /// Reveal-Radius-Sync. Tile-distance ≤ HordeRevealRadiusTiles
        /// materializes; outside triggers cleanup. Spec §3.7.
        /// </summary>
        public static void SyncRevealRadius(HordeManifest manifest, int homeMapTile, long currentTick, Map homeMap)
        {
            if (manifest == null || homeMap == null) return;
            if (Find.World == null) return;  // defensive: main menu / no Map

            for (int i = manifest.TileRecords.Count - 1; i >= 0; i--)
            {
                int tile = manifest.TileRecords[i].Tile;
                if (TileDistance(tile, homeMapTile) <= PopulationProfileMultipliers.HordeRevealRadiusTiles)
                {
                    if (!manifest.IsTileMaterialized(tile))
                        MaterializeTile(manifest, tile, homeMap);
                }
                else
                {
                    if (manifest.IsTileMaterialized(tile))
                        CleanupTile(manifest, tile, homeMap, currentTick);
                    var record = manifest.TileRecords[i];
                    record.LastSeenAtTick = currentTick;
                    manifest.TileRecords[i] = record;
                }
            }

            StaleStampGC(manifest, currentTick);
        }

        public static void StaleStampGC(HordeManifest manifest, long currentTick, int staleThresholdDays = 5)
        {
            if (manifest == null) return;
            long staleThresholdTicks = (long)(staleThresholdDays * Rimconemy.Foundation.TimeConstants.TicksPerDay);
            for (int i = manifest.Stamps.Count - 1; i >= 0; i--)
            {
                if (currentTick - manifest.Stamps[i].SpawnedAtTick > staleThresholdTicks)
                    manifest.Stamps.RemoveAt(i);
            }
            // GC stale TileRecords too (LastSeenAtTick older than threshold).
            for (int i = manifest.TileRecords.Count - 1; i >= 0; i--)
            {
                if (currentTick - manifest.TileRecords[i].LastSeenAtTick > staleThresholdTicks)
                    manifest.TileRecords.RemoveAt(i);
            }
        }

        /// <summary>
        /// Chebyshev distance in the vertical-slice tile-id space. RimWorld 1.6
        /// replaced the flat world grid with a planet grid (PlanetTile/
        /// PlanetLayer) and removed tileIDToX/Z and traversalDistanceBetween;
        /// the driver only produces records within the 5-tile leader window,
        /// where this equals |tileA - tileB|. A PlanetTile port is Phase-G+
        /// follow-up.
        /// </summary>
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
