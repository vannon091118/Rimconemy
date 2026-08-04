using Verse;

namespace Rimconemy.InfectedAutomation
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Canonical resource thresholds used across the audit-fix chain
    /// (C5 / H4 of the 2026-08-04 slop-audit). These constants replace the
    /// previously hardcoded "5 units" magic number inside
    /// <c>StoryDirector.AssignStorageHashFromCapability</c> so future
    /// tuning has one source of truth instead of three ad-hoc copies.
    ///
    /// Concept: <c>CriticalUnits{ResourceId}</c> returns the absolute unit
    /// count below which the resource is considered "critical". This is
    /// a numeric ceiling, not a per-target-stock fraction, because the
    /// Mod 03 StorageEntry exposes <c>TotalAmount</c> only (no
    /// <c>TargetStock</c> in v1). For Mod-1 baseline resources (Food,
    /// Medicine, Materials) the documented thresholds from DECISIONS.md
    /// #14 are 50/30/40 units respectively - conservative - and the
    /// generic fallback (<c>FallbackCriticalUnits</c>) is the historical
    /// magic-5 floor.
    ///
    /// Future: replace with target-fraction once Mod 03 publishes
    /// TargetStock alongside TotalAmount.
    /// </summary>
    public static class ResourceThresholds
    {
        public const string LogMarker = "v1";

        // Canonical unit-count thresholds from DECISIONS.md #14.
        public const int CriticalFoodUnits = 50;
        public const int CriticalMedicineUnits = 30;
        public const int CriticalMaterialUnits = 40;

        // Generic fallback for any other resource id.
        public const int FallbackCriticalUnits = 5;

        /// <summary>
        /// Resource-keyed critical count. Lower-case compare is enough
        /// because ThingDef.defName does not carry casing magic.
        /// </summary>
        public static int CriticalUnitsFor(string resourceId)
        {
            switch ((resourceId ?? "").ToLowerInvariant())
            {
                case "food":
                case "rawfood":
                case "meal":
                    return CriticalFoodUnits;
                case "medicine":
                case "medicines":
                case "meds":
                    return CriticalMedicineUnits;
                case "material":
                case "materials":
                case "wood":
                case "metal":
                    return CriticalMaterialUnits;
                default:
                    return FallbackCriticalUnits;
            }
        }

        /// <summary>
        /// True if <paramref name="currentAmount"/> sits below the canonical
        /// critical unit count for <paramref name="resourceId"/>.
        /// Unknown / missing inputs return false defensively.
        /// </summary>
        public static bool IsBelowCritical(string resourceId, int currentAmount)
        {
            if (currentAmount < 0) return false;
            int floor = CriticalUnitsFor(resourceId);
            return currentAmount < floor;
        }

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Log.Message(
                    "[Rimconemy.InfectedAutomation] ResourceThresholds active: " +
                    $"food<{CriticalFoodUnits}, medicine<{CriticalMedicineUnits}, " +
                    $"material<{CriticalMaterialUnits}, fallback<{FallbackCriticalUnits}).");
            }
        }
    }
}
