# H4 — Storage-Query-Vertrag

> **Owner:** Research/Design (kein Code)
> **Status:** `CODE` — `StorageSnapshot` und `StorageQuery.ReadStorage()` sind implementiert; Caravan-/Temporary-Map-, Save/Load- und vollständige Konsumenten-Gates bleiben offen.
> **Referenz:** [ROADMAP.md §3 Phase 3](../ROADMAP.md#phase-3--storage-only-ressourcenmodell), [ROADMAP.md §8.4](../ROADMAP.md#84-phase-34--storage-only--character-setup-offen)

## Zweck

Eine **einzige Read-only-Abfrage** definieren, die Story Writer, UI und Economy als gemeinsame Quelle für Survivor-Ressourcen nutzen. Kein zweites abstraktes Survivor-Inventar und kein paralleles Ledger für dieselben physischen Items.

---

## 1. Kernsignatur

```csharp
namespace Rimconemy.ScavengerInfrastructure.Storage
{
    /// <summary>
    /// Read-only storage snapshot. The single source of truth for all
    /// survivor resources. Story Writer, UI, and Economy read from this;
    /// no package may write to it directly.
    /// </summary>
    public static class StorageQuery
    {
        /// <summary>
        /// Reads the current storage state for the given scope and filter.
        /// Returns a read-only-by-contract snapshot object. No mutation of game state.
        /// </summary>
        /// <param name="scope">Which maps/storages to include.</param>
        /// <param name="filter">Which resources to include. If null, all.</param>
        /// <param name="tick">Game tick for snapshot timestamp.</param>
        /// <returns>Aggregated resource snapshot; callers must treat it as read-only.</returns>
        public static StorageSnapshot ReadStorage(
            StorageScope scope,
            ResourceFilter filter,
            long tick);
    }
}
```

---

## 2. Datenstrukturen

### `StorageSnapshot`

```csharp
public sealed class StorageSnapshot
{
    /// <summary>Snapshot schema version for save migration.</summary>
    public int SchemaVersion = 1;

    /// <summary>Game tick when this snapshot was taken.</summary>
    public long SnapshotTick;

    /// <summary>Scope used for this snapshot.</summary>
    public StorageScope Scope;

    /// <summary>Aggregated resource entries, sorted by ResourceId ascending.</summary>
    public List<StorageEntry> Entries;

    /// <summary>
    /// Maps that were unavailable at snapshot time.
    /// Key = map unique ID, Value = reason.
    /// </summary>
    public Dictionary<int, string> UnavailableMaps;

    /// <summary>Hash of all entries for deterministic comparison.</summary>
    public string ContentHash;
}
```

### `StorageEntry`

```csharp
public sealed class StorageEntry
{
    /// <summary>Stable ThingDef.defName (e.g., "WoodLog", "RawRice").</summary>
    public string ResourceId;

    /// <summary>Display label (cached from ThingDef.label).</summary>
    public string Label;

    /// <summary>Aggregated total amount (sum of all stackCounts).</summary>
    public int TotalAmount;

    /// <summary>Number of distinct stacks.</summary>
    public int StackCount;

    /// <summary>
    /// Availability status:
    /// - Available: fully accessible
    /// - Blocked: in reserved/inaccessible storage
    /// - Unavailable: on unloaded map or destroyed
    /// - Frozen: temporarily locked (e.g., during event)
    /// </summary>
    public StorageAvailability Availability;

    /// <summary>Location(s) as MapId list. "Unknown" if unloaded.</summary>
    public List<int> MapIds;

    /// <summary>Optional quality aggregation (min/avg/max).</summary>
    public QualityAggregation? Quality;

    /// <summary>Optional rot/decay aggregation.</summary>
    public RotAggregation? Rot;
}

public enum StorageAvailability
{
    Available,
    Blocked,
    Unavailable,
    Frozen
}

public struct QualityAggregation
{
    public int MinQuality;  // 0-6 (Awful-Legendary)
    public float AvgQuality;
    public int MaxQuality;
}

public struct RotAggregation
{
    public float MinRotProgress;  // 0-1
    public float AvgRotProgress;
    public int RottenStacks;  // stacks past rot threshold
    public int FreshStacks;
}
```

### `StorageScope`

```csharp
public enum StorageScope
{
    /// <summary>All player-home maps only.</summary>
    PlayerHomeMaps,

    /// <summary>All loaded maps regardless of ownership.</summary>
    AllLoadedMaps,

    /// <summary>Specific map by ID.</summary>
    SpecificMap,        // requires mapId

    /// <summary>All maps including caravan/temporary maps.</summary>
    AllMapsIncludingCaravans,
}

public struct ResourceFilter
{
    /// <summary>If non-empty, only include these ResourceIds.</summary>
    public List<string> WhitelistResourceIds;

    /// <summary>If non-empty, exclude these ResourceIds.</summary>
    public List<string> BlacklistResourceIds;

    /// <summary>Minimum availability to include. Default: Available.</summary>
    public StorageAvailability MinAvailability;

    /// <summary>If non-null, only include these ThingCategories.</summary>
    public List<string> WhitelistCategories;

    /// <summary>Include items in pawn inventories. The zero-value default is false.</summary>
    public bool IncludePawnInventory;

    /// <summary>Include items on the ground. The zero-value default is false.</summary>
    public bool IncludeGroundItems;

    /// <summary>Include items in storage zones. The zero-value default is false.</summary>
    public bool IncludeStorageZones;
}
```

---

## 3. Implementierung (Code-Abgleich)

Die nachfolgende Beschreibung ist gegen `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Storage/StorageQuery.cs` zu lesen. Die Methode ist implementiert, nicht nur Pseudocode; einzelne Randfälle bleiben ausdrücklich offen.

- `ReadStorage()` nutzt einen 250-Tick-Cache und invalidiert bei Scope-Wechsel oder explizitem `InvalidateCache()`.
- Der aktuelle Cache-Key berücksichtigt **nicht** `ResourceFilter`; Aufrufer mit unterschiedlichen Filtern müssen `ReadStorageFresh()` oder `InvalidateCache()` verwenden. Das ist eine bekannte Implementierungsgrenze, kein behaupteter Filter-Cache-Vertrag.
- `ReadStorageFresh()` umgeht den Cache.
- Maps werden aus `Find.Maps` aufgelöst; die aktuelle Implementierung enumeriert damit nur geladene Maps.
- gezählt werden nur gelagerte Things: Stockpile-Zonen und `Building_Storage`.
- Einträge werden deterministisch nach `ResourceId` sortiert und per FNV-1a gehasht.
- Qualität, Verderb und Forbidden-Status werden aggregiert.
- Caravan-/Temporary-Map-Auflösung ist noch nicht implementiert.

## 3a. Verkürzte Implementierungsskizze (Pseudocode)

Die folgende Skizze zeigt die relevanten Datenflüsse, ist nicht vollständig kompilierbar: `ResolveMaps()`, `IsInStorage()` und `ComputeStableHash()` stehen für die gleichnamigen internen Hilfen des aktuellen Codes.

```csharp
public static StorageSnapshot ReadStorage(
    StorageScope scope, ResourceFilter? filter, long tick)
{
    var entries = new Dictionary<string, StorageEntry>(StringComparer.Ordinal);
    var unavailableMaps = new Dictionary<int, string>();
    var maps = ResolveMaps(scope);

    foreach (var map in maps)
    {
        if (map == null)
            continue;

        // The implementation counts only stockpile/shelf storage; pawn
        // inventories, ground items outside stockpiles, and other buildings
        // are excluded before the optional ResourceFilter is applied.
        foreach (var thing in map.listerThings.AllThings)
        {
            if (thing == null || thing.def == null || !IsInStorage(thing, map)
                || !MatchesFilter(thing, filter))
                continue;

            var resourceId = thing.def.defName;
            if (!entries.TryGetValue(resourceId, out var entry))
            {
                entry = new StorageEntry
                {
                    ResourceId = resourceId,
                    Label = thing.def.label,
                    MapIds = new List<int>(),
                };
                entries[resourceId] = entry;
            }

            entry.TotalAmount += thing.stackCount;
            entry.StackCount++;
            if (!entry.MapIds.Contains(map.uniqueID))
                entry.MapIds.Add(map.uniqueID);

            // Quality aggregation (if applicable)
            var qualityComp = thing.TryGetComp<CompQuality>();
            if (qualityComp != null && entry.Quality == null)
            {
                entry.Quality = new QualityAggregation
                {
                    MinQuality = (int)qualityComp.Quality,
                    AvgQuality = (int)qualityComp.Quality,
                    MaxQuality = (int)qualityComp.Quality,
                };
            }
            // (simplified — production code would properly aggregate)

            // Determine availability
            if (thing.IsForbidden(Faction.OfPlayer))
                entry.Availability = StorageAvailability.Blocked;
            else
                entry.Availability = StorageAvailability.Available;
        }
    }

    // Sort entries by ResourceId for deterministic output
    var sortedEntries = entries.Values
        .OrderBy(e => e.ResourceId, StringComparer.Ordinal)
        .ToList();

    // Compute the same stable FNV-1a content hash as StorageQuery.
    var hashBuilder = new StringBuilder();
    foreach (var e in sortedEntries)
        hashBuilder.Append(e.ResourceId).Append(':')
                   .Append(e.TotalAmount).Append(';');
    var contentHash = ComputeStableHash(hashBuilder.ToString());

    return new StorageSnapshot
    {
        SchemaVersion = 1,
        SnapshotTick = tick,
        Scope = scope,
        Entries = sortedEntries,
        UnavailableMaps = unavailableMaps,
        ContentHash = contentHash,
    };
}

private static bool MatchesFilter(Thing thing, ResourceFilter? filter)
{
    if (filter == null) return true;
    var f = filter.Value;

    if (!f.IncludeGroundItems && thing.ParentHolder is Map)
        return false;
    if (!f.IncludeStorageZones && thing.IsInAnyStorage())
        return false;
    if (!f.IncludePawnInventory && thing.ParentHolder is Pawn)
        return false;

    var resourceId = thing.def.defName;
    if (f.WhitelistResourceIds?.Count > 0
        && !f.WhitelistResourceIds.Contains(resourceId))
        return false;
    if (f.BlacklistResourceIds?.Contains(resourceId) == true)
        return false;

    return true;
}
```

---

## 4. Randfälle und Regeln

| Fall | Regel | Verhalten |
|---|---|---|
| **Unloaded Map** | Map nicht in `Find.Maps` | `UnavailableMaps` bleibt im aktuellen Code leer, weil `ResolveMaps()` nur geladene `Find.Maps` enumeriert. Eine explizite `Unavailable`-Markierung für nicht geladene Maps ist ein offenes Erweiterungs-/Spike-Gate; Einträge werden nicht als 0 gezählt. |
| **Caravan/Temporary Map** | `StorageScope.AllMapsIncludingCaravans` | Aktuell werden nur geladene `Find.Maps` erfasst; Caravan-/Temporary-Map-Enumeration ist offen. |
| **Kartenwechsel** | Snapshot wird neu gebaut | Keine Übernahme alter Werte. Neuer Snapshot = neue Timestamps + Hash. |
| **Save/Load** | Snapshot wird **nicht persistiert** | `StorageSnapshot` wird aus den aktuell geladenen Maps rekonstruiert; der aktuelle Code speichert keinen Snapshot-Hash als eigenen Save-State. |
| **Cache** | Letzter Snapshot + Scope/Tick | Derselbe Scope wird bis unter 250 Ticks aus dem Cache geliefert. Scope-Wechsel, `InvalidateCache()` oder ein Alter von mindestens 250 Ticks erzwingen einen neuen Scan; Thing-Änderungen invalidieren nicht automatisch. |
| **Qualität/Verderb** | Optional, nur wenn Comp existiert | `CompQuality` / `CompRottable` aggregieren Min/Avg/Max. Fehlt der Comp → `null`. |
| **Credits/Wallet** | **Nicht** im StorageSnapshot | Credits sind Wallet-Daten, keine physischen Items. Getrennte Abfrage. |
| **Performance** | Kein Scan pro UI-Frame | Snapshot wird max. 1× pro 250 Ticks gebaut. UI liest nur den gecachten Snapshot. |
| **Null-Ergebnis** | Keine geladenen Maps/Items | `Entries` = leere Liste, nicht null. `ContentHash` = Hash von "". |
| **Doppelte Scans** | Mehrere Pakete fragen ab | `StorageQuery` in Paket 03 cached den letzten Snapshot; Konsumenten lesen die zurückgegebene Instanz und schreiben keine physischen Bestände. |

---

## 5. Konsumenten-Vertrag

| Konsument | Nutzung | Darf schreiben? |
|---|---|---|
| **Story Writer** (Phase 1) | Liest `StorageSnapshot` für `SituationSnapshot.Storage` | ❌ Nein |
| **UI / Dashboard** (Foundation) | Zeigt Ressourcen-Übersicht | ❌ Nein |
| **Economy** (Phase 6) | Liest physische Waren für Markt/Handel | ❌ Nein |
| **Scavenger Infrastructure** (Phase 3) | **Besitzer** der Read-Funktion | ✅ Ja (nur Read, nicht Write) |
| **Survival Progression** (Phase 2–4) | Liest für Need-Berechnung (Nahrung) | ❌ Nein |

---

## 6. Geplantes Save-Schema für Snapshot-Referenz

```yaml
# In Foundation-Save oder StoryState
StorageSnapshotReference:
  LastSnapshotTick: long
  LastSnapshotHash: string
  CacheValidUntilTick: long
```

Der Snapshot selbst wird **nicht persistiert**. Die folgende Referenz ist ein geplanter Vertrag für Drift-Erkennung; der aktuelle `StorageQuery`-Code schreibt diese Felder nicht eigenständig.

---

## Nächster Schritt (User)

1. `Map.listerThings.AllThings`-Enumeration gegen lokale Assembly kompilieren
2. `CompQuality`- und `CompRottable`-Casts testen
3. Storage-Scan mit einer Test-Map (3 Ressourcen, 2 Maps) durchführen und Hash-Vergleich prüfen
