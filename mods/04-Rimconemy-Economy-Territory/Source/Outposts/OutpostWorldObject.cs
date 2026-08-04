// Source/Outposts/OutpostWorldObject.cs
//
// Owner: Economy & Territory
// SPIKE: API-WORLD-01 — concrete WorldObject subclass for standing outpost
// planning records referenced by Defs/Outposts/Outposts.xml.
//
// In RimWorld 1.6 Verse.WorldObject (RimWorld.Planet.WorldObject) is abstract.
// WorldObjectDef.worldObjectClass must point at a concrete, non-abstract
// RimWorld WorldObject subclass or DirectXmlToObjectNew will fail with
// "Could not find a type named ..." during Def parsing.
//
// This stub is intentionally empty: the only contract with the Wallet
// pipeline is identity (defName) + tick stability (no per-tick behaviour).
// Any future spawn logic should live in Rimconemy.EconomyTerritory.Outposts.Outpost
// and replace this stub via a Patch operation.

using RimWorld.Planet;
using Verse;

namespace Rimconemy.EconomyTerritory.Outposts
{
    /// <summary>
    /// Concrete RimWorld.WorldObject subclass for the Rimconemy_OutpostWorldObject
    /// WorldObjectDef. Carries no simulation logic in scaffold state; the Wallet
    /// pipeline queries DefDatabase identity only.
    /// </summary>
    public class OutpostWorldObject : WorldObject
    {
        // intentionally empty — identity-only stub, replaced when game-logic takes over
    }
}
