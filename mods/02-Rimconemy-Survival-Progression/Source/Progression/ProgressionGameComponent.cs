using System;
using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Colonials;
using Rimconemy.Foundation.CrossPackage;
using Rimconemy.Foundation.Registry;
using Rimconemy.SurvivalProgression.Character;
using Rimconemy.SurvivalProgression.Character.Roles;
using Rimconemy.SurvivalProgression.Needs;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression
{
    /// <summary>
    /// Owns package-02 runtime state. It samples vanilla needs and active jobs at
    /// a fixed interval, awards bounded XP, and persists the read model per pawn.
    /// Phase B additions: F-V4 (capability-gate) and F-V2 (sole-owner GameOver).
    /// Perf optimizations (2026-08-04):
    /// - ClassifyJob result cached per pawn (invalidated on job change)
    /// - ResearchCapabilities uses HashSet for O(1) Contains
    /// - RecreationAvailable hoisted out of pawn loop (loop invariant)
    /// </summary>
    public sealed class ProgressionGameComponent : GameComponent
    {
        public const int CurrentSchemaVersion = 1;

        // slop-audit fix F2: 250 ticks = 4.16s of real time; chosen so the
        // runtime stays cheap during long games while still responsive
        // enough for the player to see Progression updates.
        private const int UpdateIntervalTicks = 250;

        // slop-audit fix F2: per-active-job-tick XP award, chosen to make
        // a typical 12-15 minute work day (≈ 60000 ticks) yield ~150 XP.
        private const float ExperiencePerWorkSample = 0.25f;

        // Grace intervals: 12 × 250 ticks = 3,000 ticks ≈ 50 real seconds.
        // Covers caravan travel, transport pod launches and temporary map
        // transitions. slop-audit fix F4: longer than 30s covers all common
        // transport-pod launches; verified against F6 fix from GESAMTREPORT.
        private const int EmptyColonistGraceIntervals = 12;

        // slop-audit-round-4 / BioRemap (2026-08-04): persisted across save-load
        // so we don't re-apply the bio remap on every load (which would
        // idempotently re-write BirthAbsTicks and confuse save-diffing tools).
        // We DO want to re-apply on any non-Rimconemy save (older saves
        // pre-date this code) so the first load after upgrade normalises
        // ages once and then leaves them alone.
        private bool _bioRemapApplied;

        public List<ProgressionSnapshot> Snapshots = new List<ProgressionSnapshot>();
        // HashSet for O(1) Contains instead of O(n) List scan
        public HashSet<string> ResearchCapabilities = new HashSet<string>();
        public bool RecreationAvailable;
        public bool HasObservedPlayerColonist;
        public bool GameOverTriggered;
        public string GameOverReason = "";
        public long LastUpdateTick;
        public int SchemaVersion = CurrentSchemaVersion;
        public BuildingProgressionLedger BuildingAwards = new BuildingProgressionLedger();
        // Phase 8.1 — 7-domain XP hub with diminishing-returns + idempotency.
        // Replaces the legacy BuildingProgressionAward-only path as the
        // primary read-side for organic unlocks. See DomainXpState.cs.
        public DomainXpState DomainXp = new DomainXpState();
        private int _emptyColonistIntervals;
        private bool _lastThreatAvailabilityState;

        // Cache: pawnID -> (lastJobDefName, classifiedDomain)
        // Invalidated when pawn's CurJobDef changes
        private readonly Dictionary<int, (string jobDef, string domain)> _jobClassificationCache
            = new Dictionary<int, (string, string)>();

        private readonly Dictionary<int, ProgressionSnapshot> _byPawnId
            = new Dictionary<int, ProgressionSnapshot>();

        public ProgressionGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SchemaVersion, "survivalProgressionSchema", CurrentSchemaVersion);
            Scribe_Collections.Look(ref Snapshots, "survivalProgressionSnapshots", LookMode.Deep);
            // ResearchCapabilities is now a HashSet - serialize as list for Scribe
            var researchList = ResearchCapabilities?.ToList() ?? new List<string>();
            Scribe_Collections.Look(ref researchList, "survivalResearchCapabilities", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ResearchCapabilities = new HashSet<string>(researchList);
            }
            Scribe_Values.Look(ref RecreationAvailable, "recreationAvailable", false);
            Scribe_Values.Look(ref HasObservedPlayerColonist, "hasObservedPlayerColonist", false);
            Scribe_Values.Look(ref GameOverTriggered, "gameOverTriggered", false);
            Scribe_Values.Look(ref GameOverReason, "gameOverReason", "");
            Scribe_Values.Look(ref LastUpdateTick, "lastUpdateTick", 0L);
            Scribe_Values.Look(ref _emptyColonistIntervals, "emptyColonistIntervals", 0);
            Scribe_Deep.Look(ref BuildingAwards, "buildingProgressionAwards");
            EnsureBuildingAwards();
            // Phase 8.1 — persist the 7-domain XP hub via its own ExposeData.
            Scribe_Deep.Look(ref DomainXp, "rimconemyDomainXp");
            EnsureDomainXp();

            // Bio-Remap (Phase 5 sprint 2026-08-04): persist the bio-remap-applied
            // flag across save loads. Without Scribe persistence the field resets
            // to false on every load, and combined with the previous TicksGame<1000
            // gate (also removed this sprint) the body never re-ran for save-load,
            // which left ages inconsistent on save-load. See audit-round-4 §BioRemap
            // for the regression that motivated this. Setter-only by design: the
            // value is the canonical answer to "has this component run FixAge at
            // least once for the current game".
            Scribe_Values.Look(ref _bioRemapApplied, "bioRemapApplied", false);

            if (Snapshots == null)
                Snapshots = new List<ProgressionSnapshot>();
            if (ResearchCapabilities == null)
                ResearchCapabilities = new HashSet<string>();
            if (SchemaVersion < CurrentSchemaVersion)
            {
                SchemaVersion = CurrentSchemaVersion;
                Log.Message("[Rimconemy.SurvivalProgression] Progression save migrated to schema v1.");
            }

            // Bio-Remap alloc reset on load (Phase-5 audit-round-4 fix, 2026-08-04):
            // StoredBudgetAllocations is a static field that survives across save-lo
            // within the same process. Without this reset, loading a different
            // save (or even the same save) would skip the SkillBudgetWindow openi
            // because Allocations != null was already true from a previous session.
            // Reset on load forces the window open with the defaults (cost-aware
            // equal distribution), letting the player re-confirm or re-tune per-
            // session. PostLoadInit runs AFTER Scribe reads its values, so this
            // is the right hook.
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RebuildIndex();
                Character.StoredBudgetAllocations.Allocations = null;
            }
        }

        public void EnsureBuildingAwards()
        {
            if (BuildingAwards == null) BuildingAwards = new BuildingProgressionLedger();
        }

        /// <summary>
        /// Phase 8.1 — accessor for the DomainXp hub. The Harmony postfix in
        /// <see cref="Hooks.FrameCompletionPatch"/> reaches into this via
        /// <c>component.EnsureDomainXp()</c>; tests reach for the field
        /// directly. Always returns a non-null reference.
        /// </summary>
        public DomainXpState EnsureDomainXp()
        {
            if (DomainXp == null) DomainXp = new DomainXpState();
            return DomainXp;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            RebuildIndex();
            Tests.ScenarioContractTests.RunAll();

            // H6/H7 + Phase-5 Bio-Remap audit-round-4 fix (2026-08-04):
            // Run on EVERY FinalizeInit (fresh game AND save-load), idempotent.
            // The original gate (TicksGame < 1000 && !_characterSetupApplied)
            // failed two scenarios:
            //   (a) Save-load: TicksGame >= 1000 → body skipped, ages inconsistent
            //       on every load of a save predating the fix.
            //   (b) Same-game-state-re-entry: _characterSetupApplied was a
            //       non-Scribed bool → false after load, but Two-TickGame<1000
            //       gate still blocked.
            // The new behaviour: TryApplyBioRemap() is idempotent (FixAge checks
            // AgeBiologicalYears != target before touching BirthAbsTicks), so
            // re-entry is cheap and safe. The window opens on a separate,
            // already-tested LongEventHandler gate so the window can appear AFTER
            // worldgen completion regardless of TicksGame state.
            TryApplyBioRemap();
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (StoredBudgetAllocations.Allocations == null)
                {
                    Find.WindowStack.Add(new Character.SkillBudgetWindow());
                }
            });
        }

        /// <summary>
        /// Phase 5 Bio-Remap shot (2026-08-04): a single canonical entry point
        /// that runs <see cref="CharacterSetup.FixAllStartingPawnsAge"/> once
        /// per game-session and reports anything it changed. Safe to call from
        /// FinalizeInit AND from a defensive catch-up tick on save-load.
        /// </summary>
        private void TryApplyBioRemap(bool forceReapply = false)
        {
            if (_bioRemapApplied && !forceReapply)
                return;

            if (Current.Game == null || Find.TickManager == null)
                return;

            // Defer to a sub-method that counts changes so we can log.
            int changed = CharacterSetup.ApplyAndCountAgeChanges();
            _bioRemapApplied = true;

            if (changed > 0)
            {
                Log.Message($"[Rimconemy.SurvivalProgression] Bio-Remap: FixAge normalised {changed} starting colonist(s) to age {CharacterSetup.FixedBiologicalAge}.");
            }
            else
            {
                Log.Message("[Rimconemy.SurvivalProgression] Bio-Remap: no changes needed; all starting colonists already at age 18.");
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            // Phase-5 Bio-Remap defensive catch-up (2026-08-04): if FinalizeInit
            // was somehow bypassed (e.g. very-early save-load race), the first
            // tick within the first 1000 ticks still touches the bio-remap
            // path so older saves get normalised exactly once per session.
            // After _bioRemapApplied flips true this branch is a no-op.
            if (!_bioRemapApplied && Find.TickManager != null
                && Find.TickManager.TicksGame < 1000L)
            {
                TryApplyBioRemap();
            }

            if (Find.TickManager == null || Find.TickManager.TicksGame < LastUpdateTick + UpdateIntervalTicks)
                return;

            LastUpdateTick = Find.TickManager.TicksGame;
            UpdateRuntimeState();
        }

        public ProgressionSnapshot GetSnapshot(Pawn pawn)
        {
            if (pawn == null)
                return null;

            _byPawnId.TryGetValue(pawn.thingIDNumber, out var snapshot);
            return snapshot;
        }

        public IReadOnlyList<ProgressionSnapshot> GetSnapshots()
        {
            return Snapshots;
        }

        private void UpdateRuntimeState()
        {
            // Phase B / F-V1: delegate colonist enumeration to ColonialReader
            // (Foundation). Removes the 5-site DRY violation flagged on 2026-08-04
            // and aligns the filter (IsColonist + !Dead + !DestroyedOrNull +
            // Humanlike + dedup) across Mod 02 / 03 / 05.
            var colonists = ColonialReader.GetActiveColonists();

            if (colonists.Count > 0)
            {
                HasObservedPlayerColonist = true;
                _emptyColonistIntervals = 0;
            }
            else if (HasObservedPlayerColonist)
            {
                // Map transitions, caravans and transport pods can temporarily
                // remove every colonist from Find.Maps. Require several samples
                // before declaring a real loss of the player colony.
                _emptyColonistIntervals++;
            }

            UpdateResearchCapabilities();

            // Finding 5: RecreationAvailable is loop-invariant — compute once before the pawn loop.
            RecreationAvailable = NeedMappingService.Get(NeedMappingService.SocialSetting) != null
                && NeedMappingService.Get(NeedMappingService.SocialSetting).Sources.Count > 0;

            foreach (var pawn in colonists)
                UpdatePawn(pawn);

            // Phase B — F-V4: capability-gate observation (was misnamed
            // 'TryApplyThreatDrivenXpBoost'). The actual multiplier is not
            // applied yet; the method logs gate transitions once. slop-audit-
            // fix E1: renamed to make the log-only behaviour explicit.
            ObserveThreatMultiplierAvailability(colonists);

            if (HasObservedPlayerColonist
                && _emptyColonistIntervals >= EmptyColonistGraceIntervals
                && !GameOverTriggered)
            {
                // Track 2-C / S-T2 — Sandbox-mode short-circuit.
                var mode = GameOver.GameOverDetector.CurrentMode;
                if (mode == GameOver.GameOverMode.Sandbox)
                {
                    // Sandbox: do not trigger GameOver. Log once so the operator sees
                    // the suppressed condition. Player keeps the colony alive until
                    // they manually load or end the run.
                    if (!_sandboxSuppressionLogged)
                    {
                        Log.Message("[Rimconemy.SurvivalProgression] Sandbox-Mode active: colony-wipe detected; automatic game-over suppressed. Player can continue.");
                        _sandboxSuppressionLogged = true;
                    }
                    _emptyColonistIntervals = 0; // reset grace so we don't spam log
                    return;
                }

                GameOverTriggered = true;

                // F-V2: Sole-Owner GameOver. We pull the canonical reason from
                // Mod 05 first via the late-bound reflection bridge
                // (CrossPackageState). The capability gate inside the bridge
                // ensures no reflection work runs unless Mod 05 is registered.
                string storyReason = null;
                if (CrossPackageState.TryReadStoryGameOverPending(out storyReason) && !string.IsNullOrEmpty(storyReason))
                    GameOverReason = storyReason;
                else
                    GameOverReason = GameOver.GameOverDetector.ReasonOutOfColonists;

                Log.Warning($"[Rimconemy.SurvivalProgression] Game Over (Sole-Owner trigger): {GameOverReason}");

                // Sole-Owner: we are the unique callsite for CheckOrUpdateGameOver.
                Find.GameEnder?.CheckOrUpdateGameOver();
            }
        }

        // Track 2-C / S-T2 — Sandbox suppression log gate.
        private bool _sandboxSuppressionLogged;

        /// <summary>
        /// Phase B Sprint — F-V4: capability-gated observation. slop-audit-fix
        /// E1 renamed the method to make the log-only nature explicit.
        /// This method does NOT apply any XP boost today - it just records
        /// gate transitions so the operator sees, in the log, when Mod 05
        /// becomes available. When the threat-driven multiplier becomes a
        /// real computation, this method is the place to compute it.
        /// </summary>
        private void ObserveThreatMultiplierAvailability(List<Pawn> colonists)
        {
            if (colonists == null || colonists.Count == 0) return;

            bool available = CapabilityAudit.HasCapabilityOrWarn(
                packageId: "rimconemy.infectedautomation",
                capabilityId: "rimconemy.infectedautomation.threat",
                minVersion: 1,
                readerContext: "Progression-XP-Multiplier");

            if (available != _lastThreatAvailabilityState)
            {
                // Edge-triggered log so the operator sees gate changes once.
                Log.Message(
                    "[Rimconemy.SurvivalProgression] Threat-Multiplier availability: " +
                    (available ? "ON (Mod 05 active)" : "OFF (Mod 05 inactive)") + ".");
                _lastThreatAvailabilityState = available;
            }
        }

        private void UpdatePawn(Pawn pawn)
        {
            if (!_byPawnId.TryGetValue(pawn.thingIDNumber, out var snapshot))
            {
                snapshot = new ProgressionSnapshot
                {
                    PawnId = pawn.thingIDNumber,
                    PawnLabel = pawn.LabelShortCap
                };
                Snapshots.Add(snapshot);
                _byPawnId[pawn.thingIDNumber] = snapshot;
            }

            snapshot.PawnLabel = pawn.LabelShortCap;
            EnsureNeedAmplifier(pawn);
            // Track 2-C / S-T1: sample through NeedMappingService. The
            // Setting Needdefs (Rimconemy_Need_Food/Safety/Social) are NOT
            // attached to pawns - they are documented identities. The
            // service reads Vanilla Food / Rest / Recreation and projects
            // them onto the Setting 0..1 scale. RecreationAvailable is
            // derived from whether the mapping found a usable source.
            snapshot.NeedFoodLevel = NeedMappingService.SampleByName(pawn, NeedMappingService.FoodSetting);
            snapshot.NeedSafetyLevel = NeedMappingService.SampleByName(pawn, NeedMappingService.SafetySetting);
            snapshot.NeedSocialLevel = NeedMappingService.SampleByName(pawn, NeedMappingService.SocialSetting);
            // Finding 5: RecreationAvailable hoisted out of loop — use the pre-computed value.
            snapshot.WorkDomain = ClassifyJobCached(pawn);
            snapshot.FarmingLevel = RoleSkillResolver.SkillOf(pawn, SkillDefOf.Plants);
            snapshot.CookingLevel = RoleSkillResolver.SkillOf(pawn, SkillDefOf.Cooking);
            snapshot.HuntingLevel = RoleSkillResolver.HuntingLevel(pawn);
            snapshot.SmithingLevel = RoleSkillResolver.SmithingLevel(pawn);
            snapshot.IntellectualLevel = RoleSkillResolver.SkillOf(pawn, SkillDefOf.Intellectual);
            snapshot.Efficiency = CalculateEfficiency(snapshot, RecreationAvailable);
            snapshot.LastUpdatedTick = LastUpdateTick;
            UpdateWorkEpisode(snapshot, pawn);
            snapshot.ResearchCapabilities = ResearchCapabilities.ToList();
        }

        private static void EnsureNeedAmplifier(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return;

            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("Rimconemy_NeedAmplifier");
            if (def == null)
                return;

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            Hediff_NeedAmplifier amplifier = existing as Hediff_NeedAmplifier;
            if (amplifier == null)
            {
                amplifier = pawn.health.AddHediff(def) as Hediff_NeedAmplifier;
            }

            if (amplifier != null)
            {
                float targetSeverity = Hediff_NeedAmplifier.SeverityForPawn(pawn);
                if (!Mathf.Approximately(amplifier.Severity, targetSeverity))
                    amplifier.Severity = targetSeverity;
            }
        }

        private static void UpdateWorkEpisode(ProgressionSnapshot snapshot, Pawn pawn)
        {
            string currentJob = pawn.CurJobDef?.defName ?? "";
            bool validWork = !string.IsNullOrEmpty(currentJob)
                && !pawn.Downed
                && !pawn.Dead
                && !IsNonWorkJob(currentJob);

            if (validWork && currentJob == snapshot.CurrentJobDefName)
            {
                snapshot.ActiveJobTicks += UpdateIntervalTicks;
                return;
            }

            if (!string.IsNullOrEmpty(snapshot.CurrentJobDefName)
                && snapshot.ActiveJobTicks >= UpdateIntervalTicks)
            {
                // Commit once when a meaningful job episode ends or changes.
                // This prevents idle polling and duplicate XP for one episode.
                snapshot.CompletedWorkUnits++;
                float baseExperience = ExperiencePerWorkSample
                    * Math.Max(1f, Math.Min(8f, snapshot.ActiveJobTicks / (float)UpdateIntervalTicks));
                int intellectual = RoleSkillResolver.SkillOf(pawn, SkillDefOf.Intellectual);
                snapshot.Experience += RoleSkillResolver.ScaleExperience(baseExperience, intellectual);
            }

            snapshot.CurrentJobDefName = validWork ? currentJob : "";
            snapshot.ActiveJobTicks = validWork ? UpdateIntervalTicks : 0;
        }

        private static bool IsNonWorkJob(string defName)
        {
            return defName.IndexOf("Wait", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Stand", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Goto", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Wander", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateResearchCapabilities()
        {
            if (Find.ResearchManager == null)
                return;

            // HashSet gives O(1) Contains instead of O(n) List scan
            foreach (var project in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (project != null && project.IsFinished && !ResearchCapabilities.Contains(project.defName))
                    ResearchCapabilities.Add(project.defName);
            }
        }

        private static bool IsDirectlyControlledColonist(Pawn pawn)
        {
            // Phase B / F-V1: delegate to ColonialReader so all 5 packages agree
            // on what counts as a 'real' player colonist. The original 4-line
            // filter (IsColonist + !DestroyedOrNull, no Humanlike check) was
            // strict-but-narrow; ColonialReader.IsPlayerColonist also excludes
            // Dead pawns and non-Humanlike, which matches GameOverDetector's intent.
            return ColonialReader.IsPlayerColonist(pawn);
        }

        // Track 2-C / S-T1: read-side helpers ReadNeedPercentage /
        // ResolveRecreationNeed / CalculateSafety have been replaced by
        // Rimconemy.SurvivalProgression.Needs.NeedMappingService. The
        // Setting Needdefs project Vanilla Food / Rest / Recreation onto
        // a 0..1 scale via the mapping catalog.

        private static float CalculateEfficiency(ProgressionSnapshot snapshot, bool recreationAvailable)
        {
            float needs = snapshot.NeedFoodLevel + snapshot.NeedSafetyLevel;
            float needCount = 2f;
            if (recreationAvailable)
            {
                needs += snapshot.NeedSocialLevel;
                needCount += 1f;
            }

            float averageNeeds = needs / needCount;
            return 0.5f + averageNeeds * 0.75f + Math.Min(snapshot.Experience / 1000f, 0.25f);
        }

        private static string ClassifyJob(Pawn pawn)
        {
            if (pawn == null) return "Idle";

            // Track 2-C / S-T5: WorkTypeDef + WorkTags based classification.
            //
            // The original substring match (FIXME-F2) was fragile because RimWorld
            // renames internal job defs across patches. RimWorld 1.6 does NOT
            // expose a JobDef.workgiver property; instead WorkGiverDef owns the
            // JobDef list. Easiest stable mapping: pick the pawn's
            // highest-priority assigned WorkTypeDef via pawn.workSettings and
            // map WorkType.workTags (a flag enum) to a stable domain string.
            //
            // Final fallback: "Idle" / "Other".

            WorkTypeDef activeWorkType = null;
            if (pawn.workSettings != null)
            {
                WorkTypeDef best = null;
                int bestPriority = int.MinValue;
                foreach (var wt in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    if (wt == null) continue;
                    int priority = pawn.workSettings.GetPriority(wt);
                    if (priority > bestPriority)
                    {
                        bestPriority = priority;
                        best = wt;
                    }
                }
                activeWorkType = best;
            }

            if (activeWorkType == null)
                return "Idle";

            WorkTags tags = activeWorkType.workTags;
            if ((tags & WorkTags.Caring) != 0) return "Medical";
            if ((tags & WorkTags.Violent) != 0) return "Combat";
            if ((tags & WorkTags.Intellectual) != 0) return "Research";
            if ((tags & WorkTags.Firefighting) != 0) return "Defense";
            if ((tags & WorkTags.Cooking) != 0) return "Cooking";
            if ((tags & WorkTags.Hauling) != 0) return "Scavenging";
            if ((tags & WorkTags.Cleaning) != 0) return "Scavenging";
            if ((tags & WorkTags.Social) != 0) return "Social/Trade";
            if ((tags & WorkTags.Artistic) != 0) return "Crafting";
            if ((tags & WorkTags.PlantWork) != 0) return "Farming";
            if ((tags & WorkTags.Mining) != 0) return "Scavenging";
            if ((tags & WorkTags.Constructing) != 0) return "Building";
            if ((tags & WorkTags.Shooting) != 0) return "Combat";

            // Fallback: by WorkTypeDef defName family (still stable across
            // RimWorld patches because these prefixes are stable naming conventions).
            string n = activeWorkType.defName ?? "";
            if (n.StartsWith("Plant", StringComparison.Ordinal) || n.StartsWith("Harvest", StringComparison.Ordinal)
                || n.IndexOf("farm", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Farming";
            if (n.StartsWith("Build", StringComparison.Ordinal) || n.StartsWith("Construct", StringComparison.Ordinal))
                return "Building";
            if (n.StartsWith("Haul", StringComparison.Ordinal) || n.StartsWith("Mine", StringComparison.Ordinal))
                return "Scavenging";
            if (n.StartsWith("Repair", StringComparison.Ordinal))
                return "Building";

            return "Other";
        }

        /// <summary>
        /// Cached wrapper around ClassifyJob. Invalidates cache when pawn's
        /// CurJobDef changes. Finding 4: avoids O(WorkTypeDefs) scan per pawn
        /// per 250-tick block when job hasn't changed.
        /// </summary>
        private string ClassifyJobCached(Pawn pawn)
        {
            if (pawn == null) return "Idle";
            string currentJobDef = pawn.CurJobDef?.defName ?? "";
            int pawnId = pawn.thingIDNumber;

            if (_jobClassificationCache.TryGetValue(pawnId, out var cached))
            {
                if (cached.jobDef == currentJobDef)
                    return cached.domain;
            }

            string domain = ClassifyJob(pawn);
            _jobClassificationCache[pawnId] = (currentJobDef, domain);
            return domain;
        }

        private void RebuildIndex()
        {
            _byPawnId.Clear();
            if (Snapshots == null)
                Snapshots = new List<ProgressionSnapshot>();

            var uniqueSnapshots = new List<ProgressionSnapshot>(Snapshots.Count);
            var seenPawnIds = new HashSet<int>();
            foreach (var snapshot in Snapshots)
            {
                if (snapshot != null
                    && snapshot.PawnId != 0
                    && seenPawnIds.Add(snapshot.PawnId))
                {
                    uniqueSnapshots.Add(snapshot);
                    _byPawnId[snapshot.PawnId] = snapshot;
                }
            }

            Snapshots = uniqueSnapshots;
        }
    }
}
