// Source/Population/PopulationProfileMultipliers.cs
//
// Owner: Infected & Automation (Package 05).
// Phase A — P6-PROGRESS §12 Daten-SSOT.
//
// Deterministic multiplier tables for the three Rimconemy SettingProfiles.
// Pure data, no RimWorld state, no random. Every public method is safe to
// call from a static constructor or a unit-test loop.
//
// If a caller passes an unknown profileId, the methods fall back to the
// Survival baseline and emit a single Log.Warning. The fallback itself is
// deterministic so save/load replay yields identical numbers.

using System.Collections.Generic;
using Verse;

namespace Rimconemy.InfectedAutomation.Population
{
    public static class PopulationProfileMultipliers
    {
        public const string ProfileRefuge = "Refuge";
        public const string ProfileSurvival = "Survival";
        public const string ProfileCollapse = "Collapse";
        private const string FallbackProfile = ProfileSurvival;

        public static readonly IReadOnlyList<string> SupportedProfiles =
            new[] { ProfileRefuge, ProfileSurvival, ProfileCollapse };

        // Cap grows by these factors each day when no kills happen.
        public static readonly IReadOnlyDictionary<string, float> DailyGrowth =
            new Dictionary<string, float>
            {
                { ProfileRefuge,   1.08f },
                { ProfileSurvival, 1.15f },
                { ProfileCollapse, 1.28f },
            };

        // Per-kill revenge quota (capped by free budget in PopulationLedger).
        public static readonly IReadOnlyDictionary<string, float> RevengeRatio =
            new Dictionary<string, float>
            {
                { ProfileRefuge,   0.4f },
                { ProfileSurvival, 0.7f },
                { ProfileCollapse, 0.9f },
            };

        // Population-Count-Threshold: Horde overlay activates (Phase D).
        // ⚠ PHASE-F NAMING ALERT: this is an *int count* (how many infected
        // exist). do NOT confuse with the float fraction HordeActivationThreshold
        // below (Phase F). Call-sites that need the count gate use
        // GetHordeThreshold(); call-sites that need the letter-firing fraction
        // gate use GetHordeActivationThreshold().
        public static readonly IReadOnlyDictionary<string, int> HordeThreshold =
            new Dictionary<string, int>
            {
                { ProfileRefuge,   220 },
                { ProfileSurvival, 150 },
                { ProfileCollapse, 80  },
            };

        // Max animal inoculations per in-game day (Phase C mechanic).
        public static readonly IReadOnlyDictionary<string, int> InoculationsPerDay =
            new Dictionary<string, int>
            {
                { ProfileRefuge,   0 },
                { ProfileSurvival, 1 },
                { ProfileCollapse, 3 },
            };

        // Minimum ticks between two consecutive animal inoculations.
        public static readonly IReadOnlyDictionary<string, long> InoculationMinIntervalTicks =
            new Dictionary<string, long>
            {
                { ProfileRefuge,   long.MaxValue / 2 },
                { ProfileSurvival, 60_000L * 7 },
                { ProfileCollapse, 60_000L * 3 },
            };

        // Phase E — AnimalInfection base chance per in-game day.
        // 0.05 = 5% per Tag (Survival); Collapse häufiger, Refuge selten.
        public static readonly IReadOnlyDictionary<string, double> AnimalInfectionBaseChance =
            new Dictionary<string, double>
            {
                { ProfileRefuge,   0.02 },
                { ProfileSurvival, 0.05 },
                { ProfileCollapse, 0.15 },
            };

        // Phase E — Multiplier auf BaseChance pro Horde-count über Threshold.
        // Refuge niedrig (0.5) damit das selten bleibt, Collapse hoch (1.5).
        public static readonly IReadOnlyDictionary<string, double> AnimalInfectionHordeScalingFactor =
            new Dictionary<string, double>
            {
                { ProfileRefuge,   0.5 },
                { ProfileSurvival, 1.0 },
                { ProfileCollapse, 1.5 },
            };

        // ────────────────────────────────────────────────────────────────────
        // Phase F — Wandering-Horde Profile-Drivers.
        // Per-Profile Multiplier, die das Wander-Verhalten + Manifest-Spawn
        // der Phase-F-Horde deterministisch aus dem aktiven Profile ableiten.
        //   - Capacity       = Anzahl der HiddenPawnStamps pro Profil
        //   - ActivationThr  = ThreatPressure-Schwelle ab der die Horde aktiv wird
        //   - LetterCooldown = Mindestabstand (Sim-Tage) zwischen zwei Horde-Letters
        //   - StagingTicks   = Dauer des "Staging"-FSM-States, bevor Attack feuert
        // Spec: docs/superpowers/specs/2026-08-05-horde-migration-design.md §4.4+§4.5.
        // ────────────────────────────────────────────────────────────────────
        public static readonly IReadOnlyDictionary<string, int> HordeCapacity =
            new Dictionary<string, int>
            {
                { ProfileRefuge,   50  },
                { ProfileSurvival, 100 },
                { ProfileCollapse, 200 },
            };

