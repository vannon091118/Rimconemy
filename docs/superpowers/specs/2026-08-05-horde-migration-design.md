# Phase F — Wandering-Horde mit World-Map-Migration

> **Phase:** F (v0.0.65 Zielversion)
> **Spec-Datum:** 2026-08-05
> **Owner:** Infected & Automation (Package 05)
> **Status:** 🟡 Brainstorming-Phase, Design approved; Implementation pending

---

## 0. Kontext + Motivation

Phase D (v0.0.63) hat einen **Marker-WorldObject** auf die World-Map gesetzt, dessen Tile deterministisch driftete — sichtbar als roter Wanderer. Der Marker hat aber **keine echten Pawns**: er ist ein UI-Punkt, kein Gameplay-Träger.

Phase F erweitert das zu einem **echten 100-200-Infizierten-Horde**, der auf der World-Map wandert, in den Reveal-Radius um den Player-Home einbricht, und sichtbar mit AI-Behaviour gegen die Siedlung kämpft. Phase D bleibt als Marker-Overlay (für die Visual-Continuity), Phase F liefert die Substrate.

**Hintergrund-Anforderung (User-Anforderung 2026-08-05):** „ab einem gewissen hohen Wert lockt man eine Zombiehorde (100-200 Infizierte) an (Die man als icon auf der minikare wandern sehen soll)".

---

## 1. Decisions (Brainstorming-Ergebnis)

| # | Decision | Wahl | Begründung kurz |
|---|---|---|---|
| D-1 | Scope | **Eine grosse Spec** über 4 Komponenten | Atomic-Delivery + Single-Review-Cycle |
| D-2 | Migration-Mechanik | **Hybrid (Leader + Reveal-Materialization)** | Tile-Drift deterministisch + Pawns nur sichtbar materialisiert → Save-Size bounded |
| D-3 | Trigger | **Threat + Effective + Profile** | Dasselbe Gate wie Phase D (IsActiveNow), nur Schwelle höher (hordeActivationThreshold) |
| D-4 | Reveal-Radius | **Tile-distance vom Home** (≤ 8) | Deterministisch, kein Caravan-Tracking nötig, gut testbar |
| D-5 | Hidden-Save | **Lightweight-Record-Schema** (volle Pawn-States) | Save-Size linear (~50 KB für 200 Pawns), Reconstruction trivial |
| D-6 | Tile-States | **4-State FSM** (Idle → Migrating → Staging → Attacking) | Sauberes State-Tracking, Staging-Timer erlaubt Vorbereitung |

---

## 2. Architektur-Überblick

```
              EXISTING (Phase D)                  │         NEW (Phase F)
                                                 │
                                            ┌────┴────┐
                                            │ Trigger │
                                            │ Gate    │ ← HordeCalculator.IsActiveNow
                                            └────┬────┘ (reuse)
                                                 │
   HordeSelector                                  │     HordeStorySelector
   ┌────────────────────┐                         │     (new StoryEventCatalog
   │ PlayerHomeTile     │                         │      node endend bei
   │ Migration cadence  │                         │      Trigger.MobilizeHorde)
   │ Hidden manifest    │                         │
   └─────────┬──────────┘                         │
             │                                    │
             ▼                                    ▼
   ┌────────────────────────┐         ┌────────────────────────┐
   │ HordeManifest (Scribe) │────────▶│ HordeMigrationDriver   │
   │  - LeaderTile          │         │ (MapComponent × home   │
   │  - Pawns[100-200]      │         │  250-Tick tick-loop   │
   │  - ScribeCollection    │         │  - TravelTile FSM      │
   │    <int, HiddenPawn…>  │         │  - Manifest-update     │
   │  - StaleStampIndex     │         │  - Profile selector    │
   └──────────┬─────────────┘         └────────┬────────────────┘
              │                                 │
              ▼                                 ▼
   ┌──────────────────────┐         ┌────────────────────────┐
   │ HiddenPawnStamp      │         │ HordeMaterialization   │
   │  - ThingID           │         │  (Service-Class, pure) │
   │  - KindDef           │         │  Reveal-enter: spawn N │
   │  - FactionDef        │         │  Reveal-leave: collect │
   │  - HealthPercent     │         │   + destroy            │
   │  - EquipmentSeed     │         │  Stale-discard after 5d│
   │  - SpawnedAtTick     │         │                        │
   │  - SourceCellHint    │         └────────────┬───────────┘
   │  ~250 bytes each     │                      │
   └──────────────────────┘                      ▼
                                          ┌────────────────────┐
   ┌──────────────────────┐                │ ChunkCleanupService│
   │ TravelTileRecord     │                │  Per-Map:          │
   │  - Tile              │                │  - materialize @tile│
   │  - Status enum       │                │  - clear @tile-leave│
   │  - LastTransitionTick│                │  - GC stale stamps │
   │  - ActiveStagingLeft │                └────────────────────┘
   │  - LastSeenAtTick    │
   └──────────────────────┘
```

