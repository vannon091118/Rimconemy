using System.Diagnostics;

namespace Rimconemy.SurvivalProgression.HarmonyPatches
{
    /// <summary>
    /// Translation hooks and seam markers for the Mining-Gate hook.
    /// Translation key "Rimconemy_MiningGate_Blocked" is owner-bound to Mod 02
    /// and must be present under
    /// mods/02-Rimconemy-Survival-Progression/Languages/English/Keyed/.
    /// </summary>
    public static class MiningHookPatch_Bootstrap
    {
        public const string MiningGateBlockedKey = "Rimconemy_MiningGate_Blocked";
    }
}
