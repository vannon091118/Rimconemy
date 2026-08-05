using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Cooking
{
    /// <summary>
    /// Keeps the low-Cooking role trait consistent for wanderers, refugees,
    /// recruits and other pawns generated after game start. The starting-pawn
    /// path still calls ApplyStartingRoleTrait directly because it is applied
    /// before normal pawn generation completes.
    /// </summary>
    [HarmonyPatch]
    public static class PawnGenerationCookingTraitPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase target = typeof(PawnGenerator).GetMethod(
                "GeneratePawn",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(PawnGenerationRequest) },
                modifiers: null);
            if (target != null) yield return target;
        }

        [HarmonyPostfix]
        public static void ApplyTrait(Pawn __result)
        {
            if (__result == null) return;
            CookingOutcomeResolver.ApplyStartingRoleTrait(__result);
        }
    }
}
