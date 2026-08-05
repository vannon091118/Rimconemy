// Source/Horde/HordeMaterializationService.cs
//
// Phase F — Pawn Materialization on Reveal-Radius Entry. Maintains
// Stamp↔state roundtrip (the Save/Load-determinism hot path). Actual
// Pawn spawning grows in Phase G+ via the Reveal-Listener hook.
//
// Spec §3.5, §6.

using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeMaterializationService
    {
        public static readonly int TicksPerDay = (int)Rimconemy.Foundation.TimeConstants.TicksPerDay;

        public static void MaterializeTile(HordeManifest manifest, int tile, Map homeMap)
        {
            if (manifest == null || homeMap == null) return;
            manifest.MarkTileMaterialized(tile, true);
        }

        public static void CleanupTile(HordeManifest manifest, int tile, Map homeMap, long currentTick)
        {
            if (manifest == null || homeMap == null) return;
            var mapPawns = homeMap.mapPawns?.AllPawnsSpawned;
            if (mapPawns != null)
            {
                for (int i = mapPawns.Count - 1; i >= 0; i--)
                {
                    var pawn = mapPawns[i];
                    if (pawn?.kindDef == null) continue;
                    if (!pawn.kindDef.defName.StartsWith("Rimconemy_Infected")) continue;
                    pawn.Destroy();
                }
            }
            manifest.MarkTileMaterialized(tile, false);
        }

        public static void StaleStampGC(HordeManifest manifest, long currentTick, int staleThresholdDays = 5)
        {
            if (manifest == null) return;
            long staleThresholdTicks = (long)staleThresholdDays * TicksPerDay;
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
    }
}
