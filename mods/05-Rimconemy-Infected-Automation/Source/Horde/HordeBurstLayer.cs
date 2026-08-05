// Source/Horde/HordeBurstLayer.cs
//
// Phase D — Per-Infected-Pawn Radial-Burst on the Home-Map.
// Iterates map.mapPawns.AllPawnsSpawned once per Regenerate, filters by
// hidden-infected faction and adds a 5-Tile radius red glow per match.

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

        public override bool Visible => base.Visible && HordeCalculator.IsActiveNow();

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);
            if (section?.map == null || section.map.mapPawns == null) return;

            float phase = HordeCalculator.ComputePulsePhase(Find.TickManager?.TicksGame ?? 0L);
            float alpha = BurstAlphaMax * phase;

            // Only draw bursts inside this section (Regenerate runs per
            // visible section, so pawns elsewhere are filtered out).
            CellRect sectionRect = section.CellRect;

            foreach (var p in section.map.mapPawns.AllPawnsSpawned)
            {
                if (p.Faction?.def == null) continue;
                if (p.Faction.def.defName != HiddenInfectedFactionDef) continue;
                if (!sectionRect.Contains(p.Position)) continue;
                AddBurst(p.Position.ToVector3(), alpha);
            }
        }

        private void AddBurst(Vector3 center, float alpha)
        {
            LayerSubMesh subMesh = GetSubMesh(MatBases.Darkness);
            Color32 color = new Color32(220, 30, 30, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * BurstRadius, 0f, Mathf.Sin(angle) * BurstRadius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * BurstRadius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * BurstRadius);
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
