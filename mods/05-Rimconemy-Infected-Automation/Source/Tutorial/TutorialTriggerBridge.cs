using Verse;
using Rimconemy.Foundation.Bridge;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    /// <summary>
    /// TutorialTriggerBridge - Static bridge for cross-package tutorial triggers.
    /// Other packages call these methods to signal tutorial events.
    /// </summary>
    public static class TutorialTriggerBridge
    {
        // These will be set by other packages via the foundation bridge
        public static bool CampfireBuilt { get; private set; }
        public static bool FirstInfectedContact { get; private set; }
        public static bool ResourceCollected { get; private set; }
        public static bool WallBuilt { get; private set; }
        public static bool GeneratorBuilt { get; private set; }
        public static bool TurretBuilt { get; private set; }
        public static bool OutpostFounded { get; private set; }
        public static bool TradeDone { get; private set; }
        
        public static void Reset()
        {
            CampfireBuilt = false;
            FirstInfectedContact = false;
            ResourceCollected = false;
            WallBuilt = false;
            GeneratorBuilt = false;
            TurretBuilt = false;
            OutpostFounded = false;
            TradeDone = false;
        }
        
        // Methods to be called by other packages
        public static void OnCampfireBuilt()
        {
            CampfireBuilt = true;
            // Publish via foundation event bridge if needed
            EventBridge.Publish("tutorial.trigger.campfire_built");
        }
        
        public static void OnFirstInfectedContact()
        {
            FirstInfectedContact = true;
            EventBridge.Publish("tutorial.trigger.first_infected_contact");
        }
        
        public static void OnResourceCollected()
        {
            ResourceCollected = true;
            EventBridge.Publish("tutorial.trigger.resource_collected");
        }
        
        public static void OnWallBuilt()
        {
            WallBuilt = true;
            EventBridge.Publish("tutorial.trigger.wall_built");
        }
        
        public static void OnGeneratorBuilt()
        {
            GeneratorBuilt = true;
            EventBridge.Publish("tutorial.trigger.generator_built");
        }
        
        public static void OnTurretBuilt()
        {
            TurretBuilt = true;
            EventBridge.Publish("tutorial.trigger.turret_built");
        }
        
        public static void OnOutpostFounded()
        {
            OutpostFounded = true;
            EventBridge.Publish("tutorial.trigger.outpost_founded");
        }
        
        public static void OnTradeDone()
        {
            TradeDone = true;
            EventBridge.Publish("tutorial.trigger.trade_done");
        }
    }
}