# STORYTELLER_ANALYSIS — RimWorld Vanilla Storyteller & Rimconemy Injection

> **Date:** 2026-08-07  
> **Status:** Architecture analysis — KORRIGIERT 2026-08-07: User-Entscheidung = eigener StorytellerDef, Vanilla wird ERSETZT  
> **Related:** `docs/DECISIONS.md §34` (korrigiert), `docs/H2-story-contract.md`, `docs/REFACTORING_PLAN.md`  
> **Decision:** Rimconemy registriert EINEN StorytellerDef. Cassandra/Phoebe/Randy werden via XML-Patch versteckt.

---

## 1. RimWorld Storyteller Architecture (RimWorld 1.6)

### 1.1 Core Classes

```
Storyteller                    ← Singleton, owned by RimWorld
  ├── storytellerDef           ← StorytellerDef (Cassandra, Phoebe, Randy, etc.)
  ├── difficultyDef            ← DifficultyDef (Peaceful → Extreme)
  ├── storytellerComps         ← List<StorytellerComp> (incident selection logic)
  ├── incidentQueue            ← IncidentQueue (pending incidents)
  │     └── List<IncidentQueueEntry>
  │           ├── FiringIncident   ← IncidentDef + StorytellerComp + IncidentParms
  │           └── int fireTick     ← when it should execute
  └── TryFire(FiringIncident, bool queued) → bool
        └── if queued=true: add to incidentQueue
            if queued=false: check baseChance, comps, fire immediately if allowed
```

### 1.2 How Vanilla Storytellers Fire Incidents

The vanilla cycle works as follows:

```
Storyteller.StorytellerTick()
  → For each StorytellerComp in storytellerComps:
      → comp.IncidentCycleTick()
        → StorytellerComp calculates if it's time to fire
        → Selects an IncidentDef based on:
            - incident.category (ThreatBig, ThreatSmall, etc.)
            - baseChance (from IncidentDef XML)
            - difficulty.incidentChanceFinal
            - colony wealth via StorytellerUtility.DefaultThreatPointsNow
            - population intent (Randy=random, Cassandra=pressure-based, Phoebe=rest-based)
        → Calls Storyteller.TryFire(incident, queued=false) to attempt immediate fire
        → RimWorld manages cooldowns internally per IncidentDef.category
```

### 1.3 Key API Surface We Use (Already Implemented)

| API | How Rimconemy Uses It | File |
|-----|----------------------|------|
| `Find.Storyteller.incidentQueue` | We queue our custom `Rimconemy_InfectedRaidIncident` via `TryFire(queued:true)` | `StoryDirector.QueueSelectedIncident()` |
| `Find.Storyteller.TryFire(fi, queued:true)` | Force-queue an incident, bypassing `baseChance` and storytellerComps | `StoryDirector.cs:390` |
| `new FiringIncident(def, comp, parms)` | Wrap our IncidentDef + source StorytellerComp + map parameters | `StoryDirector.cs:359` |
| `IncidentWorker (base class)` | Our `InfectedRaidWorker` extends this; `CanFireNowSub` + `TryExecuteWorker` | `InfectedRaidWorker.cs` |
| `DefDatabase<IncidentDef>.GetNamedSilentFail()` | Resolve our custom IncidentDef at runtime | `StoryDirector.cs:310` |

### 1.4 What We CAN Do — Pivot to Full Replacement

| Goal | How | Status |
|------|-----|--------|
| **Replace vanilla storytellers entirely** | ✅ Custom `StorytellerDef` + XML-Patch auf `<hidden>true</hidden>` für Cassandra/Phoebe/Randy | Design entschieden (DECISIONS §34 korrigiert) |
| **Full control over incident pacing** | Eigener `RimconemyStorytellerComp` feuert ALLE Incidents | Geplant |
| **DLC-Incidents durchreichen** | Optional: Vanilla-StorytellerComp als Sub-Comp instanziieren | Design-Entscheidung offen |
| **Inject custom incidents** | `TryFire(queued:false)` direkt aus dem Comp | ✅ API bekannt |
| **Read game state** | Colony wealth, pawns, mood, power, factions, storage — durch Foundation-Contracts | ✅ `BuildLiveSnapshot()` |

### 1.5 StorytellerComp API (Decompile 2026-08-07 via Mono.Cecil)

