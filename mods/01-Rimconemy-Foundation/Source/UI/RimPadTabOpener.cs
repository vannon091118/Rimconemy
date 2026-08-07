using RimWorld;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Thin MainTabWindow wrapper for the RimPad toolbar button.
    ///
    /// RimWorld's MainButtonDef.tabWindowClass requires a MainTabWindow
    /// subclass. RimPadWindow is a floating Window, not a MainTabWindow.
    /// This wrapper satisfies the contract: on open, it immediately
    /// spawns the real floating RimPadWindow and closes itself.
    ///
    /// Pattern: common in mods like Numbers, RimHUD where a toolbar
    /// button opens a floating overlay instead of a bottom tab.
    /// </summary>
    public class RimPadTabOpener : RimconemyMainTabWindow
    {
        public override void PostOpen()
        {
            base.PostOpen();

            // Spawn the floating RimPad window
            var rimPad = new RimPadWindow();
            Find.WindowStack.Add(rimPad);
            RimPadTabDrawer.SelectTab(RimPadTab.Guide);

            // Close this wrapper tab immediately — player never sees it
            Close();
        }
    }
}
