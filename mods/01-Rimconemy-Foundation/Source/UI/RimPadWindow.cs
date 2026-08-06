using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// RimPad - Floating dashboard / tablet UI for the cinematic intro and tutorial system.
    /// Inherits from RimconemyWindow for consistent chrome (draggable, close button, etc.).
    ///
    /// Guide-Tab (UX-Audit 2026-08-06): Der Guide-Tab zeigt den Tutorial-Status.
    /// Der Inhalt wird von Paket 05 via <see cref="GuideTabDrawer"/> registriert —
    /// Foundation selbst bleibt referenzfrei (kein Compile-Verweis auf 05).
    /// </summary>
    public class RimPadWindow : RimconemyWindow
    {
        /// <summary>
        /// Von Paket 05 registrierter Drawer für den Guide-Tab. Null solange
        /// kein Inhalt registriert ist — der Tab zeigt dann einen Platzhalter.
        /// Foundation kennt die Quelle des Inhalts nicht (Package-Isolation).
        /// </summary>
        public static System.Action<Rect> GuideTabDrawer;

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        private bool themeApplied = false;

        public override void DoWindowContents(Rect inRect)
        {
            // Apply theme once
            if (!themeApplied)
            {
                RimPadTheme.Apply();
                themeApplied = true;
            }

            // Draw background panel
            Widgets.DrawBoxSolid(new Rect(0, 0, inRect.width, inRect.height), RimPadTheme.PanelBackground);

            // Tab container (top 30px)
            Rect tabRect = new Rect(0, 0, inRect.width, 30f);
            RimPadTabDrawer.DrawTabs(tabRect);

            // Content area (below tabs)
            Rect contentRect = new Rect(0, 35f, inRect.width, inRect.height - 35f);
            RimPadTabDrawer.DrawSelectedTabContent(contentRect);
        }

        public override void PostOpen()
        {
            base.PostOpen();
            // Initialize tabs when window opens
            InitializeTabs();
        }

        private void InitializeTabs()
        {
            var tabs = new List<RimPadTabRecord>
            {
                new RimPadTabRecord(RimPadTab.Guide, "Rimconemy.RimPad.Tab.Guide".Translate(), DrawGuideTab),
                new RimPadTabRecord(RimPadTab.Survival, "Rimconemy.RimPad.Tab.Survival".Translate(), DrawSurvivalTab),
                new RimPadTabRecord(RimPadTab.Infrastructure, "Rimconemy.RimPad.Tab.Infrastructure".Translate(), DrawInfrastructureTab),
                new RimPadTabRecord(RimPadTab.Economy, "Rimconemy.RimPad.Tab.Economy".Translate(), DrawEconomyTab),
                new RimPadTabRecord(RimPadTab.Threat, "Rimconemy.RimPad.Tab.Threat".Translate(), DrawThreatTab),
                new RimPadTabRecord(RimPadTab.Diagnostics, "Rimconemy.RimPad.Tab.Diagnostics".Translate(), DrawDiagnosticsTab)
            };

            RimPadTabDrawer.SetTabs(tabs);
        }

        /// <summary>
        /// Öffnet das RimPad direkt im Guide-Tab (Tutorial-Status).
        /// Wird aus dem Tutorial-Dialog aufgerufen.
        /// </summary>
        public static void OpenGuide()
        {
            var window = new RimPadWindow();
            Find.WindowStack.Add(window);
            RimPadTabDrawer.SelectTab(RimPadTab.Guide);
        }

        private void DrawGuideTab(Rect rect)
        {
            if (GuideTabDrawer != null)
            {
                GuideTabDrawer(rect);
                return;
            }
            Widgets.Label(rect, "Rimconemy.RimPad.Guide.Empty".Translate());
        }

        // Placeholder draw methods - to be implemented with snapshot data in Task 9
        private void DrawSurvivalTab(Rect rect) { Widgets.Label(rect, "Rimconemy.RimPad.Tab.Todo".Translate()); }
        private void DrawInfrastructureTab(Rect rect) { Widgets.Label(rect, "Rimconemy.RimPad.Tab.Todo".Translate()); }
        private void DrawEconomyTab(Rect rect) { Widgets.Label(rect, "Rimconemy.RimPad.Tab.Todo".Translate()); }
        private void DrawThreatTab(Rect rect) { Widgets.Label(rect, "Rimconemy.RimPad.Tab.Todo".Translate()); }
        private void DrawDiagnosticsTab(Rect rect) { Widgets.Label(rect, "Rimconemy.RimPad.Tab.Todo".Translate()); }
    }
}
