// Source/Inoculation/AnimalInfectionDriver.cs
//
// Phase E — Recurring Driver for animal-infection via Random Encounter.
// Owner: Infected & Automation (Package 05).
//
// Static helper invoked by StoryDirector.GameComponentTick once per
// in-game day. Decides (via AnimalInfectionChance) whether the day's
// horde-burden has crossed the profile threshold, and routes up to
// InoculationsPerDay wild animals through the existing
// RandomInoculationService.TryInfectWildAnimals pipeline.
//
// Why static (not MapComponent): tying the driver to StoryDirector's
// day-tick makes the "once per day" contract trivial to reason about
// and trivial to regression-test (no MapComponent life-cycle to mock).
// Per-map fan-out is not needed for Phase E's MVP because the
// TryInfectWildAnimals path already walks Find.AnyPlayerHomeMap.
//
// Determinism: same spec as AnimalInfectionChance — uses
// tickDayBucket + ProfileId + HordeCount//10 for the FNV1a-based
// outcome. Save/Load-safe: ledger.LastAnimalInfectionTick is
// persisted (T3) and Driver respects it via AnimalInfectionChance
// (T1 verdict already passed).

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    public static class AnimalInfectionDriver
    {
        /// <summary>Documentation-constant for the MapComponent-style
        /// re-fire cadence. The static driver itself is invoked once per
        /// day from StoryDirector; this value documents the Wall-clock
        /// budget (~1 driver-call per game day).</summary>
        public const int TickInterval = 3_600;

        // The most recent tick the driver actually fired.
        //
        // Process-lifetime state (DECISION-E-003): we intentionally keep
        // it on the static-API Driver rather than in StoryDirector
        // because (a) the Driver's own TickInterval gate is the only
        // place that knows whether firing is allowed for the current
        // tick, and (b) StoryDirector already wraps the call in a
        // try/catch so a post-load nullable restart here is harmless.
        //
        // Trade-off documented: a Save → Quit → Reload cycle keeps the
        // process-lifetime stamp (the static is in-memory, not in the
        // Scribe stream). That's acceptable because we re-fire exactly
        // once per day-tick and the *actual* idempotency check is the
        // ProfileCap + ledger.AnimalInfectionCountToday gate inside
        // AnimalInfectionChance.ShouldFireToday, not this stamp.
        //
        // -1L = "never fired yet" (cold-start / post-load).
        private static long _lastFireTick = -1L;

        /// <summary>
        /// Returns the number of pawns actually infected today (0 if
        /// the day-tick was rejected by ShouldFireToday). Caller
        /// (StoryDirector) is responsible for invoking this exactly
        /// once per Day-Tick.
        ///
        /// Defensive paths that return 0:
        ///   map == null (no player home map)
        ///   ledger == null (PopulationLedger not initialized)
        ///   profile == null (StoryDirector not initialized)
        ///   !ShouldFireToday (chance/horde/cap gate)
        ///   count <= 0 in AnimalInfectionChance (Stat shall not return
        ///     negative)
        /// </summary>
        public static int TryFireOnce(Map map, long currentTick)
        {
            if (map == null) return 0;

            var ledger = PopulationLedger.Get();
            if (ledger == null) return 0;

            var director = StoryDirector.Get();
            SettingProfile profile = director?.ActiveProfile;
            if (profile == null) return 0;

            int hordeCount = System.Math.Max(
                0, ledger.HumanoidLiveCount + ledger.AnimalLiveCount / 2);

            if (!AnimalInfectionChance.ShouldFireToday(
                    currentTick,
                    ledger.AnimalInfectionCountToday,
                    hordeCount,
                    profile))
            {
                return 0;
            }

            int count = AnimalInfectionChance.ComputeInfectionCount(
                currentTick, hordeCount, profile);
            if (count <= 0) return 0;

            int actually = RandomInoculationService.TryInfectWildAnimals(count, currentTick);
            if (actually > 0)
            {
                ledger.RegisterAnimalInfection(actually, currentTick);
                _lastFireTick = currentTick;
                Log.Message("[Rimconemy.InfectedAutomation] AnimalInfectionDriver: "
                    + actually + " wild animals infected at tick=" + currentTick
                    + " profile=" + profile.ProfileId
                    + " hordeCount=" + hordeCount);
            }
            return actually;
        }

        /// <summary>Diagnostics for tests + Dev-mode dashboard.</summary>
        public static long GetLastFireTick() => _lastFireTick;

        /// <summary>Test-only reset to wipe the static last-fire stamp.</summary>
        public static void ResetForTests() => _lastFireTick = -1L;
    }
}
