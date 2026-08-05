// Source/Horde/HordeCalculator.cs
//
// Phase D — Horde-Overlay Pure-Logic.
// Owner: Infected & Automation (Package 05).
//
// Static utility that turns Population-Live-Counts into a single
// effective horde-strength metric and answers "is the horde currently
// active for this profile?". Pulse-phase drive is also Pure so the
// three Render-Paths (SectionLayer, BurstLayer, CameraOverlay) stay in
// lock-step without sharing mutable state.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using System;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeCalculator
    {
        /// <summary>How many tiles worth of visual radius each animal
        /// contributes. Keep this consistent with Phase C's
        /// AnimalHalfCap so reader and Horde share the same ratio.</summary>
        public const float AnimalHalfCapFactor = 0.5f;

        /// <summary>Pulse-Cycle length in ticks. 120 = 2 in-game seconds
        /// at 60 ticks/sec, i.e. one slow breath. All three Render-Paths
        /// MUST use this constant so the visual stays in lock-step.</summary>
        public const int PulseCycleTicks = 120;

        /// <summary>Hybrid counter: Humanoid + 0.5 × Animal. Clamped at 0
        /// (negative inputs or over-cap deltas go to 0). Reads ledger
        /// fields only; no IO, deterministic from inputs.</summary>
        public static int GetEffectiveCount(PopulationLedger ledger)
        {
            if (ledger == null) return 0;
            int human = System.Math.Max(0, ledger.HumanoidLiveCount);
            int animal = System.Math.Max(0, ledger.AnimalLiveCount);
            return human + (int)System.Math.Floor((double)animal * AnimalHalfCapFactor);
        }

        /// <summary>True when Effective >= HordeThreshold(profileId).
        /// ProfileId fed through StripRimconemyPrefix so SettingProfile
        /// keys ("Rimconemy_Survival") map to PopulationProfileMultipliers
        /// keys ("Survival"). null profile → Survival fallback (threshold
        /// lookup goes through the same prefix-strip path, returning
        /// "Survival" → 150).</summary>
        public static bool IsActive(int effectiveCount, SettingProfile profile)
        {
            string key = Story.StoryDirector.StripRimconemyPrefix(profile?.ProfileId);
            int threshold = PopulationProfileMultipliers.GetHordeThreshold(key);
            return effectiveCount >= threshold;
        }

        /// <summary>Pulse-Phase in 0..1, two-breath Sinusoid over
        /// <see cref="PulseCycleTicks"/> (one breath per half-cycle).
        /// Render-Paths multiply this by their per-layer alpha-max to
        /// get the current alpha. Pure API: same currentTick → same
        /// phase. Returns 0 for non-positive tick (cold-start) so initial
        /// render is at minimum-alpha (no flash).
        ///
        /// D6 spec: pattern 0 → 1 → 0 → 1 → 0 over 120 ticks (two peaks).
        /// Implementation: <c>|sin(angle)|</c> with <c>angle = mod/120 · 2π</c>:
        /// tick=0  → |sin(0)|=0        — trough/start
        /// tick=30 → |sin(π/2)|=1      — peak 1
        /// tick=60 → |sin(π)|=0        — trough
        /// tick=90 → |sin(3π/2)|=1     — peak 2
        /// tick=120→ |sin(2π)|=0       — trough/end.</summary>
        public static float ComputePulsePhase(long currentTick)
        {
            if (currentTick <= 0) return 0f;
            int mod = (int)(currentTick % PulseCycleTicks);
            float angle = (float)mod / PulseCycleTicks * 2f * (float)System.Math.PI;
            return (float)System.Math.Abs(System.Math.Sin(angle));
        }
    }
}
