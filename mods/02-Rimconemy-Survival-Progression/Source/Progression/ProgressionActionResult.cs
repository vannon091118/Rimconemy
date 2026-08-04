using System.Collections.Generic;

namespace Rimconemy.SurvivalProgression.Progression
{
    /// <summary>
    /// Phase 8.2 — Action-Result-Vertrag. Holds the canonical record of an
    /// Action-Completion event AFTER it has been accepted by DomainXpState.
    ///
    /// Vertical-Slice-Plan §Phase 8.2: contract for the result of an action
    /// that contributed experience. Replay of the same ActionKey returns
    /// WasAccepted=false (idempotent rejection). The view-model is published
    /// to other packages via the ActionResult-Vertrag in INTERFACE_CONTRACT.
    ///
    /// Expected key samples (survival-only convention; non-binding):
    ///   harvest:plantId:tick
    ///   build:thingId:completionTick
    ///   recipe:billId:outputHash
    ///   salvage:jobId:outputHash
    ///   night:defense:nightIndex
    /// </summary>
    public struct ProgressionActionResult
    {
        public string ActionKey;
        public ProgressionDomain Domain;
        public float BaseExperience;
        public float ActualExperience; // post-diminishing
        public string OutputDefName;
        public int OutputCount;
        public long CompletedTick;
        public bool WasAccepted;

        public static ProgressionActionResult Rejected(string key)
        {
            return new ProgressionActionResult
            {
                ActionKey = key ?? "",
                Domain = ProgressionDomain.Survival,
                BaseExperience = 0f,
                ActualExperience = 0f,
                OutputDefName = "",
                OutputCount = 0,
                CompletedTick = 0L,
                WasAccepted = false,
            };
        }

        public bool HasOutput => !string.IsNullOrEmpty(OutputDefName) && OutputCount > 0;

        public string Summary()
        {
            if (!WasAccepted) return $"rejected(key={ActionKey ?? ""})";
            string xp = ActualExperience.ToString("0.0");
            string output = HasOutput ? $", output={OutputDefName}x{OutputCount}" : "";
            return $"accepted(domain={Domain}, xp={xp}{output}, tick={CompletedTick})";
        }
    }

    /// <summary>
    /// Phase 8.2 helper — shared between the Hub and the Bridge. Keeps a
    /// thread-local helper class off the public surface to avoid making
    /// domain callers construct a Dictionary for a single read.
    /// </summary>
    public static class ProgressionActionKeySamples
    {
        public const string BuildPrefix = "domain:Building:completed:map=";
        public const string RecipePrefix = "domain:Processing:completed:bill=";
        public const string HarvestPrefix = "domain:Survival:harvested:plant=";
        public const string SalvagePrefix = "domain:Salvage:salvaged:output=";
        public const string NightPrefix = "domain:Survival:night:nightIndex=";

        public static IEnumerable<string> DocumentedShapes()
        {
            return new[]
            {
                "domain:Building:completed:map={map.uniqueID}:def={def.defName}:frame={frame.thingIDNumber}",
                "domain:Processing:completed:bill={billId}:outputHash={hash}",
                "domain:Survival:harvested:plant={plantDef}:tick={tick}",
                "domain:Salvage:salvaged:output={thingDef}:tick={tick}",
                "domain:Survival:night:nightIndex={nightIndex}:map={map.uniqueID}",
            };
        }
    }
}
