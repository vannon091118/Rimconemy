using System.Collections.Generic;

namespace Rimconemy.Foundation.Models
{
    /// <summary>
    /// Owner: Foundation
    /// Immutable snapshot of a package's state for UI display.
    /// Read-only; packages own their data.
    /// </summary>
    public class PackageSnapshot
    {
        public string PackageId { get; }
        public string PackageVersion { get; }
        public bool IsLoaded { get; }
        public string Status { get; }        // "Active", "Unavailable", "Blocked", "Frozen"
        public string StatusReason { get; }  // e.g. "Package not installed", "Schema incompatible"
        public IReadOnlyList<string> CapabilityIds { get; }

        public PackageSnapshot(
            string packageId,
            string packageVersion,
            bool isLoaded,
            string status,
            string statusReason,
            IReadOnlyList<string> capabilityIds)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            IsLoaded = isLoaded;
            Status = status;
            StatusReason = statusReason;
            CapabilityIds = capabilityIds;
        }
    }

    /// <summary>
    /// Represents a DLC and its load status.
    /// </summary>
    public class DlcStatus
    {
        public string DlcName { get; }
        public bool IsLoaded { get; }
        public string Reason { get; }

        public DlcStatus(string dlcName, bool isLoaded, string reason)
        {
            DlcName = dlcName;
            IsLoaded = isLoaded;
            Reason = reason;
        }
    }
}
