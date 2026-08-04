using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Canonical
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    ///
    /// VC-4 / Phase VC — Room Role Resolver.
    ///
    /// Vanilla already classifies rooms via <see cref="RoomRoleDef"/> (Kitchen,
    /// Barracks, PrisonBarracks, Workshop, …). Rimconemy MUST NOT introduce
    /// a parallel room graph — that was the drift in Phase-0 (the now-removed
    /// <c>RimconemyRoomRole</c> struct in another module). The right model is
    /// a one-pass resolver that maps vanilla role defNames to one of five
    /// functional categories used by StorageSnapshot, Market and Threat.
    /// </summary>
    public enum RimconemyRoomRole
    {
        /// <summary>Unclassified — vanilla room role had no functional match.</summary>
        Other = 0,

        /// <summary>Production (Kitchen, Workshop, Smithy, TailorBench, DrugLab, …).</summary>
        Produktion = 1,

        /// <summary>Storage (no dedicated vanilla role — detected by storage furniture).</summary>
        Lager = 2,

        /// <summary>Defense (PrisonBarracks, Barracks only when fortification tag present).</summary>
        Verteidigung = 3,

        /// <summary>Housing (Bedroom, Barracks, Royalty bedroom).</summary>
        Wohnen = 4,

        /// <summary>Infrastructure (Hospital, Laboratory, Recreation, Throne room).</summary>
        Infrastruktur = 5,
    }

    /// <summary>
    /// Canonical resolver. Read-only. Maps vanilla <see cref="Room.Role"/>
    /// to a <see cref="RimconemyRoomRole"/> via an explicit dictionary so
    /// test fixtures can override the table without touching RimWorld's
    /// immutable def set.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RoomRoleResolver
    {
        // Vanilla roles → Rimconemy functional category. Anything not in the
        // table maps to <see cref="RimconemyRoomRole.Other"/> via Lookup.
        private static Dictionary<string, RimconemyRoomRole> _vanillaToRimconemy;

        // Phase-VC / 2026-08-04 Medium-Fix (Code-Reviewer): furniture tags
        // are checked on every room resolution by exact defName match in a
        // pre-built HashSet. Pre-built once per session in
        // <see cref="RebuildFurnitureHashSets"/>; the per-room lookup is
        // O(1) per furniture instance. Old IndexOf-based logic was O(n*m)
        // per thing per room.
        private static HashSet<string> _storageFurnitureDefNames = new HashSet<string>(StringComparer.Ordinal);
        private static HashSet<string> _defenseFurnitureDefNames = new HashSet<string>(StringComparer.Ordinal);
        private static bool _furnitureReady;

        static RoomRoleResolver()
        {
            ReindexVanillaTable();
            RebuildFurnitureHashSets();
        }

        /// <summary>
        /// Builds the canonical vanilla→Rimconemy mapping. Tested via
        /// <c>FoundationCanonicalLayerTests</c> with synthetic entries.
        /// </summary>
        public static void ReindexVanillaTable()
        {
            _vanillaToRimconemy = new Dictionary<string, RimconemyRoomRole>(StringComparer.OrdinalIgnoreCase);

            // Produktion
            _vanillaToRimconemy["Kitchen"]         = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["Workshop"]        = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["Smithy"]          = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["TailorBenchRoom"] = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["DrugLab"]         = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["Brewery"]         = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["ButcherRoom"]     = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["CookingRoom"]     = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["Refinery"]        = RimconemyRoomRole.Produktion;
            _vanillaToRimconemy["FabricsWorkshop"] = RimconemyRoomRole.Produktion;

            // Lager — vanilla does not have an explicit "storage" role, so
            // the resolver detects it via furniture in <see cref="ResolveIncludingFurniture"/>.
            // (we still register a fall-through label in case a future DLC adds it)
            _vanillaToRimconemy["Storage"]         = RimconemyRoomRole.Lager;

            // Verteidigung
            _vanillaToRimconemy["PrisonBarracks"]  = RimconemyRoomRole.Verteidigung;
            _vanillaToRimconemy["PrisonCell"]      = RimconemyRoomRole.Verteidigung;

            // Wohnen
            _vanillaToRimconemy["Bedroom"]         = RimconemyRoomRole.Wohnen;
            _vanillaToRimconemy["Barracks"]        = RimconemyRoomRole.Wohnen;
            _vanillaToRimconemy["Room"]            = RimconemyRoomRole.Wohnen; // fallback vanilla role

            // Infrastruktur
            _vanillaToRimconemy["Hospital"]        = RimconemyRoomRole.Infrastruktur;
            _vanillaToRimconemy["Laboratory"]      = RimconemyRoomRole.Infrastruktur;
            _vanillaToRimconemy["RecreationRoom"]  = RimconemyRoomRole.Infrastruktur;
            _vanillaToRimconemy["ThroneRoom"]      = RimconemyRoomRole.Infrastruktur;
        }

        /// <summary>
        /// Resolve a vanilla <see cref="Room"/> to a Rimconemy role via its
        /// declared role. Does NOT inspect furniture, so the answer is
        /// instantaneous and deterministic.
        /// </summary>
        public static RimconemyRoomRole Resolve(Room room)
        {
            if (room == null) return RimconemyRoomRole.Other;
            if (_vanillaToRimconemy == null) ReindexVanillaTable();
            if (room.Role == null) return RimconemyRoomRole.Other;

            string roleName = room.Role.defName;
            if (string.IsNullOrEmpty(roleName))
            {
                roleName = room.Role.label?.ToString();
                if (string.IsNullOrEmpty(roleName)) return RimconemyRoomRole.Other;
            }

            RimconemyRoomRole role;
            if (_vanillaToRimconemy.TryGetValue(roleName, out role))
                return role;
            return RimconemyRoomRole.Other;
        }

        /// <summary>
        /// Resolve a vanilla Room to a Rimconemy role, AND up-classify the
        /// base role based on detected furniture markers:
        ///   - storage furniture in <see cref="_storageFurnitureDefNames"/> -> Lager
        ///   - defense furniture in <see cref="_defenseFurnitureDefNames"/> -> Verteidigung
        ///
        /// Per-room lookup is O(items_in_room) and each item-check is O(1).
        ///
        /// We deliberately keep this expensive variant distinct from
        /// <see cref="Resolve"/> so dashboards can choose the lighter
        /// codepath when only Role.defName is needed.
        /// </summary>
        public static RimconemyRoomRole ResolveIncludingFurniture(Room room)
        {
            RimconemyRoomRole baseRole = Resolve(room);
            if (!_furnitureReady) RebuildFurnitureHashSets();

            if (room == null || room.ContainedAndAdjacentThings == null)
                return baseRole;

            bool hasStorage = false;
            bool hasDefense = false;
            foreach (var thing in room.ContainedAndAdjacentThings)
            {
                if (thing == null || thing.def == null) continue;
                string defName = thing.def.defName;
                if (string.IsNullOrEmpty(defName)) continue;

                if (!hasStorage && _storageFurnitureDefNames.Contains(defName))
                {
                    hasStorage = true;
                    // Lager is stronger than Wohnen/Infrastruktur/Other; we
                    // can break early if the base role is Wohnen and we
                    // detect storage only.
                    if (baseRole == RimconemyRoomRole.Wohnen
                        || baseRole == RimconemyRoomRole.Infrastruktur
                        || baseRole == RimconemyRoomRole.Other)
                        return RimconemyRoomRole.Lager;
                }
                if (!hasDefense && _defenseFurnitureDefNames.Contains(defName))
                {
                    hasDefense = true;
                    if (baseRole == RimconemyRoomRole.Wohnen
                        || baseRole == RimconemyRoomRole.Produktion
                        || baseRole == RimconemyRoomRole.Other)
                        return RimconemyRoomRole.Verteidigung;
                }
            }

            // If base role was already Lager / Verteidigung the explicit mapping
            // already wins; we don't downgrade.
            return baseRole;
        }

        /// <summary>
        /// Phase-VC / 2026-08-04 Medium-Fix: rebuild the pre-classified
        /// furniture HashSets once per session by scanning
        /// <see cref="DefDatabase{ThingDef}"/>. Cheap (one-shot),
        /// O(defs) at boot, O(1) per furniture check at runtime.
        ///
        /// Membership is determined by the defName containing a known tag
        /// (case-insensitive), so vanilla Stockpile zones count as
        /// "storage" furniture for free. The set is allowed to be empty
        /// if DefDatabase is not yet populated — Defensive against calls
        /// from early-boot [StaticConstructorOnStartup] chains.
        /// </summary>
        public static void RebuildFurnitureHashSets()
        {
            _storageFurnitureDefNames = new HashSet<string>(StringComparer.Ordinal);
            _defenseFurnitureDefNames = new HashSet<string>(StringComparer.Ordinal);
            _furnitureReady = true;

            try
            {
                if (DefDatabase<ThingDef>.AllDefsListForReading == null) return;
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def == null || string.IsNullOrEmpty(def.defName)) continue;
                    string name = def.defName;
                    if (name.IndexOf("Shelf", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Container", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Stockpile", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("CargoBay", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _storageFurnitureDefNames.Add(name);
                    }
                    if (name.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Fortification", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Barricade", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _defenseFurnitureDefNames.Add(name);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation.Canonical] RoomRoleResolver RebuildFurnitureHashSets dropped: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        /// <summary>Label for the resolved role (keyed lookup w/ German fallback).</summary>
        public static string Label(RimconemyRoomRole role)
        {
            switch (role)
            {
                case RimconemyRoomRole.Produktion:    return "Rimconemy.Room.Produktion".TranslateOrFallback("Produktion");
                case RimconemyRoomRole.Lager:         return "Rimconemy.Room.Lager".TranslateOrFallback("Lager");
                case RimconemyRoomRole.Verteidigung:  return "Rimconemy.Room.Verteidigung".TranslateOrFallback("Verteidigung");
                case RimconemyRoomRole.Wohnen:        return "Rimconemy.Room.Wohnen".TranslateOrFallback("Wohnen");
                case RimconemyRoomRole.Infrastruktur: return "Rimconemy.Room.Infrastruktur".TranslateOrFallback("Infrastruktur");
                default:                              return "Rimconemy.Room.Other".TranslateOrFallback("Sonstiges");
            }
        }

        /// <summary>Bulk resolve for snapshot use (multiple rooms in one pass).</summary>
        public static Dictionary<int, RimconemyRoomRole> ResolveBulk(IEnumerable<Room> rooms)
        {
            var map = new Dictionary<int, RimconemyRoomRole>();
            if (rooms == null) return map;
            foreach (var room in rooms)
            {
                if (room == null || room.ID < 0) continue;
                map[room.ID] = Resolve(room);
            }
            return map;
        }
    }
}
