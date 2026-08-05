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
    /// Visual: draws a screen-space darkness overlay. Cells outside
    /// all colonist sight cones are dimmed (semi-transparent black).
    /// The overlay fades smoothly from visible to pitch black.
    ///
    /// Architecture:
    ///   - Visibility grid: flat float[] per cell, max across all colonists
    ///   - Update: every 60 ticks (1 second) for colonist contributions
    ///   - Render: each frame, draw darkness quads for dimmed cells
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
            }
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
            }
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

                    if (vis > 0f)
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

        private List<Pawn> GetColonists()
        {
            var result = new List<Pawn>();
            if (map?.mapPawns?.FreeColonistsSpawned == null) return result;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn != null && !pawn.Dead && pawn.Spawned && pawn.IsColonist)
                    result.Add(pawn);
            }

            return result;
        }

        // ── visibility query ───────────────────────────────────

        /// <summary>Returns the visibility at a cell [0, 1].</summary>
        public float GetVisibility(IntVec3 cell)
        {
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

        // ── visual overlay ────────────────────────────────────

        /// <summary>Draws the darkness overlay over the map.</summary>
        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (!Enabled || _visibilityGrid == null) return;

            // Only render when a map is visible.
            if (Find.CurrentMap != map) return;

            var cameraDriver = Find.CameraDriver;
            if (cameraDriver == null) return;

            CellRect viewRect = cameraDriver.CurrentViewRect;
            if (viewRect.IsEmpty) return;

            // Clip to map bounds.
            viewRect.ClipInsideMap(map);

            // Get the map camera for world→screen conversion.
            var cam = Camera.current;
            if (cam == null) return;

            const int blockSize = 8;

            for (int bz = viewRect.minZ; bz <= viewRect.maxZ; bz += blockSize)
            {
                for (int bx = viewRect.minX; bx <= viewRect.maxX; bx += blockSize)
                {
                    float avgVis = 0f;
                    int count = 0;

                    for (int dz = 0; dz < blockSize; dz++)
                    {
                        for (int dx = 0; dx < blockSize; dx++)
                        {
                            int cx = bx + dx;
                            int cz = bz + dz;
                            if (cx < 0 || cx >= _gridWidth || cz < 0 || cz >= _gridHeight) continue;
                            int idx = cz * _gridWidth + cx;
                            avgVis += _visibilityGrid[idx];
                            count++;
                        }
                    }

                    if (count == 0) continue;
                    avgVis /= count;

                    // Skip fully visible blocks.
                    if (avgVis >= 0.99f) continue;

                    // More darkness = more opaque overlay.
                    float alpha = (1f - avgVis) * 0.78f;

                    // Convert world to screen position.
                    Vector3 worldCenter = new Vector3(bx + blockSize * 0.5f, 0, bz + blockSize * 0.5f);
                    Vector3 screenPos = cam.WorldToScreenPoint(worldCenter);
                    // RimWorld OnGUI uses top-left origin.
                    float sx = screenPos.x;
                    float sy = Screen.height - screenPos.y;

                    // Estimate cell size from camera.
                    Vector3 worldCorner = new Vector3(bx + blockSize, 0, bz + blockSize);
                    Vector3 screenCorner = cam.WorldToScreenPoint(worldCorner);
                    float pixelSize = Mathf.Abs(screenCorner.x - screenPos.x);
                    if (pixelSize < 1f) pixelSize = 32f; // fallback
                    float rectX = sx - pixelSize * 0.5f;
                    float rectY = sy - pixelSize * 0.5f;

                    // Draw the darkness block.
                    var prevColor = GUI.color;
                    GUI.color = new Color(0f, 0f, 0f, alpha);
                    GUI.DrawTexture(new Rect(rectX, rectY, pixelSize, pixelSize), Texture2D.whiteTexture);
                    GUI.color = prevColor;
                }
            }
        }

        // ── static accessor ───────────────────────────────────

        public static ColonistSightSystem Get(Map map)
        {
            return map?.GetComponent<ColonistSightSystem>();
        }
    }
}
