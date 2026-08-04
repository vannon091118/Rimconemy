using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// Cross-walk between the canonical 4-family code vocabulary used by
    /// StoryEventCatalog / SettingProfile and the 8-family H2 vocabulary
    /// documented in docs/H2-story-contract.md §3.
    ///
    /// Phase 1 code is locked to the 4-family vocabulary because the 12
    /// events in StoryEventCatalog are written against these strings
    /// ("Supply", "Social", "Raid", "Collapse"). The 8 H2 families are a
    /// finer conceptual decomposition; both are valid descriptions of the
    /// same event space, and this map is the authoritative bridge.
    ///
    /// Mapping is many-to-many in both directions:
    /// - The H2 family "SupplyCrisis" maps to code "Supply".
    /// - The code family "Supply" encompasses H2 SupplyCrisis, Discovery,
    ///   TechOpportunity.
    /// Mapping is read-only at runtime; new events should pick the code
    /// vocabulary (Supply / Social / Raid / Collapse) for
    /// profile.AllowedEventFamilies checks. When the event space is
    /// unified in a later phase, this map is the canonical pivot.
    ///
    /// See: docs/H2-story-contract.md §3.
    /// </summary>
    public static class EventFamilyMap
    {
        /// <summary>Code (4-family) → preferred H2 (8-family) label.</summary>
        public static readonly IReadOnlyDictionary<string, string> CodeToH2 =
            new Dictionary<string, string>
            {
                { "Supply",   "SupplyCrisis" },
                { "Social",   "IdeologyConflict" },
                { "Raid",     "ExternalThreat" },
                { "Collapse", "TurnPoint" },
            };

        /// <summary>All H2 (8-family) labels that fall under each code family.</summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> CodeToH2All =
            new Dictionary<string, IReadOnlyList<string>>
            {
                { "Supply",   new[] { "SupplyCrisis", "Discovery", "TechOpportunity" } },
                { "Social",   new[] { "IdeologyConflict", "MoralChoice", "RestRecovery" } },
                { "Raid",     new[] { "ExternalThreat" } },
                { "Collapse", new[] { "TurnPoint" } },
            };

        /// <summary>H2 (8-family) → parent code (4-family) label.</summary>
        public static readonly IReadOnlyDictionary<string, string> H2ToCode =
            new Dictionary<string, string>
            {
                { "SupplyCrisis",      "Supply" },
                { "Discovery",         "Supply" },
                { "TechOpportunity",   "Supply" },
                { "IdeologyConflict",  "Social" },
                { "MoralChoice",       "Social" },
                { "RestRecovery",      "Social" },
                { "ExternalThreat",    "Raid" },
                { "TurnPoint",         "Collapse" },
            };

        /// <summary>Returns the H2 preferred label for a code family, or null if unknown.</summary>
        public static string ToH2(string codeFamily)
        {
            if (string.IsNullOrEmpty(codeFamily)) return null;
            return CodeToH2.TryGetValue(codeFamily, out string h2) ? h2 : null;
        }

        /// <summary>Returns the code label for an H2 family, or null if unknown.</summary>
        public static string ToCode(string h2Family)
        {
            if (string.IsNullOrEmpty(h2Family)) return null;
            return H2ToCode.TryGetValue(h2Family, out string code) ? code : null;
        }
    }
}