---

## 3. Komponenten (Detailliste)

### 3.1 `HordeManifest` (Source/Horde/HordeManifest.cs)
**Persistenz-Klasse.** Speichert den aktuellen Stand der Horde-Pawns als Lightweight-Records. Eine `HordeManifest` enthält 100-200 `HiddenPawnStamp`-Records. Single-Instance pro Spieler-Save (Scribe via `Scribe_Deep.Look`).

**Schnittstelle:**
```csharp
public sealed class HordeManifest : IExposable, ISchemaMigratable
{
    public int LeaderTile;                          // current "head" tile
    public int EffectiveSize;                        // 100-200 (per Profile)
    public ProfileId Profile;                        // which Profile drove the spawn
    public long SpawnedAtTick;                       // for stale-evaluation
    public List<HiddenPawnStamp> Stamps;             // list, not dict (stability)
    public List<TravelTileRecord> TileRecords;      // FSM state for ~10 tiles
    public int Capacity;                             // 100/150/200 by Profile
    
    public bool IsTileMaterialized(int tile);
    public void SetTileMaterialized(int tile, bool val);
    
    public void AddPawn(HiddenPawnStamp stamp);     // append-only with cap
    public void RemovePawn(string thingId);         // on death / final-cleanup
    
    public void StaleStampIndex;                    // secondary index by SpawnedAtTick
    
    public void ExposeData();                       // Scribe_Deep + Scribe_Collections
}
```

**Scribe-Strategy:**
- `Scribe_Values.Look` für `LeaderTile`, `EffectiveSize`, `Profile`, `SpawnedAtTick`, `Capacity`
- `Scribe_Collections.Look<List<HiddenPawnStamp>>` mit `LookMode.Deep`
- `Scribe_Collections.Look<List<TravelTileRecord>>` mit `LookMode.Deep`
- ISchemaMigratable Schema-Version: **1** (kein Migration-Fallout erwartet, da neues Feature)

### 3.2 `HiddenPawnStamp` (Source/Horde/HiddenPawnStamp.cs)
**Struct** (Scribe-fähig). Enthält alles für deterministische Rekonstruktion.

```csharp
public struct HiddenPawnStamp : IExposable
{
    public string ThingID;             // stable across reveals
    public string KindDefName;         // "Rimconemy_InfectedRavager" / "_InfectedWildlife"
    public string FactionDefName;      // "Rimconemy_HiddenInfectedFaction"
    public float HealthPercent;       // typically 1.0
    public int EquipmentSeedOffset;   // FNV-1a offset for gear randomness
    public long SpawnedAtTick;        // for stale-evaluation
    public int SourceCellHashHint;    // last known home-map cell hash (not exact, just for AI hint)
    
    public void ExposeData();
}
```

**Save-Grössen-Rechnung:** ~250 bytes per stamp × 200 pawns = ~50 KB. Akzeptabel (eine Hediff ist vergleichbar).

### 3.3 `TravelTileRecord` (Source/Horde/TravelTileRecord.cs)
**Struct für Tile-State-Map.** Jede Position in der Leader-Drift-Pfad-Linie (ca. 5-10 Tiles rund um LeaderTile) hat einen Record.

```csharp
public enum TravelTileStatus { Idle = 0, Migrating = 1, Staging = 2, Attacking = 3 }

public struct TravelTileRecord : IExposable
{
    public int Tile;
    public TravelTileStatus Status;
    public long LastTransitionTick;
    public int ActiveStagingTicksLeft;
    public long LastSeenAtTick;          // for stale-discard
    
    public void ExposeData();
}
```

