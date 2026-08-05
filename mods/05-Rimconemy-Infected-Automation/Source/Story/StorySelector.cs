using System;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// Pure, deterministic story event selection.
    /// Given a SettingProfile, SituationSnapshot, StoryState and
    /// StoryEventCatalog, returns exactly one event candidate
    /// or null if no event should fire.
    ///
    /// Gate G2: same snapshot + profile + seed → same event.
    ///           No duplicate execution after save/load.
    ///
    /// Specification: docs/H2-story-contract.md §4
    /// </summary>
    public static class StorySelector
    {
        /// <summary>
        /// Result of a selection attempt. Contains the selected event
        /// (or null) and diagnostic information.
        /// </summary>
        public sealed class SelectionResult
        {
            /// <summary>Selected event, or null if no candidate qualified.</summary>
            public StoryEventSpec SelectedEvent;

            /// <summary>Number of candidates after filtering.</summary>
            public int CandidateCount;

            /// <summary>Total weight of all candidates.</summary>
            public float TotalWeight;

            /// <summary>The random roll that determined the selection.</summary>
            public float RollValue;

            /// <summary>Human-readable reason for selection or rejection.</summary>
            public string Reason;

        /// <summary>
        /// Determinism key that will be stored for idempotency.
        /// Built via FNV-1a hash of the resolved determinism template.
        /// This is a stable integer hash, not a compound string.
        /// </summary>
        public string DeterminismKey;

        /// <summary>
        /// Compound idempotency key (EventId:DeterminismKey) that the
        /// caller should pass to <c>StoryState.CommitSelection</c> once
        /// the incident-fire succeeds. Audit-round-3 §3: the selector no
        /// longer writes idempotency on its own — commit is the caller's
        /// responsibility so a failed fire can re-attempt next cycle.
        /// </summary>
        public string IdempotencyKey;

        /// <summary>
        /// Selection seed carried back for <c>StoryState.CommitSelection</c>.
        /// Same value the selector passed to DeterministicRng.BuildSeed.
        /// </summary>
        public int SelectionSeed;

        /// <summary>
        /// Cooldown length (in ticks) the caller should pass to
        /// <c>StoryState.CommitSelection</c>.
        /// </summary>
        public long CooldownTicks;

        /// <summary>True if the selection produced a valid event.</summary>
        public bool HasEvent => SelectedEvent != null;
    }

        /// <summary>
        /// Selects a story event for the current tick.
        /// Returns null if no event should fire.
        /// </summary>
        /// <param name="profile">Active difficulty profile.</param>
        /// <param name="snapshot">Current situation snapshot.</param>
        /// <param name="state">Persistent story state (read/write for idempotency).</param>
        /// <param name="catalog">Available events.</param>
        /// <param name="currentTick">Current game tick.</param>
        /// <returns>Selection result with diagnostic info.</returns>
        public static SelectionResult SelectEvent(
            SettingProfile profile,
            SituationSnapshot snapshot,
            StoryState state,
            StoryEventCatalog catalog,
            long currentTick)
        {
            var result = new SelectionResult();

            if (profile == null)
            {
                result.Reason = "No active profile.";
                return result;
            }
            if (state == null)
            {
                result.Reason = "No story state available.";
                return result;
            }
            if (catalog == null)
            {
                result.Reason = "No event catalog available.";
                return result;
            }

            // ── Step 1: Hard exclusions ──────────────────────
            var candidates = new List<StoryEventSpec>();
            foreach (var evt in catalog.All())
            {
                // A1: Event family allowed by profile?
                if (profile.AllowedEventFamilies == null ||
                    !profile.AllowedEventFamilies.Contains(evt.EventFamily))
                {
                    continue;
                }

                // A1b: Event family banned by profile?
                if (profile.BannedEventFamilies != null &&
                    profile.BannedEventFamilies.Contains(evt.EventFamily))
                {
                    continue;
                }

                // A1c (Phase B 2026-08-05): Revenge-family events are only
                // eligible when StoryDirector has a non-zero revenge-pending
                // slot. The selector does not see the per-tick ledger, so a
                // hard gate here keeps revenge events OFF the candidate list
                // on day 1 of a save/load even before the day-tick block
                // recomputes the slot. Mirrors the prerequisite gate but
                // runs first so we don't waste IMO weight math on events
                // that will always be filtered downstream.
                if (evt.EventFamily == "Revenge")
                {
                    var director = Story.StoryDirector.Get();
                    if (director == null || director.LastPendingRevenge <= 0)
                    {
                        continue;
                    }
                }

                // A2: Escalation band within profile limit?
                if (evt.EscalationBand > profile.MaxEscalationBand)
                {
                    continue;
                }

                // A3: Event already active?
                if (snapshot.ActiveEventIds != null &&
                    snapshot.ActiveEventIds.Contains(evt.EventId))
                {
                    continue;
                }

                // A4: Max active events reached?
                if (snapshot.ActiveEventIds != null &&
                    snapshot.ActiveEventIds.Count >= profile.MaxActiveEvents)
                {
                    continue;
                }

                // A5: Prerequisites met?
                if (!EvaluatePrerequisites(evt, profile, snapshot))
                {
                    continue;
                }

                // A6: Exclusions triggered?
                if (EvaluateExclusions(evt, profile, snapshot))
                {
                    continue;
                }

                candidates.Add(evt);
            }

            // ── Step 1b: Resource-critical boost (slop-audit fix C3 / A3) ──────
            // When the snapshot flags AnyResourceCritical+true, Supply-family
            // events get a 3.0x weight boost so the StorySelector doesn't return
            // a non-Supply event ignoring the resource signal. This is the
            // audit-driven wiring for: "StorySelector prüft AnyResourceCritical".
            if (snapshot.AnyResourceCritical)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].EventFamily == "Supply")
                        candidates[i] = ApplyFamilyBoost(candidates[i], 3.0f);
                }
            }

            result.CandidateCount = candidates.Count;
            if (candidates.Count == 0)
            {
                result.Reason = "No event candidates passed filters.";
                return result;
            }

            // ── Step 2: Stable sort ──────────────────────────
            candidates.Sort((a, b) => string.Compare(a.EventId, b.EventId, StringComparison.Ordinal));

            // ── Step 3: Weighted selection ───────────────────
            float totalWeight = 0f;
            foreach (var evt in candidates)
                totalWeight += evt.GetWeight(profile.ProfileId);

            result.TotalWeight = totalWeight;

            if (totalWeight <= 0f)
            {
                result.Reason = "Total weight is zero or negative.";
                return result;
            }

            // ── Step 4: Deterministic roll ───────────────────
            int seed = DeterministicRng.BuildSeed(
                "{ProfileId}+{MapID}+{GameTickDay}",
                null, profile, snapshot);

            var rng = new DeterministicRng(seed);
            float roll = rng.NextFloat() * totalWeight;
            result.RollValue = roll;

            // ── Step 5: Pick winner ──────────────────────────
            float cumulative = 0f;
            foreach (var candidate in candidates)
            {
                cumulative += candidate.GetWeight(profile.ProfileId);
                if (roll <= cumulative)
                {
                    result.SelectedEvent = candidate;
                    break;
                }
            }

            // Fallback: last candidate
            if (result.SelectedEvent == null)
                result.SelectedEvent = candidates[candidates.Count - 1];

            // ── Step 6: Build determinism key ────────────────
            result.DeterminismKey = DeterministicRng.BuildSeed(
                result.SelectedEvent.DeterminismKeyTemplate,
                result.SelectedEvent, profile, snapshot).ToString();

            // ── Step 7: Idempotency check (READ-ONLY) ────────
            // Must run BEFORE cooldown so identical snapshot→same event→blocked.
            // We compute the key but do NOT write it back: that happens via
            // StoryState.CommitSelection AFTER the incident fire succeeds.
            // This is the audit-round-3 §3 fix: previously the selector
            // called state.MarkExecuted here, which burned the key even
            // when StoryDirector.QueueSelectedIncident later failed (no map,
            // no storyteller, def missing, exception). The result was a
            // Letter that never appeared and an event that never re-fired.
            string idempotencyKey = StoryState.BuildIdempotencyKey(
                result.SelectedEvent.EventId,
                result.DeterminismKey);

            if (state.HasExecuted(idempotencyKey))
            {
                result.SelectedEvent = null;
                result.Reason = $"Idempotency key already executed: {idempotencyKey}";
                return result;
            }

            // ── Step 8: Cooldown check (READ-ONLY) ──────────
            // Save event ref BEFORE nulling — the Reason string reads .Label and .EventId.
            if (state.IsOnCooldown(result.SelectedEvent.EventId, currentTick))
            {
                var blockedEvent = result.SelectedEvent;
                result.SelectedEvent = null;
                result.Reason = $"Cooldown active for '{blockedEvent.Label}' until tick {state.GetCooldownUntil(blockedEvent.EventId)}.";
                return result;
            }

            // ── Step 9: Selection is now READ-ONLY ───────────
            // Fire-or-retry semantics (audit-round-3 §3, 2026-08-04):
            // The selector no longer writes back to StoryState. It returns
            // a SelectionResult carrying the idempotency key and cooldown
            // length. StoryDirector.GameComponentTick commits the state ONLY
            // after QueueSelectedIncident reports a successful TryFire. If
            // the queue fails (no storyteller, no map, def missing, exception
            // inside TryFire), the state stays clean so the same event is
            // re-evaluated on the next evaluation cycle.
            long cooldownTicks = result.SelectedEvent.GetCooldownTicks(profile.ProfileId);

            result.IdempotencyKey = idempotencyKey;
            result.CooldownTicks = cooldownTicks;
            result.SelectionSeed = seed;
            result.Reason = $"Selected '{result.SelectedEvent.Label}' " +
                $"(roll={roll:F3}, weight={result.SelectedEvent.GetWeight(profile.ProfileId)}, " +
                $"candidates={candidates.Count}, seed={seed})";

            return result;
        }

        // ── family-boost helper (slop-audit fix C3 / A3) ────

        /// <summary>
        /// Returns a shallow-clone of <paramref name="spec"/> with each
        /// per-profile weight multiplied by <paramref name="factor"/>.
        /// Modifying in-place would mutate the shared catalog entry, so we
        /// clone. Performs no IO and never returns null for a non-null input.
        /// </summary>
        private static StoryEventSpec ApplyFamilyBoost(StoryEventSpec spec, float factor)
        {
            if (spec == null) return null;
            var boosted = new StoryEventSpec
            {
                EventId = spec.EventId,
                EventVersion = spec.EventVersion,
                EventFamily = spec.EventFamily,
                Label = spec.Label,
                Description = spec.Description,
                Prerequisites = spec.Prerequisites,
                Exclusions = spec.Exclusions,
                EscalationBand = spec.EscalationBand,
                EscalationModifier = spec.EscalationModifier,
                LetterLabel = spec.LetterLabel,
                LetterText = spec.LetterText,
                TextKey = spec.TextKey,
                DeterminismKeyTemplate = spec.DeterminismKeyTemplate,
                CooldownsDays = spec.CooldownsDays,
                FollowUpIds = spec.FollowUpIds,
                Choices = spec.Choices,
                Weights = new Dictionary<string, float>(),
            };
            if (spec.Weights != null)
            {
                foreach (var kvp in spec.Weights)
                    boosted.Weights[kvp.Key] = kvp.Value * factor;
            }
            return boosted;
        }

        // ── prerequisite / exclusion evaluation ──────────────

        /// <summary>
        /// Evaluates prerequisites against the snapshot.
        /// In this pure-model phase, conditions are matched by
        /// ConditionId against known snapshot fields.
        /// The game layer will replace this with a more sophisticated
        /// evaluator when runtime hooks are available.
        /// </summary>
        private static bool EvaluatePrerequisites(StoryEventSpec evt, SettingProfile profile, SituationSnapshot snapshot)
        {
            if (evt.Prerequisites == null) return true;

            foreach (var cond in evt.Prerequisites)
            {
                switch (cond.ConditionId)
                {
                    case "MaxActiveEvents":
                        if (snapshot.ActiveEventIds != null &&
                            snapshot.ActiveEventIds.Count >= profile.MaxActiveEvents)
                            return false;
                        break;

                    case "ActiveEvent":
                        // Prerequisite: no active event of this family
                        if (!string.IsNullOrEmpty(cond.Parameter) &&
                            snapshot.HasActiveEventOfFamily(cond.Parameter))
                            return false;
                        break;

                    case "ActiveVanillaRaid":
                        // Phase 2 can query Find.Storyteller for an active vanilla raid.
                        break;

                    case "AtLeastOneActiveSettingRule":
                        if (snapshot.ActiveSettingRuleCount <= 0)
                            return false;
                        break;

                    case "HealthLow":
                        // Fires when average colonist health drops below 0.6.
                        if (snapshot.AverageSurvivorHealth >= 0.6f)
                            return false;
                        break;

                    case "HealthCritical":
                        // Fires when average colonist health drops below 0.3.
                        if (snapshot.AverageSurvivorHealth >= 0.3f)
                            return false;
                        break;

                    case "AnyColonistInjured":
                        if (!snapshot.AnyColonistInjured)
                            return false;
                        break;

                    case "WealthAbove":
                        // Parameter: threshold as float string, e.g. "200000".
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float wThreshold))
                        {
                            if (snapshot.ColonyWealth < wThreshold)
                                return false;
                        }
                        break;

                    case "WealthBelow":
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float wBelow))
                        {
                            if (snapshot.ColonyWealth >= wBelow)
                                return false;
                        }
                        break;

                    case "ThreatAbove":
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float tAbove))
                        {
                            if (snapshot.ThreatPressure < tAbove)
                                return false;
                        }
                        break;

                    case "MoodBelow":
                        // Average mood percentage threshold (0-1 scale).
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float moodThreshold))
                        {
                            if (snapshot.AverageColonistMood >= moodThreshold)
                                return false;
                        }
                        break;

                    case "MoodAbove":
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float moodAbove))
                        {
                            if (snapshot.AverageColonistMood < moodAbove)
                            return false;
                        }
                        break;

                    case "PowerOff":
                        if (snapshot.PowerGridActive)
                            return false;
                        break;

                    case "PowerOn":
                        if (!snapshot.PowerGridActive)
                            return false;
                        break;

                    case "ColonistCountAbove":
                        if (int.TryParse(cond.Parameter, out int cAbove))
                        {
                            if (snapshot.SurvivorCount <= cAbove)
                                return false;
                        }
                        break;

                    case "ColonistCountBelow":
                        if (int.TryParse(cond.Parameter, out int cBelow))
                        {
                            if (snapshot.SurvivorCount >= cBelow)
                                return false;
                        }
                        break;

                    case "DaysSinceLastEventAbove":
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float dAbove))
                        {
                            if (snapshot.DaysSinceLastEvent < dAbove)
                                return false;
                        }
                        break;

                    case "ResourceCritical":
                        // At least one resource must be critical.
                        if (!snapshot.AnyResourceCritical)
                            return false;
                        break;

                    case "HostileFactionsAbove":
                        if (int.TryParse(cond.Parameter, out int fAbove))
                        {
                            if (snapshot.HostileFactionCount <= fAbove)
                                return false;
                        }
                        break;

                    case "RevengePending":
                        // Phase B: parameter is the minimum revenge-pending
                        // quota required. Read live from the (transient) director
                        // state so a save/load mid-day rebuild matches the
                        // post-tick recompute exactly.
                        if (int.TryParse(cond.Parameter, out int rThreshold))
                        {
                            var director = Story.StoryDirector.Get();
                            if (director == null || director.LastPendingRevenge < rThreshold)
                                return false;
                        }
                        break;

                    case "DaysSinceStartAbove":
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float dsAbove))
                        {
                            if (snapshot.DaysSinceStart < dsAbove)
                                return false;
                        }
                        break;

                    default:
                        // Unknown conditions pass through (game layer handles them)
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Evaluates exclusions against the snapshot.
        /// Returns true if ANY exclusion matches (blocking the event).
        /// </summary>
        private static bool EvaluateExclusions(StoryEventSpec evt, SettingProfile profile, SituationSnapshot snapshot)
        {
            if (evt.Exclusions == null) return false;

            foreach (var cond in evt.Exclusions)
            {
                switch (cond.ConditionId)
                {
                    case "ActiveRecoveryEvent":
                        // Phase 2: query active events for recovery family.
                        break;

                    case "NoActiveSettingRules":
                        if (snapshot.ActiveSettingRuleCount <= 0)
                            return true;
                        break;

                    case "ActiveRaidOrThreatEvent":
                        // Phase 2: query active events for raid/threat families.
                        break;

                    case "AnyResourceCritical":
                        // Block event when any resource is already critical
                        // (avoids stacking crisis events on top of each other).
                        if (snapshot.AnyResourceCritical)
                            return true;
                        break;

                    case "ThreatBelow":
                        // Don't fire low-stakes events when threat is high.
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float tBelow))
                        {
                            if (snapshot.ThreatPressure >= tBelow)
                                return true;
                        }
                        break;

                    case "HealthAbove":
                        // Don't fire health-crisis events when health is fine.
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float hAbove))
                        {
                            if (snapshot.AverageSurvivorHealth >= hAbove)
                                return true;
                        }
                        break;

                    case "MoodAbove":
                        // Don't fire mood-crisis events when mood is fine.
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float mAbove))
                        {
                            if (snapshot.AverageColonistMood >= mAbove)
                                return true;
                        }
                        break;

                    case "PowerOn":
                        // Don't fire power-outage events when power is on.
                        if (snapshot.PowerGridActive)
                            return true;
                        break;

                    case "WealthAbove":
                        // Don't fire scarcity events when colony is wealthy.
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float wAbove))
                        {
                            if (snapshot.ColonyWealth >= wAbove)
                                return true;
                        }
                        break;

                    case "DaysSinceLastEventBelow":
                        // Don't fire rapid-fire events too close together.
                        if (float.TryParse(cond.Parameter, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float dBelow))
                        {
                            if (snapshot.DaysSinceLastEvent < dBelow)
                                return true;
                        }
                        break;

                    case "HostileFactionsBelow":
                        // Don't fire diplomatic events when few hostiles.
                        if (int.TryParse(cond.Parameter, out int fBelow))
                        {
                            if (snapshot.HostileFactionCount < fBelow)
                                return true;
                        }
                        break;

                    default:
                        // Unknown exclusions: conservative — don't block
                        break;
                }
            }
            return false;
        }
    }
}
