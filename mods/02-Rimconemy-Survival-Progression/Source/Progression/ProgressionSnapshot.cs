using System.Collections.Generic;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression
{
    /// <summary>
    /// Persisted read model for one directly controlled colonist.
    /// Values are sampled from vanilla state; this package owns the XP ledger.
    /// </summary>
    public sealed class ProgressionSnapshot : IExposable
    {
        public int PawnId;
        public string PawnLabel;
        public string WorkDomain = "Unassigned";
        // Derived role read model. Animals and Artistic remain hidden source
        // skills; these values are the player-facing role layer.
        public int FarmingLevel;
        public int CookingLevel;
        public int HuntingLevel;
        public int SmithingLevel;
        public int IntellectualLevel;
        public float Experience;
        public float Efficiency = 1.0f;
        public List<string> ResearchCapabilities = new List<string>();
        public float NeedFoodLevel;
        public float NeedSafetyLevel;
        public float NeedSocialLevel;
        public string CurrentJobDefName = "";
        public int ActiveJobTicks;
        public int CompletedWorkUnits;
        public long LastUpdatedTick;

        public float Level => 1f + Experience / 100f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnId, "pawnId", 0);
            Scribe_Values.Look(ref PawnLabel, "pawnLabel", "");
            Scribe_Values.Look(ref WorkDomain, "workDomain", "Unassigned");
            Scribe_Values.Look(ref FarmingLevel, "farmingLevel", 0);
            Scribe_Values.Look(ref CookingLevel, "cookingLevel", 0);
            Scribe_Values.Look(ref HuntingLevel, "huntingLevel", 0);
            Scribe_Values.Look(ref SmithingLevel, "smithingLevel", 0);
            Scribe_Values.Look(ref IntellectualLevel, "intellectualLevel", 0);
            Scribe_Values.Look(ref Experience, "experience", 0f);
            Scribe_Values.Look(ref Efficiency, "efficiency", 1f);
            Scribe_Collections.Look(ref ResearchCapabilities, "researchCapabilities", LookMode.Value);
            Scribe_Values.Look(ref NeedFoodLevel, "needFoodLevel", 0.5f);
            Scribe_Values.Look(ref NeedSafetyLevel, "needSafetyLevel", 0.5f);
            Scribe_Values.Look(ref NeedSocialLevel, "needSocialLevel", 0.5f);
            Scribe_Values.Look(ref CurrentJobDefName, "currentJobDefName", "");
            Scribe_Values.Look(ref ActiveJobTicks, "activeJobTicks", 0);
            Scribe_Values.Look(ref CompletedWorkUnits, "completedWorkUnits", 0);
            Scribe_Values.Look(ref LastUpdatedTick, "lastUpdatedTick", 0L);

            if (ResearchCapabilities == null)
                ResearchCapabilities = new List<string>();
        }
    }
}
