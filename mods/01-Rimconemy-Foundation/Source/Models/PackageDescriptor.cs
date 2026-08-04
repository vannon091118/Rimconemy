using System.Collections.Generic;

namespace Rimconemy.Foundation.Models
{
    /// <summary>
    /// Owner: Foundation
    /// Describes a Rimconemy package registered at startup.
    /// Each feature package registers exactly one descriptor.
    /// </summary>
    public class PackageDescriptor
    {
        public string PackageId { get; }
        public string PackageVersion { get; }
        public int SaveSchemaVersion { get; }
        public IReadOnlyList<Capability> Capabilities { get; }
        public ProfileCompatibility ProfileCompatibility { get; }

        public PackageDescriptor(
            string packageId,
            string packageVersion,
            int saveSchemaVersion,
            IReadOnlyList<Capability> capabilities,
            ProfileCompatibility profileCompatibility)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            SaveSchemaVersion = saveSchemaVersion;
            Capabilities = capabilities;
            ProfileCompatibility = profileCompatibility;
        }
    }

    /// <summary>
    /// A versioned capability exposed by a package.
    /// Example: rimconemy.survivalprogression.needs.v1
    /// </summary>
    public class Capability
    {
        public string CapabilityId { get; }
        public int Version { get; }

        public Capability(string capabilityId, int version)
        {
            CapabilityId = capabilityId;
            Version = version;
        }

        public override string ToString() => $"{CapabilityId}.v{Version}";
    }

    /// <summary>
    /// Declares which profiles a package supports.
    /// </summary>
    public enum ProfileCompatibility
    {
        /// <summary>Package works standalone only, not in Full Overhaul.</summary>
        StandaloneOnly,
        /// <summary>Package works standalone and in Full Overhaul.</summary>
        StandaloneAndFull
    }
}
