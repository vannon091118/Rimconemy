// Source/Horde/HordeBurstLayer.cs
//
// Phase D — Per-Infected-Pawn Radial-Burst on the Home-Map.
// Iterates map.mapPawns.AllPawnsSpawned once per Regenerate. Filters by
// hidden-infected faction and adds a 5-Tile radius red glow per match.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeBurstLayer : SectionLayer
    {
        private const string HiddenInfectedFactionDef = "Rimconemy_HiddenInfectedFaction";

        public const float BurstRadius = 5f;
        public const float BurstAlphaMax = 0.5f;
        public const int Segments = 16;

        public HordeBurstLayer(Section section) : base(section)
        {
        }

        public override bool Visible
        {
            get
            {
                if (!base.Visible) return false;
                var ledger = PopulationLedger.Get();
                if (ledger == null) return false;
                int effective = HordeCalculator.GetEffectiveCount(ledger);
                var profile = Story.StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;
                return HordeCalculator.IsActive(effective, profile);
            }
        }

        public override void Regenerate()
        {
            try
            {
                ClearSubMeshes(MeshParts.All);
                if (section == null || section.map == null || section.map.mapPawns == null) return;

                long currentTick = Find.TickManager?.TicksGame ?? 0L;
                float phase = HordeCalculator.ComputePulsePhase(currentTick);
                float alpha = BurstAlphaMax * phase;

                // Section bounds — skip bursts outside this section (the section's
                // Regenerate is invoked per visible area, so we filter pawns on
                // section.memberRect membership).
                CellRect sectionRect = section.CellRect;

                foreach (var p in section.map.mapPawns.AllPawnsSpawned)
                {
                    if (p == null) continue;
                    if (p.Faction == null || p.Faction.def == null) continue;
                    if (p.Faction.def.defName != HiddenInfectedFactionDef) continue;
                    if (!sectionRect.Contains(p.Position)) continue;

                    Vector3 center = p.Position.ToVector3();
                    AddBurst(center, alpha);
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeBurstLayer.Regenerate: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void AddBurst(Vector3 center, float alpha)
        {
            LayerSubMesh subMesh = GetSubMesh(MatBases.Darkness);
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * BurstRadius, 0f, Mathf.Sin(angle) * BurstRadius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * BurstRadius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * BurstRadius);
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
