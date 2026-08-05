// Source/Inoculation/AnimalInfectionChance.cs
//
// Phase E — Tiersym-Infektion via Random Encounter: Pure-Logic.
// Owner: Infected & Automation (Package 05).
//
// Pure-Logic, kein IO, kein Verse-Worldstate. Aufgerufen von
// StoryDirector.TryFireProfileInfection (Day-Tick); regression-test-bar
// ohne Game-Context.
//
// Determinismus: Tages-Bucket + HordeCount|10 + ProfileId
// → FNV-1a-32bit-Hash → deterministisches Outcome pro Tag × Horde.
// Profile-Routing via PopulationProfileMultipliers (InoculationsPerDay
// als Tages-Cap; die neuen AnimalInfectionBaseChance +
// AnimalInfectionHordeScalingFactor-Getter werden in
// Phase E T2-Schritt ergänzt).

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    public static class AnimalInfectionChance
    {
        /// <summary>Hard-Cap damit nicht daily 100% Brand-Trigger. 0.95
        /// läßt noch 5% Varianz für Save-Edge-Cases.</summary>
        public const double HardCap = 0.95;

        /// <summary>
        /// Horde-Skalierung pro Tag. Multiplier auf BaseChance, wenn
        /// die Horde oberhalb des Profile-Schwellenwerts liegt.
        /// Chance = BaseChance × (1 + Scale × (hordeCount − threshold) / threshold)
        /// </summary>
        public static double ComputeChancePerDay(
            long tickDayBucket, int hordeCount, SettingProfile profile)
        {
            string key = Story.StoryDirector.StripRimconemyPrefix(profile?.ProfileId);
            double baseChance = PopulationProfileMultipliers.GetAnimalInfectionBaseChance(key);
            double scalingFactor = PopulationProfileMultipliers.GetAnimalInfectionHordeScalingFactor(key);
            int threshold = PopulationProfileMultipliers.GetHordeThreshold(key);
            // Phase E T2 — Linear scale from 0 (not from threshold). Tests:
            // T2 Survival horde=100  → 0.05*(1+1.0*100/150) == 0.0833
            // T3 Survival horde=200  → 0.05*(1+1.0*200/150) == 0.1167
            // T4 Collapse horde=50   → 0.15*(1+1.5*50/80)  == 0.2906
            // T6 (Collapse horde=0 / -10) must NOT decay below baseChance:
            // defensive clamp keeps negative horde math bounded at base.
            double ratio = threshold > 0
                ? System.Math.Max(0.0, (double)hordeCount / threshold)
                : 0.0;
            return System.Math.Min(HardCap, baseChance * (1.0 + scalingFactor * ratio));
        }

        /// <summary>
        /// Determines if today should fire: Profile-base+scale Würfel
        /// UND Per-Tag-Quota noch nicht überschritten.
        /// HordeCount wird mit FloorDiv10 quantisiert damit kleine
        /// Schwankungen (≤9) den Würfel nicht beeinflussen.
        /// </summary>
        public static bool ShouldFireToday(
            long currentTick, int todayCount, int hordeCount, SettingProfile profile)
        {
            if (profile == null) return false;
            long dayBucket = currentTick / 60000L;
            if (dayBucket < 1L) return false;
            string key = Story.StoryDirector.StripRimconemyPrefix(profile.ProfileId);
            int cap = PopulationProfileMultipliers.GetInoculationsPerDay(key);
            if (todayCount >= cap) return false;

            double chance = ComputeChancePerDay(dayBucket, hordeCount, profile);
            uint hash = FnvHash($"{dayBucket}|{key}|{hordeCount / 10}|fire");
            double roll = (hash % 10000U) / 10000.0;
            return roll < chance;
        }

        /// <summary>
        /// Anzahl von Tieren die heute infiziert werden dürfen.
        /// Deterministisch über (dayBucket|profile|horde|-count).
        /// Range: 0..InoculationsPerDay des Profiles.
        /// </summary>
        public static int ComputeInfectionCount(
            long tickDayBucket, int hordeCount, SettingProfile profile)
        {
            if (profile == null) return 0;
            string key = Story.StoryDirector.StripRimconemyPrefix(profile.ProfileId);
            int cap = PopulationProfileMultipliers.GetInoculationsPerDay(key);
            if (cap <= 0) return 0;

            uint hash = FnvHash($"cnt|{tickDayBucket}|{key}|{hordeCount / 10}");
            int rollBucket = (int)(hash % 1024U); // 0..1023
            double pct = rollBucket / 1024.0;
            // Floor gibt eine 0..cap Verteilung. Häufig 0,
            // gelegentlich cap. PopulationProfileMultipliers-InoculationsPerDay
            // ist gleichzeitig der Hardcap.
            return System.Math.Min(cap, (int)System.Math.Floor(pct * (cap + 1)));
        }

        // FNV-1a 32-bit (deterministisch same-as-other-callers).
        private static uint FnvHash(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in s)
                {
                    h ^= c;
                    h *= 16777619u;
                }
                return h;
            }
        }
    }
}
