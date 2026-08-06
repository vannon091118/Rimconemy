using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Rimconemy.Foundation.Registry;
using Rimconemy.Foundation.UI;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Tutorial Director — manages tutorial steps, triggers, and persistent state.
    /// Owner: Infected & Automation (Package 05).
    ///
    /// UX-Audit 2026-08-06:
    ///   * GameStart feuert NICHT mehr sofort in StartedNewGame, sondern als
    ///     "pending" Trigger über den ersten aktiven Tick. Das Intro-Window
    ///     (forcePause) blockiert Ticks; so erscheint der Willkommens-Schritt
    ///     erst NACH dem Intro — nicht währenddessen im LetterStack.
    ///   * FirstInfectedContact wird proaktiv gepollt (Kolonie-Sichtradius),
    ///     weil der Bridge-Fire bisher von nirgends ausgelöst wurde.
    ///   * LoadedGame öffnet den aktuell offenen Schritt erneut, damit ein
    ///     Save/Load mit offenem Dialog keinen Soft-Lock erzeugt
    ///     (CurrentStepIndex >= 0 blockierte bisher alle Folge-Schritte).
    ///   * SkipAllTutorials() überspringt den Rest der Anleitung.
    /// </summary>
    public class TutorialDirector : GameComponent
    {
        private TutorialState state;
        public TutorialState State => state;
        private List<TutorialStepDef> allSteps;
        private bool triggersRegistered;

        // Pending GameStart: wird vom ersten unpaused Tick konsumiert (nach Intro).
        private bool pendingGameStart;

        // First-Contact-Polling (in-game, kein Letter-Spam).
        private long nextContactPollTick;
        private const long ContactPollIntervalTicks = 60;   // 1s
        private const float ContactProximityCells = 18f;    // Sichtradius-Heuristik

        public TutorialDirector() { }

        // RimWorld 1.6 GameComponent requires the (Game) ctor; Verse.Game.FillComponents
        // calls Activator.CreateInstance(compType, new object[] { this }) — the Game
        // 'this' is passed and only a Game-arg ctor resolves it. Mirrors the canonical
        // pattern used by CollectiveDefenseTracker / TransparencyTracker /
        // RimconemyStartEnemiesLedger in this same package.
        public TutorialDirector(Game game) { }

        private void EnsureInitialized()
        {
            if (state == null) state = new TutorialState();
            if (allSteps == null)
            {
                allSteps = DefDatabase<TutorialStepDef>.AllDefsListForReading
                    .Where(step => step != null)
                    .OrderBy(step => step.priority)
                    .ThenBy(step => step.defName)
                    .ToList();
            }
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            state = new TutorialState();
            allSteps = null;
            pendingGameStart = true;
            nextContactPollTick = 0L;
            EnsureInitialized();
            RegisterTriggers();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            EnsureInitialized();
            RegisterTriggers();
            ReopenCurrentStepIfAny();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref state, "tutorialState");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureInitialized();
                State.MigrateIfNeeded();
            }
        }

        /// <summary>
        /// Tick-Hook: konsumiert den pending GameStart (nach dem Intro, da das
        /// Intro forcePause setzt und Ticks blockiert) und pollt den
        /// FirstInfectedContact innerhalb des Kolonie-Sichtradius.
        /// </summary>
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (state == null) return;

            if (pendingGameStart)
            {
                pendingGameStart = false;
                TryTriggerStep("GameStart");
            }

            if (State.CurrentStepIndex >= 0) return; // one dialog at a time

            long tick = Find.TickManager?.TicksGame ?? 0L;
            if (tick < nextContactPollTick) return;
            nextContactPollTick = tick + ContactPollIntervalTicks;

            if (State.CompletedSteps == null || State.CompletedSteps.Contains("Tutorial_FirstContact")) return;
            // Prereq: Campfire muss gebaut sein, bevor „Sie sind hier" sinnvoll ist.
            if (!State.CompletedSteps.Contains("Tutorial_Campfire")) return;

            if (AnyInfectedVisibleToColonist())
                TryTriggerStep("FirstInfectedContact");
        }

        private void RegisterTriggers()
        {
            if (triggersRegistered) return;

            var bridge = Scenarios.TutorialTriggerBridge.Instance as ITutorialTriggerBridge;
            if (bridge == null) return;

            bridge.OnCampfireBuilt += OnCampfireBuilt;
            bridge.OnFirstInfectedContact += OnFirstInfectedContact;
            bridge.OnWallBuilt += OnWallBuilt;
            bridge.OnResourceCollected += OnResourceCollected;
            triggersRegistered = true;
        }

        private void OnCampfireBuilt() => TryTriggerStep("CampfireBuilt");
        private void OnFirstInfectedContact() => TryTriggerStep("FirstInfectedContact");
        private void OnWallBuilt() => TryTriggerStep("WallBuilt");

        private void OnResourceCollected(ThingDef def)
        {
            TryTriggerStep("ResourceCollected");
            if (def != null) TryTriggerStep("ResourceCollected_" + def.defName);
        }

        private void TryTriggerStep(string triggerId)
        {
            EnsureInitialized();
            if (State == null || string.IsNullOrEmpty(triggerId)) return;
            if (State.CurrentStepIndex >= 0) return; // one dialog at a time
            if (State.CompletedSteps == null) State.CompletedSteps = new HashSet<string>();

            var step = SelectTriggeredStep(allSteps, triggerId, State.CompletedSteps);
            if (step == null) return;
            ShowStep(step);
        }

        /// <summary>
        /// Pure, testbare Schritt-Auswahl: liefert den ersten Schritt, dessen
        /// Trigger matcht und dessen Voraussetzungen erfüllt sind — oder null.
        /// Kein Game-Zugriff; in Tests ohne Current.Game aufrufbar.
        /// </summary>
        public static TutorialStepDef SelectTriggeredStep(
            List<TutorialStepDef> steps, string triggerId, HashSet<string> completedSteps)
        {
            if (steps == null || string.IsNullOrEmpty(triggerId) || completedSteps == null) return null;
            foreach (var step in steps)
            {
                if (step == null || string.IsNullOrEmpty(step.defName)) continue;
                if (step.trigger != triggerId) continue;
                if (completedSteps.Contains(step.defName)) continue;
                if (step.prerequisiteSteps != null)
                {
                    bool allMet = true;
                    foreach (var p in step.prerequisiteSteps)
                    {
                        if (string.IsNullOrEmpty(p) || !completedSteps.Contains(p))
                        {
                            allMet = false;
                            break;
                        }
                    }
                    if (!allMet) continue;
                }
                return step;
            }
            return null;
        }

        private void ShowStep(TutorialStepDef step)
        {
            var portrait = string.IsNullOrEmpty(step.portraitTexture)
                ? null
                : ContentFinder<Texture2D>.Get(step.portraitTexture, false);
            portrait ??= ContentFinder<Texture2D>.Get("UI/HeroArt/Storytellers/RimconemyLarge", false);

            int stepNumber = allSteps != null ? allSteps.IndexOf(step) + 1 : 0;
            int totalSteps = allSteps?.Count ?? 0;

            var letter = new RimconemyTutorialLetter
            {
                Label = step.letterLabel ?? step.defName,
                Text = step.letterText ?? string.Empty,
                Objective = step.objective,
                def = LetterDefOf.PositiveEvent,
                Portrait = portrait,
                PortraitPath = step.portraitTexture,
                StepId = step.defName,
                UnlockDefs = step.unlockDefs,
                StepNumber = stepNumber,
                TotalSteps = totalSteps
            };

            State.CurrentStepIndex = allSteps.IndexOf(step);
            // Direkt öffnen statt LetterStack: Tutorial muss den Spieler sofort
            // erreichen (LetterStack-Letters können übersehen werden).
            Find.WindowStack.Add(new Dialog_TutorialStep(letter));
        }

        public void MarkStepCompleted(string stepId)
        {
            EnsureInitialized();
            if (State == null || string.IsNullOrEmpty(stepId)) return;
            if (State.CompletedSteps == null) State.CompletedSteps = new HashSet<string>();
            State.CompletedSteps.Add(stepId);
            State.CurrentStepIndex = -1;
        }

        /// <summary>
        /// Überspringt den Rest des Tutorials: alle Schritte als abgeschlossen
        /// markieren + Dismissed-Flag setzen.
        /// </summary>
        public void SkipAllTutorials()
        {
            EnsureInitialized();
            if (State == null || allSteps == null) return;
            if (State.CompletedSteps == null) State.CompletedSteps = new HashSet<string>();
            foreach (var step in allSteps)
            {
                if (step != null && !string.IsNullOrEmpty(step.defName))
                    State.CompletedSteps.Add(step.defName);
            }
            State.CurrentStepIndex = -1;
            State.Dismissed = true;
        }

        public bool IsTutorialComplete()
        {
            EnsureInitialized();
            if (State == null || State.Dismissed) return true;
            if (State.CompletedSteps == null || allSteps == null || allSteps.Count == 0) return false;
            return allSteps.All(s => s != null && !string.IsNullOrEmpty(s.defName)
                                     && State.CompletedSteps.Contains(s.defName));
        }

        /// <summary>
        /// Re-Open des aktuellen Schritts nach Save/Load. Ohne diesen Pfad
        /// blieb CurrentStepIndex >= 0 nach dem Laden stehen und blockierte
        /// alle weiteren Schritte (Soft-Lock).
        /// </summary>
        public void ReopenCurrentStepIfAny()
        {
            EnsureInitialized();
            if (State == null || allSteps == null) return;
            if (State.CurrentStepIndex < 0 || State.CurrentStepIndex >= allSteps.Count) return;
            var step = allSteps[State.CurrentStepIndex];
            if (step == null || string.IsNullOrEmpty(step.defName)) return;
            if (State.CompletedSteps != null && State.CompletedSteps.Contains(step.defName))
            {
                State.CurrentStepIndex = -1;
                return;
            }
            // Nur öffnen, wenn nicht schon ein Dialog desselben Schritts offen ist.
            var open = Find.WindowStack?.Windows;
            if (open != null && open.Any(w => w is Dialog_TutorialStep))
                return;
            ShowStep(step);
        }

        /// <summary>
        /// Prüft, ob ein Infizierter (hidden faction) innerhalb des
        /// Sichtradius eines Kolonisten steht. Wird vom Tick gepollt.
        /// </summary>
        private static bool AnyInfectedVisibleToColonist()
        {
            var map = Find.AnyPlayerHomeMap;
            if (map == null || map.mapPawns == null) return false;

            var colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0) return false;

            var infected = map.mapPawns.AllPawnsSpawned;
            if (infected == null) return false;

            const string hiddenFaction = Scenarios.InfectedFactionUtility.HiddenFactionDefName;
            for (int c = 0; c < colonists.Count; c++)
            {
                var colonist = colonists[c];
                if (colonist == null || colonist.Dead || !colonist.Spawned) continue;
                IntVec3 colonistPos = colonist.Position;

                for (int i = 0; i < infected.Count; i++)
                {
                    var pawn = infected[i];
                    if (pawn == null || pawn.Dead || !pawn.Spawned) continue;
                    if (pawn.Faction == null || pawn.Faction.def == null) continue;
                    if (pawn.Faction.def.defName != hiddenFaction) continue;
                    if (pawn == colonist) continue;

                    if (pawn.Position.DistanceTo(colonistPos) <= ContactProximityCells
                        && GenSight.LineOfSight(colonistPos, pawn.Position, map))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void StartGuide()
        {
            EnsureInitialized();
            if (State != null && State.CompletedSteps != null && State.CompletedSteps.Count == 0)
                pendingGameStart = true;
        }

        public bool HasUnreadNotifications => State != null && State.CurrentStepIndex >= 0;

        public int TotalStepCount => allSteps?.Count ?? 0;

        public float GetGuideContentHeight()
        {
            int count = TotalStepCount;
            return 30f + 12f + count * 30f + 10f;
        }

        /// <summary>
        /// Zeichnet die Tutorial-Statusliste (RimPad Guide-Tab).
        /// Reine Darstellung — mutiert keinen State.
        /// </summary>
        public void DrawGuideContent(Rect rect)
        {
            EnsureInitialized();
            try
            {
                float y = rect.y;

                // Kopfzeile
                Text.Font = GameFont.Medium;
                GUI.color = RimconemyTheme.HeaderInk;
                Widgets.Label(new Rect(rect.x, y, rect.width, 30f),
                    "Rimconemy.Infected.Tutorial.Status".Translate());
                y += 30f;

                if (allSteps == null || allSteps.Count == 0)
                {
                    GUI.color = RimconemyTheme.Muted;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(rect.x, y, rect.width, 24f),
                        "Rimconemy.Infected.Tutorial.Empty".Translate());
                    return;
                }

                Text.Font = GameFont.Small;
                for (int i = 0; i < allSteps.Count; i++)
                {
                    var step = allSteps[i];
                    bool done = State.CompletedSteps != null && State.CompletedSteps.Contains(step.defName);
                    bool current = State.CurrentStepIndex == i;

                    GUI.color = done ? RimconemyTheme.Success
                        : current ? RimconemyTheme.Info
                        : RimconemyTheme.Muted;

                    string marker = done ? "✓ " : current ? "▶ " : "• ";
                    Widgets.Label(new Rect(rect.x, y, rect.width, 28f),
                        marker + (step.letterLabel ?? step.defName));
                    y += 30f;
                }
            }
            finally
            {
                RimconemyUi.ResetTextFontAndColor();
            }
        }

        public static TutorialDirector Get()
        {
            return Current.Game?.GetComponent<TutorialDirector>();
        }
    }
}
