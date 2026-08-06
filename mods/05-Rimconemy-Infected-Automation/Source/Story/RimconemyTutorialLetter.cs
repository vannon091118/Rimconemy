using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Custom Letter subclass with Portrait support for Tutorial steps.
    /// Owner: Infected & Automation (Paket 05).
    /// RimWorld 1.6 Letter.ReceiveLetter has NO portrait parameter — we subclass.
    /// </summary>
    public class RimconemyTutorialLetter : Letter
    {
        public Texture2D Portrait;
        public string PortraitPath;
        public string StepId;
        public List<Def> UnlockDefs;
        public string Objective;
        public int StepNumber;
        public int TotalSteps;

        // Text property for Dialog_TutorialStep
        public string Text { get; set; }

        public override void ExposeData()
        {
            base.ExposeData();
            // Texture2D is not ILoadReferenceable. The content path is
            // supplied by the Def and persisted as plain data; never use
            // UnityEditor.AssetDatabase in the runtime assembly.
            Scribe_Values.Look(ref PortraitPath, "portraitPath");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && !string.IsNullOrEmpty(PortraitPath))
                Portrait = ContentFinder<Texture2D>.Get(PortraitPath, false);
            Scribe_Values.Look(ref StepId, "stepId");
            Scribe_Collections.Look(ref UnlockDefs, "unlockDefs", LookMode.Def);
            var text = Text;
            Scribe_Values.Look(ref text, "text");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) Text = text;
            Scribe_Values.Look(ref Objective, "objective");
            Scribe_Values.Look(ref StepNumber, "stepNumber", 0);
            Scribe_Values.Look(ref TotalSteps, "totalSteps", 0);
        }

        public override void OpenLetter()
        {
            // Custom Dialog mit Portrait + Unlock-Vorschau
            Find.WindowStack.Add(new Dialog_TutorialStep(this));
        }

        protected override string GetMouseoverText()
        {
            return Label;
        }
    }
}