        public static readonly IReadOnlyDictionary<string, float> HordeActivationThreshold =
            new Dictionary<string, float>
            {
                { ProfileRefuge,   0.85f },
                { ProfileSurvival, 0.70f },
                { ProfileCollapse, 0.50f },
            };

        public static readonly IReadOnlyDictionary<string, float> HordeLetterCooldownDays =
            new Dictionary<string, float>
            {
                { ProfileRefuge,   30f },
                { ProfileSurvival, 14f },
                { ProfileCollapse, 5f  },
            };

        public static readonly IReadOnlyDictionary<string, int> HordeStagingDurationTicks =
            new Dictionary<string, int>
            {
                { ProfileRefuge,   250 * 5 },
                { ProfileSurvival, 250 * 3 },
                { ProfileCollapse, 250 * 2 },
            };

        /// <summary>
        /// Phase F — Reveal-Radius (Tile-distance). Pawns materialisieren
        /// wenn Home-Tile ≤ HordeRevealRadiusTiles; außerhalb: Cleanup.
        /// Spec §4.5.
        /// </summary>
        public const int HordeRevealRadiusTiles = 8;

        public static int GetHordeCapacity(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (HordeCapacity.TryGetValue(p, out int v)) return v;
            LogWarnFallback(p, "HordeCapacity");
            return HordeCapacity[FallbackProfile];
        }

        public static float GetHordeActivationThreshold(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (HordeActivationThreshold.TryGetValue(p, out float v)) return v;
            LogWarnFallback(p, "HordeActivationThreshold");
            return HordeActivationThreshold[FallbackProfile];
        }

        public static float GetHordeLetterCooldownDays(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (HordeLetterCooldownDays.TryGetValue(p, out float v)) return v;
            LogWarnFallback(p, "HordeLetterCooldownDays");
            return HordeLetterCooldownDays[FallbackProfile];
        }

        public static int GetHordeStagingDurationTicks(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (HordeStagingDurationTicks.TryGetValue(p, out int v)) return v;
            LogWarnFallback(p, "HordeStagingDurationTicks");
            return HordeStagingDurationTicks[FallbackProfile];
        }

        public static float GetDailyGrowth(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (DailyGrowth.TryGetValue(p, out float v)) return v;
            LogWarnFallback(p, "DailyGrowth");
            return DailyGrowth[FallbackProfile];
        }

        public static float GetRevengeRatio(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (RevengeRatio.TryGetValue(p, out float v)) return v;
            LogWarnFallback(p, "RevengeRatio");
            return RevengeRatio[FallbackProfile];
        }

        public static int GetHordeThreshold(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (HordeThreshold.TryGetValue(p, out int v)) return v;
            LogWarnFallback(p, "HordeThreshold");
            return HordeThreshold[FallbackProfile];
        }

        public static int GetInoculationsPerDay(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (InoculationsPerDay.TryGetValue(p, out int v)) return v;
            LogWarnFallback(p, "InoculationsPerDay");
            return InoculationsPerDay[FallbackProfile];
        }

        public static long GetInoculationMinInterval(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (InoculationMinIntervalTicks.TryGetValue(p, out long v)) return v;
            LogWarnFallback(p, "InoculationMinInterval");
            return InoculationMinIntervalTicks[FallbackProfile];
        }

        // Phase E — new getters for AnimalInfectionChance driver.
        public static double GetAnimalInfectionBaseChance(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (AnimalInfectionBaseChance.TryGetValue(p, out double v)) return v;
            LogWarnFallback(p, "AnimalInfectionBaseChance");
            return AnimalInfectionBaseChance[FallbackProfile];
        }

        public static double GetAnimalInfectionHordeScalingFactor(string profileId)
        {
            string p = profileId ?? FallbackProfile;
            if (AnimalInfectionHordeScalingFactor.TryGetValue(p, out double v)) return v;
            LogWarnFallback(p, "AnimalInfectionHordeScalingFactor");
            return AnimalInfectionHordeScalingFactor[FallbackProfile];
        }

        private static void LogWarnFallback(string profileId, string field)
        {
            Log.Warning("[Rimconemy.InfectedAutomation] PopulationProfileMultipliers: unknown profileId='"
                + profileId + "' for field " + field + "; falling back to Survival.");
        }
    }
}
