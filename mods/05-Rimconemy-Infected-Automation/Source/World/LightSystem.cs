using System;
using System.Collections.Generic;
using Rimconemy.Foundation;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 1 — Light System.
    ///
    /// Computes:
    ///   1. Global <see cref="EnvironmentSnapshot"/> daylight/weather fields.
    ///   2. Per-chunk <see cref="ChunkState.LightExposure"/> from daylight
    ///      + <see cref="CompGlower"/> sources on the map.
    ///
    /// Daylight is a piecewise-linear curve over in-game hours:
    ///   Night  (20..5): 0.0
    ///   Dawn   (5..7):  linear 0.0 → 1.0
    ///   Day    (7..18): 1.0
    ///   Dusk   (18..20): linear 1.0 → 0.0
    ///
    /// Weather attenuates outdoor light: clear=1.0, rain=0.7,
    /// fog=0.4, foggyRain=0.5, snow=0.8.
    /// </summary>
    public static class LightSystem
    {
        private const int DawnStartHour = 5;
        private const int DayStartHour = 7;
        private const int DuskStartHour = 18;
        private const int NightStartHour = 20;

        /// <summary>How strongly a full-radius glower contributes to
        /// its chunk's light. Conservative — a single torch doesn't
        /// light like daytime.</summary>
        private const float GlowerBaseContribution = 0.06f;

        /// <summary>Radius (in cells) for glower chunk distribution.
        /// A glower affects chunks within this distance.</summary>
        private const float GlowerScanRadius = 20f;

        private const int ChunkEdge = 16;

        /// <summary>
        /// Full refresh: builds the global EnvironmentSnapshot and
        /// recomputes LightExposure for every active chunk.
        /// </summary>
        public static EnvironmentSnapshot Refresh(Map map, Dictionary<int, ChunkState> chunks, long currentTick)
        {
            var env = new EnvironmentSnapshot { Tick = currentTick };

            if (map == null) return env;

            // ── daylight / darkness / weather ──────────────────
            float hour = HourOfDay(currentTick);
            env.DaylightFactor = DaylightCurve(hour);
            env.WeatherFactor = WeatherAttenuation(map);
            env.DarknessFactor = 1f - (env.DaylightFactor * env.WeatherFactor);

            // Apply weather to daylight for outdoor chunks.
            float outdoorLight = env.DaylightFactor * env.WeatherFactor;

            // Blackout: power grid offline.
            env.IsBlackout = !IsAnyGridPowerActive(map);

            // ── zero chunk light → start from outdoor baseline ──
            if (chunks != null)
            {
                foreach (var kv in chunks)
                {
                    kv.Value.LightExposure = outdoorLight;
                    kv.Value.LastUpdatedTick = currentTick;
                }

                // ── scan glowers and accumulate ──────────────────
                int glowerCount = ScanGlowers(map, chunks);
                env.ActiveLightSources = glowerCount;
            }

            // ── global alert: max chunk alert state ────────────
            if (chunks != null)
            {
                float maxAlert = 0f;
                foreach (var kv in chunks)
                {
                    if ((int)kv.Value.AlertState > (int)maxAlert)
                        maxAlert = (float)kv.Value.AlertState;
                }
                env.GlobalAlert = maxAlert / 3f; // normalize 0..3 → 0..1
            }

            return env;
        }

        // ── daylight curve ────────────────────────────────────

        private static float DaylightCurve(float hour)
        {
            if (hour < DawnStartHour || hour >= NightStartHour)
                return 0.0f;
            if (hour < DayStartHour)
                return (hour - DawnStartHour) / (DayStartHour - DawnStartHour);
            if (hour < DuskStartHour)
                return 1.0f;
            return 1.0f - (hour - DuskStartHour) / (NightStartHour - DuskStartHour);
        }

        private static float HourOfDay(long tick)
        {
            float dayProgress = (tick % (long)TimeConstants.TicksPerDay) / TimeConstants.TicksPerDay;
            return dayProgress * 24f;
        }

        // ── weather ───────────────────────────────────────────

        private static float WeatherAttenuation(Map map)
        {
            if (map?.weatherManager?.curWeather == null) return 0.8f;
            string w = map.weatherManager.curWeather.defName;
            if (w.Contains("Fog", StringComparison.OrdinalIgnoreCase))
                return w.Contains("Rain", StringComparison.OrdinalIgnoreCase) ? 0.5f : 0.4f;
            if (w.Contains("Rain", StringComparison.OrdinalIgnoreCase)) return 0.7f;
            if (w.Contains("Snow", StringComparison.OrdinalIgnoreCase)) return 0.8f;
            return 1.0f;
        }

        // ── power grid ────────────────────────────────────────

        private static bool IsAnyGridPowerActive(Map map)
        {
            if (map?.listerBuildings?.allBuildingsColonist == null) return true;
            var buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                var comp = buildings[i]?.TryGetComp<CompPowerTrader>();
                if (comp != null && comp.PowerOn) return false;
            }
            return true;
        }

        // ── glower scan ───────────────────────────────────────

        private static int ScanGlowers(Map map, Dictionary<int, ChunkState> chunks)
        {
            int count = 0;

            // Buildings with CompGlower.
            if (map.listerBuildings?.allBuildingsColonist != null)
            {
                foreach (var building in map.listerBuildings.allBuildingsColonist)
                {
                    var glower = building?.TryGetComp<CompGlower>();
                    if (glower == null || !glower.Glows) continue;
                    AccumulateGlower(chunks, building.Position, glower.GlowRadius);
                    count++;
                }
            }

            // Non-building things with CompGlower (campfires, torches as items).
            if (map.listerThings?.AllThings != null)
            {
                foreach (var thing in map.listerThings.AllThings)
                {
                    if (thing == null || thing is Verse.Building) continue;
                    var glower = thing.TryGetComp<CompGlower>();
                    if (glower == null || !glower.Glows) continue;
                    AccumulateGlower(chunks, thing.Position, glower.GlowRadius);
                    count++;
                }
            }

            return count;
        }

        private static void AccumulateGlower(Dictionary<int, ChunkState> chunks, IntVec3 pos, float radius)
        {
            CellToChunk(pos, out int ccx, out int ccz);
            int radiusChunks = (int)(radius / ChunkEdge) + 1;

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
                    float falloff = 1f - Math.Min(1f, dist / (radius + 1f));
                    chunk.LightExposure += GlowerBaseContribution * radius * falloff;
                }
            }
        }

        // ── cell → chunk helpers ──────────────────────────────

        public static void CellToChunk(IntVec3 cell, out int cx, out int cz)
        {
            cx = cell.x / ChunkEdge;
            cz = cell.z / ChunkEdge;
        }

        /// <summary>Quick per-cell light query for Sprint 2 infected pawns.</summary>
        public static float LightAtCell(Map map, Dictionary<int, ChunkState> chunks, IntVec3 cell)
        {
            if (map == null || chunks == null || !cell.InBounds(map)) return 0f;
            CellToChunk(cell, out int cx, out int cz);
            int key = cz * 1000 + cx;
            if (!chunks.TryGetValue(key, out var chunk)) return 0f;

            bool roofed = map.roofGrid != null && map.roofGrid.Roofed(cell);
            if (roofed)
            {
                // Roof blocks outdoor daylight; only artificial light reaches.
                long tick = Find.TickManager?.TicksGame ?? 0L;
                float daylight = DaylightCurve(HourOfDay(tick)) * WeatherAttenuation(map);
                return Math.Max(0f, chunk.LightExposure - daylight);
            }
            return chunk.LightExposure;
        }
    }
}
