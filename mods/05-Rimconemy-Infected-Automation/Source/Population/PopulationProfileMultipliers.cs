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

        // Population at which the Horde overlay activates (Phase D).
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

        private static void LogWarnFallback(string profileId, string field)
        {
            Log.Warning("[Rimconemy.InfectedAutomation] PopulationProfileMultipliers: unknown profileId='"
                + profileId + "' for field " + field + "; falling back to Survival.");
        }
    }
}
