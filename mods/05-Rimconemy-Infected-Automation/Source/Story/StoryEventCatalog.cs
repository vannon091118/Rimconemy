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
            // ── new events (event pool expansion) ──────────
            Register(Sturmloot);
            Register(BountifulHarvest);
            Register(ResourceSpoilage);
            Register(WandererArrives);
            Register(LeadershipChallenge);
            Register(PirateRaid);
            Register(MechSwarm);
            Register(Epidemic);
            Register(Betrayal);
            // ── Phase B — Revenge family (2026-08-05) ────────────────
            Register(LesserRevenge);
            Register(GreaterRevenge);

            // ── Phase F — Horde Migration (2026-08-05) ─────────────
            Register(HordeMigrationLetter);
            // ── Phase 1.5 — Sturmgut Tower (Supply family) ───────
            Register(SturmgutTower);
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

        // ═══════════════════════════════════════════════════════
        // NEW EVENTS — event pool expansion (2026-08-05)
        // ═══════════════════════════════════════════════════════            // ── Band 1 — Refuge: positive supply event ──────────
        public static readonly StoryEventSpec Sturmloot = new StoryEventSpec
        {
            EventId = "rimconemy_supply_sturmloot",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Sturmgut",
            Description = "Ein Unwetter hat Holz und Vorräte herbeigespült.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Supply"),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRecoveryEvent(),
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(3.0f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 15f },
                { "Rimconemy_Survival", 25f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 8.0f },
                { "Rimconemy_Survival", 5.0f },
            },

            EscalationBand = 1,
            EscalationModifier = 0f,

            LetterLabel = "Sturmgut",
            LetterText = "Ein Unwetter hat Holzstämme und Vorräte herbeigespült. Die Gruppe kann sie einsammeln.",
            TextKey = "Rimconemy_Sturmloot_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Collect",
                    Label = "Sammeln",
                    Effects = new List<string> { "ResourceBoost:Wood+150", "ResourceBoost:Food+50" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };

        public static readonly StoryEventSpec BountifulHarvest = new StoryEventSpec
        {
            EventId = "rimconemy_supply_bountiful_harvest",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Üppige Ernte",
            Description = "Die Felder liefern mehr als erwartet. Die Moral steigt.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Supply"),
                EventCondition.ColonistCountAbove(1),
                EventCondition.WealthBelow(200000f),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenAnyResourceCritical(),
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(2.0f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 20f },
                { "Rimconemy_Survival", 10f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 10.0f },
                { "Rimconemy_Survival", 12.0f },
            },

            EscalationBand = 1,
            EscalationModifier = 0f,

            LetterLabel = "Reiche Ernte",
            LetterText = "Die Felder tragen dieses Mal besonders gut. Vorräte für {PawnName} und die Gruppe sind gesichert.",
            TextKey = "Rimconemy_BountifulHarvest_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Stockpile",
                    Label = "Vorrat anlegen",
                    Effects = new List<string> { "ResourceBoost:Food+150", "MoodModifier:+3 for 3 days" },
                },
                new EventChoice
                {
                    ChoiceId = "Share",
                    Label = "Mit Nachbarn teilen",
                    Effects = new List<string> { "ResourceBoost:Food+50", "OpinionChange:+15", "MoodModifier:+2 for 2 days" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };

        // ── Band 2 — Survival: resource decay ────────────────
        public static readonly StoryEventSpec ResourceSpoilage = new StoryEventSpec
        {
            EventId = "rimconemy_supply_resource_spoilage",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Vorratsverderb",
            Description = "Hitze und Feuchtigkeit haben einen Teil der Vorräte ruiniert.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Supply"),
                EventCondition.ThreatAbove(0.25f),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(1.5f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 35f },
                { "Rimconemy_Collapse", 45f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 4.0f },
                { "Rimconemy_Collapse", 2.0f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.04f,

            LetterLabel = "Vorräte verdorben",
            LetterText = "{PawnName} stellt fest, dass ein Teil der Vorräte ungenießbar geworden ist. Der Verlust betrifft hauptsächlich Lebensmittel.",
            TextKey = "Rimconemy_ResourceSpoilage_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Salvage",
                    Label = "Reste retten",
                    Effects = new List<string> { "ResourceLoss:Food-30%", "MoodModifier:-2 for 1 day" },
                },
                new EventChoice
                {
                    ChoiceId = "Disinfect",
                    Label = "Aufbereiten",
                    Effects = new List<string> { "ResourceLoss:Food-15%", "WalletCost:20 Credits", "MoodModifier:-1 for 1 day" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{StorageHash}+{GameTickDay}",
        };

        // ── Band 1-2 — Social: new colonist ──────────────────
        public static readonly StoryEventSpec WandererArrives = new StoryEventSpec
        {
            EventId = "rimconemy_social_wanderer_arrives",
            EventVersion = 1,
            EventFamily = "Social",
            Label = "Wanderer",
            Description = "Ein Fremder bittet um Aufnahme in die Gruppe.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Social"),
                EventCondition.ColonistCountBelow(8),
                EventCondition.DaysSinceStartAbove(5f),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenMoodAbove(0.7f),
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(3.0f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 15f },
                { "Rimconemy_Survival", 20f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 15.0f },
                { "Rimconemy_Survival", 12.0f },
            },

            EscalationBand = 1,
            EscalationModifier = 0f,

            LetterLabel = "Fremder nähert sich",
            LetterText = "Ein Wanderer hat die Siedlung erreicht und bittet um Aufnahme. Er scheint arbeitsfähig und friedlich.",
            TextKey = "Rimconemy_WandererArrives_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Accept",
                    Label = "Aufnehmen",
                    Effects = new List<string> { "AddColonist:Wanderer", "MoodModifier:+2 for 2 days" },
                },
                new EventChoice
                {
                    ChoiceId = "Decline",
                    Label = "Abweisen",
                    Effects = new List<string> { "MoodModifier:-1 for 1 day", "OpinionChange:-5" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{PawnId}+{GameTickDay}",
        };

        // ── Band 3 — Social: power challenge ─────────────────
        public static readonly StoryEventSpec LeadershipChallenge = new StoryEventSpec
        {
            EventId = "rimconemy_social_leadership_challenge",
            EventVersion = 1,
            EventFamily = "Social",
            Label = "Führungskrise",
            Description = "{PawnName} fordert die Führung offen heraus.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Social"),
                EventCondition.ColonistCountAbove(2),
                EventCondition.MoodBelow(0.4f),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(2.0f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 55f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 7.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.07f,

            LetterLabel = "Führungssturz",
            LetterText = "{PawnName} fordert die Führungsrolle heraus und gewinnt rapide an Unterstützung. Die Gruppe steht vor einer Spaltung.",
            TextKey = "Rimconemy_LeadershipChallenge_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Challenge",
                    Label = "Herausforderung annehmen",
                    Effects = new List<string> { "PawnMood:-5 for 3 days", "GroupCohesion:-0.15", "IdeologyTension:+0.08" },
                },
                new EventChoice
                {
                    ChoiceId = "StepDown",
                    Label = "Führung abtreten",
                    Effects = new List<string> { "TransferLeadership", "MoodModifier:+5 for 5 days", "GroupCohesion:+0.05" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{IdeologyTension}+{PawnId}+{GameTickDay}",
        };

        // ── Band 2 — Raid: pirate scouts ─────────────────────
        public static readonly StoryEventSpec PirateRaid = new StoryEventSpec
        {
            EventId = "rimconemy_raid_pirate_scouts",
            EventVersion = 1,
            EventFamily = "Raid",
            Label = "Piraten-Scout",
            Description = "Piraten erkunden die Gegend auf Beute.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Raid"),
                EventCondition.WealthAbove(100000f),
                EventCondition.HostileFactionsAbove(0),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenThreatBelow(0.2f),
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(2.0f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 35f },
                { "Rimconemy_Collapse", 50f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 4.0f },
                { "Rimconemy_Collapse", 2.5f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.06f,

            LetterLabel = "Piraten gesichtet",
            LetterText = "Piraten-Scouts wurden in der Nähe der Siedlung gesichtet. Sie scheinen die Vorräte zu studieren.",
            TextKey = "Rimconemy_PirateRaid_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Defend",
                    Label = "Verteidigen",
                    Effects = new List<string> { "DefenseBonus:+0.20 for 1 day", "ResourceCost:15%" },
                },
                new EventChoice
                {
                    ChoiceId = "Bribe",
                    Label = "Bestechen",
                    Effects = new List<string> { "WalletCost:80 Credits", "IdeologyTension:-0.02" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{ThreatPressure}+{GameTickDay}",
        };

        // ── Band 3 — Raid: mechanoid surge ───────────────────
        public static readonly StoryEventSpec MechSwarm = new StoryEventSpec
        {
            EventId = "rimconemy_raid_mech_swarm",
            EventVersion = 1,
            EventFamily = "Raid",
            Label = "Mechanoidenschwarm",
            Description = "Ein Schwarm Mechanoiden nähert sich der Siedlung.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Raid"),
                EventCondition.WealthAbove(300000f),
                EventCondition.ThreatAbove(0.5f),
                EventCondition.DaysSinceStartAbove(15f),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenHealthAbove(0.8f),
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(3.0f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 65f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 4.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.12f,

            LetterLabel = "Mechanoiden im Anmarsch!",
            LetterText = "Ein Schwarm Mechanoiden bewegt sich auf die Siedlung zu. Die Bedrohung ist erheblich.",
            TextKey = "Rimconemy_MechSwarm_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "FullDefense",
                    Label = "Volle Verteidigung",
                    Effects = new List<string> { "DefenseBonus:+0.40 for 3 days", "ResourceCost:30%", "MoodModifier:-5 for 2 days" },
                },
                new EventChoice
                {
                    ChoiceId = "Evacuate",
                    Label = "Evakuieren",
                    Effects = new List<string> { "EvacuateCivilians", "StorageBlocked:75%", "IdeologyTension:+0.15" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{ThreatPressure}+{GameTickDay}",
        };

        // ── Band 3 — Collapse: epidemic ──────────────────────
        public static readonly StoryEventSpec Epidemic = new StoryEventSpec
        {
            EventId = "rimconemy_collapse_epidemic",
            EventVersion = 1,
            EventFamily = "Collapse",
            Label = "Seuchen-Ausbruch",
            Description = "Eine Krankheit breitet sich unter den Kolonisten aus.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Collapse"),
                EventCondition.HealthLow(),
                EventCondition.ColonistCountAbove(2),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(2.5f),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 50f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 6.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.09f,

            LetterLabel = "Seuche!",
            LetterText = "Eine unbekannte Krankheit breitet sich aus. {PawnName} zeigt erste Symptome. Ohne Behandlung wird die Situation kritisch.",
            TextKey = "Rimconemy_Epidemic_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Quarantine",
                    Label = "Quarantäne verhängen",
                    Effects = new List<string> { "PawnMood:-10 for 5 days", "ProductionPenalty:-30% for 3 days", "IdeologyTension:+0.05" },
                },
                new EventChoice
                {
                    ChoiceId = "Treat",
                    Label = "Behandeln",
                    Effects = new List<string> { "ResourceCost:Medicine-40", "PawnHealth:+0.2", "WalletCost:30 Credits" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{PawnId}+{GameTickDay}",
        };

        // ── Band 3 — Collapse: betrayal ──────────────────────
        public static readonly StoryEventSpec Betrayal = new StoryEventSpec
        {
            EventId = "rimconemy_collapse_betrayal",
            EventVersion = 1,
            EventFamily = "Collapse",
            Label = "Verrat",
            Description = "Ein Kolonist verrät die Gruppe an Feinde.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Collapse"),
                EventCondition.ColonistCountAbove(3),
                EventCondition.DaysSinceStartAbove(20f),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ExcludeWhenMoodAbove(0.6f),
                EventCondition.ExcludeWhenDaysSinceLastEventBelow(4.0f),
                EventCondition.ExcludeWhenHostileFactionsBelow(1),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 35f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Collapse", 10.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.15f,

            LetterLabel = "Verrat!",
            LetterText = "{PawnName} wurde dabei erwischt, Informationen an eine feindliche Fraktion zu übergeben. Der Schaden ist bereits angerichtet.",
            TextKey = "Rimconemy_Betrayal_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Exile",
                    Label = "Verbannen",
                    Effects = new List<string> { "PawnLeaves", "AllPawnsMood:-3 for 3 days", "HostileFactionReputation:-20" },
                },
                new EventChoice
                {
                    ChoiceId = "Forgive",
                    Label = "Verzeihen",
                    Effects = new List<string> { "PawnMood:-8 for 5 days", "GroupCohesion:-0.10", "ThreatPressure:+0.05" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{IdeologyTension}+{PawnId}+{GameTickDay}",
        };
        // ═══════════════════════════════════════════════════════
        // PHASE B — REVENGE FAMILY (transient, daily-decay)
        // ═══════════════════════════════════════════════════════

        public static readonly StoryEventSpec LesserRevenge = new StoryEventSpec
        {
            EventId = "rimconemy.revenge.lesser",
            EventVersion = 1,
            EventFamily = "Revenge",
            Label = "Rache-Schwarm",
            Description = "Kleiner Schwarm Infizierter rächt die gestrigen Verluste.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.RevengePendingAtLeast(1),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRaidOrThreat(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 0.7f },
                { "Rimconemy_Collapse", 0.9f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 14.0f },
                { "Rimconemy_Collapse", 7.0f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.06f,

            LetterLabel = "Rache-Schwarm",
            LetterText = "Kleine Infiziertengruppen reagieren auf die gestrigen Verluste. Sie nähern sich der Siedlung.",
            TextKey = "Rimconemy_LesserRevenge_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Defend",
                    Label = "Verteidigen",
                    Effects = new List<string> { "DefenseBonus:+0.20 for 1 day", "ResourceCost:10%" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{GameTickDay}",
        };

        public static readonly StoryEventSpec GreaterRevenge = new StoryEventSpec
        {
            EventId = "rimconemy.revenge.greater",
            EventVersion = 1,
            EventFamily = "Revenge",
            Label = "Rache-Welle",
            Description = "Eine große Welle Infizierter rächt mit aller Wucht.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.RevengePendingAtLeast(MinGreaterRevenge),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRaidOrThreat(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 0.4f },
                { "Rimconemy_Collapse", 0.7f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 21.0f },
                { "Rimconemy_Collapse", 10.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.12f,

            LetterLabel = "Rache-Welle!",
            LetterText = "Eine massive Welle Infizierter greift als Vergeltung für die vielen Verluste an. Die Wut ist spürbar.",
            TextKey = "Rimconemy_GreaterRevenge_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "FullDefense",
                    Label = "Volle Verteidigung",
                    Effects = new List<string> { "DefenseBonus:+0.40 for 2 days", "ResourceCost:25%" },
                },
                new EventChoice
                {
                    ChoiceId = "Evacuate",
                    Label = "Vorrang-Rückzug",
                    Effects = new List<string> { "EvacuateCivilians", "StorageBlocked:60%", "IdeologyTension:+0.10" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{GameTickDay}",
        };

        /// <summary>Threshold above which the greater-revenge event unlocks.
        /// 8 minted-spawns is mid-tier: well above the daily Survival baseline
        /// but comfortably below Collapse proportions.</summary>
        public const int MinGreaterRevenge = 8;

        // ═══════════════════════════════════════════════════════
        // PHASE F — HORDE MIGRATION LETTER (2026-08-05)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Wandering-Horde Letter — part of the regular weighted catalog
        /// (Raid family). The manifest spawn itself is driven by the
        /// HordeMigrationDriver tick-loop once the horde is active.
        /// </summary>
        public static readonly StoryEventSpec HordeMigrationLetter = new StoryEventSpec
        {
            EventId = "rimconemy.raid.infected_horde_migration",
            EventVersion = 1,
            EventFamily = "Raid",
            Label = "Wandernde Horde",
            Description = "Eine massive Horde Infizierter wandert auf dein Territorium zu.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveRaidOrThreat(),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 0.6f },
                { "Rimconemy_Collapse", 0.85f },
                { "Rimconemy_Refuge", 0.3f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Survival", 10.0f },
                { "Rimconemy_Collapse", 5.0f },
                { "Rimconemy_Refuge", 14.0f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.15f,

            LetterLabel = "Wandernde Horde!",
            LetterText = "Auf den Wegen rings um die Siedlung zieht eine massive Horde Infizierter ihre Bahn. Sie sind noch weit \u2014 aber sie kommen n\u00e4her. Die Horde wird alles niedermachen, was sich ihr in den Weg stellt.",
            TextKey = "Rimconemy_HordeMigrationLetter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Mobilize",
                    Label = "Horde ausl\u00f6sen",
                    Effects = new List<string> { "TriggerHordeMigration:Survival", "DefenseBonus:+0.30 for 3 days", "ResourceCost:30%" },
                },
                new EventChoice
                {
                    ChoiceId = "Fortify",
                    Label = "Siedlung verbarrikadieren",
                    Effects = new List<string> { "StorageBlocked:50%", "DefenseBonus:+0.15 for 2 days" },
                },
                new EventChoice
                {
                    ChoiceId = "Ignore",
                    Label = "Ignorieren",
                    Effects = new List<string> { "HordeEscalation:+0.20", "ThreatPressure:+0.10" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{GameTickDay}+{HordeEffective}",
        };

        // ═══════════════════════════════════════════════════════
        // PHASE 1.5 — STURMGUT EVENT FOR STAINLESS STEEL TOWER
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Sturmgut (Storm Loot) — Supply family event that spawns
        /// a StainlessSteelTower as rare storm loot. Rewards the player
        /// with an advanced defensive turret for surviving a storm.
        /// 
        /// EventId: rimconemy_supply_sturmgut_tower
        /// </summary>
        public static readonly StoryEventSpec SturmgutTower = new StoryEventSpec
        {
            EventId = "rimconemy_supply_sturmgut_tower",
            EventVersion = 1,
            EventFamily = "Supply",
            Label = "Sturmgut: Edelstahlturm",
            Description = "Nach einem heftigen Sturm wurde ein intakter Edelstahlturm in der Nähe entdeckt.",

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
                { "Rimconemy_Refuge", 0.5f },
                { "Rimconemy_Survival", 1.0f },
                { "Rimconemy_Collapse", 0.7f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 30.0f },
                { "Rimconemy_Survival", 20.0f },
                { "Rimconemy_Collapse", 15.0f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.1f,

            LetterLabel = "Sturmgut entdeckt!",
            LetterText = "Ein gewaltiger Sturm hat sich gelegt — und dabei etwas Seltenes freigegeben: einen intakten Edelstahlturm, bereit zur Inbetriebnahme. Ein Glücksfall für jede Siedlung.",
            TextKey = "Rimconemy_SturmgutTower_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Claim",
                    Label = "Turm bergen",
                    Effects = new List<string> { "SpawnThing:StainlessSteelTower+1", "DefenseBonus:+0.15 for 5 days" },
                },
                new EventChoice
                {
                    ChoiceId = "Scavenge",
                    Label = "Ausschlachten",
                    Effects = new List<string> { "ResourceBoost:StainlessSteel+30", "ResourceBoost:WeaponComponent+4", "ResourceBoost:Steel+20" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{GameTickDay}",
        };
    }
}
