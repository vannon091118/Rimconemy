using HarmonyLib;
using Rimconemy.SurvivalProgression.Progression;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression.Hooks
{
    /// <summary>
    /// Phase 8.3 — Harmony Postfix on
    /// <c>RimWorld.Frame.CompleteConstruction(Pawn worker)</c>. Spike verified
    /// 2026-08-04 against the local 1.6.4566 Assembly-CSharp.dll
    /// (Cecil-Sweep found exactly one public instance method with that
    /// name). See <c>tools/inspect/phase-8.3-construction-hooks.raw.md</c>
    /// for the sweep origin.
    ///
    /// The postfix calls
    /// <see cref="BuildingCompletionBridge.Submit(DomainXpState, ThingDef, Map, Frame, Pawn, long, float)"/>
    /// with the def produced by <c>__instance.BuildDef</c> and the map
    /// owned by the surviving <see cref="Frame"/>.
    ///
    /// Critical: This Postfix runs *after* the Frame has spawned the
    /// resulting solid <see cref="Building"/> via
    /// <see cref="Frame.MakeSolidThing"/> internally. By harvest-time
    /// the Frame may be Destroy()-ed; we therefore resolve the
    /// identity through <see cref="Frame.BuildDef"/> (string-stable for
    /// this construction-specific frame) rather than through the spawned
    /// building.
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Frame), nameof(RimWorld.Frame.CompleteConstruction))]
    public static class FrameCompletionPatch
    {
        static FrameCompletionPatch()
        {
            // Intentionally empty. Static constructors in Harmony patches are
            // reserved for shared resources (logging channels, throttling
            // counters, etc.). Today the class is stateless.
        }

        [HarmonyPostfix]
        public static void NotifyCompletion(Pawn worker, Frame __instance)
        {
            // Defensive guards for early-startup / god-mode / etc.
            if (__instance == null) return;
            if (Current.Game == null) return;
            if (__instance.Map == null) return;

            ThingDef def = __instance.BuildDef as ThingDef;
            if (def == null) return;

            var component = Current.Game.GetComponent<ProgressionGameComponent>();
            if (component == null) return;
            DomainXpState xpState = component.EnsureDomainXp();
            if (xpState == null) return;

            long tick = Find.TickManager?.TicksGame ?? 0L;

            BuildingCompletionBridge.Submit(
                xpState,
                def,
                __instance.Map,
                __instance,
                worker,
                tick);
        }
    }
}
