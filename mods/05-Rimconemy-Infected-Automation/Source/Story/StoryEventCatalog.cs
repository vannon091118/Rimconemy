using System;
using System.Collections.Generic;
using System.Globalization;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// StoryEventCatalog — the runtime catalog of StoryEventSpec records.
    ///
    /// Built exclusively from XML <see cref="StoryEventDef"/> entries loaded
    /// by RimWorld via DefDatabase. All 25 MVP events live in
    /// <c>Defs/StoryEvents/StoryEvents_Migrated.xml</c>.
    ///
    /// Modders can ADD new events or OVERRIDE existing ones via additional
    /// StoryEventDef XML files / PatchOperations.
    ///
    /// Specification: Sprint-Plan H9 (2026-08-04); XML migration 2026-08-07.
    /// </summary>
    public sealed class StoryEventCatalog
    {
        private readonly Dictionary<string, StoryEventSpec> _byId;

        public StoryEventCatalog()
        {
            _byId = new Dictionary<string, StoryEventSpec>(StringComparer.Ordinal);
            MergeFromDefDatabase();
        }

        public StoryEventSpec GetById(string eventId)
        {
            if (eventId == null) return null;
            _byId.TryGetValue(eventId, out var spec);
            return spec;
        }

        public List<StoryEventSpec> All()
        {
            var list = new List<StoryEventSpec>(_byId.Values);
            list.Sort((a, b) => string.Compare(a.EventId, b.EventId, StringComparison.Ordinal));
            return list;
        }

        /// <summary>
        /// Walks <see cref="DefDatabase{StoryEventDef}"/>.AllDefs and builds
        /// the catalog from XML-only. All 25 events live in XML Defs.
        /// Overlapping defNames are naturally handled (last wins).
        /// </summary>
        private void MergeFromDefDatabase()
        {
            try
            {
                var defs = DefDatabase<StoryEventDef>.AllDefsListForReading;
                if (defs == null) return;

                int loaded = 0;
                foreach (var def in defs)
                {
                    if (def == null || string.IsNullOrEmpty(def.defName)) continue;
                    var spec = BuildSpecFromDef(def);
                    if (spec == null) continue;
                    _byId[def.defName] = spec;
                    loaded++;
                }

                Log.Message(
                    "[Rimconemy.InfectedAutomation] StoryEventCatalog loaded: " +
                    $"{loaded} events from StoryEventDef XML.");
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] StoryEventCatalog merge failed: " +
                    $"{ex.GetType().Name}: {ex.Message}. Catalog may be empty.");
            }
        }

        /// <summary>
        /// Build a <see cref="StoryEventSpec"/> from a RimWorld-loaded
        /// <see cref="StoryEventDef"/>.
        /// </summary>
        private static StoryEventSpec BuildSpecFromDef(StoryEventDef def)
        {
            if (def == null) return null;

            var spec = new StoryEventSpec
            {
                EventId = def.defName,
                EventVersion = def.eventVersion > 0 ? def.eventVersion : 1,
                EventFamily = def.eventFamily ?? "Uncategorised",
                Label = def.label ?? def.defName,
                Description = def.description ?? "",
                EscalationBand = def.escalationBand > 0 ? def.escalationBand : 1,
                EscalationModifier = def.escalationModifier,
                LetterLabel = def.letterLabel ?? def.label ?? def.defName,
                LetterText = def.letterText ?? "",
                TextKey = def.textKey ?? (def.defName + "_Letter"),
                DeterminismKeyTemplate = def.determinismKey ?? ("{ProfileId}+{EventId}+{GameTickDay}"),
            };

            spec.Prerequisites = new List<EventCondition>();
            if (def.prerequisites != null)
            {
                foreach (var raw in def.prerequisites)
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    spec.Prerequisites.Add(EventCondition.FromXmlExpression(raw));
                }
            }

            spec.Exclusions = new List<EventCondition>();
            if (def.exclusions != null)
            {
                foreach (var raw in def.exclusions)
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    spec.Exclusions.Add(EventCondition.FromXmlExpression(raw));
                }
            }

            spec.Weights = ParseProfileList(def.weights);
            spec.CooldownsDays = ParseProfileList(def.cooldownDays);

            spec.Choices = new List<EventChoice>();
            if (def.choices != null)
            {
                foreach (var choice in def.choices)
                {
                    if (choice == null) continue;
                    spec.Choices.Add(new EventChoice
                    {
                        ChoiceId = string.IsNullOrEmpty(choice.choiceId) ? "Choice" : choice.choiceId,
                        Label = choice.label ?? choice.choiceId ?? "Choice",
                        Effects = choice.effects != null
                            ? new List<string>(choice.effects)
                            : new List<string>(),
                    });
                }
            }

            spec.FollowUpIds = new List<string>();
            if (def.followUpIds != null)
                spec.FollowUpIds.AddRange(def.followUpIds);

            return spec;
        }

        private static Dictionary<string, float> ParseProfileList(List<string> raw)
        {
            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            if (raw == null) return result;

            foreach (var line in raw)
            {
                if (string.IsNullOrEmpty(line)) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string profileId = line.Substring(0, eq).Trim();
                string valueStr = line.Substring(eq + 1).Trim();
                result[profileId] = TryParseFloat(valueStr, out float v) ? v : 0f;
            }
            return result;
        }

        private static bool TryParseFloat(string s, out float v)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
