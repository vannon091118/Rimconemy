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

        // Memo for the diagnostic Log.Warning so an un-overridden subclass
        // can't spam the player-log at 60 FPS. HashSet + Ordinal comparer;
        // prepend the canonical source string so RimconemyWindow and
        // RimconemyMainTabWindow never collide on the same caller name.
        private static readonly System.Collections.Generic.HashSet<string> _loggedFallbacks
            = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>
        /// Honest fallback when a concrete subclass forgets to override
        /// <see cref="DoWindowContents"/>. Per the audit-vs-doctrine contract
        /// (Falsifizierungsbericht status-vs-code-audit-2026-08-04 §A1) we
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
            try
            {
                // Player-facing text is intentionally plain; the crashing
                // subclass name is reserved for the log (Code-Review finding I3).
                RimconemyUi.DrawFeatureStatus(
                    inRect,
                    "Fenster-Inhalt nicht überschrieben",
                    "Dieser Bereich wird gerade gebaut. Die Rimconemy-Basisklasse schützt vor Absturz, " +
                    "zeigt nur diesen ehrlichen Marker und schreibt Details in die Spieler-Log.",
                    StatusLevel.Error);

                LogFallbackOnce("RimconemyWindow", callerName);
            }
            finally
            {
                RimconemyUi.ResetTextFontAndColor();
            }
        }

        internal static void LogFallbackOnce(string source, string callerName)
        {
            string key = source + "|" + callerName;
            lock (_loggedFallbacks)
            {
                if (_loggedFallbacks.Add(key))
                {
                    Log.Warning(
                        "[Rimconemy.Foundation] " + source + ".DoWindowContents fallback rendered; caller '"
                        + callerName + "' did not override the method. " +
                        "See docs/falsification/status-vs-code-audit-2026-08-04 §A1/A2. " +
                        "(Warning logged once per caller-type to avoid log spam.)");
                }
            }
        }

        internal static void ClearFallbackLogMemoForTests()
        {
            lock (_loggedFallbacks) { _loggedFallbacks.Clear(); }
        }

        internal static int MemoEntryCount
        {
            get { lock (_loggedFallbacks) { return _loggedFallbacks.Count; } }
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
