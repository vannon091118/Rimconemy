using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Tutorial Step Definition — data-driven tutorial steps.
    /// Owner: Infected & Automation (Paket 05).
    /// Inherits <see cref="Verse.Def"/> directly. [DefOf] is for static references
    /// to existing vanilla Defs, NOT for declaring a new Def class.
    ///
    /// Field compatibility (RimWorld 1.6):
    ///   * Modern fields (TutorialSteps.xml): priority/trigger/letterLabel/letterText/portraitTexture/unlockDefs/prerequisiteSteps
    ///   * Legacy fields (TutorialStepDefs.xml): label/text/order/triggerType/letterDefName
    /// Both XML files share this Def type so we declare both schemata here.
    /// </summary>
    public class TutorialStepDef : Def
    {
        // --- Modern schema (TutorialSteps.xml) ---
        public int priority = 0;
        public string trigger;              // "GameStart", "CampfireBuilt", "FirstInfectedContact", "WallBuilt", "ResourceCollected_ThingDefName"
        public string letterLabel;
        public string letterText;
        public string objective;            // Kurzes On-Screen-Ziel („NÄCHSTES ZIEL“-Zeile im Dialog)
        public string portraitTexture;      // Pfad für ContentFinder
        public List<Def> unlockDefs;
        public List<string> prerequisiteSteps;

        // --- Legacy schema (TutorialStepDefs.xml) ---
        // Kept so both XML def-files parse without silent field drops.
        public string text;                 // alias of letterText (resolved by TutorialDirector)
        public int order;                   // alias of priority (1-based display order)
        public string triggerType;          // alias of trigger (e.g. "OnIntroCompleted", "OnWallBuilt")
        public string letterDefName;        // defName of a LetterDef to display (looked up via DefDatabase)
    }
}