### 3.4 `HordeMigrationDriver` (Source/Horde/HordeMigrationDriver.cs)
**MapComponent** auf der Player-Home-Map. Single-Source-of-Truth für die Wandering-Horde auf World-Map-Ebene.

**Tick-Loop** (alle 250 Ticks):
```
On MapComponentTick:
  if (Scribe.mode != LoadSaveMode.Inactive) return
  homeMap = MapRegistry.GetPrimaryPlayerHomeMap()
  if (map != homeMap) return

  if (!HordeCalculator.IsActiveNow()) {
    DespawnAllWorldObjects()      // mirror HordeSpawner pattern
    return
  }

  long currentTick = Find.TickManager.TicksGame
  HordeManifest manifest = HordeManifest.Get() ?? CreateFreshManifest()

  // 1. Resolve Leader-Tile per HordeUpdateLogic-pattern
  int leaderTile = HordeUpdateLogic.ComputeHordeTile(homeMap.Tile, currentTick)

  // 2. Update TravelTile-FSM (5-tile vor Leader + Leader-Tile selbst)
  for tile in (leaderTile-5..leaderTile]:
    UpdateTileFSM(manifest, tile, currentTick)
  
  // 3. Reveal-Radius-Sync (Tile-distance <= 8)
  SyncRevealRadius(manifest, homeMap.Tile, currentTick)
}
```

**Public Read-Access für UI/Story:**
```
int GetLeaderTile();
int GetEffectiveSize();
TravelTileStatus GetTileState(int tile);
List<TravelTileRecord> GetActiveTileRecords(int window=5);
```

### 3.5 `HordeMaterializationService` (Source/Horde/HordeMaterializationService.cs)
**Statische Service-Klasse** ohne Game-Component-Existenz. Wird vom `HordeMigrationDriver` aufgerufen.

**API:**
```csharp
public static class HordeMaterializationService
{
    public static void MaterializeTile(HordeManifest manifest, int tile, Map homeMap);
    public static void CleanupTile(HordeManifest manifest, int tile, Map homeMap, long currentTick);
    public static void StaleStampGC(HordeManifest manifest, long currentTick, int staleThresholdDays=5);
}
```

**Materialisierung:** Pro Stamp:
1. `PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(stamp.KindDefName)`
2. Pawn-Spawn via `PawnGenerator.GeneratePawn(kindDef, faction)`
3. Setze `pawn.health.summaryHealth` auf `stamp.HealthPercent`
4. `homeMap.thingList` an einer freien Edge-Cell platzieren (deterministisch via ThingID-Hash)
5. Registriere den Pawn in `WorldPawnGC.SuppressDeregistration(pawn)` so er bei Tile-Wechsel nicht wiped wird

**Cleanup:** Pro Spawn auf dem Tile:
1. `pawn.Destroy()` (RimWorld konvertiert zurück zu GC-Stream)
2. Update `stamp.HealthPercent` aus `pawn.health.summaryHealth`
3. Setze `stamp.SourceCellHashHint = pawn.Position.GetHashCode()`
4. Setze `TravelTileRecord.LastSeenAtTick = currentTick`

**Stale-GC:** Pro Stamp falls `currentTick - stamp.SpawnedAtTick > 5 * TicksPerDay` UND Tile nicht materialized in den letzten 250 Ticks ⇒ `manifest.RemovePawn(stamp.ThingID)`.

### 3.6 `HordeStorySelector` (Source/Story/HordeStorySelector.cs)
**StorySelector-Erweiterung.** Neue Methode `SelectHordeMigrationLetter(StoryState state, SituationSnapshot snapshot, SettingProfile profile) -> StoryEventSpec?`.

**Trigger-Bedingung:**
```
return profile != null
    && snapshot.ThreatPressure >= HordeStorySelector.GetHordeActivationThreshold(profile)
    && HordeCalculator.IsActive(snapshot.EffectiveCount, profile)
    && !HordeManifest.Get()?.IsActive ?? false  // not already migrating
    && DaysSinceLastHordeLetter(state) >= profile.HordeLetterCooldownDays
```

