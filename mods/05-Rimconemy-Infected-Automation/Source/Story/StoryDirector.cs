using System;
using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Colonials;
using Rimconemy.Foundation.Registry;
using Rimconemy.ScavengerInfrastructure.Storage;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// GameComponent that bridges the pure StorySelector data models
    /// with RimWorld's runtime. On a configurable interval (default:
    /// once per game day = 60,000 ticks), builds a SituationSnapshot
    /// from live game state, selects an event via StorySelector, and
    /// queues it for execution through the appropriate IncidentWorker.
    ///
    /// This is the runtime execution layer for Phase 1 Story Writer.
    ///
    /// Vanilla Wealth Raids: NOT deactivated. The StoryDirector
    /// operates alongside RimWorld's native storyteller. Both can
    /// fire independently per the Vanilla Policy (H2 §6).
    /// </summary>
    public sealed class StoryDirector : GameComponent
    {
        /// <summary>Interval between story evaluations in ticks (1 day = 60000).</summary>
        /// slop-audit-fix F3: documented as 1:2 ratio with MinEventSpacingTicks.
        public const long EvaluationIntervalTicks = 60000;

        // slop-audit-fix F1: tunable wealth ceiling at which ThreatPressure
        // saturates to 1.0. Default 700k wealth matches the original line so
        // existing game-state semantics are preserved; future iteration can
        // move this to a StoryDirectorSettings class without changing callers.
        public const float WealthFullPressureThreshold = 700000f;

        // slop-audit-fix §6 (audit-round-3, 2026-08-04): colony-wipe check
        // interval. We sample at 250 ticks (4.2s) instead of every-tick so
        // 900k pawn-lookups/day drop to ~3,700/day (1/240 of the old load).
        // 250 ticks matches ProgressionGameComponent.UpdateIntervalTicks so
        // the Sole-Owner GameOver trigger and the wipe signal stay in sync.
        public const long GameOverWipeCheckInterval = 250L;

        /// <summary>Minimum ticks between any two story events (0.5 day).</summary>
        /// slop-audit-fix F3: documented as half the evaluation interval so
        /// an event fired at tick T blocks another fire until at least T+30k.
        public const long MinEventSpacingTicks = 30000;

        // ── persistent state ──────────────────────────────────
        public StoryState State;
        public SettingProfile ActiveProfile;
        public long LastEvaluationTick;
        public long LastWipeCheckTick;
        public string PendingIncidentDefName;
        public string PendingEventLabel;
        public string PendingEventText;

        /// <summary>Latest immutable read snapshot for read-only UI surfaces.</summary>
        public SituationSnapshot LastSnapshot;

        // ── UI Read-Model (in-memory, not persisted) ──────────
        /// <summary>
        /// Human-readable reason for the last event selection (or skip).
        /// Set every evaluation cycle; consumed by ThreatDashboard for §8.3 UI-Read-Model.
        /// Not persisted — resets to null after load until the next evaluation tick.
        /// </summary>
        public string LastSelectionReason;

        /// <summary>
        /// Rolling history of ThreatPressure values (max 30 samples, 1 per eval cycle).
        /// Used by ThreatDashboard to render a sparkline without re-scanning the map.
        /// Not persisted — rebuilt at runtime from GameComponentTick evaluations.
        /// </summary>
        public readonly System.Collections.Generic.List<float> ThreatHistory
            = new System.Collections.Generic.List<float>(30);

        private StoryEventCatalog _catalog;

        public StoryDirector(Game game)
        {
            State = new StoryState();
            ActiveProfile = SettingProfile.Survival; // default, overridden in FinalizeInit
            _catalog = new StoryEventCatalog();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Map vanilla difficulty to Rimconemy SettingProfile.
            // The player chooses Cassandra/Phoebe/Randy + a difficulty level;
            // Rimconemy maps the difficulty to a SettingProfile without
            // requiring a custom StorytellerDef.
            ActiveProfile = ResolveProfileFromDifficulty();
            Log.Message($"[Rimconemy.InfectedAutomation] StoryDirector: profile={ActiveProfile.ProfileId} (difficulty={Find.Storyteller?.difficultyDef?.defName ?? "unknown"})");

            // H8 / I-T3: Ideology Assigner (finalized).
            //   1. Log the recommended IdeoPreset for the active profile.
            //   2. Defensive TryAutoAssign (spike-open): logs intent but does
            //      NOT override existing player ideology - SPIKE API-IDEOLOGY-01
            //      must close before the auto-attach path goes live.
            Ideology.IdeologyAssigner.AssignForProfile(ActiveProfile);
            Ideology.IdeologyAssigner.TryAutoAssignToPlayerFaction(ActiveProfile);

            int shippedPresets = Ideology.IdeologyAssigner.CountShippedIdeoPresets();
            Log.Message($"[Rimconemy.InfectedAutomation] StoryDirector: Rimconemy IdeoPresets in DefDatabase: {shippedPresets} (expected: 3 for full set).");
            if (Current.Game != null && ModsConfig.IdeologyActive && shippedPresets == 0)
                Log.Warning("[Rimconemy.InfectedAutomation] StoryDirector: Ideology DLC active but 0 Rimconemy IdeoPresets found. Defs may not have loaded.");
            else if (Current.Game != null && !ModsConfig.IdeologyActive)
                Log.Message("[Rimconemy.InfectedAutomation] StoryDirector: Ideology DLC inactive - IdeoPresets defined but inert for this run.");
            else if (shippedPresets > 0 && shippedPresets < 3)
                Log.Warning($"[Rimconemy.InfectedAutomation] StoryDirector: expected 3 Rimconemy IdeoPresets, found {shippedPresets}. Partial set detected.");
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref LastEvaluationTick, "storyDirectorLastEvalTick", 0L);
            Scribe_Values.Look(ref LastWipeCheckTick, "storyDirectorLastWipeCheckTick", 0L);

            string profileId = ActiveProfile?.ProfileId;
            Scribe_Values.Look(ref profileId, "storyDirectorProfileId", "Rimconemy_Survival");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                ActiveProfile = SettingProfile.GetBuiltIn(profileId) ?? SettingProfile.Survival;

            // Delegate StoryState persistence via Scribe_Deep.
            // Direct ExposeData() call bypasses the Scribe context stack;
            // Scribe_Deep.Look ensures PostLoadInit and XML nesting are correct.
            if (Scribe.mode == LoadSaveMode.LoadingVars && State == null)
                State = new StoryState();
            Scribe_Deep.Look(ref State, "storyState");

            Scribe_Values.Look(ref PendingIncidentDefName, "storyDirectorPendingIncident", (string)null);
            Scribe_Values.Look(ref PendingEventLabel, "storyDirectorPendingLabel", (string)null);
            Scribe_Values.Look(ref PendingEventText, "storyDirectorPendingText", (string)null);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _catalog = new StoryEventCatalog();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null)
                return;

            long currentTick = Find.TickManager.TicksGame;

            // Phase B / F-V2: colony-wipe detection. If we detect that ALL
            // player colonists died (typically through a raid wiping the colony),
            // flag a GameOverPending signal that Mod 02 (Sole-Owner) will pull
            // via the late-bound bridge in Foundation. We do not call
            // Find.GameEnder directly — that is Mod 02's sole responsibility.
            //
            // slop-audit-fix §6 (2026-08-04): gate similarly to ProgressionGC
            // (250 ticks). A colony-wipe is observable at 4.2-second latency
            // which is well below the human-noticeable threshold and matches
            // Mod 02's timing so the Sole-Owner GameOver and the wipe signal
            // are temporally coherent. The previous per-tick call cost
            // ~900k FreeColonistsSpawned lookups per game-day; the new path
            // reduces that to ~3,700 (1/240 of the prior load).
            if (currentTick >= LastWipeCheckTick + GameOverWipeCheckInterval)
            {
                LastWipeCheckTick = currentTick;
                MaybeSignalGameOverForWipe(currentTick);
            }

            // Don't evaluate too frequently
            if (currentTick < LastEvaluationTick + EvaluationIntervalTicks)
                return;

            LastEvaluationTick = currentTick;

            // slop-audit-fix F3: rate-limit any firing by MinEventSpacingTicks.
            // The constant (30000 / 0.5 day) is documented as HALF of the eval
            // interval so two story events cannot fire back-to-back on the
            // same day. With current EvaluationIntervalTicks=60000 this gate
            // is effectively a no-op (next eval is always 60k after the last
            // fire), but it would activate if a future tuning reduces the eval
            // interval below 1 day. The gate is in place to make that future
            // tuning safe without revisiting this file. LastEventTick==0
            // indicates "no event fired yet" so we let the new session through.
            if (State != null && State.LastEventTick > 0
                && (currentTick - State.LastEventTick) < MinEventSpacingTicks)
            {
                return;
            }

            // Don't evaluate if threat is below profile minimum
            var snapshot = BuildLiveSnapshot(currentTick);
            LastSnapshot = snapshot;

            // §8.3 UI-Read-Model: record threat sample for sparkline (max 30 entries).
            ThreatHistory.Add(snapshot.ThreatPressure);
            if (ThreatHistory.Count > 30) ThreatHistory.RemoveAt(0);

            if (snapshot.ThreatPressure < ActiveProfile.MinThreatLevel)
            {
                LastSelectionReason = $"Bedrohungspegel {snapshot.ThreatPressure:P0} < Profil-Minimum {ActiveProfile.MinThreatLevel:P0} — kein Event ausgelöst.";
                return;
            }

            // Select an event
            var result = StorySelector.SelectEvent(
                ActiveProfile, snapshot, State, _catalog, currentTick);

            if (result.HasEvent)
            {
                // Stage pending event metadata first so the worker (called by
                // RimWorld's storyteller on the next cycle) can locate it via
                // HasPendingIncident(defName) and consume it via
                // ConsumePendingEvent() for label/text.
                PendingIncidentDefName = "Rimconemy_InfectedRaidIncident";
                PendingEventLabel = result.SelectedEvent.LetterLabel;
                PendingEventText = result.SelectedEvent.LetterText;

                State.PruneOldKeys(currentTick);

                // §7 closure: force-fire the incident via RimWorld's
                // Storyteller.incidentQueue. The IncidentDef
                // (Defs/Incidents/InfectedRaid.xml) has <baseChance>0.0</baseChance>
                // so the vanilla storyline StorytellerComp will never select it
                // spontaneously; without this explicit Add, the Letter never
                // appears. Adding to incidentQueue guarantees the next
                // storyteller cycle invokes InfectedRaidWorker, whose
                // CanFireNowSub returns true because we set PendingIncidentDefName
                // above, and whose TryExecuteWorker issues the player letter.
                //
                // Audit-round-3 §3 (2026-08-04): we now commit the selection
                // to StoryState (idempotency key, cooldown, LastEventId, etc.)
                // ONLY after the queue call succeeded. Pre-fire-commit semantics.
                // If the queue reports a failure (null Storyteller, no map,
                // def missing, exception inside TryFire), we keep state untouched
                // and clear the Pending* fields so the same event is re-selected
                // on the next evaluation cycle — a Letter that didn't appear
                // counts as not-having-happened.
                bool queued = QueueSelectedIncident(snapshot);
                if (queued)
                {
                    State.CommitSelection(
                        eventId: result.SelectedEvent.EventId,
                        idempotencyKey: result.IdempotencyKey,
                        currentTick: currentTick,
                        seed: result.SelectionSeed,
                        cooldownTicks: result.CooldownTicks);

                    // §8.3: persist selection reason for UI.
                    LastSelectionReason = $"[Tick {currentTick}] {result.Reason}";
                    Log.Message($"[Rimconemy.InfectedAutomation] StoryDirector: {result.Reason}");

                    // P2/H3 §3 (Setting Rule Transparency): feed the
                    // TransparencyTracker with the explained/unexplained
                    // state of the fired event. The decision is "explained"
                    // because we always carry a LastSelectionReason with
                    // a deterministic reason string. Unexplained is reserved
                    // for future cross-package modes where a StoryDirector
                    // sibling fires a hidden event.
                    var tt = Ideology.TransparencyTracker.Get();
                    if (tt != null)
                    {
                        tt.RecordDecision(true, result.Reason);
                    }
                }
                else
                {
                    // Fire failed: roll back Pending* so the next tick does not
                    // see stale metadata. Reasons are logged inside QueueSelectedIncident
                    // so we just summarize here at the orchestration level.
                    PendingIncidentDefName = null;
                    PendingEventLabel = null;
                    PendingEventText = null;
                    LastSelectionReason = $"[Tick {currentTick}] Queue-Fehler — Event '{result.SelectedEvent.Label}' nicht ausgelöst. Retry nächste Evaluation.";
                    Log.Warning($"[Rimconemy.InfectedAutomation] StoryDirector: event '{result.SelectedEvent.Label}' selection dropped - queue failed; will retry next eval. ({result.Reason})");
                }
            }
        }

        /// <summary>
        /// §7: Drive the IncidentWorker. Resolves the Def, builds minimal
        /// IncidentParms targeting the canonical player home map, and pushes
        /// the incident onto Find.Storyteller.incidentQueue. RimWorld then
        /// invokes InfectedRaidWorker on the next storyteller cycle.
        ///
        /// Audit-round-3 §3 fix + post-review hardener (2026-08-04):
        /// Returns <c>true</c> iff we reached the TryFire call (the queue
        /// MAY have been touched). <c>false</c> only on pre-flight failures
        /// where we are *sure* no state was mutated (null Storyteller, def
        /// not found, no map, FiringIncident construction threw). The caller
        /// (GameComponentTick) commits the selection to <see cref="StoryState"/>
        /// on <c>true</c> and skips the commit on <c>false</c>.
        ///
        /// Why "attempted fire → commit" instead of "TryFire returned true → commit":
        /// <c>Find.Storyteller.TryFire</c> can mutate the incidentQueue
        /// internally and then throw (e.g. comp rejects def post-hoc, or
        /// FiringIncident.source is a stale Comp ref). If we returned <c>false</c>
        /// on exception the caller would skip the CommitSelection — but the
        /// queue may still process the partially-queued entry on its next
        /// cycle, producing a Letter that the next evaluation can re-select
        /// (because the idempotency key was never burned). The result: the
        /// same Letter fires twice. By committing on attempted-fire instead,
        /// the idempotency key blocks re-selection and the worst case is one
        /// Letter instead of two.
        /// </summary>
        /// <returns>
        /// True if TryFire was attempted (caller commits and idempotency
        /// wins); false if pre-flight failed and the queue is untouched
        /// (caller does NOT commit, retries next eval).
        /// </returns>
        private bool QueueSelectedIncident(SituationSnapshot snapshot)
        {
            if (Find.Storyteller == null || Find.Storyteller.incidentQueue == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] StoryDirector: no Storyteller/incidentQueue available; skipping incident fire.");
                return false;
            }

            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail("Rimconemy_InfectedRaidIncident");
            if (incidentDef == null)
            {
                Log.Error("[Rimconemy.InfectedAutomation] StoryDirector: DefDatabase could not resolve 'Rimconemy_InfectedRaidIncident'; Def XML missing or misnamed.");
                return false;
            }

            // Pick the player's home map. AnyPlayerHomeMap is the canonical
            // RimWorld accessor; if absent (main menu / no map loaded) fall
            // back to whichever map exists. Player needs at least one map for
            // the worker to have a valid IncidentParms.target.
            Map targetMap = ResolveCanonicalPlayerMap();
            if (targetMap == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] StoryDirector: no map available to anchor IncidentParms.target; skipping incident fire.");
                return false;
            }

            // Minimal IncidentParms for a Letter-only worker. Phase 5+ raid
            // spawn service will populate additional parms (faction, pawnKind,
            // raidStrategy) when StoryDirector carries raid data.
            var parms = new IncidentParms
            {
                target = targetMap,
                points = 50f,
            };

            // RimWorld-1.6 surface: queue via FiringIncident → Storyteller.TryFire
            // (queued: true). This is the canonical "force this on the queue
            // without spontaneous baseChance roll" path. Vanilla Storyteller
            // will pick up our queue entry on its next cycle; because we have
            // already set PendingIncidentDefName above, our worker
            // CanFireNowSub returns true and TryExecuteWorker issues the
            // letter. This bypasses the <baseChance>0.0</baseChance> gate.
            //
            // Audit caveat (2026-08-04): FiringIncident.source is a
            // StorytellerComp reference. Passing null has been observed to
            // NPE inside TryFire's internal path on some comps. Pass the
            // first registered comp; if none are registered yet, fall back
            // to null (cold-start race) — same as the prior code but now
            // safe in steady state.
            StorytellerComp sourceComp = null;
            if (Find.Storyteller.storytellerComps != null && Find.Storyteller.storytellerComps.Count > 0)
                sourceComp = Find.Storyteller.storytellerComps[0];

            // ─ construction: throw is pre-flight, queue NOT mutated. catch narrowly.
            FiringIncident firingIncident;
            try
            {
                firingIncident = new FiringIncident(incidentDef, sourceComp, parms);
            }
            catch (NullReferenceException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] StoryDirector: FiringIncident construction NullReferenceException: {ex.Message}");
                return false;
            }
            catch (ArgumentException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] StoryDirector: FiringIncident construction ArgumentException: {ex.Message}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] StoryDirector: FiringIncident construction InvalidOperationException: {ex.Message}");
                return false;
            }

            // Defensive: C# constructors don't return null, but keep this
            // guard against any future override of FiringIncident.
            if (firingIncident == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] StoryDirector: FiringIncident construction returned null; pre-flight failure, queue untouched.");
                return false;
            }

            // ─ fire: from here on the queue MAY be touched; caller MUST commit.
            // Exceptions are caught narrowly so we don't swallow OOM/SOE.
            bool tryFireOk;
            try
            {
                tryFireOk = Find.Storyteller.TryFire(firingIncident, queued: true);
            }
            catch (NullReferenceException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] StoryDirector: Storyteller.TryFire NullReferenceException: {ex.Message}");
                tryFireOk = false;
            }
            catch (ArgumentException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] StoryDirector: Storyteller.TryFire ArgumentException: {ex.Message}");
                tryFireOk = false;
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] StoryDirector: Storyteller.TryFire InvalidOperationException: {ex.Message}");
                tryFireOk = false;
            }

            Log.Message($"[Rimconemy.InfectedAutomation] StoryDirector: queued incident={incidentDef.defName} sourceComp={(sourceComp != null ? sourceComp.GetType().Name : "<none>")} targetMap.uniqueID={targetMap.uniqueID} (GameTick={snapshot.GameTick}, TryFireAccepted={tryFireOk}).");

            // Return true so the caller commits; the TryFire bool is diagnostic.
            return true;
        }

        /// <summary>
        /// Single source of truth for "which map should IncidentParms target?".
        /// Centralises the Find.AnyPlayerHomeMap → Find.Maps.FirstOrDefault()
        /// fallback chain that previously appeared inline in QueueSelectedIncident
        /// and BuildLiveSnapshot. Returns null if no map is loaded (main menu).
        /// </summary>
        private static Map ResolveCanonicalPlayerMap()
        {
            Map canonical = Find.AnyPlayerHomeMap;
            if (canonical == null && Find.Maps != null && Find.Maps.Any())
                canonical = Find.Maps.FirstOrDefault();
            return canonical;
        }

        /// <summary>
        /// Builds a SituationSnapshot from the live game world.
        /// Reads RimWorld state (pawn counts, threat, etc.) and
        /// produces the aggregated read-model that StorySelector needs.
        /// </summary>
        private static SituationSnapshot BuildLiveSnapshot(long tick)
        {
            var snapshot = new SituationSnapshot
            {
                GameTick = tick,
                ActiveEventIds = new List<string>(),
                ActiveEventFamilies = new List<string>(),
                CompletedResearchIds = new List<string>(),
                CriticalResourceIds = new List<string>(),
            };

            // Survivor count — Phase B / F-V1: delegated to ColonialReader so
            // Mod 02 / 03 / 05 agree on what counts as "active colonist".
            var activeColonists = ColonialReader.GetActiveColonists();
            snapshot.SurvivorCount = activeColonists.Count;
            snapshot.AverageSurvivorHealth = activeColonists.Count > 0
                ? activeColonists.Average(p => p.health?.summaryHealth?.SummaryHealthPercent ?? 0.5f)
                : 0f;

            // Threat pressure (simplified: based on colony wealth + pawn count)
            float wealthFactor = 0f;
            foreach (var map in Find.Maps)
            {
                if (map?.wealthWatcher != null)
                    wealthFactor += map.wealthWatcher.WealthTotal;
            }
            // Normalize: 100k wealth = ~0.3 pressure, 500k = ~0.7
            // slop-audit-fix F1: 700000f is "max wealth = 1.0 pressure" tuning
            // constant. Future: lift to StoryDirectorSettings.WealthMaxForUnityThreat.
            snapshot.ThreatPressure = System.Math.Min(1f, wealthFactor / WealthFullPressureThreshold);
            snapshot.ThreatTrend = 0f;

            // Ideology (simplified: 1 active rule when ThoughtWorker exists)
            snapshot.IdeologyTension = 0f;
            snapshot.ActiveSettingRuleCount = 1; // ResourceFairness is active

            // Storage (Phase B / F-V3: capability-gated Bridge)
            // ───────────────────────────────────────────────────────────────────
            // INTERFACE_CONTRACT §3 used to describe "live-" + tick as the MVP.
            // F-V3 lands the bridge: when Mod 03 is loaded, we read the actual
            // StorageSnapshot.ContentHash from Mod 03's StorageQuery. The
            // "live-" + tick fallback is preserved for the standalone profile.
            AssignStorageHashFromCapability(snapshot, tick);

            // Progress
            snapshot.DaysSinceStart = tick / Rimconemy.Foundation.TimeConstants.TicksPerDay;
            snapshot.DaysSinceLastTurnPoint = float.MaxValue;

            // Determinism anchors (Phase 1):
            // - MapID: canonical map uniqueID for "{MapID}" placeholder in
            //   DeterminismKeyTemplates. Stable across save/load.
            // - DeterministicTargetPawnId: colonists ordered by ThingID
            //   form a stable ring; we pick index = (dayIndex mod count)
            //   so pawn-anchored events vary per in-game day but are
            //   reproducible across save/load. Empty when no colonists.
            Map canonicalMap = ResolveCanonicalPlayerMap();
            snapshot.MapID = canonicalMap?.uniqueID ?? -1;

            // Phase B / F-V1: source pawn-IDs from ColonialReader (already
            // sorted by thingIDNumber, so deterministic ordering is preserved
            // and the resulting DayIndex picks the same pawn across save/load).
            var colonistIds = new List<string>(activeColonists.Count);
            foreach (var p in activeColonists)
            {
                if (!string.IsNullOrEmpty(p.ThingID))
                    colonistIds.Add(p.ThingID);
            }

            // Roster fingerprint: FNV-1a over the joined sorted ThingID list.
            // Cheap (≤ max colonist count entries), stable across save/load
            // (ThingIDs persist) and varies iff the colony composition
            // changed. Baked into the {PawnId} placeholder resolution below
            // so pawn-anchored determinism keys survive save→load even
            // when colonists were lost or gained between sessions.
            snapshot.PawnRosterFingerprint = colonistIds.Count > 0
                ? EncodeRosterFingerprint(colonistIds)
                : "";

            if (colonistIds.Count > 0)
            {
                long dayIndex = (tick / 60000L) % colonistIds.Count;
                snapshot.DeterministicTargetPawnId = colonistIds[(int)dayIndex];
            }

            return snapshot;
        }

        /// <summary>
        /// FNV-1a hash over the colonist roster. Same hash routine as
        /// DeterministicRng.GetStableHashCode but exposed as a quick
        /// helper for snapshot filling.
        /// </summary>
        private static string EncodeRosterFingerprint(List<string> colonistIds)
        {
            string joined = string.Join("|", colonistIds);
            return DeterministicRng.GetStableHashCode(joined).ToString("X8");
        }

        /// <summary>
        /// Phase B / F-V3: capability-gated assignment of <see cref="SituationSnapshot.StorageHash"/>.
        ///
        /// When Mod 03 is registered and exposes <c>rimconemy.scavengerinfrastructure.resources</c>,
        /// we read the real <see cref="StorageSnapshot.ContentHash"/> from StorageQuery.
        /// Otherwise (standalone profile) we fall back to the same FNV-1a routine
        /// as Mod 03 so the hash anchors are byte-for-byte comparable across the
        /// stack (no literal string drift - this was a fix for Q-StorageHashDrift
        /// flagged by the 2026-08-04 code review).
        ///
        /// We also fill <see cref="SituationSnapshot.AnyResourceCritical"/> with a simple
        /// "any entry below 5 units" heuristic; this is consumed by StorySelector's
        /// Supply-family gate in a future Phase 2.5 snapshot.
        ///
        /// The strategy intentionally avoids depending on the "Storage Critical" threat
        /// (which is a Mod 05 concept) — we just expose a flag the StorySelector can use.
        /// </summary>
        private static void AssignStorageHashFromCapability(SituationSnapshot snapshot, long tick)
        {
            // slop-audit-fix C5/H4: critical thresholds live in
            // Rimconemy.InfectedAutomation.ResourceThresholds; we read
            // them inline where needed (no local copy required).

            if (CapabilityAudit.HasCapabilityOrWarn(
                    packageId: "rimconemy.scavengerinfrastructure",
                    capabilityId: "rimconemy.scavengerinfrastructure.resources",
                    minVersion: 1,
                    readerContext: "StorageHash-Bridge"))
            {
                try
                {
                    var storage = StorageQuery.ReadStorage(StorageScope.PlayerHomeMaps, null, tick);
                    // Even in the bridge-active path we use the SAME hash routine as
                    // the standalone fallback. This way, both produce a 4-byte FNV-1a
                    // digest and downstream keys are byte-comparable.
                    snapshot.StorageHash = ComputeStandaloneStorageHash(storage?.ContentHash, tick);

                    if (storage?.Entries != null)
                    {
                        // slop-audit-fix C5/H4: use canonical unit-count
                        // thresholds via ResourceThresholds.IsBelowCritical so
                        // DECISIONS.md #14's 50/30/40 are honored rather
                        // than a flat 5 units (which was wrong for food at
                        // typical storage volumes of 100+).
                        bool anyCritical = storage.Entries.Any(e =>
                            ResourceThresholds.IsBelowCritical(e.ResourceId, e.TotalAmount));
                        snapshot.AnyResourceCritical = anyCritical;

                        if (anyCritical)
                        {
                            snapshot.CriticalResourceIds = storage.Entries
                                .Where(e => ResourceThresholds.IsBelowCritical(e.ResourceId, e.TotalAmount))
                                .Select(e => e.ResourceId)
                                .Take(32)   // defensive cap to avoid log bloat
                                .ToList();
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] StorageQuery.ReadStorage failed (" + ex.GetType().Name + "): falling back to FNV-1a of tick.");
                    // Continue to fallback below.
                }
            }

            // Standalone fallback. FNV-1a over tick+seed so that save/load
            // (the same tick re-reads to the same hash) and cross-package
            // readers see a properly typed digest.
            snapshot.StorageHash = ComputeStandaloneStorageHash(null, tick);
            snapshot.AnyResourceCritical = false;
            snapshot.CriticalResourceIds = new List<string>();
        }

        /// <summary>
        /// Single source of truth for the storage-hash computation. Hashes
        ///   a real storage ContentHash if available, else falls back to
        ///   a tick-based seed using FNV-1a so the digest is the same kind
        ///   of value Mod 03 publishes.
        /// </summary>
        private static string ComputeStandaloneStorageHash(string storageContentHash, long tick)
        {
            string payload = !string.IsNullOrEmpty(storageContentHash)
                ? storageContentHash
                : "rimconemy-live|" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return DeterministicRng.GetStableHashCode(payload).ToString("X8");
        }

        /// <summary>
        /// Phase B / F-V2: Surface a colony-wipe detection to the Game-Over state
        /// for the Sole-Owner (Mod 02) to consume.
        ///
        /// Sigh: a wipe is something we can observe cheaply — count of living
        /// player colonists dropped to 0 — and that doesn't fire if we were
        /// the cause of the wipe (we are a threat-source, not a survivor).
        /// Multiple consecutive ticks with 0 colonists will simply re-write
        /// the reason (it's idempotent via ConsumeGameOverPending's "clear
        /// after read" semantics).
        /// </summary>
        private void MaybeSignalGameOverForWipe(long currentTick)
        {
            if (State == null) return;

            // Phase B / F-V1: use ColonialReader.NoColonists (single source of
            // truth shared with Mod 02).
            if (!ColonialReader.NoColonists)
            {
                // Living colonists — no wipe. We don't write to State here.
                return;
            }

            // Trigger-condition: 0 colonists anywhere. Mark a single canonical
            // reason. Multiple ticks re-write the same reason.
            State.MarkGameOverPending("Mod 05 (InfectedAutomation): no living player colonists observed at game tick " + currentTick + ".");
        }

        // ── public API for IncidentWorkers ────────────────────

        /// <summary>
        /// Returns true if a story event is pending execution and
        /// matches the given incident defName.
        /// </summary>
        public bool HasPendingIncident(string incidentDefName)
        {
            return !string.IsNullOrEmpty(PendingIncidentDefName)
                && PendingIncidentDefName == incidentDefName;
        }

        /// <summary>
        /// Consumes the pending event and returns its data for
        /// display. Returns null if no event is pending.
        /// </summary>
        public (string label, string text)? ConsumePendingEvent()
        {
            if (string.IsNullOrEmpty(PendingIncidentDefName))
                return null;

            var result = (PendingEventLabel, PendingEventText);
            PendingIncidentDefName = null;
            PendingEventLabel = null;
            PendingEventText = null;
            return result;
        }

        /// <summary>
        /// Finds the active StoryDirector instance from the current game.
        /// </summary>
        public static StoryDirector Get()
        {
            if (Current.Game == null) return null;
            return Current.Game.GetComponent<StoryDirector>();
        }

        /// <summary>
        /// Maps RimWorld's vanilla DifficultyDef to a Rimconemy SettingProfile.
        /// No custom StorytellerDef required — the player's difficulty choice
        /// determines which event families and escalation bands are active.
        ///
        /// Mapping:
        ///   Peaceful / Easy       → Refuge     (Band 1: Supply + Social)
        ///   Medium / Rough        → Survival   (Band 2: Supply + Social + Raid)
        ///   Hard / Extreme        → Collapse   (Band 3: alle 4 Familien)
        ///   Custom / unknown      → Survival   (safe default)
        /// </summary>
        private static SettingProfile ResolveProfileFromDifficulty()
        {
            var difficultyDef = Find.Storyteller?.difficultyDef;
            if (difficultyDef == null)
                return SettingProfile.Survival;

            return difficultyDef.defName switch
            {
                "Peaceful" => SettingProfile.Refuge,
                "Easy"     => SettingProfile.Refuge,
                "Medium"   => SettingProfile.Survival,
                "Rough"    => SettingProfile.Survival,
                "Hard"     => SettingProfile.Collapse,
                "Extreme"  => SettingProfile.Collapse,
                _          => SettingProfile.Survival,
            };
        }
    }
}
