// Source/Inoculation/AnimalInfectionAiOverlay.cs
//
// Phase E — Visual-Marker: rotes "!" über initiierten Tieren auf der
// Home-Map. Owner: Infected & Automation (Package 05).
//
// Static-Pure statt MapComponent — die MapComponent-Zündung ist Aufgabe
// der HorizonOverlay (eine separate MapComponent-Erweiterung die wir in
// einem späteren Sprint mit der SectionLayer-Infrastruktur koppeln).
// Diese Klasse liefert die Predikat-Logik + die Texture-Lifecycle, die
// ein zukünftiger HordeOverlay-Hook liest.
//
// Camera-Overlay wird in einer späteren Phase E+ Erweiterung an die
// UIRoot_Overlay-Postfix-Pattern angeflanscht (analog zu Phase-D
// HordeCameraOverlay).

using Rimconemy.InfectedAutomation.Population;
using RimWorld;
using UnityEngine;
using Verse;

#pragma warning disable CS0618 // DestroyedOrNull is obsolete-API-aware (defensive fallback chain)

namespace Rimconemy.InfectedAutomation.Inoculation
{
    [StaticConstructorOnStartup]
    public static class AnimalInfectionAiOverlay
    {
        // Procedurally-built 8x8 red exclamation glyph used as the
        // "infected wildlife" marker. Recreated once at static-init; the
        // caller (CameraOverlay Hook) is responsible for Graphics.Blit or
        // GUI.DrawTexture invocation. We keep it in-memory so the texture
        // reference is stable across frames.
        private static Texture2D _markerTexture;

        public const float MarkerPixelSize = 16f;

        static AnimalInfectionAiOverlay()
        {
            // 8x8 solid red — the UI render-layer scales it as needed.
            _markerTexture = new Texture2D(8, 8, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            Color red = new Color(0.85f, 0.20f, 0.20f, 1f);
            var pixels = new Color[8 * 8];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = red;
            _markerTexture.SetPixels(pixels);
            _markerTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        }

        public static Texture2D MarkerTexture => _markerTexture;

        /// <summary>Filter: predicts whether the pawn should carry the
        /// red "!" marker. Phase E MVP co-locates the marker trigger with
        /// the KindDef gate — same condition as the in-game conversion
        /// race.</summary>
        public static bool ShouldShowInfectionMarker(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.kindDef == null) return false;
            if (pawn.kindDef.defName != InoculationConverter.BrandedKindDefName) return false;
            if (pawn.Map == null) return false;
            if (pawn.Dead || pawn.DestroyedOrNull()) return false;
            return true;
        }

        /// <summary>Loads-or-resolves an existing marker texture. Hot-Path
        /// callers (RenderHook) cache the result locally per-frame.</summary>
        public static Texture2D GetOrLoadMarkerTexture()
        {
            if (_markerTexture != null) return _markerTexture;
            // Defensive re-init if Static-Constructor failed silently
            // (e.g. headless test runner that never hits the Static Init).
            _markerTexture = new Texture2D(8, 8, TextureFormat.RGBA32, mipChain: false);
            Color red = new Color(0.85f, 0.20f, 0.20f, 1f);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = red;
            _markerTexture.SetPixels(pixels);
            _markerTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return _markerTexture;
        }
    }
}