**Erweiterung** `StoryEventCatalog` — neuer Spec:
```
StoryEventSpec HordeMigrationLetter {
  EventId = "rimconemy.raid.infected_horde_migration";
  EventFamily = "Raid";
  EscalationBand = 3;
  Weights[Collapse] = 0.85f;
  Cooldowns = { Collapse: 5 days, Survival: 14 days }
  LetterLabel = "Wandernde Horde!";
  LetterText = "Eine massive Horde Infizierter wandert auf dein Territorium zu. Ankunft in ~{DaysUntilArrival} Tagen.";
  Choices = [
    Accept → Effect: "TriggerHordeMigration:profile-count"
    Refuse → Effect: "ThreatPressure:+0.10"
  ];
}
```

**Wiring:** Der `StoryEventCatalog.All()`-Catalog wird mit diesem Spec erweitert (hardcoded Seed). `StorySelector.SelectEvent` ruft optional `HordeStorySelector.SelectHordeMigrationLetter` als Bonus-Gewichtungs-Pfad auf.

### 3.7 `ChunkCleanupService` (Source/Horde/ChunkCleanupService.cs) — NEU
**Statische Service-Klasse** für die Reaveal-Radius-Migration. Wird vom `HordeMigrationDriver` alle 250 Ticks aufgerufen.

**Wann läuft's:**
- Tile-Set im Reveal-Radius: materialize-on-enter
- Tile-Set ausserhalb Reveal-Radius: cleanup-on-leave (mit Stale-Schwelle)

**Was macht's:**
- Per Tile: Filter `Map.mapPawns.AllPawnsSpawned` nach `kindDef.defName.StartsWith("Rimconemy_Infected")`
- Für jeden Match: `stamp.HealthPercent = pawn.health.summaryHealth.SummaryHealthPercent`
- `manifest.MaterializeTile[tile] = false` setzen
- `pawn.Destroy()`

**Stale-Schwelle:** TravelTileRecord mit `currentTick - LastSeenAtTick > 5 * TicksPerDay` ⇒ endgültig entfernen.

---

## 4. Data-Models (Scribe-Verträge)

### 4.1 HordeManifest initial erstellen
Falls `HordeManifest.Get() == null` UND `HordeCalculator.IsActiveNow()`:
1. `Profile = StripRimconemyPrefix(activeProfile.ProfileId)`
2. `Capacity = PopulationProfileMultipliers.GetHordeCapacity(profile)` — **neu**, profiled-data:
   - Refuge: 50 (small, defensive)
   - Survival: 100 (mid-game threat)
   - Collapse: 200 (boss-tier threat)
3. `EffectiveSize = Capacity` (deterministic, always full at startup)
4. `Stamps` mit `Capacity` × fresh `HiddenPawnStamp`-Records (FNV-1a-seeded ThingIDs)
5. `SpawnedAtTick = currentTick`

### 4.2 HiddenPawnStamp Initialisierung
Für jeden i in [0..Capacity):
```
HiddenPawnStamp {
    ThingID = $"Rimconemy_HiddenPawn_{hashRimconemy(currentTick + i):X8}",
    KindDefName = "Rimconemy_InfectedRavager" (uniform),
    FactionDefName = "Rimconemy_HiddenInfectedFaction",
    HealthPercent = 1.0f,
    EquipmentSeedOffset = i * 7 + profile.GetHashCode(),
    SpawnedAtTick = currentTick,
    SourceCellHashHint = 0
}
```

### 4.3 TravelTileRecord Initial-Status
Für initial 10 Tiles rund um LeaderTile: alle `Idle`. Beim ersten Tick beginnt Leader-Tile = `Migrating`.

### 4.4 Profile-Multiplier (Neue Defs)
Erweiterung `PopulationProfileMultipliers`:
```csharp
public static readonly IReadOnlyDictionary<string, int> HordeCapacity = new Dictionary<string,int> {
    { ProfileRefuge, 50 }, { ProfileSurvival, 100 }, { ProfileCollapse, 200 }
};
public static readonly IReadOnlyDictionary<string, float> HordeActivationThreshold = new Dictionary<string,float> {
    { ProfileRefuge, 0.85f }, { ProfileSurvival, 0.70f }, { ProfileCollapse, 0.50f }
};
public static readonly IReadOnlyDictionary<string, float> HordeLetterCooldownDays = new Dictionary<string,float> {
    { ProfileRefuge, 30f }, { ProfileSurvival, 14f }, { ProfileCollapse, 5f }
};
public static readonly IReadOnlyDictionary<string, int> HordeStagingDurationTicks = new Dictionary<string,int> {
    { ProfileRefuge, 250 * 5 }, { ProfileSurvival, 250 * 3 }, { ProfileCollapse, 250 * 2 }
};
```