Der `StorytellerComp` (abstract base, `RimWorld.StorytellerComp`) hat **KEINEN Per-Tick-Hook**.

**Virtual methods (überschreibbar):**

| Methode | Signatur | Zweck |
|---------|----------|-------|
| `Initialize()` | `public virtual void` | Einmalig beim Start |
| `MakeIntervalIncidents()` | `public virtual IEnumerable<FiringIncident> (IIncidentTarget target)` | **Haupt-Einstiegspunkt** — wird von `Storyteller.StorytellerTick()` periodisch aufgerufen |
| `GenerateParms()` | `public virtual IncidentParms (IncidentCategoryDef, IIncidentTarget)` | Parameter für Incidents generieren |
| `Notify_PawnEvent()` | `public virtual void (Pawn, AdaptationEvent, bool?)` | Benachrichtigung bei Pawn-Events |
| `Notify_DissolutionEvent()` | `public virtual void (Thing)` | Benachrichtigung bei Auflösungs-Events |
| `DebugTablesIncidentChances()` | `public virtual void` | Debug-UI |

**Der Tick-Zyklus:**
```
Storyteller.StorytellerTick()          ← non-virtual, gehört RimWorld
  └── Storyteller.MakeIncidentsForInterval()
        └── comp.MakeIntervalIncidents(target)   ← DAS ist der Hook
              └── gibt IEnumerable<FiringIncident> zurück
```

**Konsequenz für RimconemyStorytellerComp:**
- Wir brauchen **keinen** Override. Unser `StoryDirector.GameComponentTick` (60k-Intervall) ist der richtige Ansatz.
- Die `StorytellerComp` existiert primär zur **Def-Registrierung** im `StorytellerDef`.
- Falls wir später am Vanilla-Storyteller-Zyklus teilnehmen wollen: `MakeIntervalIncidents()` overriden.
- Alle konkreten Vanilla-Comps (`StorytellerComp_RandomMain`, `StorytellerComp_ClassicIntro`, etc.) overriden ausschließlich `MakeIntervalIncidents()`.

### 1.6 What We LOSE by Replacing Vanilla

| Loss | Mitigation |
|------|-----------|
| **Vanilla wealth-based raid scaling** | Eigene ThreatPressure-basierte Skalierung (bereits in `BuildLiveSnapshot`) |
| **Cassandra's adaptive difficulty** | SettingProfile-basierte Eskalation (H2: Profile mit MinThreatLevel, ThreatRiseRate) |
| **Phoebe's rest windows** | RestWindowMin/Max in SettingProfile (bereits definiert) |
| **Randy's randomness** | DeterministicRng + StorySelector (bereits implementiert) |
| **DLC quest incidents (Royalty, Ideology)** | Entweder via Sub-Comp durchreichen oder durch Rimconemy-Events ersetzen |
| **Anomaly entity incidents** | Shambler-Basis wird via §19 geerbt; Entity-Spawns ggf. via Sub-Comp |

---

## 2. Current Rimconemy Injection Architecture

### 2.1 The Pipeline

```
┌─────────────────────────────────────────────────────────┐
│                 GameComponentTick (60k ticks = 1 day)    │
├─────────────────────────────────────────────────────────┤
│  1. WipeCheck: MaybeSignalGameOverForWipe()              │
│  2. BuildLiveSnapshot() ─── reads game state             │
│     ├── ColonialReader.GetActiveColonists()              │
│     ├── MapRegistry.GetPlayerHomeMaps() → wealth         │
│     ├── StorageQuery.ReadStorage() → resource hash       │
│     ├── Find.FactionManager → hostile count              │
│     └── Pawn health, mood, power grid, research          │
│  3. EvaluateWithSnapshot()                               │
│     ├── ThreatHistory trend calculation                  │
│     ├── StorySelector.SelectEvent(profile, snapshot)     │
│     │   └── returns: event + reason + idempotencyKey     │
│     └── QueueSelectedIncident(snapshot)                  │
│         ├── new FiringIncident(def, comp, parms)         │
│         └── Find.Storyteller.TryFire(fi, queued:true)    │
│  4. Day-Growth + Reset-Daily-Counters (PopulationLedger) │
│  5. RecomputeRevenge (kills → revenge quota)             │
│  6. Inoculation (animal infection)                       │
└─────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────┐
│          Vanilla Storyteller picks up queue entry        │
│          → calls InfectedRaidWorker.CanFireNowSub()      │
│          → calls InfectedRaidWorker.TryExecuteWorker()   │
│              ├── ConsumePendingEvent() → letter text     │
│              └── InfectedRaidSpawnService.BuildPlan()    │
└─────────────────────────────────────────────────────────┘
```

