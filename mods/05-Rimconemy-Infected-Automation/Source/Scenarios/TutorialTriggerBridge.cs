using System;
using Rimconemy.Foundation.Registry;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// Tutorial Trigger Bridge — implements ITutorialTriggerBridge for TutorialDirector.
    /// Fired by other packages via CapabilityAudit.
    /// Owner: Infected & Automation (Paket 05).
    /// </summary>
    public class TutorialTriggerBridge : ITutorialTriggerBridge
    {
        public static TutorialTriggerBridge Instance { get; private set; }

        public event Action OnCampfireBuilt;
        public event Action OnFirstInfectedContact;
        public event Action OnWallBuilt;
        public event Action<ThingDef> OnResourceCollected;

        public static void FireCampfireBuilt() => Instance?.OnCampfireBuilt?.Invoke();
        public static void FireFirstInfectedContact() => Instance?.OnFirstInfectedContact?.Invoke();
        public static void FireWallBuilt() => Instance?.OnWallBuilt?.Invoke();
        public static void FireResourceCollected(ThingDef def) => Instance?.OnResourceCollected?.Invoke(def);

        public static void Initialize()
        {
            Instance = new TutorialTriggerBridge();
            Log.Message("[Rimconemy.InfectedAutomation] TutorialTriggerBridge initialized.");
        }

        // Explicit interface implementation
        event Action ITutorialTriggerBridge.OnCampfireBuilt
        {
            add => OnCampfireBuilt += value;
            remove => OnCampfireBuilt -= value;
        }

        event Action ITutorialTriggerBridge.OnFirstInfectedContact
        {
            add => OnFirstInfectedContact += value;
            remove => OnFirstInfectedContact -= value;
        }

        event Action ITutorialTriggerBridge.OnWallBuilt
        {
            add => OnWallBuilt += value;
            remove => OnWallBuilt -= value;
        }

        event Action<ThingDef> ITutorialTriggerBridge.OnResourceCollected
        {
            add => OnResourceCollected += value;
            remove => OnResourceCollected -= value;
        }
    }
}