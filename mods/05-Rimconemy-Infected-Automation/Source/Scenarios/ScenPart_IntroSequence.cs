using System;
using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.UI;
using RimWorld;
using Verse;
using Rimconemy.InfectedAutomation.Story;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// ScenPart that starts the Cinematic Intro Sequence after map generation.
    /// Creates camera cut positions and opens IntroFlowWindow.
    /// Wires the horde-flash callbacks so the Foundation intro can spawn a
    /// brief visual-only infected flash without any Foundation→05 reference.
    /// Owner: Infected & Automation (Paket 05).
    /// </summary>
    public class ScenPart_IntroSequence : ScenPart
    {
        private bool introShown;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref introShown, "introShown", false);
        }

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);

            if (map == null || !map.IsPlayerHome || introShown) return;
            introShown = true;

            // Kamera-Cut Positionen generieren
            var cuts = GenerateCameraCuts(map);

            // IntroFlowWindow erstellen und anzeigen — Horde-Flash via
            // Callbacks, damit Foundation keine Paket-05-Referenz braucht.
            var window = new IntroFlowWindow
            {
                cameraCutPositions = cuts,
                OnClosed = () => TutorialDirector.Get()?.StartGuide(),
                SpawnHordePawn = SpawnIntroHordePawn,
                DespawnPawn = DespawnIntroPawn
            };
            Find.WindowStack.Add(window);
        }

        /// <summary>
        /// Spawns one visual-only infected pawn for the intro horde flash.
        /// Visual-only: no duty, despawned by the intro window after the flash.
        /// </summary>
        private static Pawn SpawnIntroHordePawn(string defName, Faction faction, PawnKindDef kindDef, IntVec3 cell, Map map)
        {
            try
            {
                if (map == null || !cell.InBounds(map)) return null;

                var resolvedFaction = faction ?? InfectedFactionUtility.EnsureHiddenInfectedFaction();
                var resolvedKind = kindDef ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Rimconemy_InfectedRavager");
                if (resolvedFaction == null || resolvedKind == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] IntroFlowWindow: horde flash skipped because faction or pawn kind is missing.");
                    return null;
                }

                var pawn = PawnGenerator.GeneratePawn(resolvedKind, resolvedFaction);
                if (pawn == null) return null;
                pawn.mindState.duty = null;
                GenSpawn.Spawn(pawn, cell, map);
                return pawn;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] IntroFlowWindow horde spawn failed: " + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        private static void DespawnIntroPawn(Pawn pawn)
        {
            if (pawn != null && pawn.Spawned && !pawn.Destroyed)
                pawn.Destroy(DestroyMode.Vanish);
        }

        private List<IntVec3> GenerateCameraCuts(Map map)
        {
            int maxX = System.Math.Max(0, map.Size.x - 11);
            int maxZ = System.Math.Max(0, map.Size.z - 11);
            int minX = System.Math.Min(10, maxX);
            int minZ = System.Math.Min(10, maxZ);

            return new List<IntVec3>
            {
                map.Center,
                new IntVec3(minX, 0, map.Size.z / 2),
                new IntVec3(maxX, 0, map.Size.z / 2),
                new IntVec3(map.Size.x / 2, 0, minZ),
                new IntVec3(map.Size.x / 2, 0, maxZ)
            }.Distinct().ToList();
        }
    }
}