### 2.2 What We Track Externally (All Mods)

| Data Source | How We Access | Refresh Rate | Purpose |
|-------------|---------------|-------------|---------|
| **Colony Wealth** | `MapRegistry.GetPlayerHomeMaps()` → `wealthWatcher.WealthTotal` | Daily | ThreatPressure calculation |
| **Colonist Count + Health** | `ColonialReader.GetActiveColonists()` | Daily | Event prerequisites, difficulty scaling |
| **Storage Contents** | `StorageQuery.ReadStorage()` (Pkg 03 capability) | Daily | Supply crisis detection, resource hash |
| **Power Grid** | `Map.listerBuildings` → `CompPowerTrader` | Daily | Event conditions |
| **Factions** | `Find.FactionManager.AllFactionsListForReading` | Daily | Hostile faction count |
| **Ideology Tension** | `TransparencyTracker` (Pkg 05) | Per-decision | Setting rule violations |
| **Population** | `PopulationLedger.Get()` (Pkg 05) | Daily | Growth, revenge, horde cap |
| **Difficulty** | `Find.Storyteller.difficultyDef.defName` | Once (FinalizeInit) | Profile selection |
| **Tick** | `Find.TickManager.TicksGame` | Per-eval | Event timing, cooldowns, determinism |

### 2.3 External Mod Data We Could Track (But Don't Yet)

| Data | How | Benefit |
|------|-----|---------|
| **Active research** | `Find.ResearchManager.currentProj` | Tech-progression-aware event selection |
| **Quest state** | `Find.QuestManager.QuestsListForReading` | Avoid conflicting with active quests |
| **Weather/Season** | `map.weatherManager`, `GenLocalDate` | Season-aware event selection (winter=harder) |
| **Caravan state** | `Find.WorldObjects.Caravans` | Avoid firing raids when pawns are away |
| **Other mod incidents** | `DefDatabase<IncidentDef>.AllDefs` (already in IncidentClassifier) | Know what other mods have registered |
| **Map threat points** | `StorytellerUtility.DefaultThreatPointsNow(map)` | Vanilla-calibrated raid strength for comparison |
| **Raid history** | Custom ledger (already partial via PopulationLedger) | Raid frequency patterns |

---

## 3. Implementation: Custom StorytellerDef (Full Replacement)

### 3.1 StorytellerDef XML

```xml
<!-- Defs/Storyteller/Rimconemy_Storyteller.xml -->
<Defs>
  <StorytellerDef>
    <defName>Rimconemy_Storyteller</defName>
    <label>Rimconemy</label>
    <description>Der Rimconemy-Storyteller. Survival-Härte, dynamische Events, Infizierten-Druck.</description>
    <portraitLarge>UI/HeroArt/Storytellers/RimconemyLarge</portraitLarge>
    <portraitSmall>UI/HeroArt/Storytellers/RimconemySmall</portraitSmall>
    <listOrder>-100</listOrder>
    <comps>
      <li Class="Rimconemy.InfectedAutomation.Story.RimconemyStorytellerComp" />
    </comps>
  </StorytellerDef>
</Defs>
```

### 3.2 RimconemyStorytellerComp

```csharp
public class RimconemyStorytellerComp : StorytellerComp
{
    private StoryState State;
    private SettingProfile ActiveProfile;
    private StoryEventCatalog Catalog;
    private long LastEvaluationTick;

    public RimconemyStorytellerComp()
    {
        State = new StoryState();
        Catalog = new StoryEventCatalog();
    }

    // Called every tick by the vanilla storyteller loop
    protected override void IncidentCycleTick()
    {
        long currentTick = Find.TickManager?.TicksGame ?? 0L;

        // Daily evaluation (60,000 ticks)
        if (currentTick < LastEvaluationTick + 60000)
            return;
        LastEvaluationTick = currentTick;

        var snapshot = BuildLiveSnapshot(currentTick, State, ActiveProfile);
        EvaluateAndFire(snapshot, currentTick);
    }

    // ... (rest of logic ported from StoryDirector)
}
```

