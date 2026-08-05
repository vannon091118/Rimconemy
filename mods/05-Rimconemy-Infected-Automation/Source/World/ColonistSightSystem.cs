using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 2.5 — Colonist Sight System.
    ///
    /// Per-map <see cref="MapComponent"/> that computes directional
    /// sight cones for colonists and renders a darkness overlay.
    /// Modeled after Project Zomboid: forward cone is visible,
    /// sides dim, behind is dark. Light sources extend vision.
    /// Mouse cursor adds a weak glow.
    ///
    /// Visual: supplies a per-cell visibility grid to the existing
    /// world-space SectionLayer_Darkness renderer. Cells outside all
    /// colonist sight cones are dimmed without screen-space rectangles.
    ///
    /// Architecture:
    ///   - Visibility grid: flat float[] per cell, max across all colonists
    ///   - Update: every 60 ticks (1 second) for colonist contributions
    ///   - Render: vanilla SectionLayer_Darkness mesh, regenerated when dirty
    /// </summary>
    public sealed class ColonistSightSystem : MapComponent
    {
        /// <summary>How often to recompute colonist sight cones.</summary>
        public const int SightUpdateInterval = 60; // 1 second

        /// <summary>Grid resolution: how many cells per axis.</summary>
        private int _gridWidth;
        private int _gridHeight;

        /// <summary>Per-cell visibility [0, 1]. 0 = pitch black, 1 = fully visible.</summary>
        private float[] _visibilityGrid;

        /// <summary>Last tick the grid was updated.</summary>
        private int _lastUpdateTick = -1;
        private float[] _previousVisibilityGrid;

        /// <summary>True after at least one <see cref="ComputeAllColonistSight"/>
        /// has processed a non-zero set of colonists. Used by
        /// <see cref="DarknessSectionLayerLifecycle"/> to gate the
        /// AmbientVeilAlpha floor so the init / no-colonists branches do not
        /// produce a permanent global shadow.</summary>
        private bool _hasActiveSight;

        /// <summary>Cached mouse cell for glow.</summary>
        private IntVec3 _lastMouseCell = IntVec3.Invalid;

        /// <summary>Whether the overlay is currently active.</summary>
        public bool Enabled = true;

        public ColonistSightSystem(Map map) : base(map)
        {
            if (map != null)
            {
                _gridWidth = map.Size.x;
                _gridHeight = map.Size.z;
                _visibilityGrid = new float[_gridWidth * _gridHeight];
                _previousVisibilityGrid = new float[_gridWidth * _gridHeight];
                // Initialize with full visibility so colonists are not
                // invisible before the first tick computes the grid.
                for (int i = 0; i < _visibilityGrid.Length; i++)
                    _visibilityGrid[i] = 1f;
            }
        }

        /// <summary>Force recompute on next tick (e.g. after save/load).
        /// Re-initializes grid to full visibility to prevent a black screen
        /// until the first tick completes. Resets <see cref="_hasActiveSight"/>
        /// and the snapshot so the AmbientVeilAlpha applies again only after
        /// the first post-load tick has actually processed colonists.</summary>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (_visibilityGrid != null)
            {
                for (int i = 0; i < _visibilityGrid.Length; i++)
                {
                    _visibilityGrid[i] = 1f;
                    if (_previousVisibilityGrid != null)
                        _previousVisibilityGrid[i] = 1f;
                }
            }
            _lastUpdateTick = -1;
            _hasActiveSight = false;
            MarkDarknessSectionsDirty(null);
        }

        // ── tick ──────────────────────────────────────────────

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null || !Enabled) return;

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick == _lastUpdateTick) return;
            if (currentTick < _lastUpdateTick + SightUpdateInterval) return;

            _lastUpdateTick = currentTick;

            try
            {
                ComputeAllColonistSight(currentTick);
                var changedSections = ComputeChangedSections();
                MarkDarknessSectionsDirty(changedSections);
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] ColonistSightSystem.Compute failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // ── sight computation ──────────────────────────────────

        private void ComputeAllColonistSight(int currentTick)
        {
            // Reset visibility grid.
            Array.Clear(_visibilityGrid, 0, _visibilityGrid.Length);

            // Track mouse cell. Use fully-qualified to avoid namespace collision
            // with Rimconemy.InfectedAutomation.UI.
            try { _lastMouseCell = Verse.UI.MouseCell(); }
            catch { _lastMouseCell = IntVec3.Invalid; }

            // Get all colonists.
            var colonists = GetColonists();
            if (colonists.Count == 0)
            {
                // No colonists — full visibility (avoid perma-darkness).
                for (int i = 0; i < _visibilityGrid.Length; i++)
                    _visibilityGrid[i] = 1f;
                return;
            }

            // Process each colonist.
            int processedCount = 0;
            foreach (var colonist in colonists)
            {
                if (colonist == null || colonist.Dead || !colonist.Spawned) continue;
                ComputeSinglePawnSight(colonist);
                processedCount++;
            }

            // Also add mouse glow to grid.
            ApplyMouseGlow();

            // Ensure at least one colonist processed.
            if (processedCount == 0)
            {
                for (int i = 0; i < _visibilityGrid.Length; i++)
                    _visibilityGrid[i] = 1f;
                return;
            }

            _hasActiveSight = true;
        }

        private void ComputeSinglePawnSight(Pawn pawn)
        {
            IntVec3 pawnPos = pawn.Position;
            IntVec3 facing = SightConeMath.GetPawnFacing(pawn);
            float cellLight = SightConeMath.GetCellLightLevel(map, pawnPos);
            float glowerBonus = SightConeMath.ComputeNearbyGlowerBonus(map, pawnPos);

            // Max radius to scan.
            float scanRadius = SightConeMath.MaxForwardRadius + 5f;
            int cellRadius = Mathf.CeilToInt(scanRadius);

            int px = pawnPos.x;
            int pz = pawnPos.z;

            for (int dz = -cellRadius; dz <= cellRadius; dz++)
            {
                for (int dx = -cellRadius; dx <= cellRadius; dx++)
                {
                    int cx = px + dx;
                    int cz = pz + dz;
                    if (cx < 0 || cx >= _gridWidth || cz < 0 || cz >= _gridHeight) continue;

                    var cell = new IntVec3(cx, 0, cz);
                    float vis = SightConeMath.ComputeCellVisibility(
                        pawnPos, cell, facing, cellLight, glowerBonus, _lastMouseCell);

                    if (vis > 0f && (cell == pawnPos || GenSight.LineOfSight(pawnPos, cell, map)))
                    {
                        int idx = cz * _gridWidth + cx;
                        _visibilityGrid[idx] = Math.Max(_visibilityGrid[idx], vis);
                    }
                }
            }
        }

        private void ApplyMouseGlow()
        {
            if (!_lastMouseCell.IsValid || !_lastMouseCell.InBounds(map)) return;

            int mcx = _lastMouseCell.x;
            int mcz = _lastMouseCell.z;
            int radius = Mathf.CeilToInt(SightConeMath.MouseGlowRadius);

            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int cx = mcx + dx;
                    int cz = mcz + dz;
                    if (cx < 0 || cx >= _gridWidth || cz < 0 || cz >= _gridHeight) continue;

                    var cell = new IntVec3(cx, 0, cz);
                    float glow = SightConeMath.ComputeCellVisibility(
                        IntVec3.Invalid, cell, IntVec3.Invalid, 0f, 0f, _lastMouseCell);

                    if (glow > 0f)
                    {
                        int idx = cz * _gridWidth + cx;
                        _visibilityGrid[idx] = Math.Max(_visibilityGrid[idx], glow);
                    }
                }
            }
        }

        // ── colonists ─────────────────────────────────────────

        // Cached list reused every tick to avoid per-tick List allocation.
        private readonly List<Pawn> _colonistBuffer = new List<Pawn>();

        private List<Pawn> GetColonists()
        {
            _colonistBuffer.Clear();
            if (map?.mapPawns?.FreeColonistsSpawned == null) return _colonistBuffer;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn != null && !pawn.Dead && pawn.Spawned && pawn.IsColonist)
                    _colonistBuffer.Add(pawn);
            }

            return _colonistBuffer;
        }

        // ── visibility query ───────────────────────────────────

        /// <summary>Returns the visibility at a cell [0, 1].</summary>
        public float GetVisibility(IntVec3 cell)
        {
            if (!Enabled) return 1f;
            if (_visibilityGrid == null || !cell.InBounds(map)) return 1f;
            int idx = cell.z * _gridWidth + cell.x;
            if (idx < 0 || idx >= _visibilityGrid.Length) return 1f;
            return _visibilityGrid[idx];
        }

        /// <summary>Returns true if the cell is in darkness (visibility &lt; 0.5).</summary>
        public bool IsInDarkness(IntVec3 cell)
        {
            return GetVisibility(cell) < 0.5f;
        }

        // ── public accessors ───────────────────────────────────

        public static ColonistSightSystem Get(Map map)
        {
            return map?.GetComponent<ColonistSightSystem>();
        }

        /// <summary>True once at least one tick has processed a non-empty
        /// set of colonists. Stays true for the lifetime of this map; reset
        /// by <see cref="FinalizeInit"/> on save/load. Used by the darkness
        /// renderer to gate the AmbientVeilAlpha floor.</summary>
        public bool HasActiveSight() => _hasActiveSight;

        // ── internal helpers (not engine-touching) ────────────

        // Cached set reused every tick to avoid per-tick HashSet allocation.
        private readonly HashSet<int> _changedSectionsBuffer = new HashSet<int>();

        private HashSet<int> ComputeChangedSections()
        {
            _changedSectionsBuffer.Clear();
            if (_visibilityGrid == null || _previousVisibilityGrid == null)
                return null;

            for (int z = 0; z < _gridHeight; z++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    int index = z * _gridWidth + x;
                    if (Math.Abs(_visibilityGrid[index] - _previousVisibilityGrid[index]) <= 0.01f)
                        continue;
                    _changedSectionsBuffer.Add((z / Section.Size) * 100000 + (x / Section.Size));
                }
            }

            Array.Copy(_visibilityGrid, _previousVisibilityGrid, _visibilityGrid.Length);
            return _changedSectionsBuffer.Count > 0 ? _changedSectionsBuffer : null;
        }

        /// <summary>
        /// Marks each map section dirty for the existing vanilla darkness
        /// layer. Mesh generation remains in RimWorld's map-drawer lifecycle;
        /// no Unity mesh work is performed from the tick method.
        /// </summary>
        private void MarkDarknessSectionsDirty(HashSet<int> changedSections)
        {
            if (map?.mapDrawer == null) return;

            try
            {
                if (changedSections == null)
                {
                    for (int z = 0; z < _gridHeight; z += Section.Size)
                    {
                        for (int x = 0; x < _gridWidth; x += Section.Size)
                            MarkSectionDirty(x, z);
                    }
                    return;
                }

                foreach (int key in changedSections)
                {
                    int zSection = key / 100000;
                    int xSection = key % 100000;
                    MarkSectionDirty(xSection * Section.Size, zSection * Section.Size);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] Darkness sections could not be marked dirty: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void MarkSectionDirty(int x, int z)
        {
            map.mapDrawer.MapMeshDirty(
                new IntVec3(x, 0, z),
                (ulong)MapMeshFlagDefOf.GroundGlow,
                regenAdjacentCells: false,
                regenAdjacentSections: false);
        }
    }

}
