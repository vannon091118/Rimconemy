using System.Collections.Generic;
using Rimconemy.Foundation.UI;
using Rimconemy.InfectedAutomation.Ideology;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.UI
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Phase 2: Setting-/Erfahrungsfenster.
    ///
    /// Read-only dialog that lists the active Setting Rules and the
    /// technical carriers they ride on. Accessible from:
    ///   - the Ideology side-button on the Foundation dashboard
    ///   - any future Rimconemy UI tab (P1-P6 systems)
    ///
    /// Window is non-modal: closing it does NOT shut down the underlying
    /// rules. The dialog is read-only and never mutates DefDatabase or
    /// pawn state.
    ///
    /// Specification: ROADMAP §2.4 (Setting-Ideologie) and H3 §0.
    /// </summary>
    public class SettingRulesInspector : RimconemyWindow
    {
        // Shared rimconemy palette so the dialog stays tonally consistent
        // with the rest of the dashboard family. Defined as constants here
        // because the Foundation UI tokens live behind a different
        // token-name surface that varies across package revisions.
        private static readonly Color InkColor = new Color(0.65f, 0.70f, 0.78f, 1f);
        private static readonly Color AccentColor = new Color(0.80f, 0.65f, 0.30f, 1f);

        private Vector2 _scroll;

        public override Vector2 InitialSize => new Vector2(720f, 480f);

        public override void DoWindowContents(Rect inRect)
        {
            // Mandatory close button (RimWindow defaults).
            Widgets.DrawMenuSection(inRect);

            // Title
            var titleRect = new Rect(inRect.x + 12f, inRect.y + 8f, inRect.width - 24f, 32f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = AccentColor;
            Widgets.Label(titleRect, "Rimconemy Setting-Regeln");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Subtitle / pedagogy
            var noteRect = new Rect(titleRect.x, titleRect.yMax + 4f, titleRect.width, 42f);
            GUI.color = InkColor;
            Widgets.Label(noteRect,
                "Regeln, die die Rimconemy-Pakete fuer Charakter und Stimmung einsetzen. " +
                "Traeger ist jeweils ein nativer Ideology-Mechanismus. Keine zusaetzliche Religionssimulation.");
            GUI.color = Color.white;

            // Anchor reminder
            var anchorRect = new Rect(noteRect.x, noteRect.yMax + 4f, noteRect.width, 22f);
            GUI.color = InkColor;
            Widgets.Label(anchorRect,
                "Anchor: AllPlayerColonists - Adult - FreeNonSlave - Consciousness");
            GUI.color = Color.white;
            RimconemyUi.DrawFeatureStatus(
                new Rect(anchorRect.x, anchorRect.yMax + 4f, anchorRect.width, RimconemyTheme.RowHeight * 2f + 6f),
                "READ-ONLY · Regelkatalog",
                "OPEN · vollständige native Ideology-Verhaltensbindung ist noch nicht aktiv.",
                StatusLevel.Warn);

            // ScrollView with catalogue rows. Start below the status banner
            // so the first rule card cannot overlap the capability explanation.
            float statusBottom = anchorRect.yMax + 4f
                + RimconemyTheme.RowHeight * 2f + 6f;
            var scrollOuter = new Rect(
                inRect.x + 4f, statusBottom + 8f, inRect.width - 8f,
                inRect.height - (statusBottom + 8f - inRect.y));

            var rules = SettingRulesCatalog.ActiveRules();
            float rowHeight = 92f;
            float viewHeight = rules.Count * rowHeight + 12f;
            Widgets.BeginScrollView(scrollOuter, ref _scroll, new Rect(0, 0, scrollOuter.width - 16f, viewHeight));

            float y = 6f;
            foreach (var rule in rules)
            {
                var row = new Rect(0, y, scrollOuter.width - 16f, rowHeight - 6f);
                DrawRuleRow(row, rule);
                y += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private static void DrawRuleRow(Rect rect, SettingRuleEntry rule)
        {
            // Card frame
            Widgets.DrawBox(rect, 1);
            GUI.color = new Color(0.20f, 0.20f, 0.22f, 1f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            var inner = rect.ContractedBy(10f);

            var titleRect = new Rect(inner.x, inner.y, inner.width, 26f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = AccentColor;
            Widgets.Label(titleRect, rule.RuleId);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            var carrierRect = new Rect(inner.x, titleRect.yMax + 2f, inner.width, 20f);
            GUI.color = InkColor;
            Widgets.Label(carrierRect, "Traeger: " + rule.PrimaryCarrier);
            GUI.color = Color.white;

            var moodRect = new Rect(inner.x, carrierRect.yMax + 2f, inner.width, 20f);
            GUI.color = InkColor;
            Widgets.Label(moodRect, "Wirkung: " + rule.MoodOrImpact);
            GUI.color = Color.white;

            var familyRect = new Rect(inner.xMax - 130f, inner.y, 130f, 22f);
            GUI.color = AccentColor;
            Widgets.Label(familyRect, rule.Family);
            GUI.color = Color.white;
        }

        /// <summary>Entry-point helper. Foundation tabs can call this from a button.</summary>
        public static void OpenMainMenu()
        {
            Find.WindowStack?.Add(new SettingRulesInspector());
        }
    }
}
