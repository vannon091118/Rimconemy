using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Phase
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02).
    /// Phase-Contract Gate — load-time DefModExtension that signals to
    /// downstream Rimconemy patches (Bauschutt_Remap_Patches, etc.) that
    /// the early-phase path of the PHASE_PROGRESSION_CONTRACT is active.
    ///
    /// Architectural note (PHASE_PROGRESSION_CONTRACT §11):
    /// XML Patch Operation runs at Def load-time; there is no per-frame or
    /// per-map runtime gate. Activation is therefore expressed as a flag
    /// on the relevant Def, attached via PatchOperationAdd. Once the
    /// extension is present, dependent patches can use PatchOperationTest
    /// (engine-native, auto-idempotent) to gate their own additions.
    ///
    /// This is the load-time equivalent of <see cref="RimconemyStartState"/>
    /// event flags: a marker that says "the early-phase contract is engaged
    /// for this def". Honour it through presence rather than absence.
    /// </summary>
    public sealed class PhaseContractGate : DefModExtension
    {
        /// <summary>True when early-phase-via-Phase-Contract is in effect.</summary>
        public bool earlyPhaseActive = true;
    }

    /// <summary>
    /// Owner: Survival &amp; Progression. Pure-static resolver, mirrors the
    /// <see cref="MiningGateResolver"/> idiom. Returns positive semantic
    /// per PHASE_PROGRESSION_CONTRACT rules: <c>true</c> = contract active,
    /// <c>false</c> = contract inactive.
    ///
    /// Status (Code-Review 2026-08-05): WIRING-PENDING. The current
    /// XML-Patch chain uses raw <c>MayRequire="rimconemy.survivalprogression"</c>
    /// as the actual gate; this Resolver exists as the future-proofing
    /// hook for a Harmony Postfix on <see cref="ThingDef.CanBuildNow()"/> or
    /// similar runtime gate. Do NOT add a random caller until the
    /// post-fix is wired through.
    /// </summary>
    public static class PhaseContractGateResolver
    {
        public const string DefOwnerPackage = "Rimconemy.SurvivalProgression";

        public static PhaseContractGate GetExt(ThingDef def)
        {
            if (def == null) return null;
            return def.GetModExtension<PhaseContractGate>();
        }

        public static bool IsGatePresent(ThingDef def)
        {
            return GetExt(def) != null;
        }

        public static bool CanBuildForEarlyPhase(ThingDef def)
        {
            var ext = GetExt(def);
            // Defensive: a missing extension is a no-op (other mods may have
            // removed it). Only an explicit earlyPhaseActive=true flips the
            // gates true.
            return ext != null && ext.earlyPhaseActive;
        }
    }
}
