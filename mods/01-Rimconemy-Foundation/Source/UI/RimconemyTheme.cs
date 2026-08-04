using UnityEngine;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase 0-A: Central token source for all Rimconemy UI elements.
    /// Layout, spacing, semantic colors. Every Windows/Tab uses these tokens
    /// instead of bare magic numbers / Color.* direct references.
    /// </summary>
    public static class RimconemyTheme
    {
        // Layout — extracted as canonical from FoundationDashboard spacing.
        public const float SectionSpacing = 12f;
        public const float IndentSize = 16f;
        public const float RowHeight = 22f;
        public const float MiniRowHeight = 18f;
        public const float SectionTitleHeight = 30f;
        public const float SectionTitleSpacing = 2f;
        public const float Margin = 8f;
        public const float DefaultWindowPadding = 20f;

        // ScrollView defaults.
        public const float DefaultScrollbarWidth = 16f;
        public const float DefaultViewPadding = 4f;

        // Window size clamps.
        public const float MinWindowWidth = 360f;
        public const float MaxWindowWidth = 1200f;
        public const float MinWindowHeight = 240f;
        public const float MaxWindowHeight = 800f;

        // Interaction tuning constants.
        public const float HoverDarkenAmount = 0.05f;
        public const float TooltipDelayMs = 250f;

        // Semantic color palette — replaces Color.green/yellow/red/cyan/gray
        // scattered usage across packages.
        public static readonly Color Success = new Color(0.30f, 0.80f, 0.30f);
        public static readonly Color Warn = new Color(0.95f, 0.78f, 0.20f);
        public static readonly Color Error = new Color(0.90f, 0.30f, 0.30f);
        public static readonly Color Info = new Color(0.50f, 0.85f, 0.95f);
        public static readonly Color Muted = new Color(0.65f, 0.65f, 0.65f);
        public static readonly Color HeaderInk = new Color(1.00f, 0.93f, 0.82f);

        // Surface tokens. Status is always also expressed through text/badge
        // shape so the UI remains readable without colour perception.
        public static readonly Color DangerSoft = new Color(0.90f, 0.30f, 0.30f, 0.40f);
        public static readonly Color PanelInk = new Color(0.13f, 0.14f, 0.16f);
        public static readonly Color DividerInk = new Color(0.35f, 0.36f, 0.38f);

        // RimWorld typography convention: Large = window title, Medium =
        // section heading, Small = body, Tiny = metadata/tooltips.
    }
}
