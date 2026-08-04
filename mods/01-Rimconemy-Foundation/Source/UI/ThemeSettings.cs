using Rimconemy.Foundation.Save;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase 0-A: Per-save UI preference storage + Settings-row drawer.
    /// Reading/writing routes through FoundationSaveData so the value is part
    /// of the saved game state. Activation side-effect (RimThemes-Bridge) is
    /// driven by GlobalThemeOverride.ApplyIfRequested() in Bootstrap.
    /// </summary>
    public static class ThemeSettings
    {
        public static bool IsOverrideEnabled
        {
            get
            {
                var sd = Current.Game?.GetComponent<FoundationSaveData>();
                return sd?.EnableGlobalThemeOverride ?? false;
            }
        }

        public static void SetOverride(bool enabled)
        {
            var sd = Current.Game?.GetComponent<FoundationSaveData>();
            if (sd == null) return;
            sd.EnableGlobalThemeOverride = enabled;
        }

        /// <summary>
        /// Settings-row drawer for options tabs. The caller provides a Listing_Standard
        /// already initialised. Caller decides the placement (FoundationDashboard
        /// or a dedicated Mod-Settings panel).
        /// </summary>
        public static void DrawSettingsRow(Listing_Standard listing)
        {
            bool current = IsOverrideEnabled;
            listing.CheckboxLabeled("RimconemySettings.GlobalTheme.Title".Translate(), ref current);
            if (current != IsOverrideEnabled)
                SetOverride(current);
            listing.Label("RimconemySettings.GlobalTheme.Help".Translate());
        }
    }
}
