using System.Collections.Generic;
using Rimconemy.Foundation.Save;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 1 — Per-Chunk State.
    ///
    /// Each 16×16-cell chunk holds its own aggregated perception
    /// data. ChunkAI (Sprint 2) reads this to decide infected
    /// behavior. The ChunkGridComponent owns the dictionary of
    /// these and persists them via Scribe.
    ///
    /// Implements <see cref="ISchemaMigratable"/> per the save
    /// contract — Sprint 6 will add concrete migration steps.
    /// </summary>
    public sealed class ChunkState : IExposable, ISchemaMigratable
    {
        /// <summary>Chunk X coordinate (cell.x / 16).</summary>
        public int ChunkX;

        /// <summary>Chunk Z coordinate (cell.z / 16).</summary>
        public int ChunkZ;

        /// <summary>
        /// Aggregated light exposure in [0, 1]. Combines outdoor
        /// daylight + artificial glowers mapped to this chunk.
        /// 0 = pitch black, 1 = full noon / well-lit interior.
        /// </summary>
        public float LightExposure;

        /// <summary>
        /// Aggregated noise level in [0, 1]. Computed from active
        /// generators, fueled devices, and — Sprint 3 — combat sounds.
        /// 0 = silent, 1 = generator room at full load.
        /// </summary>
        public float NoiseLevel;

        /// <summary>
        /// Attraction score [0, 2] computed from LightExposure and
        /// NoiseLevel, weighted by DarknessFactor. At night, light
        /// and noise become more attractive (magnetic) to infected.
        /// NOT persisted — recomputed every refresh cycle.
        /// ChunkAI uses this to pick where infected wander toward.
        /// </summary>
        public float Attraction;

        /// <summary>Alert escalation level for this chunk.</summary>
        public ChunkAlertState AlertState;

        /// <summary>
        /// Set of ThingID numbers (pawn.thingIDNumber) that have been
        /// recently observed in this chunk. Used by ChunkAI to
        /// prioritize targets without iterating the whole map.
        /// </summary>
        public HashSet<int> KnownTargets = new HashSet<int>();

        /// <summary>Game tick when this chunk was last refreshed.</summary>
        public long LastUpdatedTick;

        // ── ISchemaMigratable ────────────────────────────────

        /// <summary>Schema version for save migration. Sprint 6 adds steps.</summary>
        public int SchemaVersion = 1;

        int ISchemaMigratable.CurrentSchemaVersion => 1;
        int ISchemaMigratable.SchemaVersion { get => SchemaVersion; set => SchemaVersion = value; }
        public string ClassId => "rimconemy.infectedautomation.chunkState";
        public IList<SchemaStep> Steps => System.Array.Empty<SchemaStep>();
        public void MigrateIfNeeded() { this.RunMigration(); }

        // ── constructors ─────────────────────────────────────

        public ChunkState() { }

        public ChunkState(int cx, int cz)
        {
            ChunkX = cx;
            ChunkZ = cz;
        }

        /// <summary>Stable chunk key: z * 1000 + x (safe up to 999 chunks/axis — vanilla max is ~21).</summary>
        public int ChunkKey => ChunkZ * 1000 + ChunkX;

        public bool IsStale(long currentTick, long maxAgeTicks)
        {
            return currentTick - LastUpdatedTick > maxAgeTicks;
        }

        public override string ToString()
        {
            return $"[{ChunkX},{ChunkZ}] light={LightExposure:F2} noise={NoiseLevel:F2} attr={Attraction:F2} alert={AlertState} targets={KnownTargets.Count}";
        }

        // ── Scribe ──────────────────────────────────────────

        public void ExposeData()
        {
            Scribe_Values.Look(ref ChunkX, "chunkX", 0);
            Scribe_Values.Look(ref ChunkZ, "chunkZ", 0);
            Scribe_Values.Look(ref LightExposure, "lightExposure", 0f);
            Scribe_Values.Look(ref NoiseLevel, "noiseLevel", 0f);
            Scribe_Values.Look(ref AlertState, "alertState", ChunkAlertState.Dormant);
            Scribe_Values.Look(ref LastUpdatedTick, "lastUpdatedTick", 0L);
            Scribe_Values.Look(ref SchemaVersion, "schemaVersion", 1);
            // Attraction is NOT persisted — derived data, recomputed every cycle.

            // HashSet<int> → List<int> for Scribe compatibility.
            List<int> targetsList = null;
            if (Scribe.mode == LoadSaveMode.Saving && KnownTargets != null)
            {
                targetsList = new List<int>(KnownTargets);
            }
            Scribe_Collections.Look(ref targetsList, "knownTargets", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && targetsList != null)
            {
                KnownTargets = new HashSet<int>(targetsList);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                KnownTargets = new HashSet<int>();
            }
        }
    }
}
