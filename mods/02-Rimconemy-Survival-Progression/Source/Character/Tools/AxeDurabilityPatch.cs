using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Tools
{
    /// <summary>
    /// Harmony patch on Verb_MeleeAttack.TryCastNextBurstShot.
    /// Degrades axe durability on each successful melee hit.
    /// When durability reaches zero, the weapon is destroyed.
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastNextBurstShot")]
    public static class AxeDurabilityPatch
    {
        [HarmonyPostfix]
        public static void DegradeAxeOnHit(ref bool __result, Verb __instance)
        {
            // Only degrade on successful hits
            if (!__result) return;

            var weapon = __instance.EquipmentSource;
            if (weapon == null) return;

            var comp = weapon.TryGetComp<CompAxeDurability>();
            if (comp == null) return;

            comp.UseOnce();

            // Weapon breaks when uses reach zero
            if (comp.RemainingUses <= 0)
            {
                // DestroyMode.Vanish removes without dropping items or corpse
                weapon.Destroy(DestroyMode.Vanish);
            }
        }
    }
}