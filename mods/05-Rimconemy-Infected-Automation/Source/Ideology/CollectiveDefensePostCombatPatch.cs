// Source/Ideology/CollectiveDefensePostCombatPatch.cs
//
// Setting Rule: CollectiveDefense (H3 §2). Package 05 has no Harmony
// PatchAll (Bootstrap registers patches explicitly, cf.
// DarknessSectionLayerLifecycle / HordeCameraOverlay), so the postfix
// must be installed with an explicit harmony.Patch call — a bare
// [HarmonyPatch] attribute would be inert.
//
// We hook Pawn.PostApplyDamage because it has the most stable signature
// in RimWorld 1.6 (DamageInfo + totalDamageDealt). BattleLog internals
// changed signatures across 1.5-x and 1.6, so we keep this patch on a
// stable API surface.
//
// Aggregate step runs once per 600 ticks (~10 in-game seconds) from the
// tracker's own GameComponentTick override (no Harmony patch). The
// aggregate path detects participants vs. shirkers and applies thoughts.
//
// Specification: docs/H3-ideology-influence-matrix.md §2.

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    public static class Pawn_PostApplyDamage_CollectiveDefense
    {
        private const string HarmonyId = "rimconemy.infectedautomation.collective-defense";

        private static bool _installed;

        /// <summary>
        /// Installs the PostApplyDamage postfix once during Package 05
        /// bootstrap. Fail-closed: a missing hook logs and keeps the
        /// setting rule dormant instead of throwing at startup.
        /// </summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            try
            {
                var target = AccessTools.Method(typeof(Pawn), nameof(Pawn.PostApplyDamage));
                if (target == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] CollectiveDefensePostCombatPatch: Pawn.PostApplyDamage missing; setting rule dormant.");
                    return;
                }

                var harmony = new Harmony(HarmonyId);
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(Pawn_PostApplyDamage_CollectiveDefense), nameof(Postfix)));
                Log.Message("[Rimconemy.InfectedAutomation] CollectiveDefensePostCombatPatch: PostApplyDamage postfix installed.");
            }
            catch (Exception ex)
            {
                // Fail closed: a missing hook must not break combat.
                Log.Warning("[Rimconemy.InfectedAutomation] CollectiveDefensePostCombatPatch install failed; setting rule dormant: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // Postfix — runs after every damage application on a pawn.
        public static void Postfix(Pawn __instance, DamageInfo dinfo)
        {
            try
            {
                if (Current.Game == null) return;
                var tracker = Current.Game.GetComponent<CollectiveDefenseTracker>();
                if (tracker == null) return;
                if (__instance == null) return;

                // Mark the target pawn as "in combat" so even pawns that
                // take damage without shooting back are recognised as
                // combatants (defenders take hits too).
                if (__instance.IsColonistPlayerControlled)
                    tracker.RecordParticipation(__instance.thingIDNumber);

                // The initiator of the damage is also counted if a player
                // colonist caused the damage.
                if (dinfo.Instigator is Pawn instigator && instigator.IsColonistPlayerControlled)
                    tracker.RecordParticipation(instigator.thingIDNumber);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] Pawn_PostApplyDamage_CollectiveDefense postfix failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
