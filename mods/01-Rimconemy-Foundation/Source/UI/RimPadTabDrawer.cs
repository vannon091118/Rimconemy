using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Handles tab drawing and selection for the RimPad window.
    /// </summary>
    public static class RimPadTabDrawer
    {
        private static int selectedTabIndex = 0;
        private static List<RimPadTabRecord> tabs = new List<RimPadTabRecord>();
        
        public static void SetTabs(List<RimPadTabRecord> newTabs)
        {
            tabs = newTabs;
            selectedTabIndex = 0;
        }
        
        public static void DrawTabs(Rect tabContainerRect)
        {
            if (tabs == null || tabs.Count == 0) return;
            
            float tabWidth = tabContainerRect.width / tabs.Count;
            for (int i = 0; i < tabs.Count; i++)
            {
                Rect tabRect = new Rect(tabContainerRect.x + (i * tabWidth), tabContainerRect.y, tabWidth, tabContainerRect.height);
                bool isSelected = (i == selectedTabIndex);
                
                Widgets.DrawHighlightIfMouseover(tabRect);
                Widgets.DrawBoxSolid(tabRect, isSelected ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.15f, 0.15f, 0.15f));
                
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tabRect, tabs[i].Label);
                Text.Anchor = TextAnchor.UpperLeft;
                
                if (Mouse.IsOver(tabRect) && Widgets.ButtonInvisible(tabRect))
                {
                    selectedTabIndex = i;
                }
            }
        }
        
        public static void DrawSelectedTabContent(Rect contentRect)
        {
            if (tabs.Count == 0) return;
            tabs[selectedTabIndex].DrawContent(contentRect);
        }
        
        public static int SelectedTabIndex => selectedTabIndex;
        public static RimPadTab SelectedTab => tabs.Count > 0 ? tabs[selectedTabIndex].Tab : RimPadTab.Survival;
    }
}