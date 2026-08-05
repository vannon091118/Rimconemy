// Source/Outposts/OutpostWorldObject.cs
//
// Owner: Economy & Territory
//
// RimWorld 1.6 def-system requirement (2026-08-05): Verse.WorldObject
// (RimWorld.Planet.WorldObject) is abstract. WorldObjectDef.worldObjectClass
// MUST point at a concrete, non-abstract RimWorld WorldObject subclass
// or DirectXmlToObjectNew fails with "Could not find a type named ..."
// during Def parsing.
//
// This leaf class is therefore kept EMPTY BY DESIGN — the only contract
// with the Wallet pipeline is identity (defName) + tick stability (no
// per-tick behaviour). Any future spawn logic should live in
// Rimconemy.EconomyTerritory.Outposts.Outpost and migrate this def-class
// away via a Patch operation; the class itself remains the
// def-system-anatomical complement.

using RimWorld.Planet;
using Verse;

namespace Rimconemy.EconomyTerritory.Outposts
{
    /// <summary>
    /// Concrete RimWorld.WorldObject subclass for the
    /// Rimconemy_OutpostWorldObject WorldObjectDef. Carries no simulation
    /// logic; the Wallet pipeline queries DefDatabase identity only.
    /// </summary>
    public class OutpostWorldObject : WorldObject
    {
        // intentionally empty — RimWorld def-system requirement; no
        // simulation or wallet state lives on this class.
    }
}
