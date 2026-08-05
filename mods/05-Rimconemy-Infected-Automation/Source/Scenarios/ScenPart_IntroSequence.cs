using Verse;
using RimWorld;
namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// ScenPart that triggers the cinematic intro sequence on map generation.
    /// </summary>
    public class ScenPart_IntroSequence : ScenPart
    {
        private bool introShown;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref introShown, "introShown", false);
        }

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            if (map == null || !map.IsPlayerHome || introShown)
                return;

            // Set the marker before opening the window so repeated map-generation
            // callbacks cannot enqueue the intro twice.
            introShown = true;
            Find.WindowStack.Add(new Rimconemy.InfectedAutomation.UI.IntroFlowWindow());
        }
        
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            // No editables needed
            base.DoEditInterface(listing);
        }
    }
}