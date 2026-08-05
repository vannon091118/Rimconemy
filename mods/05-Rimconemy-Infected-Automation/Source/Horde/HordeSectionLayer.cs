// Source/Horde/HordeSectionLayer.cs
//
// Phase D — SectionLayer that draws a pulsing concentric red circle
// around the Home-Map center. Red RGB + alpha-driven pulse, full-section
// submesh per Regenerate.

using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeSectionLayer : SectionLayer
    {
        // Three ring radii (cells) — small inner, medium mid, large outer.
        private const float InnerRadius = 4f;
        private const float MidRadius = 10f;
        private const float OuterRadius = 18f;

        // Pulse-alpha ceilings for the three rings.
        private const float InnerAlphaMax = 0.55f;
        private const float MidAlphaMax = 0.35f;
        private const float OuterAlphaMax = 0.15f;

        public HordeSectionLayer(Section section) : base(section)
        {
        }

        public override bool Visible => base.Visible && HordeCalculator.IsActiveNow();

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);
            Map map = section.map;
            if (map == null) return;

            // Spec: "pulsierender Kreis um die Home-Map-Mitte" — one circle
            // around the map center. Vertices are world-space, so the single
            // section containing the map center draws the whole ring; letting
            // every section within reach draw it would stack ~9 copies of the
            // same geometry (z-fighting + 9× triangles).
            if (!section.CellRect.Contains(map.Center)) return;

            Vector3 center = map.Center.ToVector3();
            float phase = HordeCalculator.ComputePulsePhase(Find.TickManager?.TicksGame ?? 0L);

            AddRadialRing(center, InnerRadius, InnerAlphaMax, phase);
            AddRadialRing(center, MidRadius, MidAlphaMax, phase);
            AddRadialRing(center, OuterRadius, OuterAlphaMax, phase);
        }

        private void AddRadialRing(Vector3 center, float radius, float alphaMax, float phase)
        {
            float alpha = alphaMax * phase;
            const int Segments = 32;

            LayerSubMesh subMesh = GetSubMesh(MatBases.Darkness);
            Color32 color = new Color32(220, 30, 30, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * radius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * radius);
                subMesh.verts.Add(a);
                subMesh.verts.Add(b);
                subMesh.verts.Add(center);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
            }
        }
    }
}
