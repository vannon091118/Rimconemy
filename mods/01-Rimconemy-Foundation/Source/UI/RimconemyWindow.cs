using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase 0-A: Default Window chrome (close-X, close-button, absorb-input,
    /// draggable). Enforces Minimum-Sizes via SetInitialSizeAndPosition-Hook.
    /// Subklassen überschreiben InitialSize.
    /// </summary>
    public class RimconemyWindow : Window
    {
        protected RimconemyWindow()
        {
            doCloseButton = true;
            doCloseX = true;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
            draggable = true;
            forcePause = false;
        }

        public override Vector2 InitialSize => Vector2.zero; // Subklassen pflicht.

        public override void DoWindowContents(Rect inRect)
        {
            throw new System.NotImplementedException(
                "RimconemyWindow.DoWindowContents must be overridden by the concrete subclass.");
        }

        protected override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            if (windowRect.width < RimconemyTheme.MinWindowWidth)
                windowRect.width = RimconemyTheme.MinWindowWidth;
            if (windowRect.height < RimconemyTheme.MinWindowHeight)
                windowRect.height = RimconemyTheme.MinWindowHeight;
        }
    }
}
