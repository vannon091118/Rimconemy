using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Reuses RimWorld's existing SectionLayer_Darkness lifecycle instead of
    /// registering a competing layer. The prefix replaces only the vanilla
    /// darkness mesh when the Rimconemy sight component is active.
    ///
    /// The mesh is built in map/world coordinates, one contiguous quad per
    /// cell. This deliberately avoids OnGUI, WorldToScreenPoint and screen
    /// pixel rectangles, which were the source of the checkerboard gaps.
    /// </summary>
    public static class DarknessSectionLayerLifecycle
    {
        private const string HarmonyId = "rimconemy.infectedautomation.darkness-sectionlayer";
        // 2026-08-05 v2: raised 0.82 → 1.0 so out-of-cone cells reach COMPLETE
        // black. The previous 0.82 cap left every "pitch black" cell with
        // ≈18 % residual ambient bleed-through which read as "dim" rather
        // than "inkritschwarz". The user explicitly asked for a hard cutoff.
        private const float MaxOverlayAlpha = 1.0f;
        /// <summary>Curve exponent for the sqrt → pow mapping. Lower values
        /// push the curve toward a steep visibility-vs-alpha inversion so
        /// adjacent cells (vis 0.90 vs 0.85) produce visibly different alpha
        /// (≈3 % vs ≈12 %) instead of subtle gradients. 0.4 picked so that
        /// vis=0.5 maps to ≈73 % and vis=0.0 maps to 100 %.</summary>
        private const float AlphaCurveExponent = 0.4f;
        // Conditional ambient veil (2026-08-05): a subtle 4 % black overlay
        // applied on highly visible cells (visibility ≥ ~0.99) so the screen
        // never reads as fully bright. Applied only when the live
        // <see cref="ColonistSightSystem.HasActiveSight"/> gate is true,
        // i.e. once at least one tick has processed colonists. Init and
        // no-colonists branches keep the previous "no perma-darkness"
        // contract intact.
        private const float AmbientVeilAlpha = 0.04f;

        private static readonly FieldInfo SectionField =
            AccessTools.Field(typeof(SectionLayer), "section");

        private static bool _installed;
        private static bool _loggedFirstRebuild;

        // Reusable vertex-lattice buffer. Sections vary in size, so the
        // array is resized only when the current section is larger than
        // the previous allocation. This avoids a new float[] per section
        // per Regenerate tick.
        // Safe to be static: RimWorld game logic is single-threaded;
        // only one map's sections regenerate per frame.
        private static float[] _alphaGridBuffer = Array.Empty<float>();

        /// <summary>Installs the narrow prefix once during Package 05 bootstrap.</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            try
            {
                var target = AccessTools.Method(
                    typeof(SectionLayer_Darkness), nameof(SectionLayer_Darkness.Regenerate));
                if (target == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] Darkness SectionLayer API missing; vanilla renderer retained.");
                    return;
                }

                var harmony = new Harmony(HarmonyId);
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(DarknessSectionLayerLifecycle), nameof(RegeneratePrefix)));

                var visibleGetter = AccessTools.PropertyGetter(
                    typeof(SectionLayer_Darkness), nameof(SectionLayer_Darkness.Visible));
                if (visibleGetter != null)
                {
                    harmony.Patch(
                        visibleGetter,
                        postfix: new HarmonyMethod(
                            typeof(DarknessSectionLayerLifecycle), nameof(VisiblePostfix)));
                }

                Log.Message("[Rimconemy.InfectedAutomation] Darkness SectionLayer world-space renderer installed.");
            }
            catch (Exception ex)
            {
                // Fail closed: vanilla darkness remains authoritative if the
                // local game build changes its internal layer surface.
                Log.Warning("[Rimconemy.InfectedAutomation] Darkness SectionLayer install failed; vanilla renderer retained: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Replaces vanilla regeneration only for maps carrying our sight grid.
        /// Returning true keeps vanilla behavior when the API or component is
        /// unavailable, so a failed optional renderer cannot break map loading.
        /// </summary>
        private static bool RegeneratePrefix(SectionLayer_Darkness __instance)
        {
            try
            {
                if (__instance == null || SectionField == null) return true;
                var section = SectionField.GetValue(__instance) as Section;
                var sight = ColonistSightSystem.Get(section?.map);
                if (sight == null || !sight.Enabled)
                    return true;

                // Harmony bool-prefix semantics: false skips the original
                // only after our mesh was built successfully. Any failed
                // optional rebuild must let vanilla regenerate the layer.
                return !Rebuild(__instance, section, sight);
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] Darkness SectionLayer regeneration failed; vanilla fallback used: "
                    + ex.GetType().Name + ": " + ex.Message);
                return true;
            }
        }

        private static void VisiblePostfix(
            SectionLayer_Darkness __instance,
            ref bool __result)
        {
            try
            {
                if (__instance == null || SectionField == null) return;
                var section = SectionField.GetValue(__instance) as Section;
                var sight = ColonistSightSystem.Get(section?.map);
                if (sight != null && sight.Enabled)
                    __result = true;
            }
            catch
            {
                // Keep the vanilla visibility result on unexpected API state.
            }
        }

        private static bool Rebuild(
            SectionLayer_Darkness layer,
            Section section,
            ColonistSightSystem sight)
        {
            if (layer == null || section?.map == null || sight == null)
                return false;

            var subMesh = layer.GetSubMesh(MatBases.Darkness);
            if (subMesh == null)
                return false;

            CellRect rect = section.CellRect;
            int width = rect.Width;
            int height = rect.Height;
            if (width <= 0 || height <= 0)
                return false;

            subMesh.Clear(MeshParts.All);
            subMesh.disabled = false;

            float altitude = AltitudeLayer.Darkness.AltitudeFor();

            // ConditionalVeil (2026-08-05): ambient floor only when the
            // sight system has processed at least one tick with colonists.
            // Until then the overlay stays at the documented "no perma-
            // darkness" zero baseline so init / loading screens stay clean.
            float ambientVeil = sight.HasActiveSight() ? AmbientVeilAlpha : 0f;

            // Build a complete section lattice. Corner vertices are shared
            // between neighboring cells and receive averaged alpha, so the
            // GPU interpolates the transition instead of exposing cell seams.
            int alphaGridLen = (width + 1) * (height + 1);
            if (_alphaGridBuffer.Length < alphaGridLen)
                _alphaGridBuffer = new float[alphaGridLen];
            for (int z = 0; z <= height; z++)
            {
                for (int x = 0; x <= width; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    AddCellAlpha(rect.minX + x - 1, rect.minZ + z - 1, section.map, sight, ambientVeil, ref sum, ref count);
                    AddCellAlpha(rect.minX + x, rect.minZ + z - 1, section.map, sight, ambientVeil, ref sum, ref count);
                    AddCellAlpha(rect.minX + x - 1, rect.minZ + z, section.map, sight, ambientVeil, ref sum, ref count);
                    AddCellAlpha(rect.minX + x, rect.minZ + z, section.map, sight, ambientVeil, ref sum, ref count);
                    _alphaGridBuffer[z * (width + 1) + x] = count == 0 ? 0f : sum / count;
                }
            }

            // One shared vertex lattice for the whole Section. Shared corner
            // colors are what make neighboring cell quads blend continuously.
            for (int z = 0; z <= height; z++)
            {
                for (int x = 0; x <= width; x++)
                {
                    subMesh.verts.Add(new Vector3(
                        rect.minX + x, altitude, rect.minZ + z));
                    AddColor(subMesh, _alphaGridBuffer[z * (width + 1) + x]);
                }
            }

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int v00 = z * (width + 1) + x;
                    int v10 = v00 + 1;
                    int v11 = (z + 1) * (width + 1) + x + 1;
                    int v01 = (z + 1) * (width + 1) + x;

                    // Counter-clockwise from above: keep the overlay's
                    // normal facing upward so backface culling cannot make
                    // the whole darkness mesh disappear.
                    subMesh.tris.Add(v00);
                    subMesh.tris.Add(v11);
                    subMesh.tris.Add(v10);
                    subMesh.tris.Add(v00);
                    subMesh.tris.Add(v01);
                    subMesh.tris.Add(v11);
                }
            }

            subMesh.disabled = subMesh.tris.Count == 0;
            subMesh.FinalizeMesh(MeshParts.All);
            layer.RefreshSubMeshBounds();

            // One diagnostic marker proves that the custom mesh path reached
            // a real Section regeneration. The visual effect still requires
            // a live in-game observation; this marker deliberately does not
            // claim that the overlay was visible on screen.
            if (!_loggedFirstRebuild)
            {
                _loggedFirstRebuild = true;
                Log.Message("[Rimconemy.InfectedAutomation] Darkness SectionLayer first custom mesh rebuilt: "
                    + width + "x" + height + " cells, black vertex overlay, alpha-driven visibility.");
            }
            return true;
        }

        private static void AddCellAlpha(
            int x,
            int z,
            Map map,
            ColonistSightSystem sight,
            float ambientVeil,
            ref float sum,
            ref int count)
        {
            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(map)) return;
            sum += ComputeOverlayAlpha(sight.GetVisibility(cell), MaxOverlayAlpha, ambientVeil);
            count++;
        }

        private static void AddColor(LayerSubMesh subMesh, float alpha)
        {
            subMesh.colors.Add(CreateOverlayColor(alpha));
        }

        /// <summary>
        /// Builds the vertex color consumed by <see cref="MatBases.Darkness"/>.
        /// The Darkness material uses vertex color as the black-overlay
        /// multiplier: white RGB is neutral and therefore leaves the map
        /// unchanged, while black RGB actually darkens it. Alpha remains the
        /// sole visibility-derived channel.
        /// </summary>
        internal static Color32 CreateOverlayColor(float alpha)
        {
            byte alphaByte = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
            return new Color32(0, 0, 0, alphaByte);
        }

        /// <summary>Returns whether a generated overlay color is black RGB.
        /// Kept as a pure invariant for startup regression tests.</summary>
        internal static bool IsBlackOverlayColor(Color32 color)
        {
            return color.r == 0 && color.g == 0 && color.b == 0;
        }

        /// <summary>Converts visibility [0,1] into black overlay alpha.
        /// Curve: <c>pow(1-vis, 0.4)</c> with <paramref name="maxAlpha"/>
        /// scaling (default 1.0). The aggressive exponent makes the
        /// visibility-vs-alpha mapping steep so adjacent cells in the cone
        /// produce visibly distinct alpha bands (Abstufungen). The
        /// <paramref name="ambientVeilAlpha"/> parameter is an optional
        /// floor applied to highly-visible cells so ambient lighting never
        /// reads as fully bright. ConditionalVeil (2026-08-05): callers
        /// gate the veil on <see cref="ColonistSightSystem.HasActiveSight"/>
        /// so init / no-colonists branches stay at zero alpha.</summary>
        internal static float ComputeOverlayAlpha(float visibility, float maxAlpha, float ambientVeilAlpha)
        {
            visibility = Mathf.Clamp01(visibility);
            maxAlpha = Mathf.Clamp01(maxAlpha);
            float curve = Mathf.Pow(1f - visibility, AlphaCurveExponent) * maxAlpha;
            return Mathf.Max(curve, ambientVeilAlpha);
        }

        /// <summary>Pure mesh-buffer invariant used by startup regression tests.</summary>
        internal static bool ValidateMeshBuffers(
            int vertexCount,
            int colorCount,
            int triangleCount)
        {
            return ValidateMeshBuffers(vertexCount, colorCount, triangleCount, vertexCount - 1);
        }

        internal static bool ValidateMeshBuffers(
            int vertexCount,
            int colorCount,
            int triangleCount,
            int maxTriangleIndex)
        {
            return vertexCount >= 0
                && colorCount == vertexCount
                && triangleCount >= 0
                && triangleCount % 3 == 0
                && maxTriangleIndex >= -1
                && maxTriangleIndex < vertexCount;
        }
    }
}
