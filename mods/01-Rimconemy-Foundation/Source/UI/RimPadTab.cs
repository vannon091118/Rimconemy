using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Enumeration of RimPad tabs.
    /// </summary>
    public enum RimPadTab
    {
        Survival,
        Infrastructure,
        Economy,
        Threat,
        Diagnostics
    }
    
    /// <summary>
    /// Simple wrapper for tab record with drawing callback.
    /// </summary>
    public class RimPadTabRecord
    {
        public RimPadTab Tab;
        public string Label;
        public System.Action<Rect> DrawContent;
        
        public RimPadTabRecord(RimPadTab tab, string label, System.Action<Rect> drawContent)
        {
            Tab = tab;
            Label = label;
            DrawContent = drawContent;
        }
    }
}