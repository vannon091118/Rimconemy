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

        // ── new condition factories (event pool expansion) ──────────

        public static EventCondition HealthLow()
        {
            return new EventCondition
            {
                ConditionId = "HealthLow",
                Parameter = null,
                Description = "Average colonist health below 60%.",
            };
        }

        public static EventCondition HealthCritical()
        {
            return new EventCondition
            {
                ConditionId = "HealthCritical",
                Parameter = null,
                Description = "Average colonist health below 30%.",
            };
        }

        public static EventCondition AnyColonistInjured()
        {
            return new EventCondition
            {
                ConditionId = "AnyColonistInjured",
                Parameter = null,
                Description = "At least one colonist has a major injury.",
            };
        }

        public static EventCondition WealthAbove(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "WealthAbove",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Colony wealth above {threshold:F0}.",
            };
        }

        public static EventCondition WealthBelow(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "WealthBelow",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Colony wealth below {threshold:F0}.",
            };
        }

        public static EventCondition ThreatAbove(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "ThreatAbove",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Threat pressure above {threshold:P0}.",
            };
        }

        public static EventCondition MoodBelow(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "MoodBelow",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Average mood below {threshold:P0}.",
            };
        }

        public static EventCondition MoodAbove(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "MoodAbove",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Average mood above {threshold:P0}.",
            };
        }

        public static EventCondition PowerOff()
        {
            return new EventCondition
            {
                ConditionId = "PowerOff",
                Parameter = null,
                Description = "Colony power grid is offline.",
            };
        }

        public static EventCondition PowerOn()
        {
            return new EventCondition
            {
                ConditionId = "PowerOn",
                Parameter = null,
                Description = "Colony power grid is online.",
            };
        }

        public static EventCondition ColonistCountAbove(int count)
        {
            return new EventCondition
            {
                ConditionId = "ColonistCountAbove",
                Parameter = count.ToString(),
                Description = $"More than {count} colonists.",
            };
        }

        public static EventCondition ColonistCountBelow(int count)
        {
            return new EventCondition
            {
                ConditionId = "ColonistCountBelow",
                Parameter = count.ToString(),
                Description = $"Fewer than {count} colonists.",
            };
        }

        public static EventCondition DaysSinceLastEventAbove(float days)
        {
            return new EventCondition
            {
                ConditionId = "DaysSinceLastEventAbove",
                Parameter = days.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"At least {days:F1} days since last event.",
            };
        }

        public static EventCondition ResourceCritical()
        {
            return new EventCondition
            {
                ConditionId = "ResourceCritical",
                Parameter = null,
                Description = "At least one resource is critically low.",
            };
        }

        public static EventCondition HostileFactionsAbove(int count)
        {
            return new EventCondition
            {
                ConditionId = "HostileFactionsAbove",
                Parameter = count.ToString(),
                Description = $"More than {count} hostile factions.",
            };
        }

        public static EventCondition RevengePendingAtLeast(int threshold)
        {
            return new EventCondition
            {
                ConditionId = "RevengePending",
                Parameter = threshold.ToString(),
                Description = $"StoryDirector.LastPendingRevenge >= {threshold}.",
            };
        }

        public static EventCondition DaysSinceStartAbove(float days)
        {
            return new EventCondition
            {
                ConditionId = "DaysSinceStartAbove",
                Parameter = days.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"At least {days:F1} days since game start.",
            };
        }

        // ── new exclusion factories ─────────────────────────────────

        public static EventCondition ExcludeWhenAnyResourceCritical()
        {
            return new EventCondition
            {
                ConditionId = "AnyResourceCritical",
                Parameter = null,
                Description = "Blocked when any resource is already critical.",
            };
        }

        public static EventCondition ExcludeWhenThreatBelow(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "ThreatBelow",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Blocked when threat pressure is below {threshold:P0}.",
            };
        }

        public static EventCondition ExcludeWhenHealthAbove(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "HealthAbove",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Blocked when average health is above {threshold:P0}.",
            };
        }

        public static EventCondition ExcludeWhenMoodAbove(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "MoodAbove",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Blocked when average mood is above {threshold:P0}.",
            };
        }

        public static EventCondition ExcludeWhenPowerOn()
        {
            return new EventCondition
            {
                ConditionId = "PowerOn",
                Parameter = null,
                Description = "Blocked when power grid is online.",
            };
        }

        public static EventCondition ExcludeWhenWealthAbove(float threshold)
        {
            return new EventCondition
            {
                ConditionId = "WealthAbove",
                Parameter = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Blocked when colony wealth is above {threshold:F0}.",
            };
        }

        public static EventCondition ExcludeWhenDaysSinceLastEventBelow(float days)
        {
            return new EventCondition
            {
                ConditionId = "DaysSinceLastEventBelow",
                Parameter = days.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Description = $"Blocked when less than {days:F1} days since last event.",
            };
        }

        public static EventCondition ExcludeWhenHostileFactionsBelow(int count)
        {
            return new EventCondition
            {
                ConditionId = "HostileFactionsBelow",
                Parameter = count.ToString(),
                Description = $"Blocked when fewer than {count} hostile factions.",
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