### 4.5 Reveal-Radius-Konstante
```csharp
public const int HordeRevealRadiusTiles = 8;
```

---

## 5. Tick-Loop (Pseudocode, Determinismus-Garantie)

```
Phase F MigrationDriver State Machine (MapComponent, home map, 250-tick cadence)
═════════════════════════════════════════════════════════════════════════════

On every MapComponentTick (60Hz):
  if Scribe.mode != Inactive: return        // never write during save/load
  if map != primaryPlayerHomeMap: return
  
On every 250-Tick boundary (cadence):
  currentTick = Find.TickManager.TicksGame
  if !HordeCalculator.IsActiveNow():
    DespawnManifestAndWorldObjects()
    return
  
  manifest = HordeManifest.Get() ?? CreateFreshManifest(currentTick)
  leaderTile = HordeUpdateLogic.ComputeHordeTile(homeMap.Tile, currentTick)
  
  // (1) Update TravelTile-FSM for the rolling 5-tile window behind leader
  for tile in (leaderTile - 5 .. leaderTile]:
    record = manifest.GetOrCreateTileRecord(tile)
    match record.Status:
      case Idle:       record.Status = Migrating; record.LastTransitionTick = currentTick
      case Migrating:  record.Status = Staging;   record.LastTransitionTick = currentTick
                       record.ActiveStagingTicksLeft = profile.HordeStagingDurationTicks[profile]
      case Staging:    if record.ActiveStagingTicksLeft <= 0:
                         record.Status = Attacking; record.LastTransitionTick = currentTick
                         QueueAttackIncident(tile, profile)
                       else:
                         record.ActiveStagingTicksLeft -= 250
      case Attacking:  record.Status = Idle;       record.LastTransitionTick = currentTick
  
  // (2) Reveal-Radius-Sync (8-tile distance)
  for tile of all revealedTiles(manifest):
    int distToHome = TileDistance(tile, homeMap.Tile)
    if distToHome <= 8:
      if !manifest.IsTileMaterialized(tile):
        HordeMaterializationService.MaterializeTile(manifest, tile, homeMap)
    else:
      if manifest.IsTileMaterialized(tile):
        HordeMaterializationService.CleanupTile(manifest, tile, homeMap, currentTick)
  
  // (3) Stale-GC (5 Tage Schwelle)
  HordeMaterializationService.StaleStampGC(manifest, currentTick, staleThresholdDays=5)
```

---

## 6. StorySelector Integration

### 6.1 Extend `StorySelector.SelectEvent`
In `StorySelector.SelectEvent`, nach dem bisherigen `Selection`:
```
// Phase F Extension: optional HordeMigration-Outlook
if (result.HasEvent == false || result.SelectedEvent?.EventFamily != "Raid")
    return result;

var hordeLetter = HordeStorySelector.SelectHordeMigrationLetter(state, snapshot, profile);
if (hordeLetter != null && hordeLetter != result.SelectedEvent)
    result.SelectedEvent = hordeLetter;
```

### 6.2 HordeStorySelector-Logik

```
SelectHordeMigrationLetter:
  manifest = HordeManifest.Get()
  if (manifest == null || manifest.EffectiveSize == 0) return null  // not yet active
  
  // Refresh Profile-driven cooldown
  state.PruneOldKeys(currentTick)  // reuse existing pattern
  
  if (DaysSinceLastHordeLetter >= cooldownDays[profile]) -> Letter fires
  
  // ThreatGate
  if (snapshot.ThreatPressure < HordeActivationThreshold[profile]) return null
  
  // EffectiveGate
  if (!HordeCalculator.IsActive(snapshot.EffectiveCount, profile)) return null
  
  return HordeMigrationLetter (from StoryEventCatalog)
```

