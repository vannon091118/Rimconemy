using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.HarmonyPatches
{
    /// <summary>
    /// Tree Cutting Gate (Phase Survival Progression).
    ///
    /// Blocks Designator_PlantsCut.CanDesignateCell until the player has
    /// at least one Rimconemy_Axe (Schrott-Axt) in their colony's inventory
    /// or equipped by a colonist.
    ///
    /// This enforces the progression gate: trees cannot be cut for wood
    /// until the survivor crafts their first steel+wood axe.
    ///
    /// Vanilla API: Designator_PlantsCut.CanDesignateCell(IntVec3 c) -> AcceptanceReport
    /// </summary>
    [HarmonyPatch(typeof(Designator_PlantsCut), nameof(Designator_PlantsCut.CanDesignateCell))]
    public static class Designator_PlantsCut_TreeCuttingGate_Patch
    {
        private const string AxeDefName = "Rimconemy_Axe";

        public static void Postfix(IntVec3 c, ref AcceptanceReport __result)
        {
            try
            {
                // If vanilla already rejected, keep rejection
                if (!__result.Accepted) return;

                var map = Find.CurrentMap;
                if (map == null) return;

                // Check if the target is actually a tree/plant that can be cut
                var things = c.GetThingList(map);
                if (things == null) return;

                bool hasCuttablePlant = false;
                for (int i = 0; i < things.Count; i++)
                {
                    var t = things[i];
                    if (t is Plant plant && plant.def.plant.IsTree)
                    {
                        hasCuttablePlant = true;
                        break;
                    }
                }

                // Only block if trying to cut a tree
                if (!hasCuttablePlant) return;

                // Check if colony has any Rimconemy_Axe
                if (!ColonyHasAxe(map))
                {
                    // Block with translation key
                    __result = new AcceptanceReport("Rimconemy.TreeCutting.RequiresAxe".Translate());
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Rimconemy.SurvivalProgression] Designator_PlantsCut_TreeCuttingGate_Patch: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the colony has at least one Rimconemy_Axe anywhere:
        /// - Equipped by a colonist
        /// - In inventory of a colonist
        /// - In any storage/stack on the map
        /// </summary>
        private static bool ColonyHasAxe(Map map)
        {
            // Check colonists' equipment and inventory
            if (map.mapPawns?.FreeColonists != null)
            {
                foreach (var colonist in map.mapPawns.FreeColonists)
                {
                    if (colonist == null || colonist.Dead) continue;

                    // Check equipped weapon
                    if (colonist.equipment?.Primary?.def?.defName == AxeDefName)
                        return true;

                    // Check inventory
                    if (colonist.inventory?.innerContainer != null)
                    {
                        for (int i = 0; i < colonist.inventory.innerContainer.Count; i++)
                        {
                            var thing = colonist.inventory.innerContainer[i];
                            if (thing?.def?.defName == AxeDefName)
                                return true;
                        }
                    }

                    // Check apparel (unlikely but safe)
                    if (colonist.apparel?.WornApparel != null)
                    {
                        foreach (var app in colonist.apparel.WornApparel)
                        {
                            if (app?.def?.defName == AxeDefName)
                                return true;
                        }
                    }
                }
            }

            // Check all things on map (storage, ground, etc.)
            if (map.listerThings?.AllThings != null)
            {
                var allThings = map.listerThings.AllThings;
                for (int i = 0; i < allThings.Count; i++)
                {
                    var thing = allThings[i];
                    if (thing?.def?.defName == AxeDefName)
                        return true;
                }
            }

            return false;
        }
    }
}
