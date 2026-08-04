using System;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// Immutable specification for one story event. Each event has
    /// prerequisites, exclusions, per-profile weights/cooldowns,
    /// escalation bands, player choices and follow-up event ids.
    ///
    /// Specification: docs/H2-story-contract.md §2
    /// Gate G2: deterministic selection via EventId-ordering + weight.
    /// </summary>
    public sealed class StoryEventSpec
    {
        // ── identity ──────────────────────────────────────────
        public string EventId;
        public int EventVersion;
        public string EventFamily;
        public string Label;
        public string Description;

        // ── prerequisites / exclusions ───────────────────────
        /// <summary>All prerequisites must evaluate to true.</summary>
        public List<EventCondition> Prerequisites;

        /// <summary>If any exclusion evaluates to true, the event is blocked.</summary>
        public List<EventCondition> Exclusions;

        // ── per-profile tuning ───────────────────────────────
        /// <summary>Weight per profile ID. Higher = more likely.</summary>
        public Dictionary<string, float> Weights;

        /// <summary>Cooldown in days per profile ID.</summary>
        public Dictionary<string, float> CooldownsDays;

        /// <summary>Escalation band (0-3).</summary>
        public int EscalationBand;

        /// <summary>Threat increase when player ignores the event.</summary>
        public float EscalationModifier;

        // ── UI / text ─────────────────────────────────────────
        public string LetterLabel;
        public string LetterText;       // may contain {placeholders}
        public string TextKey;

        // ── choices ──────────────────────────────────────────
        public List<EventChoice> Choices;

        // ── follow-up ────────────────────────────────────────
        public List<string> FollowUpIds;

        // ── determinism ──────────────────────────────────────
        /// <summary>
        /// Template for the determinism key. Placeholders:
        /// {ProfileId}, {EventId}, {StorageHash}, {IdeologyTension},
        /// {ThreatPressure}, {PawnId}, {GameTickDay}
        /// </summary>
        public string DeterminismKeyTemplate;

        // ── helpers ──────────────────────────────────────────

        /// <summary>Cooldown in ticks for the given profile.</summary>
        public long GetCooldownTicks(string profileId)
        {
            if (CooldownsDays != null && CooldownsDays.TryGetValue(profileId, out float days))
                return (long)(days * Rimconemy.Foundation.TimeConstants.TicksPerDay);
            return 0;
        }

        /// <summary>Weight for the given profile. Returns 0 if not configured.</summary>
        public float GetWeight(string profileId)
        {
            if (Weights != null && Weights.TryGetValue(profileId, out float w))
                return w;
            return 0f;
        }
    }

    /// <summary>
    /// A condition that must be true for an event to fire (prerequisite)
    /// or must be false (exclusion). This is a pure data descriptor;
    /// evaluation is done by the game layer, not by this model.
    /// </summary>
    public sealed class EventCondition
    {
        /// <summary>Machine-readable condition id, e.g. "ActiveEvent".</summary>
        public string ConditionId;

        /// <summary>Optional parameter (e.g. event family name).</summary>
        public string Parameter;

        /// <summary>Human-readable description for diagnostics.</summary>
        public string Description;

        public static EventCondition ActiveEvent(string eventFamily)
        {
            return new EventCondition
            {
                ConditionId = "ActiveEvent",
                Parameter = eventFamily,
                Description = $"No active event of family '{eventFamily}' may exist.",
            };
        }

        public static EventCondition MaxActiveEventsReached()
        {
            return new EventCondition
            {
                ConditionId = "MaxActiveEvents",
                Parameter = null,
                Description = "Profile.MaxActiveEvents limit not yet reached.",
            };
        }

        public static EventCondition ActiveRecoveryEvent()
        {
            return new EventCondition
            {
                ConditionId = "ActiveRecoveryEvent",
                Parameter = null,
                Description = "No active recovery event in progress.",
            };
        }

        public static EventCondition ActiveVanillaRaid()
        {
            return new EventCondition
            {
                ConditionId = "ActiveVanillaRaid",
                Parameter = null,
                Description = "No active vanilla raid in progress.",
            };
        }

        public static EventCondition ActiveRaidOrThreat()
        {
            return new EventCondition
            {
                ConditionId = "ActiveRaidOrThreatEvent",
                Parameter = null,
                Description = "No active raid or threat event in progress.",
            };
        }

        public static EventCondition ActiveSettingRules()
        {
            return new EventCondition
            {
                ConditionId = "AtLeastOneActiveSettingRule",
                Parameter = null,
                Description = "At least one ideology setting rule is active.",
            };
        }

        public static EventCondition NoActiveSettingRules()
        {
            return new EventCondition
            {
                ConditionId = "NoActiveSettingRules",
                Parameter = null,
                Description = "No active setting rules exist (blocks ideology conflict).",
            };
        }

        /// <summary>
        /// Build an EventCondition from a single line of XML, e.g.
        ///   <c>profile.MaxActiveEvents erreicht</c>
        ///   <c>AtLeastOneActiveSettingRule()</c>
        ///   <c>NOT ActiveEvent(IdeologyConflict)</c>
        /// Allowed when the line is a free-form descriptive text - we keep
        /// the whole text as Description and leave ConditionId empty so the
        /// evaluator logs a debug line and the event remains eligible
        /// (defensive, never blocks on a parse miss).
        /// </summary>
        public static EventCondition FromXmlExpression(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new EventCondition { ConditionId = "Unparsed", Description = "<empty>" };

            string trimmed = raw.Trim();

            // NOT prefix - we treat it as a NotActiveEvent when the inner
            // expression is a simple call.
            if (trimmed.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase))
            {
                string inner = trimmed.Substring(4).Trim();
                int lpar = inner.IndexOf('(');
                int rpar = inner.LastIndexOf(')');
                if (lpar > 0 && rpar > lpar)
                {
                    string name = inner.Substring(0, lpar).Trim();
                    string args = inner.Substring(lpar + 1, rpar - lpar - 1).Trim();
                    return new EventCondition
                    {
                        ConditionId = name,
                        Parameter = args,
                        Description = "NOT " + name + "(" + args + "): " + trimmed,
                    };
                }
            }

            // Plain function call "Name(arg)" - extract name and arg.
            int lp = trimmed.IndexOf('(');
            int rp = trimmed.LastIndexOf(')');
            if (lp > 0 && rp > lp)
            {
                string name = trimmed.Substring(0, lp).Trim();
                string args = trimmed.Substring(lp + 1, rp - lp - 1).Trim();
                return new EventCondition
                {
                    ConditionId = name,
                    Parameter = args,
                    Description = trimmed,
                };
            }

            // Free text (e.g. German narrative "profile.MaxActiveEvents erreicht")
            // - keep descriptive only. The Evaluator is unaffected because
            // ConditionId is empty so this row is skipped silently.
            return new EventCondition
            {
                ConditionId = "FreeText",
                Parameter = null,
                Description = trimmed,
            };
        }
    }

    /// <summary>
    /// A choice the player can make during an event.
    /// </summary>
    public sealed class EventChoice
    {
        /// <summary>Stable choice id (e.g. "RationResources").</summary>
        public string ChoiceId;

        /// <summary>UI label.</summary>
        public string Label;

        /// <summary>Declared effects (descriptive, evaluated by game layer).</summary>
        public List<string> Effects;
    }
}