### 3.3 Hide Vanilla Storytellers (XML Patch)

```xml
<!-- Patches/HideVanillaStorytellers.xml -->
<Patch>
  <Operation Class="PatchOperationFindMod">
    <mods><li>Rimconemy.Foundation</li></mods>
    <match Class="PatchOperationSequence">
      <operations>
        <li Class="PatchOperationAdd">
          <xpath>/Defs/StorytellerDef[defName="Cassandra"]</xpath>
          <value><hidden>true</hidden></value>
        </li>
        <li Class="PatchOperationAdd">
          <xpath>/Defs/StorytellerDef[defName="Phoebe"]</xpath>
          <value><hidden>true</hidden></value>
        </li>
        <li Class="PatchOperationAdd">
          <xpath>/Defs/StorytellerDef[defName="Randy"]</xpath>
          <value><hidden>true</hidden></value>
        </li>
      </operations>
    </match>
  </Operation>
</Patch>
```

### 3.4 Pro/Contra: Full Replacement

| Pro | Contra |
|-----|--------|
| Volle Kontrolle über Pacing | DLC-Incidents müssen manuell behandelt werden |
| Kein Wettlauf mit Vanilla | Vanilla-Quests gehen verloren (oder müssen re-implementiert werden) |
| Klare UX (ein Storyteller) | Höherer Implementierungsaufwand |
| Per-Tick-Evaluation (keine 60k-Latenz) | Save-Migration für bestehende Spiele nötig |
| SettingProfile direkt im Storyteller | Mod-Konflikte mit anderen Storyteller-Mods möglich |

---

## 4. StoryDirector → RimconemyStorytellerComp: Migration Plan

### 4.1 What Changes

```
CURRENT:
  StoryDirector : GameComponent
    ├── GameComponentTick → evaluates every 60k ticks
    ├── QueueSelectedIncident → FiringIncident + TryFire(queued=true)
    └── Concern overload: wipe-check, eval, growth, revenge, inoculation

TARGET:
  RimconemyStorytellerComp : StorytellerComp
    ├── IncidentCycleTick → evaluates every tick (with internal 60k gate)
    ├── FireSelectedIncident → TryFire(queued=false)
    ├── Inherits from StorytellerComp (vanilla loop integration)
    └── Split concerns: StoryScheduler + IncidentDispatcher + GrowthManager
```

### 4.2 Migration Steps

1. **Create** `Rimconemy_Storyteller.xml` (StorytellerDef in Mod 05/Defs/)
2. **Create** `RimconemyStorytellerComp.cs` extending `StorytellerComp`
3. **Port** `StoryDirector` logic into the Comp:
   - `GameComponentTick` → `IncidentCycleTick`
   - `BuildLiveSnapshot` → static utility (no change)
   - `QueueSelectedIncident` → `TryFire(queued=false)` (direct, not queued)
4. **Create** `HideVanillaStorytellers.xml` Patch
5. **Remove** `StoryDirector : GameComponent` registration
6. **Verify** only Rimconemy storyteller appears in selection screen
7. **Test** full incident cycle: Snapshot → Select → Fire → Letter

### 4.3 What We Gain from Custom StorytellerDef

| Feature | Side-by-Side (alt) | Full Replacement (neu) |
|---------|-------------------|----------------------|
| Incident-Kontrolle | Nur unsere Events; Vanilla feuert parallel | ALLE Incidents gehen durch UNS |
| Pacing | 60k-Tick-Polling | Per-Tick-Steuerung |
| Cooldowns | Eigenes System neben Vanilla | Ein einziges, konsistentes System |
| UX | Spieler sieht 4 Storyteller (3 Vanilla + unsichtbarer Director) | Spieler sieht NUR Rimconemy |
| DLC-Incidents | Vanilla feuert sie | Wir entscheiden: durchreichen oder ersetzen |
| Mod-Kompatibilität | Vanilla-Storyteller-Mods funktionieren | Mods die Vanilla-Storyteller patchen greifen ins Leere |

### 4.3 Rewiring Steps

