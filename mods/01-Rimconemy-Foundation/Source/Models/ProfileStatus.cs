namespace Rimconemy.Foundation.Models
{
    /// <summary>
    /// Owner: Foundation
    /// Profile status determined from loaded and compatible packages.
    /// </summary>
    public enum ProfileStatus
    {
        /// <summary>Only Foundation is loaded; no feature packages present.</summary>
        Standalone,
        /// <summary>Foundation plus some but not all feature packages;
        /// or DLCs missing for Full.</summary>
        Partial,
        /// <summary>All five Rimconemy packages, all five DLCs, all schemas compatible.</summary>
        FullOverhaul
    }
}
