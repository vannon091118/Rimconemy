using Rimconemy.Foundation.UI;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Phase
{
    /// <summary>
    /// Phase-First Progress HUDTab-style overlay (PHASE_PROGRESSION_CONTRACT.md §10).
    /// Extends the Foundation <see cref="RimconemyMainTabWindow"/> base and uses only
    /// <see cref="RimconemyUi"/> toolkit primitives + <see cref="RimconemyTheme"/> colors.
    ///
    /// Honest-fallback: per status-vs-code-audit-2026-08-04 §A2, the render pass never
    /// throws; null-map / pre-game conditions render a single muted banner.
    /// </summary>
    public sealed class PhaseProgressWindow : RimconemyMainTabWindow
    {
        public override Vector2 InitialSize => new Vector2(720f, 460f);

        public override void DoWindowContents(Rect inRect)
        {
            var snap = PhaseProgressResolver.Resolve(Find.CurrentMap);

            float width = inRect.width - RimconemyTheme.Margin * 2f;
            float y = inRect.y + RimconemyTheme.Margin;

            // ── 1) Section title ──────────────────────────────────────
            RimconemyUi.DrawSectionTitle(
                new Rect(inRect.x + RimconemyTheme.Margin, y, width, 30f),
                "Rimconemy.PhaseProgress.Title",
                GameFont.Medium);
            y += 36f;

            if (!string.IsNullOrEmpty(snap.EmptyReason))
            {
                RimconemyUi.DrawFeatureStatus(
                    new Rect(inRect.x + RimconemyTheme.Margin, y, width, 96f),
                    "Rimconemy.PhaseProgress.Empty.Title",
                    "Rimconemy.PhaseProgress.Empty.Detail",
                    StatusLevel.Muted);
                RimconemyUi.ResetTextFontAndColor();
                return;
            }

            // ── 2) Current-phase progress card ────────────────────────
            var headerCardRect = new Rect(inRect.x + RimconemyTheme.Margin, y, width, 96f);
            string phaseLabel = RimconemyUi.T(snap.CurrentPhaseLabelKey);
            RimconemyUi.DrawStatCard(
                headerCardRect,
                "★",
                phaseLabel,
                snap.Percent + "%",
                snap.Percent / 100f,
                snap.Percent >= 100f ? StatusLevel.Success
                    : snap.Percent >= 50f ? StatusLevel.Info : StatusLevel.Warn);
            y += 110f;

            // ── 3) Next-milestone row ─────────────────────────────────
            var nextRect = new Rect(inRect.x + RimconemyTheme.Margin, y, width, 64f);
            if (!string.IsNullOrEmpty(snap.NextMilestoneLabelKey))
            {
                RimconemyUi.DrawFeatureStatus(
                    nextRect,
                    "Rimconemy.PhaseProgress.NextMilestone",
                    RimconemyUi.T(snap.NextMilestoneLabelKey),
                    StatusLevel.Info);
            }
            else
            {
                RimconemyUi.DrawFeatureStatus(
                    nextRect,
                    "Rimconemy.PhaseProgress.PhaseCompleteTitle",
                    "Rimconemy.PhaseProgress.PhaseCompleteDetail",
                    StatusLevel.Success);
            }
            y += 76f;

            // ── 4) Overall progress strip ─────────────────────────────
            var overallRect = new Rect(inRect.x + RimconemyTheme.Margin, y, width, 64f);
            RimconemyUi.DrawStatCard(
                overallRect,
                "⊙",
                "Rimconemy.PhaseProgress.Overall",
                snap.OverallPercent + "%",
                snap.OverallPercent / 100f,
                snap.OverallPercent >= 100f ? StatusLevel.Success : StatusLevel.Info);
            y += 76f;

            // ── 5) Tip-region: tooltip with full milestone summary ────
            var tipRect = new Rect(inRect.x + RimconemyTheme.Margin, y, width, 28f);
            TooltipHandler.TipRegion(
                tipRect,
                "Rimconemy.PhaseProgress.Tip".Translate(snap.TotalMilestonesMet,
                    snap.TotalMilestonesAcrossPhases, snap.OverallPercent));
            Widgets.Label(tipRect, "Rimconemy.PhaseProgress.TipShort".Translate());
            y += 36f;

            RimconemyUi.ResetTextFontAndColor();
        }

    }
}
