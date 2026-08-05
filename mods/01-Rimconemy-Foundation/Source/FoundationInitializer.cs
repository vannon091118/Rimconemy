using Verse;
using RimWorld;
using Rimconemy.Foundation.Bridge;

namespace Rimconemy.Foundation
{
    /// <summary>
    /// Static constructor on startup to initialize foundation systems.
    /// Registers foundation capabilities in the CapabilityAudit.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class FoundationInitializer
    {
        static FoundationInitializer()
        {
            // Register foundation capabilities
            CapabilityAudit.RegisterCapability("rimconemy.foundation", "event.bridge");
            CapabilityAudit.RegisterCapability("rimconemy.foundation", "capability.audit");
            CapabilityAudit.RegisterCapability("rimconemy.foundation", "rimpad.ui");
            
            Verse.Log.Message("[Rimconemy.Foundation] FoundationInitializer: Capabilities registered.");
        }
    }
}