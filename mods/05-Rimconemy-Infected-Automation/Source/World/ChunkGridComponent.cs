using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 1 — Chunk Grid Component.
    ///
    /// Per-map <see cref="MapComponent"/> that divides the map into
    /// 16×16-cell chunks and aggregates light, noise, targets and
    /// alert state for each chunk on a 250-tick refresh interval.
    ///
    /// Architecture principle: the chunk thinks, not the individual
    /// pawn. Infected pawns (Sprint 2) read chunk state to decide
    /// behavior — they don't compute their own environment.
    ///
    /// Public API:
    ///   • <see cref="GetGlobalSnapshot"/> — current EnvironmentSnapshot.
    ///   • <see cref="GetChunkState(IntVec3)"/> — ChunkState for a cell.
    ///   • <see cref="AllChunks"/> — all active chunks for bulk ops.
    /// </summary>
    public sealed class ChunkGridComponent : MapComponent
    {
        public const long RefreshIntervalTicks = 250;
        public const long MaxStaleAgeTicks = 1500; // 25s = 6 refresh cycles

        // ── persistent state ──────────────────────────────────
        private Dictionary<int, ChunkState> _chunks = new Dictionary<int, ChunkState>();
        private EnvironmentSnapshot _globalEnv;
        private long _lastRefreshTick = -1;

        // ── Scribe helpers ────────────────────────────────────
        private List<int> _sxKeys;
        private List<ChunkState> _sxValues;

        public ChunkGridComponent(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;

            long currentTick = Find.TickManager?.TicksGame ?? 0L;
            if (currentTick == _lastRefreshTick) return;
            if (currentTick < _lastRefreshTick + RefreshIntervalTicks) return;

            _lastRefreshTick = currentTick;

            try
            {
                // 1. Light: builds global env + per-chunk LightExposure.
                _globalEnv = LightSystem.Refresh(map, _chunks, currentTick);

                // 2. Noise: per-chunk NoiseLevel.
                int loudSources = NoiseSystem.Refresh(map, _chunks, currentTick);
                _globalEnv.ActiveLoudSources = loudSources;

                // 3. Attraction: combine light + noise weighted by darkness.
                ComputeAttraction();

                // 4. Decay stale chunks.
                DecayStaleChunks(currentTick);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] ChunkGridComponent.Refresh failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // ── attraction ────────────────────────────────────────

        /// <summary>
        /// Computes <see cref="ChunkState.Attraction"/> for every
        /// chunk. At night (high DarknessFactor), light and noise
        /// become more magnetic. The formula comes from the Sprint
        /// plan's <c>PerceptionMath.ComputeAttraction</c>:
        ///   lightWeight = 1 + darknessFactor × 1.25
        ///   noiseWeight = 1 + darknessFactor × 0.75
        ///   attraction  = light × lightWeight + noise × noiseWeight
        /// </summary>
        private void ComputeAttraction()
        {
            if (_globalEnv == null || _chunks == null) return;

            float darkness = _globalEnv.DarknessFactor;

            foreach (var kv in _chunks)
            {
                var chunk = kv.Value;
                chunk.Attraction = PerceptionMath.ComputeAttraction(
                    chunk.LightExposure, chunk.NoiseLevel, darkness);
            }
        }

        // ── decay ─────────────────────────────────────────────

        private void DecayStaleChunks(long currentTick)
        {
            var toRemove = new List<int>();
            foreach (var kv in _chunks)
            {
                var chunk = kv.Value;
                if (!chunk.IsStale(currentTick, MaxStaleAgeTicks)) continue;

                // Alert: step down one level per decay pass.
                if (chunk.AlertState > ChunkAlertState.Dormant)
                    chunk.AlertState = (ChunkAlertState)((int)chunk.AlertState - 1);

                // KnownTargets: clear very old targets.
                if (chunk.KnownTargets.Count > 0 && chunk.AlertState == ChunkAlertState.Dormant)
                    chunk.KnownTargets.Clear();

                chunk.LastUpdatedTick = currentTick;

                // Remove chunks that have decayed to zero and have no
                // targets. Keeps the dictionary from growing boundlessly
                // for chunks that were touched once and never again.
                if (chunk.AlertState == ChunkAlertState.Dormant
                    && chunk.NoiseLevel < 0.01f
                    && chunk.LightExposure < 0.01f
                    && chunk.KnownTargets.Count == 0)
                {
                    toRemove.Add(kv.Key);
                }
            }

            foreach (int key in toRemove)
                _chunks.Remove(key);
        }

        // ── public API ───────────────────────────────────────

        /// <summary>Returns the most recent global environment snapshot.</summary>
        public EnvironmentSnapshot GetGlobalSnapshot() => _globalEnv;

        /// <summary>
        /// Returns the ChunkState for the chunk containing the given
        /// cell. Creates the chunk lazily if it doesn't exist yet.
        /// </summary>
        public ChunkState GetChunkState(IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map)) return null;

            LightSystem.CellToChunk(cell, out int cx, out int cz);
            int key = cz * 1000 + cx;

            if (!_chunks.TryGetValue(key, out var chunk))
            {
                chunk = new ChunkState(cx, cz);
                _chunks[key] = chunk;
            }
            return chunk;
        }

        /// <summary>All active chunks for bulk iteration.</summary>
        public IEnumerable<ChunkState> AllChunks() => _chunks.Values;

        /// <summary>
        /// Notifies a chunk that a target pawn was observed here.
        /// Called by the infected pawn adapter (Sprint 2) when a
        /// survivor is spotted.
        /// </summary>
        public void ReportTarget(IntVec3 cell, int pawnThingId)
        {
            var chunk = GetChunkState(cell);
            if (chunk == null) return;

            chunk.KnownTargets.Add(pawnThingId);
            if (chunk.AlertState < ChunkAlertState.Assault)
                chunk.AlertState = ChunkAlertState.Investigating;

            long tick = Find.TickManager?.TicksGame ?? 0L;
            chunk.LastUpdatedTick = tick;
        }

        // ── save / load ──────────────────────────────────────

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _lastRefreshTick, "chunkGridLastRefresh", -1L);

            // Serialize dictionary → parallel lists.
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _sxKeys = new List<int>(_chunks.Count);
                _sxValues = new List<ChunkState>(_chunks.Count);
                foreach (var kv in _chunks)
                {
                    _sxKeys.Add(kv.Key);
                    _sxValues.Add(kv.Value);
                }
            }

            Scribe_Collections.Look(ref _sxKeys, "chunkKeys", LookMode.Value);
            Scribe_Collections.Look(ref _sxValues, "chunkValues", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _chunks = new Dictionary<int, ChunkState>();
                if (_sxKeys != null && _sxValues != null)
                {
                    int count = Math.Min(_sxKeys.Count, _sxValues.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var chunk = _sxValues[i];
                        if (chunk != null)
                            _chunks[_sxKeys[i]] = chunk;
                    }
                }
                _sxKeys = null;
                _sxValues = null;

                // Trigger a full refresh on next tick so the global
                // env snapshot is rebuilt from the loaded chunks.
                _lastRefreshTick = -1;
            }
        }

        // ── static accessor ───────────────────────────────────

        public static ChunkGridComponent Get(Map map)
        {
            return map?.GetComponent<ChunkGridComponent>();
        }
    }
}
