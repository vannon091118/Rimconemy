using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Building
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// P6 — Task 8: Bauschutt → Wand/Tür-Remap.
    ///
    /// Read-Model Service: plans the conversion of a discovered Bauschutt
    /// amount into Wall / Door material via the existing storage system
    /// and emits a <see cref="RemapProposal"/> carrying a 1:1 wall + 10:1
    /// door ratio. Service does NOT mutate the world; the workbench UI
    /// confirms and applies the proposal downstream. The actual mechanic
    /// gates (zone placement, blueprint construction, quality carry-over)
    /// belong to a User Live-Test phase.
    ///
    /// Specification: docs/P6-PROGRESS.md Task 8; H4-§3 storage contract
    /// is the single source of resource counts.
    /// </summary>
    public static class BauschuttRemapService
    {
        // Canonical Package-03 material identity. Keep this aligned with the
        // single ThingDef used by StorageQuery, Economy transfers, and Wall stuff.
        public const string BauschuttDefName = "Rimconemy_ConstructionDebris";

        public struct RemapProposal
        {
            public int BauschuttCount;
            public int WallUnitCount;       // produced wall units (1:1 for Phase-6 MVP)
            public int DoorUnitCount;
            public string ReasonBlocked;
        }

        /// <summary>
        /// Walks the player-home storage for Bauschutt material and returns
        /// a proposal. Returns a "blocked" proposal when no Bauschutt exists
        /// in storage or when Current.Game is null.
        /// </summary>
        public static RemapProposal PlanRemapForCurrentMap()
        {
            try
            {
                if (Current.Game == null)
                    return new RemapProposal { ReasonBlocked = "No active game" };

                long tick = Find.TickManager?.TicksGame ?? 0L;
                var snapshot = Storage.StorageQuery.ReadStorage(
                    Storage.StorageScope.PlayerHomeMaps, null, tick);

                int count = 0;
                if (snapshot?.Entries != null)
                {
                    foreach (var e in snapshot.Entries)
                    {
                        if (e != null && e.ResourceId == BauschuttDefName)
                        {
                            count = e.TotalAmount;
                            break;
                        }
                    }
                }

                if (count <= 0)
                {
                    return new RemapProposal { ReasonBlocked = "No Bauschutt in storage" };
                }

                // Floor: 1 Bauschutt → 1 Wall, 10 Bauschutt → 1 Door.
                int walls = count;
                int doors = count / 10;
                return new RemapProposal
                {
                    BauschuttCount = count,
                    WallUnitCount = walls,
                    DoorUnitCount = doors,
                    ReasonBlocked = null,
                };
            }
            catch (System.Exception ex)
            {
                return new RemapProposal
                {
                    ReasonBlocked = "BauschuttRemapService exception: " + ex.GetType().Name,
                };
            }
        }
    }
}
