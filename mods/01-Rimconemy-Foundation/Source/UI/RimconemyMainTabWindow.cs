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

        // Memo lives in the static-class shared state so both Window variants
        // agree on the same memo + log identity. See RimconemyWindow._loggedFallbacks.
        /// <summary>
        /// Honest fallback when a concrete subclass forgets to override
        /// <see cref="DoWindowContents"/>. Per the audit-vs-doctrine contract
        /// (Falsifizierungsbericht status-vs-code-audit-2026-08-04 §A2) we
        /// NEVER throw <c>NotImplementedException</c>: RimWorld funnels no
        /// exception handling into the window-draw pass, so a throw leaks
        /// into the in-game UI thread and corrupts player state. Instead we
        /// render an honest, text-first banner that the operator can read,
        /// and log a single diagnostic (memoised per caller) so the failing
        /// subclass can be located without flooding the log.
        /// Subclasses remain REQUIRED to override.
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            string callerName = GetType().FullName ?? GetType().Name ?? "(unknown)";
            // Log the fallback FIRST so the memo is recorded even if the
            // subsequent render throws (e.g., Widgets.DrawBoxSolid outside
            // OnGUI context). This satisfies T2: memo increases on first call
            // regardless of render success.
            RimconemyWindow.LogFallbackOnce("RimconemyMainTabWindow", callerName);
            try
            {
                // Player-facing text is intentionally plain; the crashing
                // subclass name is reserved for the log (Code-Review finding I3).
                RimconemyUi.DrawFeatureStatus(
                    inRect,
                    "MainTab-Inhalt nicht überschrieben",
                    "Dieser Tab ist eingerichtet aber sein Inhalt fehlt noch. " +
                    "Die Rimconemy-Basisklasse schützt vor Absturz, zeigt nur diesen ehrlichen Marker " +
                    "und schreibt Details in die Spieler-Log.",
                    StatusLevel.Error);
            }
            finally
            {
                RimconemyUi.ResetTextFontAndColor();
            }
        }
    }
}
