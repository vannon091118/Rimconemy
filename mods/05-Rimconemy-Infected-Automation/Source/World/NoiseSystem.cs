using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 1 — Noise System.
    ///
    /// Computes per-chunk noise from persistent sound sources:
    ///   - Generators with <see cref="CompPowerTrader"/> that are
    ///     actively producing power.
    ///   - Fueled devices (<see cref="CompRefuelable"/> with fuel).
    ///
    /// Noise falls off with distance (1 / (1 + d²/r²)). The
    /// system returns the count of active loud sources for the
    /// global EnvironmentSnapshot.ActiveLoudSources counter.
    ///
    /// Sprint 3 extends this with transient noise events: gunshots,
    /// explosions, breaking doors/barricades, screams.
    /// </summary>
    public static class NoiseSystem
    {
        /// <summary>Base noise from a running generator.</summary>
        private const float GeneratorBaseNoise = 0.30f;

        /// <summary>Base noise from a fueled device (campfire, smithy).</summary>
        private const float FueledBaseNoise = 0.15f;

        /// <summary>Noise radius in cells. Affected chunks are within
        /// this distance of the source.</summary>
        private const float NoiseRadius = 25f;

        private const int ChunkEdge = 16;

        /// <summary>
        /// Full refresh: zeros all chunk noise, scans the map for
        /// noise sources, distributes their contribution to nearby
        /// chunks with inverse-square falloff.
        /// </summary>
        /// <returns>Number of active loud sources found.</returns>
        public static int Refresh(Map map, Dictionary<int, ChunkState> chunks, long currentTick)
        {
            int sourceCount = 0;
            if (map == null || chunks == null) return sourceCount;

            // Zero noise in all chunks.
            foreach (var kv in chunks)
                kv.Value.NoiseLevel = 0f;

            // Scan buildings.
            if (map.listerBuildings?.allBuildingsColonist != null)
            {
                foreach (var building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building == null) continue;
                    float noise = ComputeBuildingNoise(building);
                    if (noise > 0f)
                    {
                        DistributeNoise(chunks, building.Position, noise);
                        sourceCount++;
                    }
                }
            }

            // Scan non-building things.
            if (map.listerThings?.AllThings != null)
            {
                foreach (var thing in map.listerThings.AllThings)
                {
                    if (thing == null || thing is Verse.Building) continue;
                    float noise = ComputeThingNoise(thing);
                    if (noise > 0f)
                    {
                        DistributeNoise(chunks, thing.Position, noise);
                        sourceCount++;
                    }
                }
            }

            // Normalize: clamp per-chunk noise to [0, 1].
            foreach (var kv in chunks)
            {
                if (kv.Value.NoiseLevel > 1f)
                    kv.Value.NoiseLevel = 1f;
            }

            return sourceCount;
        }

        // ── noise source detection ───────────────────────────

        private static float ComputeBuildingNoise(Verse.Building building)
        {
            // Power trader: generators hum when producing power.
            var power = building.TryGetComp<CompPowerTrader>();
            if (power != null && power.PowerOn)
            {
                // Type detection via def name heuristic.
                string defName = building.def?.defName ?? "";
                if (defName.Contains("Solar", StringComparison.OrdinalIgnoreCase))
                    return 0.02f;
                if (defName.Contains("Wind", StringComparison.OrdinalIgnoreCase))
                    return 0.10f;
                return GeneratorBaseNoise;
            }

            // Refuelable with fuel: campfire, smithy emit crackle.
            var fuel = building.TryGetComp<CompRefuelable>();
            if (fuel != null && fuel.HasFuel)
                return FueledBaseNoise;

            return 0f;
        }

        private static float ComputeThingNoise(Thing thing)
        {
            var fuel = thing.TryGetComp<CompRefuelable>();
            if (fuel != null && fuel.HasFuel)
                return FueledBaseNoise;
            return 0f;
        }

        // ── noise distribution ───────────────────────────────

        private static void DistributeNoise(Dictionary<int, ChunkState> chunks, IntVec3 pos, float baseNoise)
        {
            LightSystem.CellToChunk(pos, out int ccx, out int ccz);
            int radiusChunks = (int)(NoiseRadius / ChunkEdge) + 2;

            for (int dz = -radiusChunks; dz <= radiusChunks; dz++)
            {
                for (int dx = -radiusChunks; dx <= radiusChunks; dx++)
                {
                    int cx = ccx + dx;
                    int cz = ccz + dz;
                    int key = cz * 1000 + cx;
                    if (!chunks.TryGetValue(key, out var chunk)) continue;

                    var center = new IntVec3(
                        cx * ChunkEdge + ChunkEdge / 2, 0,
                        cz * ChunkEdge + ChunkEdge / 2);
                    float dist = pos.DistanceTo(center);
                    float falloff = 1f / (1f + (dist * dist) / (NoiseRadius * NoiseRadius));
                    chunk.NoiseLevel += baseNoise * falloff;
                }
            }
        }

        /// <summary>Quick per-cell noise query for Sprint 2.</summary>
        public static float NoiseAtCell(Map map, Dictionary<int, ChunkState> chunks, IntVec3 cell)
        {
            if (map == null || chunks == null || !cell.InBounds(map)) return 0f;
            LightSystem.CellToChunk(cell, out int cx, out int cz);
            int key = cz * 1000 + cx;
            return chunks.TryGetValue(key, out var chunk) ? chunk.NoiseLevel : 0f;
        }
    }
}
