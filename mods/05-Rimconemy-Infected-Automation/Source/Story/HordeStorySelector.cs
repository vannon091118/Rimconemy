using System;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    /// <summary>
    /// Phase F (2026-08-05) — Wahl des "Wandernde Horde"-Briefes.
    /// Owner: Infected &amp; Automation (Package 05).
    ///
    /// Treiber-Gates (D-3):
    ///   - ThreatPressure >= Profile-spezifischem HordeActivationThreshold
    ///   - EffectiveCount-Schwelle via HordeCalculator.IsActive(effective, profile)
    ///   - CooldownDays seit letztem Horde-Letter (Profile-spez.)
    ///   - HordeManifest nicht bereits aktiv (kein Doppel-Spawn)
    ///
    /// Architektur: Letter-only Outcome — der eigentliche Manifest-Spawn
    /// passiert erst nach Spieler-Accept via Choice-Effect-Hook
    /// ProcessTriggerHordeMigrationEffect. Damit bleibt die Wander-Horde
    /// ein vom Spieler bewusst ausgelöstes Ereignis.
    ///
    /// Spec: docs/superpowers/specs/2026-08-05-horde-migration-design.md §4.
    /// </summary>
    public static class HordeStorySelector
    {
        /// <summary>Def-Style EventId. Registered in StoryEventCatalog.SeedHardcodedCatalog.</summary>
        public static readonly string HordeMigrationLetterId = "rimconemy.raid.infected_horde_migration";

        /// <summary>
        /// Pure, deterministic letter-selection for the Wandering-Horde.
        ///
        /// Returns the HordeMigrationLetter iff all 4 gates open; otherwise null.
        /// Same snapshot + profile + tick → same return value.
        /// </summary>
        public static StoryEventSpec SelectHordeMigrationLetter(
            StoryState state,
            SituationSnapshot snapshot,
            SettingProfile profile,
            long currentTick)
        {
            if (profile == null || snapshot == null) return null;

            string key = StripRimconemyPrefix(profile.ProfileId);

            // (1) ThreatGate
            float threshold = PopulationProfileMultipliers.GetHordeActivationThreshold(key);
            if (snapshot.ThreatPressure < threshold) return null;

            // (2) EffectiveGate — effective count must trip Horde activation
            var ledger = PopulationLedger.Get();
            if (ledger == null) return null;
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            if (!HordeCalculator.IsActive(effective, profile)) return null;

            // (3) Already-active Manifest? Don't double-fire.
            var manifest = HordeManifest.Get();
            if (manifest != null && manifest.EffectiveSize > 0) return null;

            // (4) CooldownGate — StoryState.EventCooldowns is eventId→expiresAtTick.
            // TicksPerDay is float (60000f), so cast the product to long.
            float cooldownDays = PopulationProfileMultipliers.GetHordeLetterCooldownDays(key);
            long cooldownTicks = (long)(cooldownDays * Rimconemy.Foundation.TimeConstants.TicksPerDay);
            if (state != null && state.EventCooldowns != null
                && state.EventCooldowns.TryGetValue(HordeMigrationLetterId, out long expiresAtTick)
                && currentTick < expiresAtTick)
                return null;

            // Construct catalog at call-site (same pattern StoryDirector uses);
            // the hardcoded + XML merge is fast and deterministic.
            var catalog = new StoryEventCatalog();
            return catalog.GetById(HordeMigrationLetterId);
        }

        /// <summary>
        /// Effect-Hook: Choice "Mobilize" ruft diesen Hook mit
        /// pattern <c>TriggerHordeMigration:profile-count</c> auf.
        /// Im aktuellen Sprint wird nur die Profile-Komponente gelesen;
        /// die Count-Komponente fliesst in die Manifest-Reichweite ein.
        /// </summary>
        public static bool ProcessTriggerHordeMigrationEffect(string effectArg, long currentTick)
        {
            if (string.IsNullOrEmpty(effectArg)) return false;

            string profileKey = StripRimconemyPrefix(effectArg.Split(':')[0]);
            var manifest = HordeManifest.CreateOrExpand(profileKey, currentTick);
            return manifest != null;
        }

        /// <summary>
        /// Normalisierung: <c>"Rimconemy_Survival"</c> → <c>"Survival"</c>.
        /// Falls id leer ist: Fallback auf "Survival" (Hausordnungs-Profil).
        /// </summary>
        public static string StripRimconemyPrefix(string id)
        {
            if (id == null) return "Survival";
            string t = id.Trim();
            if (t.Length == 0) return "Survival";
            const string prefix = "Rimconemy_";
            return t.StartsWith(prefix, StringComparison.Ordinal)
                ? t.Substring(prefix.Length)
                : t;
        }
    }
}
