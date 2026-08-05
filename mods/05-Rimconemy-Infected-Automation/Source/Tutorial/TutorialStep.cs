using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    /// <summary>
    /// TutorialStep - Runtime wrapper for tutorial step definitions.
    /// Handles trigger checking and showing the tutorial step.
    /// </summary>
    public class TutorialStep
    {
        public TutorialStepDef Def;
        private bool triggered = false;
        
        public TutorialStep(TutorialStepDef def)
        {
            Def = def;
        }
        
        public bool CheckTrigger()
        {
            if (triggered) return false;
            
            switch (Def.triggerType)
            {
                case TutorialStepDef.TriggerType.OnIntroCompleted:
                    triggered = TutorialDirector.IsIntroCompleted;
                    break;
                case TutorialStepDef.TriggerType.OnCampfireBuilt:
                    triggered = TutorialTriggerBridge.CampfireBuilt;
                    break;
                case TutorialStepDef.TriggerType.OnFirstInfectedContact:
                    triggered = TutorialTriggerBridge.FirstInfectedContact;
                    break;
                case TutorialStepDef.TriggerType.OnResourceCollected:
                    triggered = TutorialTriggerBridge.ResourceCollected;
                    break;
                case TutorialStepDef.TriggerType.OnWallBuilt:
                    triggered = TutorialTriggerBridge.WallBuilt;
                    break;
                case TutorialStepDef.TriggerType.OnGeneratorBuilt:
                    triggered = TutorialTriggerBridge.GeneratorBuilt;
                    break;
                case TutorialStepDef.TriggerType.OnTurretBuilt:
                    triggered = TutorialTriggerBridge.TurretBuilt;
                    break;
                case TutorialStepDef.TriggerType.OnOutpostFounded:
                    triggered = TutorialTriggerBridge.OutpostFounded;
                    break;
                case TutorialStepDef.TriggerType.OnTradeDone:
                    triggered = TutorialTriggerBridge.TradeDone;
                    break;
            }
            
            return triggered;
        }
        
        public void ShowStep()
        {
            // Use the predefined LetterDef that has the portrait icon set via XML
            var letterDef = DefDatabase<LetterDef>.GetNamed(Def.letterDefName, false);
            if (letterDef == null)
            {
                Log.Error($"Could not find LetterDef '{Def.letterDefName}' for tutorial step {Def.defName}");
                letterDef = LetterDefOf.PositiveEvent; // fallback
            }
            
            Find.LetterStack.ReceiveLetter(Def.label, Def.text, letterDef, Def.lookTargets, 
                                          null, null, null, null, 0, true);
        }
    }
}