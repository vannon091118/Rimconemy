namespace Rimconemy.SurvivalProgression.Progression
{
    /// <summary>
    /// Phase 8.1 — Seven Progression domains. The order is stable across
    /// save/load: domain enum values MUST NOT be renumbered. New domains
    /// are append-only and get the next numeric slot.
    ///
    /// Vertical-Slice-Plan §Phase 8.1: Survival, Salvage, Firecraft, Building,
    /// Processing, Machinery, Defense.
    /// </summary>
    public enum ProgressionDomain
    {
        Survival = 0,
        Salvage = 1,
        Firecraft = 2,
        Building = 3,
        Processing = 4,
        Machinery = 5,
        Defense = 6,
    }

    /// <summary>
    /// Helpers shared by DomainXpState, UnlockService and the Bridge.
    /// </summary>
    public static class ProgressionDomainUtility
    {
        public const int DomainCount = 7;

        public static bool IsValid(ProgressionDomain domain)
        {
            int ordinal = (int)domain;
            return ordinal >= 0 && ordinal < DomainCount;
        }

        /// <summary>
        /// Returns the stable string used as dictionary/scribe key.
        /// Equal to <c>domain.ToString()</c> but routed through this helper
        /// so a future rename of the enum (or localisation) does not break
        /// save compatibility.
        /// </summary>
        public static string Key(ProgressionDomain domain)
        {
            return domain.ToString();
        }
    }
}
