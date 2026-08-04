using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    ///
    /// Read-only catalog of active Setting Rules and their visible
    /// technical carriers. Production-side consumer is
    /// <see cref="UI.SettingRulesInspector"/> which renders the
    /// catalog in the Foundation UI dashboard; tests consume it via
    /// the section in dev/automation runs.
    ///
    /// The catalog does not mutate DefDatabase or Pawn state. It is
    /// idempotent and safe to query any time after GameComponent init.
    ///
    /// Specification: ROADMAP §2.4 (Setting-Ideologie) and H3.
    /// </summary>
    public static class SettingRulesCatalog
    {
        /// <summary>Returns the active SettingRule entries that ship with this package.</summary>
        public static List<SettingRuleEntry> ActiveRules()
        {
            var rules = new List<SettingRuleEntry>();

            rules.Add(BuildEntry(
                ruleId: "Rimconemy_SettingRule_ResourceFairness",
                issue: "Rimconemy_Needs" /* could be Ref to a real Issue if present */,
                primaryCarrier: "PreceptDef (H3 §1) + ThoughtWorker_ResourceFairness",
                moodOrImpact: "+3 / -5 / -8 (kumulativ)",
                family: "ResourceFairness"));

            rules.Add(BuildEntry(
                ruleId: "Rimconemy_SettingRule_CollectiveDefense",
                issue: "Rimconemy_CollectiveDefenseIssue",
                primaryCarrier: "PreceptDef Rimconemy_Role_Defender (Precept_RoleMulti) + Harmony PostApplyDamage",
                moodOrImpact: "+5 / -8 + Gruppenreward +3",
                family: "Defense"));

            rules.Add(BuildEntry(
                ruleId: "Rimconemy_SettingRule_Transparency",
                issue: "Rimconemy_TransparencyIssue",
                primaryCarrier: "PreceptDef + ThoughtWorker_Transparency + StoryDirector-Bridge",
                moodOrImpact: "+2 / -6...-12 (kumulativ)",
                family: "Governance"));

            return rules;
        }

        /// <summary>Returns the count of SettingRule entries known by the catalog.</summary>
        public static int ActiveRuleCount => 3;

        private static SettingRuleEntry BuildEntry(string ruleId, string issue, string primaryCarrier, string moodOrImpact, string family)
        {
            return new SettingRuleEntry
            {
                RuleId = ruleId,
                Issue = issue,
                PrimaryCarrier = primaryCarrier,
                MoodOrImpact = moodOrImpact,
                Family = family,
            };
        }
    }

    /// <summary>Plain record for a Setting Rule. UI-/test-side consumer.</summary>
    public struct SettingRuleEntry
    {
        public string RuleId;
        public string Issue;
        public string PrimaryCarrier;
        public string MoodOrImpact;
        public string Family;
    }
}
