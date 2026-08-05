using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// ScenPart that spawns starting wood piles (WoodLog stacks) near the
    /// player's starting location on map generation.
    ///
    /// Provides basic starting resource so the survivor can build/craft
    /// before they have access to tree cutting (which requires the axe).
    ///
    /// Hook: ScenPart.PostMapGenerate(Map)
    /// </summary>
    public class ScenPart_StartingWoodPiles : ScenPart
    {
        public const string DefName_WoodLog = "WoodLog";

        // Number of wood log stacks to spawn
        private const int WoodStackCount = 3;

        // Amount per stack
        private const int WoodPerStack = 50;

        // Search radius around map center / player start
        private const int SpawnRadius = 15;

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);

            try
            {
                if (map == null) return;

                var woodDef = ThingDef.Named(DefName_WoodLog);
                if (woodDef == null)
                {
                    Log.Warning($"[Rimconemy.InfectedAutomation] ScenPart_StartingWoodPiles: WoodLog def not found.");
                    return;
                }

                // Find a good spawn area near map center (where player typically starts)
                IntVec3 spawnCenter = map.Center;
                
                // Try to find player's starting spot if available
                var startSpot = FindStartSpot(map);
                if (startSpot.IsValid)
                    spawnCenter = startSpot;

                int spawned = 0;
                for (int i = 0; i < WoodStackCount; i++)
                {
                    // Find a valid cell near the center
                    IntVec3 cell;
                    if (!CellFinder.TryFindRandomCellNear(spawnCenter, map, SpawnRadius, c => CanSpawnWoodAt(c, map), out cell))
                    {
                        // Fallback: just use a random walkable cell near center
                        if (!CellFinder.TryFindRandomCellNear(spawnCenter, map, SpawnRadius, c => c.Walkable(map), out cell))
                            continue;
                    }

                    // Create the wood stack
                    Thing woodStack = ThingMaker.MakeThing(woodDef);
                    woodStack.stackCount = WoodPerStack;

                    try
                    {
                        GenSpawn.Spawn(woodStack, cell, map);
                        spawned++;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[Rimconemy.InfectedAutomation] Failed to spawn wood stack: {ex.Message}");
                    }
                }

                if (spawned > 0)
                {
                    Log.Message($"[Rimconemy.InfectedAutomation] ScenPart_StartingWoodPiles: spawned {spawned} wood pile(s) ({WoodPerStack} each) on map {map.uniqueID}.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rimconemy.InfectedAutomation] ScenPart_StartingWoodPiles.PostMapGenerate: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Tries to find the player's starting location from the map's
        /// starting pawns or the map center as fallback.
        /// </summary>
        private static IntVec3 FindStartSpot(Map map)
        {
            // Check for any colonist pawns already spawned
            if (map.mapPawns?.FreeColonists != null)
            {
                foreach (var colonist in map.mapPawns.FreeColonists)
                {
                    if (colonist != null && !colonist.Dead && colonist.Spawned)
                        return colonist.Position;
                }
            }

            // Check for starting spot from map's Parent (settlement)
            if (map.Parent != null && map.Parent is MapParent)
            {
                // Map center is usually the start
                return map.Center;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Checks if a cell is valid for spawning wood (walkable, not roofed, not too close to other things).
        /// </summary>
        private static bool CanSpawnWoodAt(IntVec3 c, Map map)
        {
            if (!c.Walkable(map)) return false;
            if (c.Roofed(map)) return false;
            
            // Don't spawn on top of other things
            var things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t.def.category == ThingCategory.Item || t.def.category == ThingCategory.Building)
                    return false;
            }
            return true;
        }
    }
}
