using System.Linq;
using Rimconemy.Foundation.UI;
using Rimconemy.SurvivalProgression.Character.Roles;
using Rimconemy.SurvivalProgression.Progression;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.UI
{
    /// <summary>
    /// Read-only progression view for a selected colonist. Registration remains
    /// deferred until the RimWorld 1.6 inspect-tab injection API is verified.
    /// </summary>
    [StaticConstructorOnStartup]
    public class ProgressionPawnTab : RimconemyInspectTab
    {
        public static readonly ProgressionPawnTab Instance = new ProgressionPawnTab();

        private int _lastPawnId;
        private Vector2 _scrollPosition;

        static ProgressionPawnTab()
        {
            Log.Message("[Rimconemy.SurvivalProgression] ProgressionPawnTab init — registration deferred pending RimWorld 1.6 API verification.");
        }

        public ProgressionPawnTab()
        {
            labelKey = "Rimconemy";
        }

        protected override float PaneTopY => 30f;

        public override bool IsVisible
        {
            get
            {
                if (!base.IsVisible) return false;
                var pawn = Find.Selector?.SingleSelectedThing as Pawn;
                return pawn != null && pawn.IsColonist && !pawn.Dead;
            }
        }

        protected override bool StillValid => IsVisible;

        protected override void FillTab()
        {
            var pawn = Find.Selector?.SingleSelectedThing as Pawn;
            if (pawn == null) return;
            if (pawn.thingIDNumber != _lastPawnId)
            {
                _lastPawnId = pawn.thingIDNumber;
                _scrollPosition = Vector2.zero;
            }

            var component = Current.Game?.GetComponent<ProgressionGameComponent>();
            var snapshot = component?.GetSnapshot(pawn);
            float width = size.x - RimconemyTheme.Margin * 2f;
            float height = size.y;
            var view = new Rect(0f, 0f, width, 500f);
            Widgets.BeginScrollView(new Rect(0f, 0f, size.x, height), ref _scrollPosition, view);
            float y = 4f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 30f), "Rimconemy · " + pawn.LabelShortCap);
            y += 34f;
            if (snapshot == null)
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 55f), "Rimconemy.Survival.EmptyPawn");
                Widgets.EndScrollView();
                RimconemyUi.ResetTextFontAndColor();
                return;
            }

            Text.Font = GameFont.Small;
            RimconemyUi.DrawStatCard(new Rect(0f, y, width / 2f - 4f, 62f), "XP", "Erfahrung", snapshot.Experience.ToString("0.0") + "  L" + snapshot.Level.ToString("0.0"),
                (snapshot.Experience % 100f) / 100f, StatusLevel.Info);
            RimconemyUi.DrawStatCard(new Rect(width / 2f + 4f, y, width / 2f - 4f, 62f), "★", "Effizienz", snapshot.Efficiency.ToString("P0"),
                snapshot.Efficiency, snapshot.Efficiency >= 0.75f ? StatusLevel.Success : StatusLevel.Warn);
            y += 76f;

            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Survival.Needs", GameFont.Medium);
            y += 30f;
            float third = (width - 8f) / 3f;
            DrawNeed(new Rect(0f, y, third, 42f), "Nahrung", snapshot.NeedFoodLevel);
            DrawNeed(new Rect(third + 4f, y, third, 42f), "Sicherheit", snapshot.NeedSafetyLevel);
            DrawNeed(new Rect((third + 4f) * 2f, y, third, 42f), "Sozial", snapshot.NeedSocialLevel);
            y += 56f;

            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Survival.Specialization", GameFont.Medium);
            y += 30f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Arbeitsbereich", snapshot.WorkDomain ?? "Unassigned");
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Arbeitseinheiten", snapshot.CompletedWorkUnits.ToString());
            y += 28f;

            // Rimconemy role layer: Animals and Artistic stay hidden as
            // vanilla source skills; the player sees the derived roles that
            // actually matter to the new character system.
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Role.Title", GameFont.Medium);
            y += 30f;
            DrawRoleRow(new Rect(0f, y, width, 22f), "Rimconemy.Role.Farming", RoleSkillResolver.SkillOf(pawn, SkillDefOf.Plants));
            y += 22f;
            DrawRoleRow(new Rect(0f, y, width, 22f), "Rimconemy.Role.Cooking", RoleSkillResolver.SkillOf(pawn, SkillDefOf.Cooking));
            y += 22f;
            DrawRoleRow(new Rect(0f, y, width, 22f), "Rimconemy.Role.Hunting", RoleSkillResolver.HuntingLevel(pawn));
            y += 22f;
            DrawRoleRow(new Rect(0f, y, width, 22f), "Rimconemy.Role.Smithing", RoleSkillResolver.SmithingLevel(pawn));
            y += 22f;
            DrawRoleRow(new Rect(0f, y, width, 22f), "Rimconemy.Role.Intellectual", RoleSkillResolver.SkillOf(pawn, SkillDefOf.Intellectual));
            y += 28f;

            if (pawn.skills?.skills != null)
            {
                foreach (var skill in pawn.skills.skills.Where(s => s.Level > 0).OrderByDescending(s => s.Level).Take(4))
                {
                    string passion = skill.passion > 0 ? " ★" : "";
                    RimconemyUi.DrawRow(new Rect(8f, y, width - 8f, 22f), skill.def.label + passion, "Level " + skill.Level);
                    y += 22f;
                }
            }
            y += 8f;
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Survival.Traits", GameFont.Medium);
            y += 30f;
            var traits = pawn.story?.traits?.allTraits?.Where(t => t.def.defName.StartsWith("Rimconemy_")).ToList();
            if (traits == null || traits.Count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 30f), "Rimconemy.Survival.NoTraits");
                y += 34f;
            }
            else
            {
                foreach (var trait in traits)
                {
                    bool major = trait.Degree > 0;
                    RimconemyUi.DrawStatusBadge(new Rect(8f, y, width - 8f, 22f),
                        (major ? "★ " : "– ") + trait.LabelCap, major ? StatusLevel.Warn : StatusLevel.Info);
                    y += 24f;
                }
            }

            y += 8f;
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Survival.StoryContribution", GameFont.Medium);
            y += 30f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Letztes Update", "Tick " + snapshot.LastUpdatedTick);
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Status", snapshot.CompletedWorkUnits > 0 ? "Aktiv" : "Noch keine Ereignisse",
                snapshot.CompletedWorkUnits > 0 ? RimconemyTheme.Success : RimconemyTheme.Muted);

            Widgets.EndScrollView();
            RimconemyUi.ResetTextFontAndColor();
        }

        private static void DrawRoleRow(Rect rect, string labelKey, int level)
        {
            RimconemyUi.DrawRow(rect, labelKey.Translate(), "Level " + level);
        }

        private static void DrawNeed(Rect rect, string label, float value)
        {
            RimconemyUi.DrawNeedBar(new Rect(rect.x, rect.y, rect.width, 16f), value,
                value >= 0.65f ? RimconemyTheme.Success : value >= 0.35f ? RimconemyTheme.Warn : RimconemyTheme.Error,
                label + "  " + value.ToString("P0"));
        }

        protected override void CloseTab() { }
    }
}
