using System;
using System.Collections.Generic;

namespace Rimconemy.Foundation.Bridge
{
    /// <summary>
    /// Capability audit system for cross-package capability detection.
    /// Packages register their capabilities at startup, and other packages can check
    /// if a capability is available without direct DLL references.
    /// </summary>
    public static class CapabilityAudit
    {
        private static readonly Dictionary<string, HashSet<string>> capabilities = 
            new Dictionary<string, HashSet<string>>();
        
        /// <summary>
        /// Register a capability for a package.
        /// </summary>
        public static void RegisterCapability(string packageId, string capability)
        {
            if (!capabilities.ContainsKey(packageId))
                capabilities[packageId] = new HashSet<string>();
            
            capabilities[packageId].Add(capability);
        }
        
        /// <summary>
        /// Check if a package has a specific capability.
        /// </summary>
        public static bool HasCapability(string packageId, string capability)
        {
            return capabilities.TryGetValue(packageId, out var caps) && caps.Contains(capability);
        }
        
        /// <summary>
        /// Get all capabilities for a package.
        /// </summary>
        public static HashSet<string> GetCapabilities(string packageId)
        {
            if (capabilities.TryGetValue(packageId, out var caps))
                return new HashSet<string>(caps);
            return new HashSet<string>();
        }
        
        /// <summary>
        /// Clear all registered capabilities (for testing).
        /// </summary>
        public static void Clear()
        {
            capabilities.Clear();
        }
    }
}