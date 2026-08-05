using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    /// <summary>
    /// TutorialDirector - GameComponent that manages the tutorial flow.
    /// Handles step progression, state persistence, and completion tracking.
    /// </summary>
    public class TutorialDirector : GameComponent
    {
        public TutorialState State;
        private List<TutorialStep> steps = new List<TutorialStep>();
        private int currentStepIndex = 0;
        private bool introCompleted = false;
        
        public TutorialDirector() : base()
        {
            State = new TutorialState();
            InitializeSteps();
        }
        
        public static bool IsIntroCompleted => 
            Current.Game.GetComponent<TutorialDirector>()?.introCompleted ?? false;
        
        public void NotifyIntroCompleted()
        {
            introCompleted = true;
            State.IntroCompleted = true;
        }
        
        private void InitializeSteps()
        {
            // Load all tutorial step defs and order them
            var allDefs = DefDatabase<TutorialStepDef>.AllDefsListForReading;
            var orderedDefs = allDefs.OrderBy(d => d.order).ToList();
            
            // Convert to runtime steps
            steps = orderedDefs.Select(def => new TutorialStep(def)).ToList();
        }
        
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            
            // Skip if tutorial is disabled or completed
            if (State.Completed || State.DismissedAll) return;
            if (!introCompleted) return; // Wait for intro to finish
            
            // Check if we've shown all steps
            if (currentStepIndex >= steps.Count)
            {
                State.Completed = true;
                return;
            }
            
            var currentStep = steps[currentStepIndex];
            if (currentStep.CheckTrigger())
            {
                currentStep.ShowStep();
                currentStepIndex++;
                // Mark step as shown in state
                State.MarkStepShown(currentStep.Def.defName);
            }
        }
        
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Re-initialize steps after loading
            InitializeSteps();
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref State, "tutorialState");
        }
    }
}