1. **Analyze** `StorytellerComp` abstract class API (methods, lifecycle)
2. **Create** `RimconemyStorytellerComp : StorytellerComp` with `IncidentCycleTick()`
3. **Register** via XML patch on base `StorytellerDef` (patch ALL vanilla storytellers)
4. **Move** real-time concerns from `StoryDirector.GameComponentTick` → `IncidentCycleTick`
5. **Keep** daily evaluation in `StoryScheduler.GameComponentTick`
6. **Split** `QueueSelectedIncident` into `IncidentDispatcher`
7. **Verify** gate: both vanilla + Rimconemy incidents fire correctly
8. **Test** with all 3 vanilla storytellers (Cassandra, Phoebe, Randy)

### 4.4 Risks

| Risk | Mitigation |
|------|-----------|
| StorytellerComp breaks on RimWorld update | API is stable since 1.0; comps are the standard extension point |
| Conflicts with other storyteller mods | Our comp is additive (only adds, never removes) |
| Double-firing (our comp + vanilla comp) | Use `idempotencyKey` in StoryState to dedup |
| Performance: per-tick eval | Only read-write cheap state; heavy work stays on daily cycle |

---

## 5. Dynamic Events Based on Other Mods

### 5.1 Data Sources We Can Read from Other Mods

| Source | API | Mod Detection |
|--------|-----|---------------|
| **Any mod's IncidentDef** | `DefDatabase<IncidentDef>.AllDefs` | Already in `IncidentClassifier` |
| **Any mod's FactionDef** | `DefDatabase<FactionDef>.AllDefs` | Detect faction mods |
| **Any mod's PawnKindDef** | `DefDatabase<PawnKindDef>.AllDefs` | Detect creature/race mods |
| **Any mod's ThingDef** | `DefDatabase<ThingDef>.AllDefs` | Detect item/resource mods |
| **Any mod's ResearchDef** | `DefDatabase<ResearchProjectDef>.AllDefs` | Detect tech mods |
| **Any mod's HediffDef** | `DefDatabase<HediffDef>.AllDefs` | Detect health/ailment mods |
| **Active mods list** | `ModsConfig.ActiveModsInLoadOrder` | Know what's loaded |
| **Mod-specific Harmony patches** | Reflection on `Harmony` instance | Detect if a specific mod patched something |
| **Map components from other mods** | `map.components` | Access other mods' MapComponent state |

### 5.2 Event Generation Strategy

**Principle:** We read DEF databases (which are immutable after load) and LIVE state (which changes during gameplay) to generate dynamic events.

```csharp
// Example: Generate a "faction war" event if two hostile factions from mods exist
public StoryEventSpec? GenerateFactionWarEvent(SituationSnapshot snap) {
    var hostileFactions = DefDatabase<FactionDef>.AllDefs
        .Where(f => !f.isPlayer && !f.hidden && f != Faction.OfPlayer.def)
        .ToList();
    
    if (hostileFactions.Count >= 2 && snap.HostileFactionCount >= 2) {
        return new StoryEventSpec {
            EventId = "dynamic_faction_war",
            EventFamily = EventFamily.ExternalThreat,
            Weight = 1.0f + (hostileFactions.Count * 0.2f),
            LetterText = $"{hostileFactions[0].label} und {hostileFactions[1].label} kämpfen um dein Territorium."
        };
    }
    return null;
}
```

### 5.3 What Other Mods Could WE React To (Examples)

| Mod Type | Detection | Event Idea |
|----------|-----------|------------|
| **Faction mods** (e.g., Rimsenal, VFE) | Count `FactionDef`s with `!isPlayer && !hidden` | "Faction war" — two hostile factions clash near your colony |
| **Race mods** (e.g., Android Tiers, Alien) | Count `PawnKindDef`s with non-human `race` | "New species spotted" — refugee from modded race seeks shelter |
| **Tech mods** (e.g., Rimatomics, Save Our Ship) | Check `ResearchProjectDef` prefixes | "Rival tech breakthrough" — enemy faction completes research |
| **Creature mods** (e.g., Alpha Animals) | Count `PawnKindDef`s with `AnimalBase` designation | "Exotic migration" — modded animal herd passes through, can be hunted |
| **Weather mods** (e.g., Climate Cycle) | Check `GameConditionDef` | Adapt event flavor to extreme weather |
| **Weapon mods** (e.g., CE) | Check `ThingDef` with `IsWeapon` | "Arms dealer" event — offers modded weapons |
| **Biome mods** | Check `map.Biome.defName` | Biome-specific events |

