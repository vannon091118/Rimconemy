using Rimconemy.Foundation.UI;
using Rimconemy.SurvivalProgression.Progression;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.UI
{
    /// <summary>
    /// Read-only player-facing dashboard for Food, Safety, Social, XP and
    /// Game Over. Layout and semantic colours come from Foundation's toolkit.
    /// </summary>
    public sealed class SurvivalProgressionDashboard : RimconemyMainTabWindow
    {
        private Vector2 _scrollPosition;

        public override Vector2 InitialSize => new Vector2(700f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            var component = Current.Game?.GetComponent<ProgressionGameComponent>();
            if (component == null)
            {
                RimconemyUi.DrawEmptyState(inRect, "Rimconemy.Survival.Unavailable");
                return;
            }

            float width = inRect.width - RimconemyTheme.DefaultScrollbarWidth;
            int count = component.Snapshots?.Count ?? 0;
            float cardHeight = 122f;
            float contentHeight = 122f + Mathf.Max(1, count) * cardHeight + RimconemyTheme.Margin * 2f;
            var outer = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            var view = new Rect(0f, 0f, width, contentHeight);
            Widgets.BeginScrollView(outer, ref _scrollPosition, view);

            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 32f), "Rimconemy · Survival & Progression");
            y += 36f;
            Text.Font = GameFont.Tiny;
            GUI.color = RimconemyTheme.Muted;
            Widgets.Label(new Rect(0f, y, width, 18f),
                "Kolonisten: " + count + "  ·  Schema v" + ProgressionGameComponent.CurrentSchemaVersion
                + "  ·  Update: Tick " + component.LastUpdateTick);
            GUI.color = Color.white;
            y += 24f;

            StatusLevel loopStatus = component.GameOverTriggered
                ? StatusLevel.Error
                : component.HasObservedPlayerColonist ? StatusLevel.Success : StatusLevel.Warn;
            string loopLabel = component.GameOverTriggered
                ? "! GAME OVER  " + (component.GameOverReason ?? "")
                : component.HasObservedPlayerColonist ? "OK  Survival loop active" : "– Waiting for first colonist";
            RimconemyUi.DrawStatusBadge(new Rect(0f, y, width, 24f), loopLabel, loopStatus);
            y += 32f;

            int columns = width >= 680f ? 4 : 2;
            float cardWidth = (width - (columns - 1) * RimconemyTheme.Margin) / columns;
            DrawHeaderCard(0, y, cardWidth, "#", "Kolonisten", count.ToString(),
                count > 0 ? StatusLevel.Success : StatusLevel.Warn, columns);
            DrawHeaderCard(1, y, cardWidth, "↻", "Social", component.RecreationAvailable ? "Aktiv" : "Wartet",
                component.RecreationAvailable ? StatusLevel.Success : StatusLevel.Warn, columns);
            DrawHeaderCard(2, y, cardWidth, "⌁", "Forschung", component.ResearchCapabilities.Count.ToString(), StatusLevel.Info, columns);
            DrawHeaderCard(3, y, cardWidth, "T", "Letzter Tick", component.LastUpdateTick.ToString(), StatusLevel.Info, columns);
            y += columns == 4 ? 72f : 144f;

            if (count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 80f), "Rimconemy.Survival.Empty");
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var snapshot = component.Snapshots[i];
                    if (snapshot == null) continue;
                    int column = i % columns;
                    int row = i / columns;
                    DrawSnapshot(snapshot, new Rect(column * (cardWidth + RimconemyTheme.Margin), y + row * cardHeight, cardWidth, cardHeight));
                }
            }

            Widgets.EndScrollView();
            RimconemyUi.ResetTextFontAndColor();
        }

        private static void DrawHeaderCard(int index, float y, float cardWidth, string icon, string label,
            string value, StatusLevel level, int columns)
        {
            int row = index / columns;
            int column = index % columns;
            float x = column * (cardWidth + RimconemyTheme.Margin);
            RimconemyUi.DrawStatCard(new Rect(x, y + row * 72f, cardWidth, 60f), icon, label, value, -1f, level);
        }

        private static void DrawSnapshot(ProgressionSnapshot snapshot, Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 7f, rect.width - 16f, 22f),
                (snapshot.PawnLabel ?? "—") + "  —  " + (snapshot.WorkDomain ?? "Unassigned"));
            Text.Font = GameFont.Small;
            float y = rect.y + 32f;
            DrawNeed("Nahrung", snapshot.NeedFoodLevel, new Rect(rect.x + 8f, y, rect.width - 16f, 18f));
            y += 20f;
            DrawNeed("Sicherheit", snapshot.NeedSafetyLevel, new Rect(rect.x + 8f, y, rect.width - 16f, 18f));
            y += 20f;
            DrawNeed("Sozial", snapshot.NeedSocialLevel, new Rect(rect.x + 8f, y, rect.width - 16f, 18f));
            y += 20f;
            float xp = (snapshot.Experience % 100f) / 100f;
            RimconemyUi.DrawNeedBar(new Rect(rect.x + 8f, y, rect.width - 16f, 9f), xp,
                xp >= 0.7f ? RimconemyTheme.Success : RimconemyTheme.Info);
            GUI.color = RimconemyTheme.Muted;
            Widgets.Label(new Rect(rect.x + 8f, y + 10f, rect.width - 16f, 18f),
                "XP " + snapshot.Experience.ToString("0.0") + "  ·  L" + snapshot.Level.ToString("0.0")
                + "  ·  Effizienz " + snapshot.Efficiency.ToString("P0"));
            GUI.color = Color.white;
        }

        private static void DrawNeed(string label, float value, Rect rect)
        {
            RimconemyUi.DrawNeedBar(rect, value, value >= 0.65f ? RimconemyTheme.Success : value >= 0.35f ? RimconemyTheme.Warn : RimconemyTheme.Error, label + "  " + value.ToString("P0"));
        }
    }
}
