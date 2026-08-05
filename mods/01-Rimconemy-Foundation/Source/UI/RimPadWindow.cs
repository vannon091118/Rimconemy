using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// RimPad - Floating dashboard / tablet UI for the cinematic intro and tutorial system.
    /// Inherits from RimconemyWindow for consistent chrome (draggable, close button, etc.).
    /// </summary>
    public class RimPadWindow : RimconemyWindow
    {
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
                new RimPadTabRecord(RimPadTab.Survival, "Survival", DrawSurvivalTab),
                new RimPadTabRecord(RimPadTab.Infrastructure, "Infrastructure", DrawInfrastructureTab),
                new RimPadTabRecord(RimPadTab.Economy, "Economy", DrawEconomyTab),
                new RimPadTabRecord(RimPadTab.Threat, "Threat", DrawThreatTab),
                new RimPadTabRecord(RimPadTab.Diagnostics, "Diagnostics", DrawDiagnosticsTab)
            };
            
            RimPadTabDrawer.SetTabs(tabs);
        }
        
        // Placeholder draw methods - to be implemented with snapshot data in Task 9
        private void DrawSurvivalTab(Rect rect) { Widgets.Label(rect, "Survival tab - TODO"); }
        private void DrawInfrastructureTab(Rect rect) { Widgets.Label(rect, "Infrastructure tab - TODO"); }
        private void DrawEconomyTab(Rect rect) { Widgets.Label(rect, "Economy tab - TODO"); }
        private void DrawThreatTab(Rect rect) { Widgets.Label(rect, "Threat tab - TODO"); }
        private void DrawDiagnosticsTab(Rect rect) { Widgets.Label(rect, "Diagnostics tab - TODO"); }
    }
}