### 6.3 Choice-Outcome in `InfectedRaidSpawnService`
Neue Effect-Hook: `Effects["TriggerHordeMigration:profile-count"]` wird aufgelöst.

```
ProcessEffect("TriggerHordeMigration", profile):
  profile = ParseProfile(arg)
  cap = PopulationProfileMultipliers.GetHordeCapacity(profile)
  HordeManifest.CreateOrExpand(profile, currentTick, cap)  // deterministic
  return Success
```

---

## 7. Tests (TDD-Pattern, ~30 Tests)

### 7.1 `HordeManifestTests.cs` (8 Tests)
- T1: Manifest initialised with Profile-count capacity
- T2: Stamp append-and-cap (no overflow)
- T3: RemovePawn removes from list
- T4: Scribe roundtrip preserves Counts and Stamps
- T5: Empty manifest Scribe roundtrip
- T6: StaleStampIndex updates on remove
- T7: Capacity per Profile (50/100/200) deterministic
- T8: TravelTileRecord sérialisiert mit allen Feldern

### 7.2 `HordeMigrationDriverTests.cs` (10 Tests)
- T9: Tick-Loop FSM transitions (Idle → Migrating → Staging → Attacking → Idle)
- T10: Staging-Timer countdown
- T11: Leader-Tile matches ComputeHordeTile
- T12: Manifest not active → driver returns early
- T13: Multiple tiles in window get FSM transitions
- T14: Tile distance independent of leader direction
- T15: Profile-staging-duration differs Survival vs Collapse
- T16: Idempotent firing (same tick, same state)
- T17: Save/Load re-resumes tile-state
- T18: Despawn-on-inactive

### 7.3 `TravelTileStateTests.cs` (4 Tests)
- T19: Status enum ordering
- T20: Stale-Discard boundary
- T21: LastSeenAtTick update
- T22: Scribe roundtrip enum + count

### 7.4 `HordeMaterializationTests.cs` (6 Tests)
- T23: Reveal-enter materializes stamp → Pawn
- T24: Determinism-Seed rebuilds same gear
- T25: Reveal-leave collects + destroys
- T26: Health-percent preserved across reveal cycle
- T27: Stale-discard after 5-day threshold
- T28: Pawn rebuild preserves kindDef/faction

### 7.5 `HordeStorySelectorTests.cs` (4 Tests)
- T29: ThreatGate fires only above threshold
- T30: CooldownDays respected
- T31: Profile-weighted selection
- T32: Effect-Hook TriggerHordeMigration expand

---

## 8. Falsification §H — Live-Beleg

Sektion in `docs/falsification/infected__HordeMigration.md` (zu erstellen).

### Schritte für User-Pflicht-Lauf
1. Setup: Survival-Kolonie gründen, Dev-Mode an
2. Population-Druck aufbauen: 150+ Human-Pawns erzeugen via spawn-script oder Kills
3. Warten bis ThreatPressure ≥ 0.70 (Collapse-Threshold)
4. Erwartung im Player.log: `[Rimconemy.InfectedAutomation] HordeLetter-fired: activation-threshold=0.70 threat=0.92`
5. Player akzeptiert Letter-Choice "Mobilize"
6. Erwartung: Manifest spawned mit 100 Stamps (Survival-Capacity)
7. Warten 5+ In-Game-Tage
8. World-Map zeigt jetzt nicht nur Marker-Dot, sondern **echte Pawns** die mitwandern (HUD-Click auf WorldObject → pawn-list)
9. Caravan-Reveal in Tile-Distance ≤ 8 ⇒ Pawns materialisieren, sind sichtbar/spielbar
10. Save → Quit → Reload ⇒ Manifest round-trippt, Pawns erscheinen am letzten tile-anchor
11. Reveal-Radius verlassen ⇒ Pawns dematerialisieren, HealthPercent wird zurück in Stamp geschrieben

### Akzeptanz-Gates §H
- [ ] H-1: runtime_test PASS / 5 packages / Build clean
- [ ] H-2: 30/30 Tests (HordeManifest+Driver+TileState+Materialization+StorySelector) grün
- [ ] H-3: Phase-D-Regression D1-D12 weiterhin grün (backward-compat)
- [ ] H-4: Live-Beleg im Player.log dokumentiert
- [ ] H-5: Save/Load Manifest-Roundtrip dokumentiert
- [ ] H-6: Reveal-Cycle deterministisch (gleicher Seed → gleicher Gear)