### 5.4 Implementation Approach: EventTemplate + Dynamic Resolution

```
┌────────────────────────────────────────────────┐
│  EventTemplate (XML Def or code)               │
│  ┌──────────────────────────────────────────┐  │
│  │ TemplateId: "dynamic_faction_clash"       │  │
│  │ Family: ExternalThreat                    │  │
│  │ MinFactionsRequired: 2                    │  │
│  │ WeightBase: 1.0                           │  │
│  │ WeightPerFaction: 0.2                     │  │
│  │ LetterTemplate: "{faction1} vs {faction2}"│  │
│  └──────────────────────────────────────────┘  │
│                    ↓                            │
│  DynamicResolver (at selection time)            │
│  ┌──────────────────────────────────────────┐  │
│  │ 1. Reads DefDatabase<FactionDef>         │  │
│  │ 2. Picks 2 hostile non-player factions   │  │
│  │ 3. Renders LetterTemplate with real names│  │
│  │ 4. Sets up faction relation change       │  │
│  └──────────────────────────────────────────┘  │
└────────────────────────────────────────────────┘
```

---

## 6. Summary: Pivot to Full Replacement

| Component | Current | Target | Reason |
|-----------|---------|--------|--------|
| Storyteller-Modell | GameComponent läuft neben Vanilla | Eigener StorytellerDef ERSETZT Vanilla | User-Entscheidung: nur EIN Storyteller |
| StoryDirector | 1,086 LOC GameComponent | RimconemyStorytellerComp : StorytellerComp | Native RimWorld-Integration |
| Incident-Feuer | `TryFire(queued=true)` in Vanilla-Queue | `TryFire(queued=false)` direkt aus Comp | Kein Umweg über Queue |
| Vanilla-Storyteller | Sichtbar + aktiv | `<hidden>true</hidden>` via XML-Patch | Unsichtbar, inaktiv |
| DLC-Incidents | Vanilla feuert sie automatisch | Entscheidung offen: Sub-Comp oder manuell | Design-Frage |
| Schwierigkeit | Map von `difficultyDef` | Eigene Difficulty im StorytellerDef | Unabhängig von Vanilla |

### Offene Design-Fragen (vor Implementierung zu klären)

1. **DLC-Incidents:** Sollen Royalty-Quests, Ideology-Rituale und Anomaly-Entities weiterhin feuern? Wenn ja: als Sub-Comp durchreichen oder manuell via `TryFire` auslösen?
2. **Difficulty-Auswahl:** Behält der Spieler die Vanilla-Difficulty-Auswahl (Peaceful→Extreme)? Oder wird die Difficulty über SettingProfile ausschließlich im Rimconemy-System gesteuert?
3. **Save-Migration:** ✅ GEKLÄRT — Clean Break. Keine Migration. Alte Saves werden abgewiesen (Error-Dialog + Return to Main Menu). Backup-Disclaimer im Launcher. Maximale Freiheit für Save-Format v2.
4. **Andere Mods:** Was passiert mit Mods die `Find.Storyteller.def` auf Cassandra/Phoebe/Randy prüfen?

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-08-07 | Initial analysis: vanilla storyteller architecture, injection points, alternatives | Buffy (Freebuff) |
| 2026-08-07 | **KORREKTUR:** User-Pivot von "alongside vanilla" zu "full replacement". DECISIONS §34 überschrieben. StorytellerDef + StorytellerComp als Zielarchitektur. Vanilla-Storyteller via XML-Patch verstecken. | Buffy (Freebuff) |
| 2026-08-07 | **Decompile:** StorytellerComp via Mono.Cecil dekompiliert. Hat KEINEN Per-Tick-Hook. `Storyteller.StorytellerTick()` (non-virtual) treibt den Zyklus und ruft `MakeIntervalIncidents()` auf Comps. Unser GameComponentTick-Ansatz ist korrekt — Comp existiert zur Def-Registrierung, nicht für Tick-Logik. | Buffy (Freebuff) |
