using HarmonyLib;
using Rimconemy.SurvivalProgression.Mining;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.HarmonyPatches
{
    /// <summary>
    /// Minimal Mining-Gate Reader (PHASE_PROGRESSION_CONTRACT.md §2 + §6).
    ///
    /// Vanilla API surface picked from the local 1.6.4566 strings-dump
    /// (2026-08-05): `Designator_Mine.CanDesignateCell(IntVec3 c) -> AcceptanceReport`
    /// — cell-targeting designator; confirmed Mineable class exists.
    ///
    /// Reader hook kept conservative after build evidence:
    ///   - UI/Click: Designator_Mine.CanDesignateCell Postfix  (verified-compiling)
    ///
    /// AI-side auto-mine remains Vanilla until the next Per-method instrumented
    /// postfix lands. Currently documented as OPEN in PHASE_PROGRESSION_CONTRACT §7.
    /// </summary>

    [HarmonyPatch(typeof(Designator_Mine), nameof(Designator_Mine.CanDesignateCell))]
    public static class Designator_Mine_MiningGate_Patch
    {
        // Vanilla signature: CanDesignateCell(IntVec3 c) -> AcceptanceReport.
        public static void Postfix(IntVec3 c, ref AcceptanceReport __result)
        {
            try
            {
                if (!__result.Accepted) return;
                var map = Find.CurrentMap;
                if (map == null) return;

                // GetThingList returns everything at the cell (pawn + Corpse + Mineable).
                // Filter to Mineable only via explicit foreach (avoids System.Linq).
                Mineable mine = null;
                var things = c.GetThingList(map);
                if (things != null)
                {
                    for (int i = 0; i < things.Count; i++)
                    {
                        var t = things[i];
                        if (t is Mineable m)
                        {
                            mine = m;
                            break;
                        }
                    }
                }
                if (mine == null || mine.def == null) return;
                if (!MiningGateResolver.IsBlockingMineable(mine.def)) return;

                // AcceptanceReport construction in 1.6: only the implicit
                // operator from string is available. Vanilla's direct 2-arg
                // constructor is not exposed. Use the operator path.
                var ext = MiningGateResolver.GetExt(mine.def);
                var reasonText = "Rimconemy_MiningGate_Blocked".Translate(ext.minMiningLevel);
                __result = new AcceptanceReport(reasonText);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Rimconemy.SurvivalProgression] Designator_Mine_MiningGate_Patch: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
