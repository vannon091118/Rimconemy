using System;
using System.Collections.Generic;
using Rimconemy.Foundation;
using RimWorld;
using Verse;
using Verse.AI;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 2 — Chunk Controller.
    ///
    /// Per-map <see cref="MapComponent"/> that orchestrates infected
    /// pawn behavior. Reads chunk data from <see cref="ChunkGridComponent"/>
    /// and applies deterministic behavior state transitions:
    ///   Dormant → Roaming → Investigating → Assault
    ///
    /// Architecture: the chunk thinks, not the pawn. Infected pawns
    /// read their chunk's state and the global environment, then the
    /// controller assigns movement/attack jobs via vanilla pathfinding.
    ///
    /// Public API:
    ///   • <see cref="RegisterPawn"/>/<see cref="UnregisterPawn"/> — lifecycle.
    ///   • <see cref="GetState"/> — query a pawn's behavioral state.
    ///   • <see cref="AllPawnStates"/> — bulk iteration.
    /// </summary>
    public sealed class ChunkController : MapComponent
    {
        /// <summary>DefName of the hidden infected faction.</summary>
        private const string InfectedFactionDefName = "Rimconemy_HiddenInfectedFaction";

        /// <summary>How often the controller evaluates all infected pawns.</summary>
        public const long EvaluateIntervalTicks = 250;

        /// <summary>Maximum distance an infected pawn roams from its spawn cell.</summary>
        public const float MaxRoamDistance = 60f;

        /// <summary>Roaming movement: distance to pick a wander target.</summary>
        public const float RoamStepDistance = 20f;

        // ── persistent state ──────────────────────────────────
        private Dictionary<int, InfectedPawnState> _pawnStates = new Dictionary<int, InfectedPawnState>();
        private long _lastEvaluateTick = -1;

        // ── Scribe helpers ────────────────────────────────────
        private List<int> _sxPawnIds;
        private List<InfectedPawnState> _sxStates;

        public ChunkController(Map map) : base(map) { }

        // ── tick ──────────────────────────────────────────────

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;

            long currentTick = Find.TickManager?.TicksGame ?? 0L;
            if (currentTick == _lastEvaluateTick) return;
            if (currentTick < _lastEvaluateTick + EvaluateIntervalTicks) return;

            _lastEvaluateTick = currentTick;

            try
            {
                var chunkGrid = ChunkGridComponent.Get(map);
                var env = chunkGrid?.GetGlobalSnapshot();

                if (env == null)
                {
                    // No ChunkGridComponent or no snapshot yet — can't evaluate.
                    return;
                }

                AutoDetectInfectedPawns(currentTick);
                EvaluateAllPawns(chunkGrid, env, currentTick);
                CleanupDeadPawns(currentTick);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] ChunkController.Evaluate failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // ── auto-detection ─────────────────────────────────────

        /// <summary>
        /// Scans the map for infected pawns (Rimconemy_HiddenInfectedFaction)
        /// that aren't yet registered and auto-registers them.
        /// This bridges the gap between spawn (InfectedRaidWorker) and
        /// behavior (ChunkController) so newly spawned infected pawns
        /// immediately receive behavior.
        /// </summary>
        private void AutoDetectInfectedPawns(long currentTick)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null) return;
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || pawn.Destroyed) continue;
                if (_pawnStates.ContainsKey(pawn.thingIDNumber)) continue;
                if (!IsInfectedPawn(pawn)) continue;

                _pawnStates[pawn.thingIDNumber] = new InfectedPawnState(
                    pawn.thingIDNumber, pawn.Position, currentTick);
            }
        }

        /// <summary>
        /// Returns true if the pawn belongs to the hidden infected faction.
        /// Only infected pawns receive behavior from this controller.
        /// </summary>
        private static bool IsInfectedPawn(Pawn pawn)
        {
            if (pawn?.Faction == null) return false;
            return pawn.Faction.def?.defName == InfectedFactionDefName;
        }

        // ── evaluation ────────────────────────────────────────

        private void EvaluateAllPawns(ChunkGridComponent chunkGrid, EnvironmentSnapshot env, long currentTick)
        {
            if (_pawnStates.Count == 0) return;

            float sightRadius = InfectedBehaviorTransition.ComputeInfectedSightRadius(env);

            foreach (var kv in _pawnStates)
            {
                var state = kv.Value;
                if (state == null) continue;

                Pawn pawn = FindPawn(state.PawnThingId);
                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                {
                    state.IsInactive = true;
                    continue;
                }

                state.LastEvaluateTick = currentTick;
                state.LastSightRadius = sightRadius;

                // Read chunk at pawn position.
                var chunk = chunkGrid.GetChunkState(pawn.Position);

                // Per-pawn deterministic seed.
                long tickDay = currentTick / (long)TimeConstants.TicksPerDay;
                int seed = Story.DeterministicRng.GetStableHashCode(
                    $"{pawn.thingIDNumber}|{tickDay}|{map.uniqueID}");
                var rng = new Story.DeterministicRng(seed);

                // Check for colonist visibility.
                Pawn visibleColonist = InfectedBehaviorTransition.FindVisibleColonist(
                    pawn, map, sightRadius);

                // Compute next behavior.
                var nextBehavior = InfectedBehaviorTransition.ComputeNext(
                    state.CurrentBehavior, chunk, env,
                    visibleColonist != null,
                    state.TicksInState(currentTick),
                    ref rng);

                // Apply transition.
                if (nextBehavior != state.CurrentBehavior)
                {
                    state.TransitionTo(nextBehavior, currentTick);
                }

                // Apply job for current behavior.
                ApplyBehaviorJob(pawn, state, chunk, chunkGrid, env, visibleColonist, ref rng, currentTick);
            }
        }

        // ── behavior → job ────────────────────────────────────

        private void ApplyBehaviorJob(
            Pawn pawn,
            InfectedPawnState state,
            ChunkState chunk,
            ChunkGridComponent chunkGrid,
            EnvironmentSnapshot env,
            Pawn visibleColonist,
            ref Story.DeterministicRng rng,
            long currentTick)
        {
            // Don't interrupt an existing attack job.
            if (state.CurrentBehavior == InfectedBehaviorState.Assault
                && pawn.CurJob != null
                && pawn.CurJob.def == JobDefOf.AttackMelee)
                return;

            switch (state.CurrentBehavior)
            {
                case InfectedBehaviorState.Dormant:
                    // Stay still. Clear any pending jobs.
                    if (pawn.CurJob != null && pawn.CurJob.def != JobDefOf.Wait)
                    {
                        pawn.jobs.StopAll();
                    }
                    break;

                case InfectedBehaviorState.Roaming:
                    ApplyRoamingJob(pawn, state, chunkGrid, env, ref rng);
                    break;

                case InfectedBehaviorState.Investigating:
                    ApplyInvestigatingJob(pawn, state, chunk, chunkGrid, ref rng);
                    break;

                case InfectedBehaviorState.Assault:
                    ApplyAssaultJob(pawn, state, visibleColonist, currentTick);
                    break;
            }
        }

        // ── roaming ───────────────────────────────────────────

        private void ApplyRoamingJob(
            Pawn pawn,
            InfectedPawnState state,
            ChunkGridComponent chunkGrid,
            EnvironmentSnapshot env,
            ref Story.DeterministicRng rng)
        {
            // Pick a random direction biased toward attractive chunks.
            IntVec3 target = PickRoamTarget(pawn, state, chunkGrid, ref rng);
            state.TargetCell = target;

            var job = JobMaker.MakeJob(JobDefOf.Goto, target);
            job.locomotionUrgency = LocomotionUrgency.Walk;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        private IntVec3 PickRoamTarget(
            Pawn pawn,
            InfectedPawnState state,
            ChunkGridComponent chunkGrid,
            ref Story.DeterministicRng rng)
        {
            // Strategy: pick 5 random directions, score each by the
            // target chunk's attraction. Pick the best one.
            const int candidates = 5;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = -1f;

            for (int i = 0; i < candidates; i++)
            {
                float angle = rng.NextFloat() * 360f;
                float dist = RoamStepDistance * (0.5f + rng.NextFloat() * 1.0f);
                float rad = angle * MathF.PI / 180f;
                int dx = (int)(MathF.Cos(rad) * dist);
                int dz = (int)(MathF.Sin(rad) * dist);
                var cell = pawn.Position + new IntVec3(dx, 0, dz);

                if (!cell.InBounds(map)) continue;

                // Score by chunk attraction.
                var targetChunk = chunkGrid?.GetChunkState(cell);
                float score = targetChunk?.Attraction ?? 0f;

                if (score > bestScore && cell.Walkable(map))
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            if (bestCell.IsValid && bestCell.Walkable(map))
                return bestCell;

            // Fallback: pick any walkable cell near spawn.
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var cell = state.SpawnCell + new IntVec3(
                    rng.NextInt(20) - 10, 0, rng.NextInt(20) - 10);
                if (cell.InBounds(map) && cell.Walkable(map))
                    return cell;
            }

            // Last resort: use vanilla cell finder near the pawn's position.
            // This handles the case where SpawnCell is no longer walkable
            // (e.g., a wall was built on it).
            IntVec3 lastResort = CellFinder.RandomClosewalkCellNear(pawn.Position, map, 10);
            if (lastResort.IsValid)
                return lastResort;

            // Absolute last fallback — return pawn's own position.
            return pawn.Position;
        }

        // ── investigating ─────────────────────────────────────

        private void ApplyInvestigatingJob(
            Pawn pawn,
            InfectedPawnState state,
            ChunkState chunk,
            ChunkGridComponent chunkGrid,
            ref Story.DeterministicRng rng)
        {
            // Move toward the center of the suspicious chunk.
            IntVec3 target;

            if (chunk != null)
            {
                int centerX = chunk.ChunkX * 16 + 8;
                int centerZ = chunk.ChunkZ * 16 + 8;
                target = new IntVec3(centerX, 0, centerZ);
            }
            else
            {
                // No chunk data — wander toward noise.
                target = PickRoamTarget(pawn, state, chunkGrid, ref rng);
            }

            if (!target.InBounds(map))
                target = pawn.Position;

            // Find a walkable cell near the target.
            IntVec3 walkable = target;
            if (!walkable.Walkable(map))
            {
                walkable = CellFinder.RandomClosewalkCellNear(target, map, 5);
            }

            state.TargetCell = walkable;

            var job = JobMaker.MakeJob(JobDefOf.Goto, walkable);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        // ── assault ───────────────────────────────────────────

        private void ApplyAssaultJob(
            Pawn pawn,
            InfectedPawnState state,
            Pawn visibleColonist,
            long currentTick)
        {
            if (visibleColonist == null) return;

            state.TargetColonistId = visibleColonist.thingIDNumber;
            state.TargetCell = visibleColonist.Position;

            // Use vanilla melee attack job.
            var job = JobMaker.MakeJob(JobDefOf.AttackMelee, visibleColonist);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.canBashDoors = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        // ── cleanup ───────────────────────────────────────────

        private void CleanupDeadPawns(long currentTick)
        {
            var toRemove = new List<int>();
            foreach (var kv in _pawnStates)
            {
                if (kv.Value == null || kv.Value.IsInactive)
                {
                    toRemove.Add(kv.Key);
                    continue;
                }

                Pawn pawn = FindPawn(kv.Key);
                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                    toRemove.Add(kv.Key);
            }

            foreach (int id in toRemove)
                _pawnStates.Remove(id);
        }

        private static Pawn FindPawn(int thingId)
        {
            // Search all loaded maps for the pawn by iterating
            // mapPawns.AllPawnsSpawned (canonical 1.6 path).
            if (Find.Maps == null) return null;
            foreach (var m in Find.Maps)
            {
                if (m?.mapPawns?.AllPawnsSpawned == null) continue;
                foreach (var p in m.mapPawns.AllPawnsSpawned)
                {
                    if (p != null && p.thingIDNumber == thingId)
                        return p;
                }
            }
            return null;
        }

        // ── public API ───────────────────────────────────────

        /// <summary>
        /// Registers a newly spawned infected pawn. If the pawn is
        /// already registered, updates its spawn cell.
        /// Called by the spawn bridge or by auto-detection.
        /// Only pawns from Rimconemy_HiddenInfectedFaction are accepted.
        /// </summary>
        public void RegisterPawn(Pawn pawn, long currentTick)
        {
            if (pawn == null || pawn.thingIDNumber <= 0) return;
            if (!IsInfectedPawn(pawn)) return;

            if (_pawnStates.TryGetValue(pawn.thingIDNumber, out var existing))
            {
                existing.SpawnCell = pawn.Position;
                existing.IsInactive = false;
                return;
            }

            _pawnStates[pawn.thingIDNumber] = new InfectedPawnState(
                pawn.thingIDNumber, pawn.Position, currentTick);
        }

        /// <summary>Removes a pawn from the controller.</summary>
        public void UnregisterPawn(int pawnThingId)
        {
            _pawnStates.Remove(pawnThingId);
        }

        /// <summary>Returns the behavioral state for a given pawn.</summary>
        public InfectedPawnState GetState(int pawnThingId)
        {
            _pawnStates.TryGetValue(pawnThingId, out var state);
            return state;
        }

        /// <summary>All active pawn states for bulk operations.</summary>
        public IEnumerable<InfectedPawnState> AllPawnStates()
        {
            foreach (var kv in _pawnStates)
                if (kv.Value != null && !kv.Value.IsInactive)
                    yield return kv.Value;
        }

        /// <summary>Number of active infected pawns.</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var kv in _pawnStates)
                    if (kv.Value != null && !kv.Value.IsInactive)
                        count++;
                return count;
            }
        }

        // ── save / load ──────────────────────────────────────

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _lastEvaluateTick, "chunkCtrlLastEval", -1L);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _sxPawnIds = new List<int>(_pawnStates.Count);
                _sxStates = new List<InfectedPawnState>(_pawnStates.Count);
                foreach (var kv in _pawnStates)
                {
                    if (kv.Value == null) continue;
                    _sxPawnIds.Add(kv.Key);
                    _sxStates.Add(kv.Value);
                }
            }

            Scribe_Collections.Look(ref _sxPawnIds, "pawnStateIds", LookMode.Value);
            Scribe_Collections.Look(ref _sxStates, "pawnStates", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _pawnStates = new Dictionary<int, InfectedPawnState>();
                if (_sxPawnIds != null && _sxStates != null)
                {
                    int count = Math.Min(_sxPawnIds.Count, _sxStates.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var state = _sxStates[i];
                        if (state != null)
                        {
                            state.IsInactive = false;
                            state.LastSightRadius = 0f;
                            _pawnStates[_sxPawnIds[i]] = state;
                        }
                    }
                }
                _sxPawnIds = null;
                _sxStates = null;
                _lastEvaluateTick = -1;
            }
        }

        // ── static accessor ───────────────────────────────────

        public static ChunkController Get(Map map)
        {
            return map?.GetComponent<ChunkController>();
        }
    }
}
