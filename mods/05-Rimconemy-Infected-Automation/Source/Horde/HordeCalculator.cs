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
        /// <summary>Pulse-Cycle length in ticks. 120 = 2 in-game seconds
        /// at 60 ticks/sec, i.e. one slow breath. All three Render-Paths
        /// MUST use this constant so the visual stays in lock-step.</summary>
        public const int PulseCycleTicks = 120;

        /// <summary>Hybrid counter: Humanoid + 0.5 × Animal. Clamped at 0.
        /// Reads ledger fields only; no IO, deterministic from inputs.
        /// null ledger → 0 (horde inactive).</summary>
        public static int GetEffectiveCount(PopulationLedger ledger)
        {
            if (ledger == null) return 0;
            return Math.Max(0, ledger.HumanoidLiveCount) + Math.Max(0, ledger.AnimalLiveCount) / 2;
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

        /// <summary>Live gate shared by all three render paths. Reads the
        /// current ledger + active profile and answers whether the horde
        /// should be drawn right now.</summary>
        public static bool IsActiveNow()
        {
            var ledger = PopulationLedger.Get();
            if (ledger == null) return false;
            var profile = Story.StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;
            return IsActive(GetEffectiveCount(ledger), profile);
        }

        /// <summary>Pulse-Phase in 0..1, two-breath Sinusoid over
        /// <see cref="PulseCycleTicks"/> (one breath per half-cycle).
        /// Render-Paths multiply this by their per-layer alpha-max to
        /// get the current alpha. Pure API: same currentTick → same
        /// phase; tick=0 yields 0 (minimum alpha, no cold-start flash).
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
            int mod = (int)(currentTick % PulseCycleTicks);
            float angle = (float)mod / PulseCycleTicks * 2f * (float)System.Math.PI;
            return (float)System.Math.Abs(System.Math.Sin(angle));
        }
    }
}
