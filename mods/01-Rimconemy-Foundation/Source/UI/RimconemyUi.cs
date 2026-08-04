using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Semantic status levels for DrawStatusBadge.
    /// Resolves to RimconemyTheme semantic colors. Plugins should never
    /// reference RimWorld.Color.* directly — always map to one of these.
    /// </summary>
    public enum StatusLevel { Success, Warn, Error, Info, Muted }

    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase 0-A: Static UI toolkit. DrawSectionTitle/DrawRow/DrawStatusBadge/
    /// DrawNeedBar/DrawEmptyState/DrawHighlightedInteractable/
    /// BeginStandardScrollView/Indent/Section/ResetTextFontAndColor.
    ///
    /// All drawing methods use RimconemyTheme constants and semantic colors.
    /// Try/finally guards reset Text.Font and GUI.color so callers don't bleed.
    /// </summary>
    public static class RimconemyUi
    {
        /// <summary>Section-title row. Default Medium font, HeaderInk.</summary>
        public static void DrawSectionTitle(Rect rect, string key, GameFont font = GameFont.Medium)
        {
            try
            {
                Text.Font = font;
                GUI.color = RimconemyTheme.HeaderInk;
                Widgets.Label(rect, key.Translate());
            }
            finally
            {
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }

        /// <summary>Two-column row: leftLabel (default ink) + rightValue (semantic, optional).</summary>
        public static void DrawRow(Rect rect, string leftLabel, string rightValue, Color? valueColor = null)
        {
            float half = rect.width / 2f;
            var labelRect = new Rect(rect.x, rect.y, half - 4f, rect.height);
            var valRect = new Rect(rect.x + half + 4f, rect.y, half - 4f, rect.height);
            try
            {
                Text.Font = GameFont.Small;
                Widgets.Label(labelRect, leftLabel);
                GUI.color = valueColor ?? Color.white;
                Widgets.Label(valRect, rightValue);
            }
            finally
            {
                GUI.color = Color.white;
            }
        }

        /// <summary>Inline status badge. StatusLevel → RimconemyTheme.*</summary>
        public static void DrawStatusBadge(Rect rect, string label, StatusLevel level)
        {
            Color c = level switch
            {
                StatusLevel.Success => RimconemyTheme.Success,
                StatusLevel.Warn => RimconemyTheme.Warn,
                StatusLevel.Error => RimconemyTheme.Error,
                StatusLevel.Info => RimconemyTheme.Info,
                _ => RimconemyTheme.Muted,
            };
            try
            {
                GUI.color = c;
                Widgets.Label(rect, label);
            }
            finally
            {
                GUI.color = Color.white;
            }
        }

        /// <summary>Horizontal-need/progress-bar with empty-background (Muted) and filled colour.</summary>
        public static void DrawNeedBar(Rect rect, float fillFraction, Color fillColor, string label = null)
        {
            fillFraction = Mathf.Clamp01(fillFraction);
            try
            {
                GUI.color = RimconemyTheme.Muted;
                Widgets.DrawBox(rect);
                var fillRect = new Rect(rect.x, rect.y, rect.width * fillFraction, rect.height);
                GUI.color = fillColor;
                Widgets.DrawBox(fillRect);
                if (!string.IsNullOrEmpty(label))
                    Widgets.Label(rect, label);
            }
            finally
            {
                GUI.color = Color.white;
            }
        }

        /// <summary>Centered muted message used when a list is empty.</summary>
        public static void DrawEmptyState(Rect rect, string messageKey)
        {
            try
            {
                GUI.color = RimconemyTheme.Muted;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, messageKey.Translate());
            }
            finally
            {
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        /// <summary>
        /// Hoverable + clickable rectangle with optional TooltipHandler-TipRegion.
        /// Used as a uniform invocation for buttons that need no visual chrome
        /// beyond the highlight.
        /// </summary>
        public static void DrawHighlightedInteractable(Rect rect, Action onClick, string tooltipKey = null)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            if (Widgets.ButtonInvisible(rect))
                onClick?.Invoke();
            if (!string.IsNullOrEmpty(tooltipKey))
                TooltipHandler.TipRegion(rect, tooltipKey.Translate());
        }

        /// <summary>
        /// Wrapped BeginScrollView/EndScrollView pattern. Caller supplies content
        /// via Action. We provide standard outer rect + position. Caller is responsible
        /// for the Inner-View rect (usually derived from content height).
        /// </summary>
        public static void BeginStandardScrollView(Rect viewRect, Rect scrollOuter, ref Vector2 scrollPosition, Action contentDrawer)
        {
            Widgets.BeginScrollView(scrollOuter, ref scrollPosition, viewRect);
            try { contentDrawer?.Invoke(); }
            finally { Widgets.EndScrollView(); }
        }

        /// <summary>Indent helper: pushes inner rect right by `levels` indent-units.</summary>
        public static Rect Indent(Rect inner, int levels)
        {
            if (levels < 0) levels = 0;
            return new Rect(inner.x + RimconemyTheme.IndentSize * levels,
                            inner.y,
                            inner.width - RimconemyTheme.IndentSize * levels,
                            inner.height);
        }

        /// <summary>Section-Title-Rect: Section-Title-High-Punkt + 2f Spacing, full width.</summary>
        public static Rect Section(Rect inRect)
            => new Rect(inRect.x, inRect.y,
                        inRect.width, RimconemyTheme.SectionTitleHeight + RimconemyTheme.SectionTitleSpacing);

        /// <summary>
        /// Draws a compact stat card with a status icon, label, value and
        /// optional progress bar. Purely visual; no state is mutated.
        /// </summary>
        public static void DrawStatCard(Rect rect, string icon, string label, string value,
            float fillFraction = -1f, StatusLevel level = StatusLevel.Info)
        {
            try
            {
                GUI.color = RimconemyTheme.PanelInk;
                Widgets.DrawBoxSolid(rect, RimconemyTheme.PanelInk);
                GUI.color = RimconemyTheme.DividerInk;
                Widgets.DrawBox(rect);
                GUI.color = Color.white;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(rect.x + RimconemyTheme.Margin, rect.y + 5f, rect.width - RimconemyTheme.Margin * 2f, 18f),
                    (icon ?? "") + "  " + (label ?? ""));
                Text.Font = GameFont.Medium;
                GUI.color = StatusColor(level);
                Widgets.Label(new Rect(rect.x + RimconemyTheme.Margin, rect.y + 22f, rect.width - RimconemyTheme.Margin * 2f, 24f), value ?? "—");
                if (fillFraction >= 0f)
                    DrawNeedBar(new Rect(rect.x + RimconemyTheme.Margin, rect.yMax - 10f, rect.width - RimconemyTheme.Margin * 2f, 5f), fillFraction, StatusColor(level));
            }
            finally
            {
                ResetTextFontAndColor();
            }
        }

        /// <summary>Draws a deterministic line sparkline from normalized samples.</summary>
        public static void DrawSparkline(Rect rect, IList<float> samples, Color color)
        {
            try
            {
                GUI.color = RimconemyTheme.PanelInk;
                Widgets.DrawBoxSolid(rect, RimconemyTheme.PanelInk);
                GUI.color = RimconemyTheme.DividerInk;
                Widgets.DrawBox(rect);
                if (samples == null || samples.Count < 2) return;
                GUI.color = color;
                float min = float.MaxValue;
                float max = float.MinValue;
                for (int i = 0; i < samples.Count; i++)
                {
                    min = Mathf.Min(min, samples[i]);
                    max = Mathf.Max(max, samples[i]);
                }
                float range = Mathf.Max(0.001f, max - min);
                for (int i = 1; i < samples.Count; i++)
                {
                    float x1 = rect.x + (i - 1) * rect.width / (samples.Count - 1);
                    float x2 = rect.x + i * rect.width / (samples.Count - 1);
                    float y1 = rect.yMax - ((samples[i - 1] - min) / range) * rect.height;
                    float y2 = rect.yMax - ((samples[i] - min) / range) * rect.height;
                    Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), color, 2f);
                }
            }
            finally
            {
                GUI.color = Color.white;
            }
        }

        /// <summary>Draws inline tabs and returns the selected tab index.</summary>
        public static int DrawTabs(Rect rect, IList<string> labels, int selectedIndex)
        {
            if (labels == null || labels.Count == 0) return 0;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, labels.Count - 1);
            float width = rect.width / labels.Count;
            for (int i = 0; i < labels.Count; i++)
            {
                var tabRect = new Rect(rect.x + i * width, rect.y, width - 2f, rect.height);
                if (Widgets.ButtonText(tabRect, labels[i] ?? "—"))
                    selectedIndex = i;
                if (i == selectedIndex)
                {
                    GUI.color = RimconemyTheme.Info;
                    Widgets.DrawLineHorizontal(tabRect.x, tabRect.yMax - 2f, tabRect.width);
                    GUI.color = Color.white;
                }
                TooltipHandler.TipRegion(tabRect, (labels[i] ?? "—") + "");
            }
            return selectedIndex;
        }

        /// <summary>Draws a bounded countdown bar with text and semantic status.</summary>
        public static void DrawCountdown(Rect rect, long remainingTicks, long totalTicks, string label)
        {
            float fraction = totalTicks <= 0L ? 0f : Mathf.Clamp01(remainingTicks / (float)totalTicks);
            StatusLevel level = fraction <= 0.2f ? StatusLevel.Error : fraction <= 0.5f ? StatusLevel.Warn : StatusLevel.Success;
            DrawNeedBar(new Rect(rect.x, rect.yMax - 7f, rect.width, 6f), fraction, StatusColor(level));
            DrawStatusBadge(new Rect(rect.x, rect.y, rect.width, rect.height - 8f),
                (label ?? "") + "  " + FormatTicks(remainingTicks), level);
            TooltipHandler.TipRegion(rect, "Remaining game ticks: " + remainingTicks);
        }

        /// <summary>Draws a segmented pressure gauge plus a readable percentage.</summary>
        public static void DrawPressureGauge(Rect rect, float pressure, string label)
        {
            pressure = Mathf.Clamp01(pressure);
            int segments = 10;
            float gap = 2f;
            float segmentWidth = (rect.width - gap * (segments - 1)) / segments;
            int filled = Mathf.CeilToInt(pressure * segments);
            for (int i = 0; i < segments; i++)
            {
                StatusLevel level = i < 4 ? StatusLevel.Success : i < 7 ? StatusLevel.Warn : StatusLevel.Error;
                GUI.color = i < filled ? StatusColor(level) : RimconemyTheme.Muted;
                Widgets.DrawBoxSolid(new Rect(rect.x + i * (segmentWidth + gap), rect.y, segmentWidth, rect.height), GUI.color);
            }
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, rect.yMax + 2f, rect.width, RimconemyTheme.RowHeight),
                (label ?? "") + "  " + pressure.ToString("P0"));
        }

        private static Color StatusColor(StatusLevel level)
        {
            switch (level)
            {
                case StatusLevel.Success: return RimconemyTheme.Success;
                case StatusLevel.Warn: return RimconemyTheme.Warn;
                case StatusLevel.Error: return RimconemyTheme.Error;
                case StatusLevel.Info: return RimconemyTheme.Info;
                default: return RimconemyTheme.Muted;
            }
        }

        private static string FormatTicks(long ticks)
        {
            if (ticks <= 0L) return "0d";
            return (ticks / Rimconemy.Foundation.TimeConstants.TicksPerDay).ToString("0.0") + "d";
        }

        /// <summary>Resets Text.Font and GUI.color to default state.</summary>
        public static void ResetTextFontAndColor()
        {
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}
