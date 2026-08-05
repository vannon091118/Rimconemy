// Source/Horde/HordeSectionLayer.cs
//
// Phase D — SectionLayer that draws a pulsing concentric red circle
// around the Home-Map center. Reuses the Visibility-clean pattern from
// DarknessSectionLayerLifecycle: red RGB + alpha-driven pulse, no
// per-cell mesh regeneration, full-section submesh per Regenerate.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
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

        public override bool Visible
        {
            get
            {
                if (!base.Visible) return false;
                return HordeCalculator.IsActive(
                    HordeCalculator.GetEffectiveCount(PopulationLedger.Get()),
                    StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival);
            }
        }

        public override void Regenerate()
        {
            try
            {
                ClearSubMeshes(MeshParts.All);
                long currentTick = Find.TickManager?.TicksGame ?? 0L;
                float phase = HordeCalculator.ComputePulsePhase(currentTick);

                Vector3 center = new Vector3(
                    section.botLeft.x + 17f, 0f, section.botLeft.z + 17f);

                AddRadialRing(center, InnerRadius, InnerAlphaMax, phase);
                AddRadialRing(center, MidRadius, MidAlphaMax, phase);
                AddRadialRing(center, OuterRadius, OuterAlphaMax, phase);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeSectionLayer.Regenerate: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void AddRadialRing(Vector3 center, float radius, float alphaMax, float phase)
        {
            float alpha = alphaMax * phase * 0.85f; // 0..α_max, multiplied by phase for breathing.
            const int Segments = 32;

            // Pattern-Reference: DarknessSectionLayerLifecycle uses
            // MatBases.Darkness for an alpha-overlay sub-mesh. The same
            // material supports red RGB via per-vertex colour; we layer
            // three concentric translucent red triangles per Ring.
            LayerSubMesh subMesh = GetSubMesh(MatBases.Darkness);
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * radius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * radius);
                subMesh.verts.Add(a);
                subMesh.verts.Add(b);
                subMesh.verts.Add(center);
                Color32 color = new Color32(220, 30, 30, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
            }
        }
    }
}
