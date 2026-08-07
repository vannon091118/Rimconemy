using System;
using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Colonials;
using Rimconemy.Foundation.Maps;
using Rimconemy.Foundation.Registry;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.ScavengerInfrastructure.Storage;
using RimWorld;
using Verse;

using RandomInoculationService = Rimconemy.InfectedAutomation.Inoculation.RandomInoculationService;
using AnimalInfectionChance   = Rimconemy.InfectedAutomation.Inoculation.AnimalInfectionChance;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    ///
    /// RimconemyStorytellerComp — the evaluation engine registered via
    /// <c>Rimconemy_Storyteller.xml</c>. Replaces the vanilla storyteller
    /// cycle. Contains the core evaluation pipeline migrated from
    /// StoryDirector.
    ///
    /// Architecture:
    ///   - BuildLiveSnapshot  → static utility (reads game state)
    ///   - EvaluateWithSnapshot → instance method (runs StorySelector + fires)
    ///   - QueueSelectedIncident → instance method (TryFire wrapper)
    ///   - DailyEvaluate() → main entry point called by StoryDirector
    ///
    /// Design: DECISIONS §34 (korrigiert), STORYTELLER_ANALYSIS.md,
    ///         STORYTELLER_DESIGN_DECISIONS.md
    /// </summary>
    public class RimconemyStorytellerComp : StorytellerComp
    {
        // ── Constants ─────────────────────────────────────────
        public const long EvaluationIntervalTicks = 60000;
        public const float WealthFullPressureThreshold = 700000f;
        public const long GameOverWipeCheckInterval = 250L;
        public const long MinEventSpacingTicks = 30000;

        private bool _bootstrapEmitted;
        private StoryEventCatalog _catalog;

        public RimconemyStorytellerComp()
        {
            _catalog = new StoryEventCatalog();
        }

        /// <summary>
        /// Rebuild the catalog (called after ExposeData loads).
        /// </summary>
        public void RebuildCatalog()
        {
            _catalog = new StoryEventCatalog();
        }

        // ── Bootstrap ─────────────────────────────────────────

        /// <summary>
        /// Logs bootstrap info exactly once per game session.
        /// </summary>
        public void EmitBootstrapIfNeeded()
        {
            if (_bootstrapEmitted) return;
            _bootstrapEmitted = true;

            var director = StoryDirector.Get();
            string profileId = director?.ActiveProfile?.ProfileId ?? "unknown";

            Log.Message(
                "[Rimconemy.InfectedAutomation] RimconemyStorytellerComp active. " +
                "Storyteller=Rimconemy, " +
                "profile=" + profileId + ", " +
                "difficulty=" + (Find.Storyteller?.difficultyDef?.defName ?? "unknown") + ", " +
                "tick=" + (Find.TickManager?.TicksGame ?? 0L));
        }

        // ── Daily Evaluation Entry Point ──────────────────────

        /// <summary>
        /// Full daily evaluation pipeline. Called from StoryDirector.GameComponentTick.
        /// Contains: wipe-check, eval-gate, snapshot build, event selection,
        /// incident queuing, day-growth, revenge recompute, inoculation.
        /// </summary>
        public void DailyEvaluate(StoryDirector director)
        {
            if (Find.TickManager == null) return;

            long currentTick = Find.TickManager.TicksGame;

            // Wipe check
            if (currentTick >= director.LastWipeCheckTick + GameOverWipeCheckInterval)
            {
                director.LastWipeCheckTick = currentTick;
                MaybeSignalGameOverForWipe(director, currentTick);
            }

            // Eval interval gate
            if (currentTick < director.LastEvaluationTick + EvaluationIntervalTicks)
                return;

            director.LastEvaluationTick = currentTick;

            // Min-event-spacing gate
            if (director.State != null && director.State.LastEventTick > 0
                && (currentTick - director.State.LastEventTick) < MinEventSpacingTicks)
                return;

            // Build snapshot and evaluate
            var snapshot = BuildLiveSnapshot(currentTick, director.State, director.ActiveProfile);
            EvaluateWithSnapshot(director, snapshot, currentTick);

            // Day-Growth + Reset + Recompute-Revenge
            try
            {
                var ledger = PopulationLedger.Get();
                if (ledger != null)
                {
                    ledger.ApplyDailyGrowthTick();
                    ledger.ResetDailyCounters();
                }
                director.RecomputeRevengeAfterDayTick(ledger, director.ActiveProfile, currentTick);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    "Day-Growth/Reset/Recompute block raised " +
                    ex.GetType().Name + ": " + ex.Message);
            }

            // Inoculation
            Map playerHomeForInoculation = ResolveCanonicalPlayerMap();
            if (playerHomeForInoculation != null)
            {
                RandomInoculationService.TryInfectRandom(playerHomeForInoculation, currentTick);
                TryFireProfileInfection(director, currentTick);
            }
        }

        // ── Snapshot Building ─────────────────────────────────

        /// <summary>
        /// Builds a SituationSnapshot from the live game world.
        /// Static — reads RimWorld state without instance dependency.
        /// </summary>
        public static SituationSnapshot BuildLiveSnapshot(
            long tick, StoryState state = null, SettingProfile profile = null)
        {
            var snapshot = new SituationSnapshot
            {
                GameTick = tick,
                SnapshotUpdatedTick = tick,
                ActiveEventIds = new List<string>(),
                ActiveEventFamilies = new List<string>(),
                CompletedResearchIds = new List<string>(),
                CriticalResourceIds = new List<string>(),
            };

            var activeColonists = ColonialReader.GetActiveColonists();
            snapshot.SurvivorCount = activeColonists.Count;
            snapshot.AverageSurvivorHealth = activeColonists.Count > 0
                ? activeColonists.Average(p => p.health?.summaryHealth?.SummaryHealthPercent ?? 0.5f)
                : 0f;

            float wealthFactor = 0f;
            foreach (var map in MapRegistry.GetPlayerHomeMaps())
            {
                if (map?.wealthWatcher != null)
                    wealthFactor += map.wealthWatcher.WealthTotal;
            }
            snapshot.ThreatPressure = System.Math.Min(1f, wealthFactor / WealthFullPressureThreshold);

            if (profile != null)
            {
                float floor = profile.ProfileId switch
                {
                    "Rimconemy_Refuge" => 0.05f,
                    "Rimconemy_Survival" => 0.15f,
                    "Rimconemy_Collapse" => 0.10f,
                    _ => 0.10f
                };
                snapshot.ThreatPressure = System.Math.Max(snapshot.ThreatPressure, floor);
            }

            snapshot.ThreatTrend = 0f;
            snapshot.ColonyWealth = wealthFactor;

            snapshot.AverageColonistMood = activeColonists.Count > 0
                ? activeColonists.Average(p =>
                {
                    var need = p.needs?.mood;
                    return need != null ? need.CurLevel : 0.5f;
                })
                : 0.5f;

            snapshot.PowerGridActive = false;
            foreach (var map in MapRegistry.GetPlayerHomeMaps())
            {
                if (map == null) continue;
                var things = map.listerBuildings?.allBuildingsColonist;
                if (things == null) continue;
                for (int i = 0; i < things.Count; i++)
                {
                    var b = things[i];
                    if (b == null) continue;
                    var comp = b.TryGetComp<CompPowerTrader>();
                    if (comp != null && comp.PowerOn)
                    {
                        snapshot.PowerGridActive = true;
                        break;
                    }
                }
                if (snapshot.PowerGridActive) break;
            }

            snapshot.HostileFactionCount = 0;
            if (Find.FactionManager != null)
            {
                foreach (var faction in Find.FactionManager.AllFactionsListForReading)
                {
                    if (faction == null || faction.IsPlayer) continue;
                    if (faction.HostileTo(Faction.OfPlayer))
                        snapshot.HostileFactionCount++;
                }
            }

            snapshot.ActiveResearchCount = 0;
            snapshot.AnyColonistInjured = activeColonists.Any(p =>
                (p.health?.summaryHealth?.SummaryHealthPercent ?? 0.5f) < 0.6f);

            snapshot.DaysSinceLastEvent = (state != null && state.LastEventTick > 0)
                ? (tick - state.LastEventTick) / (float)Rimconemy.Foundation.TimeConstants.TicksPerDay
                : float.MaxValue;

            snapshot.IdeologyTension = 0f;
            snapshot.ActiveSettingRuleCount = 1;

            AssignStorageHashFromCapability(snapshot, tick);

            snapshot.DaysSinceStart = tick / Rimconemy.Foundation.TimeConstants.TicksPerDay;
            snapshot.DaysSinceLastTurnPoint = float.MaxValue;

            Map canonicalMap = ResolveCanonicalPlayerMap();
            snapshot.MapID = canonicalMap?.uniqueID ?? -1;

            var colonistIds = new List<string>(activeColonists.Count);
            foreach (var p in activeColonists)
            {
                if (!string.IsNullOrEmpty(p.ThingID))
                    colonistIds.Add(p.ThingID);
            }

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

        private static string EncodeRosterFingerprint(List<string> colonistIds)
        {
            string joined = string.Join("|", colonistIds);
            return DeterministicRng.GetStableHashCode(joined).ToString("X8");
        }

        private static void AssignStorageHashFromCapability(SituationSnapshot snapshot, long tick)
        {
            if (CapabilityAudit.HasCapabilityOrWarn(
                    packageId: "rimconemy.scavengerinfrastructure",
                    capabilityId: "rimconemy.scavengerinfrastructure.resources",
                    minVersion: 1,
                    readerContext: "StorageHash-Bridge"))
            {
                try
                {
                    var storage = StorageQuery.ReadStorage(StorageScope.PlayerHomeMaps, null, tick);
                    snapshot.StorageHash = ComputeStandaloneStorageHash(storage?.ContentHash, tick);

                    if (storage?.Entries != null)
                    {
                        bool anyCritical = storage.Entries.Any(e =>
                            ResourceThresholds.IsBelowCritical(e.ResourceId, e.TotalAmount));
                        snapshot.AnyResourceCritical = anyCritical;

                        if (anyCritical)
                        {
                            snapshot.CriticalResourceIds = storage.Entries
                                .Where(e => ResourceThresholds.IsBelowCritical(e.ResourceId, e.TotalAmount))
                                .Select(e => e.ResourceId)
                                .Take(32)
                                .ToList();
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] StorageQuery.ReadStorage failed (" +
                        ex.GetType().Name + "): falling back to FNV-1a of tick.");
                }
            }

            snapshot.StorageHash = ComputeStandaloneStorageHash(null, tick);
            snapshot.AnyResourceCritical = false;
            snapshot.CriticalResourceIds = new List<string>();
        }

        private static string ComputeStandaloneStorageHash(string storageContentHash, long tick)
        {
            string payload = !string.IsNullOrEmpty(storageContentHash)
                ? storageContentHash
                : "rimconemy-live|" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return DeterministicRng.GetStableHashCode(payload).ToString("X8");
        }

        private static Map ResolveCanonicalPlayerMap()
        {
            return MapRegistry.GetPrimaryPlayerHomeMap();
        }

        // ── Evaluation ────────────────────────────────────────

        /// <summary>
        /// Shared evaluation logic: threat history, event selection, incident queuing.
        /// </summary>
        public void EvaluateWithSnapshot(
            StoryDirector director, SituationSnapshot snapshot, long currentTick)
        {
            director.LastSnapshot = snapshot;
            director.ThreatHistory.Add(snapshot.ThreatPressure);
            if (director.ThreatHistory.Count > 30) director.ThreatHistory.RemoveAt(0);

            if (director.ThreatHistory.Count >= 2)
            {
                float sum = 0f;
                for (int i = 0; i < director.ThreatHistory.Count - 1; i++)
                    sum += director.ThreatHistory[i];
                float avg = sum / (director.ThreatHistory.Count - 1);
                float raw = snapshot.ThreatPressure - avg;
                snapshot.ThreatTrend = System.Math.Max(-1f, System.Math.Min(1f, raw * 2f));
            }
            else
            {
                snapshot.ThreatTrend = 0f;
            }

            if (snapshot.ThreatPressure < director.ActiveProfile.MinThreatLevel)
            {
                director.LastSelectionReason =
                    $"Bedrohungspegel {snapshot.ThreatPressure:P0} < Profil-Minimum " +
                    $"{director.ActiveProfile.MinThreatLevel:P0} — kein Event ausgelöst.";
                return;
            }

            var result = StorySelector.SelectEvent(
                director.ActiveProfile, snapshot, director.State, _catalog, currentTick);

            if (result.HasEvent)
            {
                director.PendingIncidentDefName = "Rimconemy_InfectedRaidIncident";
                director.PendingEventLabel = result.SelectedEvent.LetterLabel;
                director.PendingEventText = result.SelectedEvent.LetterText;

                director.State.PruneOldKeys(currentTick);

                bool queued = QueueSelectedIncident(director, snapshot);
                if (queued)
                {
                    director.State.CommitSelection(
                        eventId: result.SelectedEvent.EventId,
                        idempotencyKey: result.IdempotencyKey,
                        currentTick: currentTick,
                        seed: result.SelectionSeed,
                        cooldownTicks: result.CooldownTicks);

                    director.LastSelectionReason = $"[Tick {currentTick}] {result.Reason}";
                    Log.Message($"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: {result.Reason}");

                    var tt = Ideology.TransparencyTracker.Get();
                    if (tt != null)
                    {
                        tt.RecordDecision(true, result.Reason);
                    }
                }
                else
                {
                    director.PendingIncidentDefName = null;
                    director.PendingEventLabel = null;
                    director.PendingEventText = null;
                    director.LastSelectionReason =
                        $"[Tick {currentTick}] Queue-Fehler — " +
                        $"Event '{result.SelectedEvent.Label}' nicht ausgelöst. " +
                        "Retry nächste Evaluation.";
                    Log.Warning(
                        $"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                        $"event '{result.SelectedEvent.Label}' selection dropped - " +
                        $"queue failed; will retry next eval. ({result.Reason})");
                }
            }
        }

        // ── Incident Queuing ──────────────────────────────────

        /// <summary>
        /// Queues the selected incident onto Find.Storyteller.incidentQueue.
        /// Returns true if TryFire was attempted; false on pre-flight failure.
        /// </summary>
        public bool QueueSelectedIncident(StoryDirector director, SituationSnapshot snapshot)
        {
            if (Find.Storyteller == null || Find.Storyteller.incidentQueue == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    "no Storyteller/incidentQueue available; skipping incident fire.");
                return false;
            }

            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail("Rimconemy_InfectedRaidIncident");
            if (incidentDef == null)
            {
                Log.Error("[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    "DefDatabase could not resolve 'Rimconemy_InfectedRaidIncident'.");
                return false;
            }

            Map targetMap = ResolveCanonicalPlayerMap();
            if (targetMap == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    "no map available; skipping incident fire.");
                return false;
            }

            var parms = new IncidentParms
            {
                target = targetMap,
                points = 50f,
            };

            StorytellerComp sourceComp = null;
            if (Find.Storyteller.storytellerComps != null && Find.Storyteller.storytellerComps.Count > 0)
                sourceComp = Find.Storyteller.storytellerComps[0];

            FiringIncident firingIncident;
            try
            {
                firingIncident = new FiringIncident(incidentDef, sourceComp, parms);
            }
            catch (NullReferenceException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    $"FiringIncident NullReferenceException: {ex.Message}");
                return false;
            }
            catch (ArgumentException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    $"FiringIncident ArgumentException: {ex.Message}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    $"FiringIncident InvalidOperationException: {ex.Message}");
                return false;
            }

            if (firingIncident == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    "FiringIncident construction returned null.");
                return false;
            }

            bool tryFireOk;
            try
            {
                tryFireOk = Find.Storyteller.TryFire(firingIncident, queued: true);
            }
            catch (NullReferenceException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    $"TryFire NullReferenceException: {ex.Message}");
                tryFireOk = false;
            }
            catch (ArgumentException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    $"TryFire ArgumentException: {ex.Message}");
                tryFireOk = false;
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                    $"TryFire InvalidOperationException: {ex.Message}");
                tryFireOk = false;
            }

            Log.Message(
                $"[Rimconemy.InfectedAutomation] RimconemyStorytellerComp: " +
                $"queued incident={incidentDef.defName} " +
                $"sourceComp={(sourceComp != null ? sourceComp.GetType().Name : "<none>")} " +
                $"targetMap.uniqueID={targetMap.uniqueID} " +
                $"(GameTick={snapshot.GameTick}, TryFireAccepted={tryFireOk}).");

            return true;
        }

        // ── Helpers ───────────────────────────────────────────

        private void MaybeSignalGameOverForWipe(StoryDirector director, long currentTick)
        {
            if (director.State == null) return;
            bool colonistsPresent = !ColonialReader.NoColonists;
            director.State.MarkGameOverPending(
                "Mod 05 (InfectedAutomation): no living player colonists observed " +
                "at game tick " + currentTick + ".",
                colonistsPresent);
        }

        private void TryFireProfileInfection(StoryDirector director, long currentTick)
        {
            var ledger = PopulationLedger.Get();
            if (ledger == null || director.ActiveProfile == null) return;

            int hordeCount = System.Math.Max(
                0, ledger.HumanoidLiveCount + ledger.AnimalLiveCount / 2);
            if (!AnimalInfectionChance.ShouldFireToday(
                    currentTick, ledger.AnimalInfectionCountToday, hordeCount,
                    director.ActiveProfile))
                return;

            int count = AnimalInfectionChance.ComputeInfectionCount(
                currentTick, hordeCount, director.ActiveProfile);
            if (count <= 0) return;

            int actually = RandomInoculationService.TryInfectWildAnimals(count, currentTick);
            if (actually > 0)
            {
                ledger.RegisterAnimalInfection(actually, currentTick);
                Log.Message(
                    "[Rimconemy.InfectedAutomation] RimconemyStorytellerComp." +
                    "TryFireProfileInfection: " +
                    actually + " wild animals infected at tick=" + currentTick +
                    " profile=" + director.ActiveProfile.ProfileId +
                    " hordeCount=" + hordeCount +
                    " ledger.AnimalInfectionCountToday=" +
                    ledger.AnimalInfectionCountToday);
            }
        }
    }
}
