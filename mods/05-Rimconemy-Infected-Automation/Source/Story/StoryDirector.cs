using System;
using System.Collections.Generic;
using Rimconemy.Foundation.Colonials;
using Rimconemy.Foundation.Maps;
using Rimconemy.InfectedAutomation.Population;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    ///
    /// GameComponent that owns the runtime state (StoryState, SettingProfile,
    /// pending incident metadata, revenge slots) and delegates the evaluation
    /// pipeline to <see cref="RimconemyStorytellerComp"/>.
    ///
    /// Design: DECISIONS §34 (korrigiert), STORYTELLER_ANALYSIS.md.
    /// </summary>
    public sealed class StoryDirector : GameComponent
    {
        // ── Constants ─────────────────────────────────────────
        public const long EvaluationIntervalTicks = 60000;
        public const float WealthFullPressureThreshold = 700000f;
        public const long GameOverWipeCheckInterval = 250L;
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
        public readonly List<float> ThreatHistory = new List<float>(30);

        // ── Phase B — transient revenge slot (NOT SCRIBED) ──────────
        public int LastPendingRevenge;
        public long LastRevengeRefreshTick;

        private StoryEventCatalog _catalog;
        private RimconemyStorytellerComp _comp;

        public StoryDirector(Game game)
        {
            State = new StoryState();
            ActiveProfile = SettingProfile.Survival;
            _catalog = new StoryEventCatalog();
            _comp = new RimconemyStorytellerComp();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Map vanilla difficulty to Rimconemy SettingProfile via the Comp
            // (Storyteller-centric logic lives on the Comp).
            ActiveProfile = RimconemyStorytellerComp.ResolveProfileFromDifficulty();
            Log.Message($"[Rimconemy.InfectedAutomation] StoryDirector: profile={ActiveProfile.ProfileId} (difficulty={Find.Storyteller?.difficultyDef?.defName ?? "unknown"})");

            // H8 / I-T3: Ideology Assigner (finalized).
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

            if (Scribe.mode == LoadSaveMode.LoadingVars && State == null)
                State = new StoryState();
            Scribe_Deep.Look(ref State, "storyState");

            Scribe_Values.Look(ref PendingIncidentDefName, "storyDirectorPendingIncident", (string)null);
            Scribe_Values.Look(ref PendingEventLabel, "storyDirectorPendingLabel", (string)null);
            Scribe_Values.Look(ref PendingEventText, "storyDirectorPendingText", (string)null);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _catalog = new StoryEventCatalog();
                if (_comp == null) _comp = new RimconemyStorytellerComp();
                _comp.RebuildCatalog();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (_comp == null) return;
            _comp.EmitBootstrapIfNeeded();
            _comp.DailyEvaluate(this);
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
        /// Dev-mode shortcut: immediately runs one evaluation cycle.
        /// Delegates to RimconemyStorytellerComp.
        /// </summary>
        public void EvaluateNow(long currentTick)
        {
            LastEvaluationTick = currentTick - EvaluationIntervalTicks;
            var snapshot = RimconemyStorytellerComp.BuildLiveSnapshot(currentTick, State, ActiveProfile);
            if (_comp != null)
                _comp.EvaluateWithSnapshot(this, snapshot, currentTick);
            Log.Message("[Rimconemy.InfectedAutomation] StoryDirector.EvaluateNow triggered from Dev mode.");
        }

        // ── Phase B — Revenge-Coupling Public API ───────────────────────

        /// <summary>
        /// Read-only accessor for the current day's revenge-pending quota.
        /// </summary>
        public int GetPendingRevengeanceForToday() => LastPendingRevenge;

        /// <summary>
        /// Decrements the revenge-pending slot by the actual number of
        /// pawns spawned in a raid-bridge run.
        /// </summary>
        public void DecrementPendingRevenge(int actuallySpawned)
        {
            if (actuallySpawned <= 0) return;
            LastPendingRevenge = Math.Max(0, LastPendingRevenge - actuallySpawned);
        }

        /// <summary>
        /// Phase B — strips the "Rimconemy_" prefix from profile IDs for
        /// PopulationProfileMultipliers key lookup.
        /// </summary>
        public static string StripRimconemyPrefix(string id)
        {
            if (id == null) return "Survival";
            string trimmed = id.Trim();
            if (trimmed.Length == 0) return "Survival";
            const string prefix = "Rimconemy_";
            return trimmed.StartsWith(prefix) ? trimmed.Substring(prefix.Length) : trimmed;
        }

        /// <summary>
        /// Phase B — recompute the revenge quota at end of day-tick.
        /// Called from RimconemyStorytellerComp.DailyEvaluate.
        /// </summary>
        public void RecomputeRevengeAfterDayTick(
            PopulationLedger ledger, SettingProfile profile, long currentTick)
        {
            if (currentTick == LastRevengeRefreshTick) return;
            if (ledger == null) return;
            LastRevengeRefreshTick = currentTick;
            string key = StripRimconemyPrefix(profile?.ProfileId);
            float ratio = PopulationProfileMultipliers.GetRevengeRatio(key);
            int freeBudgetRaw = ledger.Cap - ledger.HumanoidLiveCount;
            int freeBudget = (int)Math.Min(int.MaxValue, Math.Max(0, freeBudgetRaw));
            int raw = (int)Math.Floor((float)ledger.RecentKillsToday * ratio);
            LastPendingRevenge = Math.Max(0, Math.Min(raw, freeBudget));
        }
    }
}
