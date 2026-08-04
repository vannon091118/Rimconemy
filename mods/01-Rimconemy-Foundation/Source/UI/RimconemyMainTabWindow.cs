using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase 0-A: Default-Chrome for MainTab-anchored dashboard windows.
    /// Close-Button/Close-X werden für MainTabs deaktiviert (RimWorld-Konvention).
    /// DoWindowContents ist abstract-Slot; Subklassen überschreiben.
    /// </summary>
    public class RimconemyMainTabWindow : MainTabWindow
    {
        public override void PreOpen()
        {
            base.PreOpen();
            doCloseButton = false;
            doCloseX = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            throw new System.NotImplementedException(
                "RimconemyMainTabWindow.DoWindowContents must be overridden by the concrete subclass.");
        }
    }
}
