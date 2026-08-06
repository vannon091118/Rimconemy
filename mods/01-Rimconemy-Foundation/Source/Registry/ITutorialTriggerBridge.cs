using System;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Registry
{
    /// <summary>
    /// Bridge interface for Tutorial triggers.
    /// Implemented by packages that want to notify TutorialDirector of events.
    /// Owner: Foundation (Paket 01) — Interface contract.
    /// Implemented by: Survival (Paket 02), Scavenger (Paket 03), Infected (Paket 05).
    /// </summary>
    public interface ITutorialTriggerBridge
    {
        event Action OnCampfireBuilt;
        event Action OnFirstInfectedContact;
        event Action OnWallBuilt;
        event Action<ThingDef> OnResourceCollected;
    }
}