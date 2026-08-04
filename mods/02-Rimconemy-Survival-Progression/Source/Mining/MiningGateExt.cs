using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Mining
{
    /// <summary>
    /// DefModExtension attached to Vanilla compact-steel and CompactSlagSteel mineables.
    /// Purpose: phase-gated Mining >= 8 reader anchor (PHASE_PROGRESSION_CONTRACT.md §2).
    /// Final Policy: DefModExt is data only; the actual Skill check is done by
    /// `MiningHookPatch` (a minimal Reader: Designator_Mine.CanDesignateCell Postfix
    /// + Mineable.TrySpawnYield Prefix).
    /// No Transpiler. No persistent state. Live-burden by Skill/Patch-resolver.
    /// </summary>
    public class MiningGateExt : DefModExtension
    {
        /// <summary>
        /// Required minimum Mining Skill for the steel yield to occur.
        /// Default: 8.
        /// </summary>
        public int minMiningLevel = 8;

        /// <summary>
        /// Whether the patch should additionally require a Pawn at the mining
        /// site. False means the gate is enforced on every Cell-click regardless
        /// of pawn identity (useful when reading Defs statically).
        /// </summary>
        public bool requirePawn = true;
    }

    public static class MiningGateResolver
    {
        public const string DefOwnerPackage = "Rimconemy.SurvivalProgression";

        public static MiningGateExt GetExt(ThingDef def)
        {
            if (def == null) return null;
            return def.GetModExtension<MiningGateExt>();
        }

        public static bool IsBlockingMineable(ThingDef def)
        {
            return GetExt(def) != null;
        }

        /// <summary>
        /// Positive semantic: returns true if the gate is OPEN and mining is
        /// allowed. Returns false if mining should be blocked. Used by the
        /// MiningHookPatch classes.
        /// </summary>
        public static bool CanMine(ThingDef def, Pawn miner = null)
        {
            var ext = GetExt(def);
            if (ext == null) return true; // Vanilla mineables unaffected.

            if (ext.requirePawn)
            {
                if (miner == null) return false; // No pawn = no mineable mine.
                var skill = miner.skills?.GetSkill(SkillDefOf.Mining);
                if (skill == null) return false;
                if (skill.Level < ext.minMiningLevel) return false;
            }
            return true;
        }
    }
}
