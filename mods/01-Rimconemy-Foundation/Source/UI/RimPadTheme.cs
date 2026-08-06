using Verse;
using RimWorld;
using UnityEngine;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Tablet/Pip-Boy style theme extensions for the RimPad.
    /// Provides additional theme tokens on top of RimconemyTheme.
    /// </summary>
    public static class RimPadTheme
    {
        /// <summary>
        /// Apply tablet-specific styling overrides.
        /// </summary>
        public static void Apply()
        {
            // Tablet/Pip-Boy style overrides - use Small font for terminal feel
            Text.Font = GameFont.Small;
            // Text color will be set via Text.color or Widgets.Label with color
        }

        public static Color PanelBackground => new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark semi-transparent
        public static Color AccentColor => new Color(1f, 0.55f, 0f); // Amber
        public static Color WarningColor => new Color(0.8f, 0.2f, 0.2f); // Red
    }
}