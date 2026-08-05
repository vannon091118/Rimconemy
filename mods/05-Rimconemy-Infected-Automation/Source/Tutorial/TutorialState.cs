using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    /// <summary>
    /// TutorialState - Persistent state for the tutorial system.
    /// Handles completion tracking, dismissal tracking, and schema migration.
    /// </summary>
    public class TutorialState : IExposable
    {
        public HashSet<string> CompletedSteps = new HashSet<string>();
        public HashSet<string> DismissedSteps = new HashSet<string>();
        public bool DismissedAll = false;
        public bool Completed = false;
        public bool IntroCompleted = false;
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref CompletedSteps, "completedSteps", LookMode.Value);
            Scribe_Collections.Look(ref DismissedSteps, "dismissedSteps", LookMode.Value);
            Scribe_Values.Look(ref DismissedAll, "dismissedAll", false);
            Scribe_Values.Look(ref Completed, "completed", false);
            Scribe_Values.Look(ref IntroCompleted, "introCompleted", false);
        }
        
        public void MarkStepShown(string stepDefName)
        {
            CompletedSteps.Add(stepDefName);
        }
        
        public bool IsStepShown(string stepDefName)
        {
            return CompletedSteps.Contains(stepDefName);
        }
        
        public void DismissStep(string stepDefName)
        {
            DismissedSteps.Add(stepDefName);
        }
        
        public bool IsStepDismissed(string stepDefName)
        {
            return DismissedSteps.Contains(stepDefName);
        }
    }
}