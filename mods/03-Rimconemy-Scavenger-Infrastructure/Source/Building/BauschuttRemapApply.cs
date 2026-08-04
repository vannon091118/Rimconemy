using System;
using System.Collections.Generic;
using Rimconemy.ScavengerInfrastructure.Storage;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Building
{
    /// <summary>
    /// Phase-3.2 (2026-08-04): ApplyRemap — first real Map-mutation in Paket 03.
    ///
    /// Owner: Scavenger Infrastructure (Package 03) per INTERFACE_CONTRACT §9.1.
    ///
    /// Hook-Architektur:
    ///   1. <see cref="ApplyInput"/> ist die testbare Eingabe-Schicht —
    ///      Tests können sie mit synthetischen Werten füllen ohne echte Map.
    ///   2. <see cref="ApplyRemap"/> ist der Storage-Bridge-Entry-Point:
    ///      liest <see cref="StorageQuery.ReadStorage"/> und füllt <c>ApplyInput</c>.
    ///   3. <see cref="ApplyRemapCore"/> ist die vanilla-vanilla-Mutation: blue-print
    ///      placement via <c>GenPlace.TryPlaceBlueprint</c>. Keine Vanilla-Patch.
    ///   4. <see cref="Designator_BuildWallBauschutt"/> ist der UI-Hook: vanilla
    ///      Architektur-Tab ruft über ProcessInput <c>ApplyRemap</c> auf ohne
    ///      Harmony-Transpiler.
    ///
    /// Owner-Constraint:
    ///   - Paket 03 darf NUR die Map mutieren (Blueprints/Buildings).
    ///   - Paket 03 darf NICHT in Wallet oder andere Inventare schreiben
    ///     (Paket 04). Wenn ein zukünftiger Hook das brauchen würde,
    ///     Capability-Audit-Gate einsetzen (INTERFACE_CONTRACT §9.4).
    ///   - Storage-Reads sind via <see cref="StorageQuery"/> read-only.
    ///
    /// Test-Seams (alle static, default = Produktivverhalten):
    ///   - <see cref="CandidateCellOverride"/> ersetzt enumerator.
    ///   - <see cref="WallSlotPredicateOverride"/> ersetzt occupied-check.
    ///   - <see cref="StuffDefResolver"/> ersetzt DefDatabase (für Test-mock).
    ///   - <see cref="BlueprintPlacerOverride"/> ersetzt GenPlace.TryPlaceBlueprint.
    ///   - <see cref="PlaceAttempts"/> zählt Place-Versuche.
    /// Keine aktiven Test-Effekte bei Default-Werten (alle null / 0).
    ///
    /// Vanilla-Healthy-Verification:
    ///   - <c>GenPlace.TryPlaceBlueprint</c> ist vanilla-native; keine Hooks.
    ///   - Position-Picker ergreift Edge-Cells deterministisch
    ///     (kleinster (x,z) zuerst) — reproduzierbar für Regression-Tests.
    ///   - Reale Build-Phase-Verifikation bleibt Runtime-Gate via
    ///     scripts/runtime_test.sh — der in-process-Test verifiziert nur
    ///     den Placement-Count und die Verhinderung von Doppel-Platzierung.
    /// </summary>
    public static class BauschuttRemapApply
    {
        /// <summary>Vanilla Wall-ThingName für Blueprint-Platzierung.</summary>
        private const string WallThingDefName = "Wall";

        /// <summary>Hard-Cap für eine einzelne Apply-Iteration.</summary>
        public const int MaxWallsPerApply = 200;

        // ── Test-Seams ───────────────────────────────────────────────
        // Default-Werte: null = Produktivverhalten, 0 = kein Increment.

        /// <summary>
        /// Optional Test-Hook: wenn != null, ersetzt dies
        /// <see cref="EnumerateWallsCandidateCells"/>. Tests setzen eine
        /// deterministische Liste von <see cref="IntVec3"/>-Cells, um die
        /// Success-Path-Test auch ohne Real-Map zu ermöglichen.
        /// </summary>
        public static List<IntVec3> CandidateCellOverride = null;

        /// <summary>
        /// Optional Test-Hook: wenn != null, ersetzt dies
        /// <see cref="IsBuildableWallSlot"/>. Tests setzen eine Funktion die
        /// immer true liefert, um die Slot-Logik zu umgehen.
        /// </summary>
        public static Func<IntVec3, Map, bool> WallSlotPredicateOverride = null;

        /// <summary>
        /// Optional Test-Hook: wenn != null, ersetzt dies
        /// <c>DefDatabase&lt;ThingDef&gt;.GetNamedSilentFail</c>. Tests können
        /// damit eine Fake-Stuff-Definition liefern, ohne Def-Database-Eintrag.
        /// </summary>
        public static Func<string, ThingDef> StuffDefResolver = null;

        /// <summary>
        /// Optional Test-Hook: wenn != null, ersetzt dies den Vanilla Wall-
        /// Def-Lookup. Tests umgehen damit die DefDatabase-Abhängigkeit.
        /// </summary>
        public static Func<string, ThingDef> WallDefResolver = null;

        /// <summary>
        /// Optional Test-Hook: wenn != null, liefert dies die Faction
        /// für den Blueprint-Build (überschreibt <c>input.BuilderFaction ?? Faction.OfPlayer</c>).
        /// </summary>
        public static Func<Faction> FactionOverride = null;

        /// <summary>
        /// Optional Test-Hook: wenn != null, ersetzt dies
        /// <c>GenPlace.TryPlaceBlueprint</c>. Tests setzen eine Lambda die
        /// zählt + einen nicht-null Blueprint-Stub liefert — so kann der
        /// Placement-Count deterministisch gemessen werden, ohne echten
        /// Blueprint-Construction-Cycle.
        /// </summary>
        public static Func<ThingDef, IntVec3, Map, Rot4, Faction, ThingDef, Blueprint> BlueprintPlacerOverride = null;

        /// <summary>
        /// Counter: wird bei jedem Place-Versuch inkrementiert (vor dem Aufruf
        /// von GenPlace.TryPlaceBlueprint oder der Override-Lambda). Tests resetten
        /// auf 0 und lesen nach der Operation.
        /// </summary>
        public static int PlaceAttempts = 0;

        /// <summary>
        /// Cleanup-Methode für Tests: setzt alle Seams auf Default zurück.
        /// Sicher vor Test-Order-Effects.
        /// </summary>
        public static void ResetTestSeams()
        {
            CandidateCellOverride = null;
            WallSlotPredicateOverride = null;
            StuffDefResolver = null;
            WallDefResolver = null;
            FactionOverride = null;
            BlueprintPlacerOverride = null;
            PlaceAttempts = 0;
        }

        /// <summary>
        /// Eingabe-Container. Test-freundlich: enthält Map, Bauschutt-Count und
        /// Material-Stuff. Wird von <see cref="ApplyRemap"/> aus StorageQuery
        /// gefüllt; Tests füllen ihn synthetisch.
        /// </summary>
        public struct ApplyInput
        {
            /// <summary>Ziel-Map. null wenn blockiert.</summary>
            public Map TargetMap;

            /// <summary>Bauschutt-Count, der zur Verfügung steht (aus Storage-Snapshot).</summary>
            public int BauschuttAvailable;

            /// <summary>Optional: Stuff-ThingDef für Blueprint-Material (default BauschuttDefName).</summary>
            public string BauschuttStuffDefName;

            /// <summary>Optional: Faction, der die neuen Blueprints gehören sollen (default Player).</summary>
            public Faction BuilderFaction;
        }

        /// <summary>
        /// Result-Container. Belegt die tatsächliche Map-Mutation.
        /// </summary>
        public struct ApplyResult
        {
            /// <summary>Anzahl platzierter Wall-Blueprints (= BauschuttConsumed bei Success).</summary>
            public int WallsPlaced;

            /// <summary>Bauschutt-Count der aus Storage gelesen wurde und Blueprints speist.</summary>
            public int BauschuttConsumed;

            /// <summary>Zellen, an denen Blueprints platziert wurden (deterministisch sortiert).</summary>
            public List<IntVec3> PlacedAt;

            /// <summary>Zellen, die nicht platzierbar waren. Listet die ersten 5 mit Grund.</summary>
            public List<string> PlacementFailures;

            /// <summary>Wenn != null: ApplyRemap hat früh abgebrochen (z.B. kein Map, kein Bauschutt).</summary>
            public string ReasonBlocked;
        }

        /// <summary>
        /// Test-barer Kern. Nimmt eine fertige <see cref="ApplyInput"/> entgegen
        /// und gibt <see cref="ApplyResult"/> zurück. Wird vom UI-Hook-Schritt
        /// mit synthetischer oder realer Storage-Bridge verwendet.
        /// Verwendet die Test-Seams wenn sie gesetzt sind, sonst Produktivverhalten.
        /// </summary>
        public static ApplyResult ApplyRemapCore(ApplyInput input)
        {
            var result = new ApplyResult
            {
                WallsPlaced = 0,
                BauschuttConsumed = 0,
                PlacedAt = new List<IntVec3>(),
                PlacementFailures = new List<string>(),
                ReasonBlocked = null,
            };

            try
            {
                // TargetMap-Bypass: wenn Test-Cells via Seam geliefert werden,
                // ist Map nicht zwingend. Production-Pfad verlangt weiterhin Map.
                if (input.TargetMap == null && CandidateCellOverride == null)
                {
                    result.ReasonBlocked = "TargetMap is null";
                    return result;
                }

                if (input.BauschuttAvailable <= 0)
                {
                    result.ReasonBlocked = "No Bauschutt available";
                    return result;
                }

                // Wall ThingDef (vanilla) — DefDatabase oder Test-Seam.
                ThingDef wallDef = null;
                if (WallDefResolver != null)
                {
                    wallDef = WallDefResolver(WallThingDefName);
                }
                else
                {
                    wallDef = DefDatabase<ThingDef>.GetNamedSilentFail(WallThingDefName);
                }
                if (wallDef == null)
                {
                    result.ReasonBlocked = "Vanilla Wall ThingDef not found: " + WallThingDefName;
                    return result;
                }

                string stuffName = string.IsNullOrEmpty(input.BauschuttStuffDefName)
                    ? BauschuttRemapService.BauschuttDefName
                    : input.BauschuttStuffDefName;

                ThingDef stuffDef = null;
                if (StuffDefResolver != null)
                {
                    stuffDef = StuffDefResolver(stuffName);
                }
                else if (!string.IsNullOrEmpty(stuffName))
                {
                    stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(stuffName);
                }

                if (stuffDef == null)
                {
                    result.ReasonBlocked = "Stuff ThingDef not found: " + stuffName;
                    return result;
                }

                Faction faction = FactionOverride != null
                    ? FactionOverride()
                    : (input.BuilderFaction ?? Faction.OfPlayer);
                if (faction == null)
                {
                    result.ReasonBlocked = "Builder Faction is null";
                    return result;
                }

                Map map = input.TargetMap;
                int remaining = input.BauschuttAvailable;
                int toPlace = Math.Min(remaining, MaxWallsPerApply);

                IEnumerable<IntVec3> candidateSource = CandidateCellOverride != null
                    ? (IEnumerable<IntVec3>)CandidateCellOverride
                    : EnumerateWallsCandidateCells(map, toPlace);

                foreach (var cell in candidateSource)
                {
                    if (remaining <= 0) break;

                    bool slotOk = WallSlotPredicateOverride != null
                        ? WallSlotPredicateOverride(cell, map)
                        : IsBuildableWallSlot(map, cell);

                    if (!slotOk)
                    {
                        if (result.PlacementFailures.Count < 5)
                            result.PlacementFailures.Add(cell + ": occupied");
                        continue;
                    }

                    // Vanilla-mutation via GenPlace.TryPlaceBlueprint
                    PlaceAttempts += 1;
                    Blueprint blueprint;
                    if (BlueprintPlacerOverride != null)
                    {
                        blueprint = BlueprintPlacerOverride(wallDef, cell, map, Rot4.North, faction, stuffDef);
                    }
                    else
                    {
                        blueprint = GenPlace.TryPlaceBlueprint(
                            wallDef,
                            cell,
                            map,
                            Rot4.North,
                            faction,
                            stuffDef);
                    }

                    if (blueprint == null)
                    {
                        if (result.PlacementFailures.Count < 5)
                            result.PlacementFailures.Add(cell + ": GenPlace returned null");
                        continue;
                    }

                    result.WallsPlaced += 1;
                    result.BauschuttConsumed += 1;
                    result.PlacedAt.Add(cell);
                    remaining -= 1;
                }

                if (result.WallsPlaced == 0 && remaining == toPlace)
                {
                    result.ReasonBlocked = "No buildable wall slot found on Map";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ReasonBlocked = "ApplyRemapCore exception: " + ex.GetType().Name;
                return result;
            }
        }

        /// <summary>
        /// Storage-Bridge-Entry-Point. Liest die aktuelle Storage-Snapshot,
        /// filtert auf BauschuttDefName, und ruft <see cref="ApplyRemapCore"/>
        /// mit dem ersten gefundenen Player-Home-Map.
        /// </summary>
        public static ApplyResult ApplyRemap()
        {
            try
            {
                if (Current.Game == null)
                {
                    return new ApplyResult
                    {
                        ReasonBlocked = "No active game",
                        PlacedAt = new List<IntVec3>(),
                        PlacementFailures = new List<string>(),
                    };
                }

                long tick = Find.TickManager?.TicksGame ?? 0L;
                var snapshot = StorageQuery.ReadStorage(
                    StorageScope.PlayerHomeMaps, null, tick);

                int bauschuttCount = 0;
                if (snapshot?.Entries != null)
                {
                    foreach (var e in snapshot.Entries)
                    {
                        if (e != null && e.ResourceId == BauschuttRemapService.BauschuttDefName)
                        {
                            bauschuttCount = e.TotalAmount;
                            break;
                        }
                    }
                }

                Map targetMap = null;
                if (Find.Maps != null)
                {
                    foreach (var m in Find.Maps)
                    {
                        if (m != null && m.IsPlayerHome)
                        {
                            targetMap = m;
                            break;
                        }
                    }
                }

                var input = new ApplyInput
                {
                    TargetMap = targetMap,
                    BauschuttAvailable = bauschuttCount,
                    BauschuttStuffDefName = BauschuttRemapService.BauschuttDefName,
                    BuilderFaction = Faction.OfPlayer,
                };

                return ApplyRemapCore(input);
            }
            catch (Exception ex)
            {
                return new ApplyResult
                {
                    ReasonBlocked = "ApplyRemap exception: " + ex.GetType().Name,
                    PlacedAt = new List<IntVec3>(),
                    PlacementFailures = new List<string>(),
                };
            }
        }

        // ── Position-Picker (deterministisch, edge first) ─────────────

        /// <summary>
        /// Iteriert Map-Zellen deterministisch: erst x von 0..sizeX-1 und z von 0..sizeZ-1.
        /// Liefert bis <paramref name="limit"/> Cells.
        /// </summary>
        private static IEnumerable<IntVec3> EnumerateWallsCandidateCells(Map map, int limit)
        {
            if (map == null) yield break;
            int sizeX = map.Size.x;
            int sizeZ = map.Size.z;
            int yielded = 0;

            for (int x = 0; x < sizeX && yielded < limit; x++)
            {
                for (int z = 0; z < sizeZ && yielded < limit; z++)
                {
                    yield return new IntVec3(x, 0, z);
                    yielded += 1;
                }
            }
        }

        /// <summary>
        /// Wand-Slot ist baubar, wenn die Cell keine bereits platzierte
        /// Wand-Ting hat und in der Map-EDGES-Zone liegt (deterministischer
        /// Edge-First-Pattern).
        /// </summary>
        private static bool IsBuildableWallSlot(Map map, IntVec3 cell)
        {
            if (map == null) return false;
            if (!cell.InBounds(map)) return false;

            // Edge-zone: x oder z am Rand (1-cell-buffer für visuell Place-Markierung)
            int xEdge = (cell.x == 0 || cell.x == map.Size.x - 1);
            int zEdge = (cell.z == 0 || cell.z == map.Size.z - 1);
            if (!xEdge && !zEdge) return false;

            // Bereits belegt?
            var things = cell.GetThingList(map);
            if (things != null)
            {
                foreach (var t in things)
                {
                    if (t == null) continue;
                    if (t.def?.defName == WallThingDefName) return false;
                    if (t.def?.defName == BauschuttRemapService.BauschuttDefName) return false;
                }
            }
            return true;
        }
    }
}
