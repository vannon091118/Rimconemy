using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Character
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02)
    /// Track A — Phase 1: Skill-Budget Window using Phase 0-A tokens.
    ///
    /// Migrated changes vs. legacy:
    ///  - Window base class: RimconemyWindow (instead of vanilla Window)
    ///  - All magic-number spacings replaced with RimconemyTheme.*
    ///  - Hardcoded German strings replaced with Keyed lookups
    ///  - Combat skills (Shooting/Melee) ARE now eligible (H5 §2)
    ///  - Cost model uses SkillBudgetCalculator.CostForLevel (linear 0-10, progressive 11+)
    ///  - Live budget status (over/fine/warning) and Trait-Zone badge
    ///  - Tooltip on every skill row from skillDef.description
    /// </summary>
    public class SkillBudgetWindow : RimconemyWindow
    {
        private const float LabelWidth = 150f;
        private const float SliderWidth = 220f;
        private const float ValueWidth = 40f;

        private readonly Dictionary<SkillDef, int> _allocations;
        private Vector2 _scrollPosition;
        private bool _applied;

        public override Vector2 InitialSize =>
            new Vector2(620f, 640f);

        public SkillBudgetWindow()
        {
            _allocations = SkillBudgetCalculator.BuildDefaultAllocation(GetEligibleSkills());
        }

        public override void PostClose()
        {
            base.PostClose();
            // If player closed without applying, fall back to default distribution.
            if (_applied) return;
            StoredBudgetAllocations.Allocations = new Dictionary<SkillDef, int>(_allocations);
            CharacterSetup.ApplyStoredBudget();
        }

        public override void DoWindowContents(Rect inRect)
        {
            int used = SkillBudgetCalculator.CalculateSpentPoints(_allocations);
            int remaining = SkillBudgetCalculator.TotalBudget - used;
            Color statusColor = remaining < 0 ? RimconemyTheme.Error
                              : (remaining == 0 ? RimconemyTheme.Success : RimconemyTheme.Warn);

            // Section title
            RimconemyUi.DrawSectionTitle(
                new Rect(inRect.x, inRect.y, inRect.width, RimconemyTheme.SectionTitleHeight),
                "Rimconemy.UI.SkillBudget.Window.Title",
                GameFont.Medium);
            float y = inRect.y + RimconemyTheme.SectionTitleHeight + RimconemyTheme.SectionTitleSpacing;

            // Budget row
            string budgetText = "Rimconemy.UI.SkillBudget.Budget.Label".Translate()
                .Replace("{0}", used.ToString())
                .Replace("{1}", SkillBudgetCalculator.TotalBudget.ToString());
            GUI.color = statusColor;
            var budgetRect = new Rect(inRect.x, y, inRect.width, RimconemyTheme.RowHeight);
            Widgets.Label(budgetRect, budgetText);
            GUI.color = Color.white;
            // Tooltip on budget row
            string balanceStr = (used - SkillBudgetCalculator.NeutralCenter).ToString("+0;-0;0");
            TooltipHandler.TipRegion(budgetRect,
                "Rimconemy.UI.SkillBudget.Tooltip".Translate().Replace("{0}", balanceStr));
            y += RimconemyTheme.RowHeight;

            // Zone badge
            int balance = used - SkillBudgetCalculator.NeutralCenter;
            var zone = SkillBudgetCalculator.Classify(balance);
            string zoneKey;
            Color zoneColor;
            switch (zone)
            {
                case SkillBudgetCalculator.TraitZone.PositiveLight:
                case SkillBudgetCalculator.TraitZone.PositiveStrong:
                    zoneKey = "Rimconemy.UI.SkillBudget.Zone.Positive";
                    zoneColor = RimconemyTheme.Success;
                    break;
                case SkillBudgetCalculator.TraitZone.NegativeLight:
                case SkillBudgetCalculator.TraitZone.NegativeStrong:
                    zoneKey = "Rimconemy.UI.SkillBudget.Zone.Negative";
                    zoneColor = RimconemyTheme.Warn;
                    break;
                default:
                    zoneKey = "Rimconemy.UI.SkillBudget.Zone.Buffer";
                    zoneColor = RimconemyTheme.Muted;
                    break;
            }
            GUI.color = zoneColor;
            var zoneRect = new Rect(inRect.x, y, inRect.width, RimconemyTheme.RowHeight);
            Widgets.Label(zoneRect, zoneKey.Translate());
            GUI.color = Color.white;
            y += RimconemyTheme.RowHeight;

            y += RimconemyTheme.SectionSpacing;

            // Skills scroll list
            float innerWidth = inRect.width - RimconemyTheme.DefaultScrollbarWidth - RimconemyTheme.DefaultWindowPadding;
            float remainingHeight = inRect.yMax - y - RimconemyTheme.RowHeight * 2f - RimconemyTheme.SectionSpacing;
            var scrollOuter = new Rect(inRect.x, y, inRect.width - RimconemyTheme.DefaultWindowPadding, remainingHeight);
            var viewInner = new Rect(0f, 0f, innerWidth,
                _allocations.Count * (RimconemyTheme.RowHeight + 4f) + RimconemyTheme.DefaultViewPadding * 2f);

            Widgets.BeginScrollView(scrollOuter, ref _scrollPosition, viewInner);
            var rowRect = new Rect(0f, 0f, viewInner.width, RimconemyTheme.RowHeight + 4f);
            foreach (var kvp in _allocations.OrderBy(k => k.Key.defName, System.StringComparer.Ordinal))
            {
                DrawSkillRow(rowRect, kvp.Key, kvp.Value);
                rowRect.y += RimconemyTheme.RowHeight + 4f;
            }
            Widgets.EndScrollView();

            // Bottom buttons
            var buttonRect = new Rect(inRect.x, inRect.yMax - RimconemyTheme.RowHeight * 1.5f,
                inRect.width / 2f - 5f, RimconemyTheme.RowHeight + 4f);
            if (Widgets.ButtonText(buttonRect, "Rimconemy.UI.SkillBudget.Button.Standard".Translate()))
            {
                var standard = SkillBudgetCalculator.BuildDefaultAllocation(_allocations.Keys);
                foreach (var k in _allocations.Keys.ToList())
                    _allocations[k] = standard.ContainsKey(k) ? standard[k] : SkillBudgetCalculator.MinPerSkill;
            }

            var applyRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, inRect.width / 2f - 5f, buttonRect.height);
            GUI.color = (remaining == 0) ? RimconemyTheme.Success : RimconemyTheme.Muted;
            if (Widgets.ButtonText(applyRect, "Rimconemy.UI.SkillBudget.Button.Apply".Translate()) && remaining == 0)
            {
                ApplyAndClose();
            }
            GUI.color = Color.white;

            // Warning text when over-budget
            if (remaining < 0)
            {
                var warnRect = new Rect(inRect.x, buttonRect.y - RimconemyTheme.RowHeight - 4f, inRect.width, RimconemyTheme.RowHeight);
                GUI.color = RimconemyTheme.Error;
                Widgets.Label(warnRect, "Rimconemy.UI.SkillBudget.Warning.OverBudget".Translate());
                GUI.color = Color.white;
            }
        }

        private void DrawSkillRow(Rect row, SkillDef skillDef, int current)
        {
            float rowH = RimconemyTheme.RowHeight;

            var labelRect = new Rect(row.x, row.y, LabelWidth, rowH);
            Widgets.Label(labelRect, skillDef.label.CapitalizeFirst());

            var decRect = new Rect(labelRect.xMax + 5f, row.y, 24f, rowH);
            if (Widgets.ButtonText(decRect, "-") && current > SkillBudgetCalculator.MinPerSkill)
                _allocations[skillDef] = SkillBudgetCalculator.ClampLevel(current - 1);

            var valRect = new Rect(decRect.xMax + 4f, row.y, ValueWidth, rowH);
            GUI.color = RimconemyTheme.HeaderInk;
            Widgets.Label(valRect, current.ToString());
            GUI.color = Color.white;

            var incRect = new Rect(valRect.xMax + 4f, row.y, 24f, rowH);
            int nextCost = SkillBudgetCalculator.CostForLevel(current + 1)
                         - SkillBudgetCalculator.CostForLevel(current);
            int currentSpent = SkillBudgetCalculator.CalculateSpentPoints(_allocations);
            if (Widgets.ButtonText(incRect, "+") &&
                current < SkillBudgetCalculator.MaxSkillLevel &&
                currentSpent + nextCost <= SkillBudgetCalculator.TotalBudget)
            {
                _allocations[skillDef] = SkillBudgetCalculator.ClampLevel(current + 1);
            }

            var sliderRect = new Rect(incRect.xMax + 8f, row.y + 4f, SliderWidth, rowH - 8f);
            int newVal = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, current,
                SkillBudgetCalculator.MinPerSkill, SkillBudgetCalculator.MaxSkillLevel, true));

            if (newVal != current)
            {
                int delta = SkillBudgetCalculator.CostForLevel(newVal) - SkillBudgetCalculator.CostForLevel(current);
                currentSpent = SkillBudgetCalculator.CalculateSpentPoints(_allocations);
                if (currentSpent + delta <= SkillBudgetCalculator.TotalBudget || delta < 0)
                    _allocations[skillDef] = newVal;
            }

            // Tooltip covers the entire row (per-skill description)
            string tip = string.IsNullOrEmpty(skillDef.description) ? skillDef.label : skillDef.description;
            var wholeRow = new Rect(row.x, row.y, row.width, rowH);
            TooltipHandler.TipRegion(wholeRow, tip);
        }

        private void ApplyAndClose()
        {
            if (_applied) return;
            _applied = true;
            StoredBudgetAllocations.Allocations = new Dictionary<SkillDef, int>(_allocations);
            CharacterSetup.ApplyStoredBudget();
            Messages.Message("Rimconemy.UI.SkillBudget.Message.Applied".Translate(), MessageTypeDefOf.PositiveEvent);
            Close();
        }

        private static List<SkillDef> GetEligibleSkills()
        {
            return new List<SkillDef>(CharacterSetup.EligibleSkills);
        }
    }

    /// <summary>
    /// Holds skill budget allocations between the Window and CharacterSetup.
    /// Owner: Survival &amp; Progression (Package 02)
    /// </summary>
    public static class StoredBudgetAllocations
    {
        public static Dictionary<SkillDef, int> Allocations;
    }
}
