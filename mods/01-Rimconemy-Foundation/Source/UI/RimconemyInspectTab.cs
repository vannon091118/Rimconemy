using RimWorld;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase 0-A: Convenience base class for InspectTabBase-derived tabs.
    /// Marked abstract because InspectTabBase exposes multiple abstract
    /// members (IsVisible/Hidden/VisibleInBlueprintMode/TutorHighlightTagClosed,
    /// DoTabGUI/OnOpen/TabTick/TabUpdate/Notify_*). Concrete packages subclass
    /// this with full overrides — we keep the canonical chrome parent so
    /// all packages share an inspect-tab class hierarchy.
    /// </summary>
    public abstract class RimconemyInspectTab : InspectTabBase
    {
    }
}
