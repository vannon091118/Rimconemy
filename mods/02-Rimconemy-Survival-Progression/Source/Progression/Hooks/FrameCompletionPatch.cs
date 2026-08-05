using HarmonyLib;
using Rimconemy.SurvivalProgression.Character.Construction;
using Rimconemy.SurvivalProgression.Progression;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression.Hooks
{
    /// <summary>Vanilla 1.6 construction completion bridge.</summary>
    [HarmonyPatch(typeof(RimWorld.Frame), nameof(RimWorld.Frame.CompleteConstruction))]
    public static class FrameCompletionPatch
    {
        public sealed class CompletionState
        {
            public Map Map;
            public IntVec3 Position;
            public ThingDef Def;
        }

        [HarmonyPrefix]
        public static void CaptureCompletionState(Frame __instance, out CompletionState __state)
        {
            // The Frame is destroyed by CompleteConstruction; capture its
            // identity before the postfix runs so the finished building can
            // be found at the frame cell afterwards.
            __state = __instance == null ? null : new CompletionState
            {
                Map = __instance.Map,
                Position = __instance.Position,
                Def = __instance.BuildDef as ThingDef,
            };
        }

        [HarmonyPostfix]
        public static void NotifyCompletion(Pawn worker, Frame __instance, CompletionState __state)
        {
            if (__state == null || Current.Game == null) return;

            Map map = __state.Map;
            ThingDef def = __state.Def;
            if (map == null || def == null || !__state.Position.InBounds(map)) return;

            // CompleteConstruction has already spawned the solid building. Find
            // the exact result at the frame cell rather than retaining the
            // destroyed frame as the source of HP state.
            Building finished = FindFinishedBuilding(map, __state.Position, def);
            if (finished != null)
                BuilderDurability.ApplyToBuilding(finished, worker);

            var component = Current.Game.GetComponent<ProgressionGameComponent>();
            if (component == null) return;
            DomainXpState xpState = component.EnsureDomainXp();
            if (xpState == null) return;

            long tick = Find.TickManager?.TicksGame ?? 0L;
            BuildingCompletionBridge.Submit(xpState, def, map, __instance, worker, tick);
        }

        private static Building FindFinishedBuilding(Map map, IntVec3 position, ThingDef def)
        {
            var things = map.thingGrid.ThingsListAtFast(position);
            if (things == null) return null;
            for (int i = 0; i < things.Count; i++)
            {
                Building building = things[i] as Building;
                if (building != null && building.def == def) return building;
            }
            return null;
        }
    }
}
