using System;
using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// StoryEventCatalog — the runtime catalog of StoryEventSpec records.
    ///
    /// Built from two sources (I-T2 / 2026-08-04):
    ///   1. Hardcoded MVP catalog (12 events across 4 families) — primary
    ///      content, lives in this file below the class declaration.
    ///   2. XML <see cref="StoryEventDef"/> entries loaded by RimWorld via
    ///      DefDatabase. The catalog calls
    ///      <see cref="MergeFromDefDatabase"/> after the hardcoded register
    ///      so XML defs can ADD new events OR OVERRIDE existing hardcoded
    ///      events on defName match. Override lets designers tune an event
    ///      without recompiling.
    ///
    /// Specification: Sprint-Plan H9 (2026-08-04)
    /// </summary>
    public sealed class StoryEventCatalog
    {
        private readonly Dictionary<string, StoryEventSpec> _byId;

        public StoryEventCatalog()
        {
            _byId = new Dictionary<string, StoryEventSpec>(StringComparer.Ordinal);
            SeedHardcodedCatalog();
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

        private void Register(StoryEventSpec spec)
        {
            if (spec == null || string.IsNullOrEmpty(spec.EventId)) return;
            _byId[spec.EventId] = spec;
        }

        private void SeedHardcodedCatalog()
        {
            // Band 1 - Refuge
            Register(StockpileBonus);
            Register(FactionEnvoy);
            Register(InternalDispute);
            // Band 2 - Survival
            Register(Shortage);
            Register(InfectedScouts);
            Register(IdeologySchism);
            Register(TradeOpportunity);
            // Band 3 - Collapse
            Register(InfectedHorde);
            Register(ResourceCollapse);
            Register(Desertion);
            Register(PowerStruggle);
            Register(LastCache);
        }

        /// <summary>
        /// Walks <see cref="DefDatabase{StoryEventDef}"/>.AllDefs and merges
        /// each entry into <c>_byId</c>. New EventIds are added; existing
        /// EventIds (already in the hardcoded catalog) are OVERWRITTEN by the
        /// XML version so designers can tune events without a recompile.
        ///
        /// Defensive: silently ignores nulls, empty EventIds, and defs whose
        /// weights / cooldownDays cannot be parsed. The hardcoded catalog is
        /// the SSOT; XML overlays cannot delete entries.
        /// </summary>
        private void MergeFromDefDatabase()
        {
            try
            {
                var defs = DefDatabase<StoryEventDef>.AllDefsListForReading;
                if (defs == null) return;

                int overlays = 0;
                int additions = 0;
                foreach (var def in defs)
                {
                    if (def == null) continue;
                    var eventId = def.defName;
                    if (string.IsNullOrEmpty(eventId)) continue;

                    var spec = BuildSpecFromDef(def);
                    if (spec == null) continue;

                    bool wasOverride = _byId.ContainsKey(eventId);
                    _byId[eventId] = spec;
                    if (wasOverride) overlays++; else additions++;
                }

                Log.Message(
                    "[Rimconemy.InfectedAutomation] StoryEventCatalog merged: " +
                    $"{_byId.Count} total, {overlays} overlays, {additions} additions from StoryEventDefs.");
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] MergeFromDefDatabase guarded exception: " +
                    $"{ex.GetType().Name}: {ex.Message}. Falling back to hardcoded catalog only.");
            }
        }

        /// <summary>
        /// Build a <see cref="StoryEventSpec"/> from a RimWorld-loaded
        /// <see cref="StoryEventDef"/>. Returns null when the def cannot
        /// produce a usable spec.
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

        /// <summary>
        /// Parses the XML profile-list into a Dictionary&lt;ProfileId, float&gt;.
        /// Supported notation today is the canonical "Key=Value" form, e.g.
        /// <c>Rimconemy_Refuge=10</c>. Lines without '=', empty lines, parse
        /// failures and unsupported wrapped notations are silently skipped so
        /// a single bad row does not poison the whole catalog.
        /// When a future swept-form parser is added, this method is the
        /// single pivot point: see Spec note in H9.
        /// </summary>
        private static Dictionary<string, float> ParseProfileList(List<string> raw)
        {
            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            if (raw == null) return result;

            foreach (var line in raw)
            {
                if (string.IsNullOrEmpty(line)) continue;

                // "Key=Value" notation is the only one supported today.
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

        // ═══════════════════════════════════════════════════════
        // BAND 1 — Refuge (Supply + Social only)
        // ═══════════════════════════════════════════════════════

        public static readonly StoryEventSpec StockpileBonus = new StoryEventSpec
        {
            EventId = "rimconemy.supply.stockpile_bonus",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Vorratsbonus",
            Description = "Ein Handelstrupp bringt unerwartete Vorräte.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Supply"),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRecoveryEvent(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 30f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 4.0f },
            },

            EscalationBand = 1,
            EscalationModifier = 0f,

            LetterLabel = "Handelstrupp",
            LetterText = "Ein neutraler Handelstrupp hat Vorräte abgeworfen. Deine Gruppe kann sie einsammeln.",
            TextKey = "Rimconemy_StockpileBonus_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Accept",
                    Label = "Vorräte annehmen",
                    Effects = new List<string> { "ResourceBoost:Food+100", "ResourceBoost:Meds+20" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };

        public static readonly StoryEventSpec FactionEnvoy = new StoryEventSpec
        {
            EventId = "rimconemy.social.faction_envoy",
            EventVersion = 1,
            EventFamily = "Social",
            Label = "Gesandter",
            Description = "Ein Gesandter einer anderen Gruppe trifft ein.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Social"),
            },
            Exclusions = null,

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 25f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 6.0f },
            },

            EscalationBand = 1,
            EscalationModifier = 0.01f,

            LetterLabel = "Gesandter eingetroffen",
            LetterText = "Ein Gesandter der {FactionName} ist eingetroffen und möchte verhandeln.",
            TextKey = "Rimconemy_FactionEnvoy_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Trade",
                    Label = "Handeln",
                    Effects = new List<string> { "OpenTradeWindow", "OpinionChange:+10" },
                },
                new EventChoice
                {
                    ChoiceId = "Decline",
                    Label = "Ablehnen",
                    Effects = new List<string> { "OpinionChange:-5", "IdeologyTension:+0.02" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{GameTickDay}",
        };

        public static readonly StoryEventSpec InternalDispute = new StoryEventSpec
        {
            EventId = "rimconemy.social.internal_dispute",
            EventVersion = 1,
            EventFamily = "Social",
            Label = "Interner Streit",
            Description = "Zwei Siedler geraten in einen Konflikt.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Social"),
            },
            Exclusions = null,

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 20f },
                { "Rimconemy_Survival", 25f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 8.0f },
                { "Rimconemy_Survival", 6.0f },
            },

            EscalationBand = 1,
            EscalationModifier = 0.02f,

            LetterLabel = "Streit in der Gruppe",
            LetterText = "{PawnA} und {PawnB} sind aneinandergeraten. Die Stimmung leidet.",
            TextKey = "Rimconemy_InternalDispute_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Mediate",
                    Label = "Schlichten",
                    Effects = new List<string> { "BothPawnsMood:-3 for 2 days", "IdeologyTension:-0.03" },
                },
                new EventChoice
                {
                    ChoiceId = "Ignore",
                    Label = "Ignorieren",
                    Effects = new List<string> { "BothPawnsMood:-8 for 3 days", "IdeologyTension:+0.05" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{IdeologyTension}+{GameTickDay}",
        };

        // ═══════════════════════════════════════════════════════
        // BAND 2 — Survival (Supply + Social + Raid)
        // ═══════════════════════════════════════════════════════

        public static readonly StoryEventSpec Shortage = new StoryEventSpec
        {
            EventId = "rimconemy.supply.shortage",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Versorgungskrise",
            Description = "Ein kritischer Lagerbestand unterschreitet das Minimum.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Supply"),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRecoveryEvent(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 20f },
                { "Rimconemy_Survival", 50f },
                { "Rimconemy_Collapse", 60f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 5.0f },
                { "Rimconemy_Survival", 3.0f },
                { "Rimconemy_Collapse", 1.5f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.03f,

            LetterLabel = "Vorräte schwinden",
            LetterText = "Der Bestand an {ResourceName} ist kritisch niedrig ({CurrentAmount}/{MinAmount}).",
            TextKey = "Rimconemy_Shortage_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "RationResources",
                    Label = "Ressourcen rationieren",
                    Effects = new List<string>
                    {
                        "FreezeConsumption:2.0 days",
                        "IdeologyTension:+0.05",
                        "MoodModifier:-3 for 2 days",
                    },
                },
                new EventChoice
                {
                    ChoiceId = "SeekExternalHelp",
                    Label = "Externe Hilfe suchen",
                    Effects = new List<string>
                    {
                        "TriggerTradingOpportunity",
                        "IdeologyTension:+0.02",
                        "WalletCost:50 Credits",
                    },
                },
                new EventChoice
                {
                    ChoiceId = "Ignore",
                    Label = "Ignorieren",
                    Effects = new List<string>
                    {
                        "ThreatPressure:+0.03",
                        "MoodModifier:-5 for 1 day",
                    },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };

        public static readonly StoryEventSpec InfectedScouts = new StoryEventSpec
        {
            EventId = "rimconemy.raid.infected_scouts",
            EventVersion = 1,
            EventFamily = "Raid",
            Label = "Infizierte Späher",
            Description = "Eine kleine Gruppe Infizierter nähert sich.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Raid"),
                EventCondition.ActiveVanillaRaid(),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRaidOrThreat(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 45f },
                { "Rimconemy_Collapse", 60f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 3.0f },
                { "Rimconemy_Collapse", 1.5f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.05f,

            LetterLabel = "Infizierte gesichtet",
            LetterText = "Eine kleine Gruppe Infizierter wurde in der Nähe gesichtet. Sie scheinen zu suchen.",
            TextKey = "Rimconemy_InfectedScouts_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "PrepareDefense",
                    Label = "Verteidigung vorbereiten",
                    Effects = new List<string> { "DefenseBonus:+0.25 for 1 day", "ResourceCost:10%" },
                },
                new EventChoice
                {
                    ChoiceId = "Hide",
                    Label = "Verstecken",
                    Effects = new List<string> { "StorageBlocked:20%", "IdeologyTension:+0.03" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{ThreatPressure}+{GameTickDay}",
        };

        public static readonly StoryEventSpec IdeologySchism = new StoryEventSpec
        {
            EventId = "rimconemy.social.ideology_schism",
            EventVersion = 1,
            EventFamily = "Social",
            Label = "Ideologische Spaltung",
            Description = "Die Gruppe droht sich ideologisch zu spalten.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Social"),
                EventCondition.ActiveSettingRules(),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.NoActiveSettingRules(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 35f },
                { "Rimconemy_Collapse", 55f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 5.0f },
                { "Rimconemy_Collapse", 3.0f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.04f,

            LetterLabel = "Spaltung droht",
            LetterText = "Die Spannungen in der Gruppe haben einen kritischen Punkt erreicht. {PawnName} stellt die Regeln in Frage.",
            TextKey = "Rimconemy_IdeologySchism_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "EnforceRule",
                    Label = "Regel durchsetzen",
                    Effects = new List<string> { "TargetPawnMood:-8 for 3 days", "IdeologyTension:-0.05" },
                },
                new EventChoice
                {
                    ChoiceId = "Compromise",
                    Label = "Kompromiss suchen",
                    Effects = new List<string> { "TargetPawnMood:+3 for 2 days", "IdeologyTension:+0.02" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{IdeologyTension}+{PawnId}+{GameTickDay}",
        };

        public static readonly StoryEventSpec TradeOpportunity = new StoryEventSpec
        {
            EventId = "rimconemy.supply.trade_opportunity",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Handelsangebot",
            Description = "Ein Händler bietet Ressourcen gegen Credits.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Supply"),
            },
            Exclusions = null,

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 30f },
                { "Rimconemy_Collapse", 25f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 4.0f },
                { "Rimconemy_Collapse", 3.0f },
            },

            EscalationBand = 2,
            EscalationModifier = 0f,

            LetterLabel = "Handelsangebot",
            LetterText = "Ein Händler bietet {ResourceName} im Tausch gegen Credits an.",
            TextKey = "Rimconemy_TradeOpportunity_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Accept",
                    Label = "Annehmen",
                    Effects = new List<string> { "WalletCost:50 Credits", "ResourceBoost:{ResourceName}+50" },
                },
                new EventChoice
                {
                    ChoiceId = "Decline",
                    Label = "Ablehnen",
                    Effects = new List<string> { "IdeologyTension:+0.01" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };

        // ═══════════════════════════════════════════════════════
        // BAND 3 — Collapse (Supply + Social + Raid + Collapse)
        // ═══════════════════════════════════════════════════════

        public static readonly StoryEventSpec InfectedHorde = new StoryEventSpec
        {
            EventId = "rimconemy.raid.infected_horde",
            EventVersion = 1,
            EventFamily = "Raid",
            Label = "Infizierten-Horde",
            Description = "Eine große Welle Infizierter stürmt auf die Siedlung zu.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Raid"),
                EventCondition.ActiveVanillaRaid(),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRaidOrThreat(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 80f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 2.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.10f,

            LetterLabel = "Horde im Anmarsch!",
            LetterText = "Eine große Horde Infizierter bewegt sich auf die Siedlung zu. Geschätzte Stärke: {Strength}.",
            TextKey = "Rimconemy_InfectedHorde_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "PrepareDefense",
                    Label = "Verteidigung vorbereiten",
                    Effects = new List<string> { "DefenseBonus:+0.30 for 2 days", "ResourceCost:20%" },
                },
                new EventChoice
                {
                    ChoiceId = "Evacuate",
                    Label = "Evakuieren",
                    Effects = new List<string> { "EvacuateCivilians", "StorageBlocked:50%", "IdeologyTension:+0.10" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{ThreatPressure}+{GameTickDay}",
        };

        public static readonly StoryEventSpec ResourceCollapse = new StoryEventSpec
        {
            EventId = "rimconemy.collapse.resource_collapse",
            EventVersion = 1,
            EventFamily = "Collapse",
            Label = "Ressourcenkollaps",
            Description = "Mehrere Ressourcen brechen gleichzeitig ein.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Collapse"),
            },
            Exclusions = null,

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 70f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 3.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.08f,

            LetterLabel = "Kollaps!",
            LetterText = "Mehrere kritische Ressourcen sind gleichzeitig erschöpft. Die Gruppe steht vor dem Kollaps.",
            TextKey = "Rimconemy_ResourceCollapse_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Prioritize",
                    Label = "Priorisieren",
                    Effects = new List<string> { "FreezeOneResource", "OtherResources:-30%", "IdeologyTension:+0.05" },
                },
                new EventChoice
                {
                    ChoiceId = "Abandon",
                    Label = "Aufgeben",
                    Effects = new List<string> { "LoseAllResources", "ThreatPressure:-0.15", "MoodModifier:-15 for 5 days" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };

        public static readonly StoryEventSpec Desertion = new StoryEventSpec
        {
            EventId = "rimconemy.collapse.desertion",
            EventVersion = 1,
            EventFamily = "Collapse",
            Label = "Desertion",
            Description = "Ein Siedler droht zu desertieren.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Collapse"),
            },
            Exclusions = null,

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 60f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 5.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.06f,

            LetterLabel = "Desertion droht",
            LetterText = "{PawnName} hat angekündigt, die Gruppe zu verlassen. Die Situation ist zu viel für ihn.",
            TextKey = "Rimconemy_Desertion_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Convince",
                    Label = "Überreden",
                    Effects = new List<string> { "TargetPawnMood:+5 for 3 days", "IdeologyTension:-0.03" },
                },
                new EventChoice
                {
                    ChoiceId = "LetGo",
                    Label = "Gehen lassen",
                    Effects = new List<string> { "PawnLeaves", "AllPawnsMood:-5 for 2 days", "IdeologyTension:+0.05" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{PawnId}+{GameTickDay}",
        };

        public static readonly StoryEventSpec PowerStruggle = new StoryEventSpec
        {
            EventId = "rimconemy.social.power_struggle",
            EventVersion = 1,
            EventFamily = "Social",
            Label = "Machtkampf",
            Description = "Zwei Siedler kämpfen um die Führungsrolle.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Social"),
            },
            Exclusions = null,

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 50f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 6.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.05f,

            LetterLabel = "Machtkampf",
            LetterText = "{PawnA} fordert die Führungsrolle von {PawnB} heraus. Die Gruppe ist gespalten.",
            TextKey = "Rimconemy_PowerStruggle_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "SideWithLeader",
                    Label = "Führung unterstützen",
                    Effects = new List<string> { "ChallengerMood:-10 for 5 days", "GroupCohesion:+0.05" },
                },
                new EventChoice
                {
                    ChoiceId = "SideWithChallenger",
                    Label = "Herausforderer unterstützen",
                    Effects = new List<string> { "LeaderMood:-10 for 5 days", "GroupCohesion:-0.10" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{IdeologyTension}+{GameTickDay}",
        };

        public static readonly StoryEventSpec LastCache = new StoryEventSpec
        {
            EventId = "rimconemy.supply.last_cache",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Letztes Versteck",
            Description = "Ein verstecktes Lager wurde entdeckt — die letzte Chance.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Supply"),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRecoveryEvent(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 40f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 8.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0f,

            LetterLabel = "Verstecktes Lager",
            LetterText = "Ein verstecktes Vorratslager wurde entdeckt. Einmalige Ressourcen stehen zur Verfügung.",
            TextKey = "Rimconemy_LastCache_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Loot",
                    Label = "Plündern",
                    Effects = new List<string> { "ResourceBoost:Food+200", "ResourceBoost:Meds+50", "ResourceBoost:Materials+100" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };
    }
}
