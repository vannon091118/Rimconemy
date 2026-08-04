using System;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    /// Read by Foundation via service bus.
    ///
    /// A difficulty profile that defines threat escalation,
    /// event families, rest windows, ideology tension and
    /// deterministic seed rules for one playthrough.
    ///
    /// Specification: docs/H2-story-contract.md §1
    /// </summary>
    public sealed class SettingProfile
    {
        // ── identity ──────────────────────────────────────────
        public string ProfileId;
        public int ProfileVersion;
        public string Label;
        public string Description;

        // ── threat / escalation ──────────────────────────────
        public float MinThreatLevel;
        public float MaxThreatLevel;
        public float ThreatRiseRate;    // per day
        public float ThreatFallRate;    // per day

        // ── event pacing ─────────────────────────────────────
        public float RestWindowMinDays;
        public float RestWindowMaxDays;
        public float EventCooldownGlobalDays;
        public int MaxActiveEvents;
        public int MaxEscalationBand;

        // ── event families ───────────────────────────────────
        public List<string> AllowedEventFamilies;
        public List<string> BannedEventFamilies;

        // ── resources / ideology ─────────────────────────────
        public float ResourceScarcityMultiplier;
        public float IdeologyTensionCap;
        public float IdeologyTensionDecay;

        // ── turn points / seed ───────────────────────────────
        public float TurnPointMinDays;
        /// <summary>
        /// Documentation-only: describes how the seed is derived.
        /// The actual seed is built in DeterministicRng.BuildSeed.
        /// </summary>
        public string SeedRule;

        // ── helpers ──────────────────────────────────────────
        public float CooldownGlobalTicks => DaysToTicks(EventCooldownGlobalDays);
        public float RestWindowMinTicks => DaysToTicks(RestWindowMinDays);
        public float RestWindowMaxTicks => DaysToTicks(RestWindowMaxDays);

        private static float DaysToTicks(float days) => days * Rimconemy.Foundation.TimeConstants.TicksPerDay;

        // ── built-in profiles ────────────────────────────────

        public static readonly SettingProfile Refuge = new SettingProfile
        {
            ProfileId = "Rimconemy_Refuge",
            ProfileVersion = 1,
            Label = "Zuflucht",
            Description = "Aufbau einer kleinen Zuflucht. Krisen sind selten und werden früh angekündigt. Konflikte lösen sich überwiegend durch Dialog.",
            MinThreatLevel = 0.0f,
            MaxThreatLevel = 0.40f,
            ThreatRiseRate = 0.02f,
            ThreatFallRate = 0.05f,
            RestWindowMinDays = 3.0f,
            RestWindowMaxDays = 7.0f,
            EventCooldownGlobalDays = 1.5f,
            MaxActiveEvents = 1,
            MaxEscalationBand = 1,
            AllowedEventFamilies = new List<string> { "Supply", "Social" },
            BannedEventFamilies = new List<string> { "Raid", "Collapse" },
            ResourceScarcityMultiplier = 0.7f,
            IdeologyTensionCap = 0.35f,
            IdeologyTensionDecay = 0.08f,
            TurnPointMinDays = 10f,
            SeedRule = "MapID + GameTickDay",
        };

        public static readonly SettingProfile Survival = new SettingProfile
        {
            ProfileId = "Rimconemy_Survival",
            ProfileVersion = 1,
            Label = "Überleben",
            Description = "Hartes tägliches Überleben. Versorgung und Bedrohung konkurrieren ständig. Lagerbestand ist entscheidend. Soziale Folgen von Entscheidungen werden sichtbar.",
            MinThreatLevel = 0.2f,
            MaxThreatLevel = 0.7f,
            ThreatRiseRate = 0.05f,
            ThreatFallRate = 0.03f,
            RestWindowMinDays = 1.0f,
            RestWindowMaxDays = 3.0f,
            EventCooldownGlobalDays = 1.0f,
            MaxActiveEvents = 2,
            MaxEscalationBand = 2,
            AllowedEventFamilies = new List<string> { "Supply", "Social", "Raid" },
            BannedEventFamilies = new List<string> { "Collapse" },
            ResourceScarcityMultiplier = 1.0f,
            IdeologyTensionCap = 0.60f,
            IdeologyTensionDecay = 0.05f,
            TurnPointMinDays = 20f,
            SeedRule = "MapID + GameTickDay",
        };

        public static readonly SettingProfile Collapse = new SettingProfile
        {
            ProfileId = "Rimconemy_Collapse",
            ProfileVersion = 1,
            Label = "Zusammenbruch",
            Description = "Zusammenbruch unter permanentem Druck. Keine kostenlose Erholung. Wendepunkte früh möglich. Konflikte können Rollen, Mood und Folgeevents stark verändern.",
            MinThreatLevel = 0.15f,
            MaxThreatLevel = 1.00f,
            ThreatRiseRate = 0.10f,
            ThreatFallRate = 0.01f,
            RestWindowMinDays = 0.5f,
            RestWindowMaxDays = 2.0f,
            EventCooldownGlobalDays = 0.5f,
            MaxActiveEvents = 2,
            MaxEscalationBand = 3,
            AllowedEventFamilies = new List<string> { "Supply", "Social", "Raid", "Collapse" },
            BannedEventFamilies = new List<string>(),
            ResourceScarcityMultiplier = 1.5f,
            IdeologyTensionCap = 0.90f,
            IdeologyTensionDecay = 0.02f,
            TurnPointMinDays = 5f,
            SeedRule = "MapID + GameTickDay",
        };

        /// <summary>Returns the built-in profile by ID, or null.</summary>
        public static SettingProfile GetBuiltIn(string profileId)
        {
            if (profileId == Refuge.ProfileId) return Refuge;
            if (profileId == Survival.ProfileId) return Survival;
            if (profileId == Collapse.ProfileId) return Collapse;
            return null;
        }
    }
}
