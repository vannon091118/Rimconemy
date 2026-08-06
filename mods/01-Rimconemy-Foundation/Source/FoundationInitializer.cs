using Verse;

namespace Rimconemy.Foundation
{
    /// <summary>
    /// [StaticConstructorOnStartup] anchor only. Capabilities are managed
    /// internally in <see cref="Rimconemy.Foundation.Registry.PackageRegistry"/>
    /// + vordeklarierte Liste aus <c>INTERFACE_CONTRACT.md §2</c>
    /// (profile, colonials, eventlog). Der frühere
    /// <c>Rimconemy.Foundation.Bridge.CapabilityAudit</c> wurde im Audit-Sprint
    /// 2026-08-04 (F-V4) entfernt; diese Klasse bleibt nur als Bootstrap-Log-Anker
    /// für MainTab-/HUD-Listener, die auf "Foundation war schon vor mir da" prüfen.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class FoundationInitializer
    {
        static FoundationInitializer()
        {
            Log.Message("[Rimconemy.Foundation] Anchor: PackageRegistry + INTERFACE_CONTRACT §2 capabilities live; downstream listeners may subscribe.");
        }
    }
}