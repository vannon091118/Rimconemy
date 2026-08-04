using HarmonyLib;
using RimWorld;
using Verse;
using Rimconemy.InfectedAutomation.Ideology;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Setting Rule: CollectiveDefense (H3 §2).
    ///
    /// Hooks Pawn.PostApplyDamage so every damage the pawn takes or
    /// deals can be observed. The participating pawn (the one being
    /// damaged or the initiator) is registered in
    /// <see cref="CollectiveDefenseTracker"/>.
    ///
    /// We hook PostApplyDamage because it has the most stable signature
    /// in RimWorld 1.6 (DamageInfo + totalDamageDealt). BattleLog
    /// internals changed signatures across 1.5-x and 1.6, so we keep
    /// this patch on a stable API surface.
    ///
    /// Aggregate step runs once per 600 ticks (10 seconds) from a
    /// postfix on GameComponent.GameComponentTick on the tracker
    /// instance. The aggregate path detects participants vs. shirkers
    /// and applies thoughts.
    ///
    /// Specification: docs/H3-ideology-influence-matrix.md §2.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Pawn_PostApplyDamage_CollectiveDefense
    {
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
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] Pawn_PostApplyDamage_CollectiveDefense postfix failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Drives the Aggregate step from the tracker's GameComponentTick.
    /// Runs every 600 ticks (~10 in-game seconds). When at least one
    /// colonist appeared in combat during the prior window the
    /// tracker computes shirkers/defenders and applies the relevant
    /// thoughts.
    /// </summary>
    [HarmonyPatch(typeof(GameComponent), nameof(GameComponent.GameComponentTick))]
    public static class GameComponentTick_CollectiveDefense
    {
        private const int AggregateIntervalTicks = 600;
        private static int _tickCounter;

        [HarmonyPostfix]
        [HarmonyPriority(5000)]
        public static void Postfix(GameComponent __instance)
        {
            try
            {
                if (__instance is not CollectiveDefenseTracker tracker) return;
                _tickCounter++;
                if (_tickCounter < AggregateIntervalTicks) return;
                _tickCounter = 0;
                if (Current.Game == null) return;

                var participants = new HashSet<int>();
                var shirkers = new HashSet<int>();
                tracker.ComputeAndApply(participants, shirkers);
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] CollectiveDefense aggregate failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
