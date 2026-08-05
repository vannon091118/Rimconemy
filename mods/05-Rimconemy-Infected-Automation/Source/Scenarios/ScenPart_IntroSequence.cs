using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// ScenPart that triggers the cinematic intro sequence on map generation.
    /// </summary>
    public class ScenPart_IntroSequence : ScenPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            // No data to expose for now
        }
        
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            // Add the intro window - this will pause the game and show our sequence
            Find.WindowStack.Add(new Rimconemy.InfectedAutomation.UI.IntroFlowWindow());
        }
        
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            // No editables needed
            base.DoEditInterface(listing);
        }
    }
}