---

## 9. Versionierung + Capability-Contract

| Package | Version | Neue Capability |
|---|---|---|
| 05 (InfectedAutomation) | 0.0.64 → **0.0.68** (current; Phase F Substrate V1 in 0.0.68, weitere Substrate-Folgeschritte je nach Plan) | `rimconemy.infectedautomation.horde_migration` v1 |

INTERFACE_CONTRACT.md §9.1 Eintrag:
> `rimconemy.infectedautomation.horde_migration` v1 — Owner: Infected. Sole-Owner `HordeManifest`/`HordeMigrationDriver`. Lese-Surface für Mod 04 (Economy/Territory) via Bridge nur falls Outpost-Threat-Coupling gewünscht (§33 Outpost-Ökonomie).

---

## 10. Akzeptanz-Gates (Master)

| # | Gate | Pass-Kriterium | Status |
|---|---|---|---|
| F-1 | Phase-D-Regression | D1-D12 weiterhin grün | 🟡 pending impl |
| F-2 | HordeManifest Tests | 8/8 grün | 🟡 pending impl |
| F-3 | MigrationDriver Tests | 10/10 grün | 🟡 pending impl |
| F-4 | TravelTile Tests | 4/4 grün | 🟡 pending impl |
| F-5 | Materialization Tests | 6/6 grün | 🟡 pending impl |
| F-6 | StorySelector Tests | 4/4 grün | 🟡 pending impl |
| F-7 | Build clean | 0 warnings, 0 errors | 🟡 pending impl |
| F-8 | runtime_test PASS | warnings=0, all 5 packages registered | 🟡 pending impl |
| F-9 | Save/Load Roundtrip | Manifest-Scribe-belegt | 🟡 pending impl |
| F-10 | Reveal-Radius-Determinism | gleicher Seed → gleicher Gear | 🟡 pending impl |
| F-11 | Falsification §H | Live-Beleg in 11 Schritten dokumentiert | 🟡 User-Pflicht nach impl |
| F-12 | Performance | <500ms pro 250-tick-cadence-tick | 🟡 pending impl |

---

## 11. Cross-Package READ (laut INTERFACE_CONTRACT §9.3)

Phase F konsumiert:
- `PopulationLedger.Get()` (intra-package, v0.0.64 bereits)
- `HordeCalculator.IsActiveNow()` (intra-package, v0.0.63)
- `HordeUpdateLogic.ComputeHordeTile()` (intra-package, v0.0.63)
- `StorySelector.SelectEvent()` (intra-package, v0.0.63)
- `MapRegistry.GetPrimaryPlayerHomeMap()` (Foundation, v0.0.x)
- `CapabilityAudit.HasCapabilityOrWarn(...)` (Foundation, v0.0.x)

Phase F veröffentlicht: `HordeManifest`, `HordeMigrationDriver`, `TravelTileRecord` (alle intra-package). Late-Bound-Bridge `Foundation.CrossPackage.TryReadHordeMigrationState` falls 04 (Economy) Outpost-Threat-Coupling braucht — derzeit nicht aktiviert, wird in Phase G+ nachgerüstet falls nötig.

**Kein neuer Cross-Package-Cycle entsteht** (DECISION-D-001 vom 2026-08-05 sauber dokumentiert).

---

## 12. Verweise

- **Spec-Grundlage:** D-1 bis D-6 (Brainstorming-Decisions oben)
- **Phase D Referenz:** `docs/falsification/infected__ThreatPressure.md` — HordeCalculator, HordeSpawner, etc.
- **Phase E Referenz:** `docs/falsification/infected__AnimalInfection.md` — PopulationLedger, Profile-Multipliers, AnimalInfectionChance Pattern
- **INTERFACE_CONTRACT:** §9.3 Cycle-Free-Topologie beibehalten
- **DECISIONS:** siehe DECISION-D-001 (cycle-break 2026-08-05)

---

*Spec genehmigt durch User 2026-08-05. Implementation beginnt nach writing-plans-Skill.*
