using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    /// <summary>
    /// TutorialStepDef - Definition class for tutorial steps.
    /// Defines trigger conditions, letter content, and unlocks.
    /// </summary>
    public class TutorialStepDef : Def
    {
        public new string label;
        public string text;
        public int order = 0;
        public TriggerType triggerType = TriggerType.OnIntroCompleted;
        public string letterDefName = ""; // Reference to defined LetterDef with portrait icon
        public LookTargets lookTargets;
        public List<string> unlockDefs = new List<string>(); // Def names to unlock
        
        public enum TriggerType
        {
            OnIntroCompleted,
            OnCampfireBuilt,
            OnFirstInfectedContact,
            OnResourceCollected,
            OnWallBuilt,
            OnGeneratorBuilt,
            OnTurretBuilt,
            OnOutpostFounded,
            OnTradeDone
        }
    }
}