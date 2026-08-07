# STORYTELLER_ANALYSIS — RimWorld Vanilla Storyteller & Rimconemy Injection

> **Date:** 2026-08-07  
> **Status:** Architecture analysis — how RimWorld storytellers work, what we can hook, what we can't  
> **Related:** `docs/DECISIONS.md §34`, `docs/H2-story-contract.md`, `docs/REFACTORING_PLAN.md`  
> **Decision:** We do NOT use a custom StorytellerDef (DECISIONS §34). We inject via `FiringIncident` + `TryFire(queued:true)`.

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

### 1.4 What We CANNOT Do (Without Replacing Vanilla Storyteller)

| Goal | Feasibility | Reason |
|------|------------|--------|
| **Replace vanilla storyteller entirely** | ❌ | Would require a custom `StorytellerDef` + rimworld XML patching. This would REMOVE Cassandra/Phoebe/Randy and all their incident logic. DECISIONS §34 explicitly rejected this. |
| **Intercept/mute vanilla incidents** | ❌ | The `TryFire` call chain is not virtual — we'd need a Harmony Patch on Storyteller.TryFire, which is fragile and conflicts with other mods. |
| **Modify storytellerComps at runtime** | ⚠️ Risky | `storytellerComps` is a public list, but mutating it would affect all mods. Other mods may add/remove comps. |
| **Read incidentQueue contents** | ⚠️ Partial | `IncidentQueue` is public, but `IncidentQueueEntry` fields are mostly private. We can check `Count` but not individual entries. |

### 1.5 What We CAN Do

| Goal | How | Status |
|------|-----|--------|
| **Inject custom incidents** | `FiringIncident` + `TryFire(queued:true)` → pushes onto incidentQueue | ✅ Already implemented |
| **Run alongside vanilla storyteller** | Our `GameComponentTick` (60,000 tick interval) evaluates independently; vanilla storyteller continues its own cycle | ✅ Already implemented |
| **Read vanilla difficulty** | `Find.Storyteller.difficultyDef` → map to Rimconemy SettingProfile | ✅ `ResolveProfileFromDifficulty()` |
| **Read game state** | Colony wealth, pawns, mood, power, factions, storage — all through Foundation contracts | ✅ `BuildLiveSnapshot()` |
| **Track external mod state** | StorageQuery (Pkg 03), PopulationLedger (Pkg 05), ColonialReader (Foundation) | ✅ Capability-gated |
| **Add StorytellerComp** | We COULD register a custom `StorytellerComp` that runs every tick alongside vanilla comps | ⚠️ Not explored — see §3 |

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

## 3. Alternative Approaches

### 3.1 Option A: Custom StorytellerComp (Recommended Alternative to StorytellerDef)

**What it is:** RimWorld allows mods to register custom `StorytellerComp` classes that run inside the vanilla storyteller loop. This is the **standard modding pattern** for adding custom incident logic WITHOUT replacing Cassandra/Phoebe/Randy.

**How it works:**
```xml
<!-- In a StorytellerDef patch -->
<StorytellerDef>
  <comps>
    <li Class="Rimconemy.InfectedAutomation.Story.RimconemyStorytellerComp" />
  </comps>
</StorytellerDef>
```

```csharp
public class RimconemyStorytellerComp : StorytellerComp
{
    protected override void IncidentCycleTick()
    {
        // Called every tick by the vanilla storyteller
        // We can check our own conditions and fire incidents
    }
}
```

**Advantages over current approach:**
- Runs in the same tick cycle as vanilla storyteller (no 60k-tick latency)
- Can read `IncidentParms` populated by vanilla (threat points, faction)
- Can participate in cooldown tracking alongside vanilla incidents
- Standard RimWorld modding pattern — other mods expect it
- No need for `GameComponentTick` polling

**Disadvantages:**
- Still runs alongside vanilla storyteller (does not replace it)
- Must respect vanilla's cooldown system
- Harder to independently control evaluation interval
- Requires patching all vanilla StorytellerDefs (or a base-def XML patch)

### 3.2 Option B: Harmony Patch on Storyteller.TryFire (NOT Recommended)

**What it is:** Use Harmony to prefix/postfix `Storyteller.TryFire()` to intercept, modify, or suppress vanilla incidents.

**Advantages:**
- Can see EVERY incident before it fires
- Can modify `IncidentParms.points` or block incidents
- Can inject additional logic into the vanilla fire path

**Disadvantages (why DECISIONS §34 rejected this):**
- Harmony patches on Storyteller are fragile — break on game updates
- Conflicts with every other storyteller-modifying mod
- One bad Harmony patch crashes the entire storyteller loop
- Debugging is extremely difficult (no stack traces in IL-patched code)
- Violates the "alongside vanilla" policy

### 3.3 Option C: Full Custom StorytellerDef (Rejected)

**What DECISIONS §34 says:** Rimconemy betreibt **keinen** eigenen `StorytellerDef`. Vanilla-Storyteller bleibt autoritativ.

**Why rejected:**
- Removes player choice (no more Cassandra/Phoebe/Randy)
- DLC storyteller comps (Anomaly, Royalty) would need re-implementation
- Massive maintenance burden for every RimWorld update
- Mod compatibility nightmare
- Our value-add is dynamic events on top of vanilla, not replacing it

### 3.4 Option D: Hybrid — StorytellerComp + current GameComponent (Synthesis)

**Recommendation:** Keep our current `GameComponentTick` approach but ADD a `StorytellerComp` for specific real-time hooks.

| Responsibility | Current | Proposed |
|---------------|---------|----------|
| Daily event evaluation | GameComponentTick (daily) | GameComponentTick (daily) — keep |
| Real-time threat pressure reading | N/A | StorytellerComp (per-tick) — add |
| Vanilla incident awareness | N/A | StorytellerComp can read `IncidentQueue` count |
| Cooldown participation | Our own `StoryState` | Can use vanilla's per-category cooldowns |
| Incident injection | `TryFire(queued:true)` | Same + can also use `TryFire(queued:false)` for immediate |

---

## 4. StoryDirector → RimconemyStoryteller: Rewiring Plan

### 4.1 What Changes

```
CURRENT:
  StoryDirector : GameComponent
    ├── GameComponentTick → evaluates every 60k ticks
    ├── QueueSelectedIncident → FiringIncident + TryFire(queued=true)
    └── Concern overload: wipe-check, eval, growth, revenge, inoculation

PROPOSED:
  RimconemyStoryteller (renamed, split):
    ├── RimconemyStorytellerComp : StorytellerComp     ← NEW: runs in vanilla loop
    │   ├── IncidentCycleTick → real-time hooks
    │   ├── Can read vanilla incidentQueue state
    │   └── Can fire via TryFire(queued=false) for immediate response
    │
    ├── StoryScheduler : GameComponent                  ← KEPT: daily evaluation
    │   ├── GameComponentTick (60k)
    │   ├── BuildLiveSnapshot
    │   └── EvaluateWithSnapshot → StorySelector
    │
    └── IncidentDispatcher                              ← SPLIT from StoryDirector
        └── QueueSelectedIncident → TryFire
```

### 4.2 What We Gain from StorytellerComp

| Feature | Without StorytellerComp | With StorytellerComp |
|---------|------------------------|---------------------|
| React to vanilla incidents in real time | ❌ Can't see them | ✅ `IncidentQueue` visible |
| Participate in vanilla cooldown tracking | ❌ Our own separate tracking | ✅ Shared cooldown system |
| Read threat points calibrated by difficulty | ❌ Must calculate ourselves | ✅ `StorytellerUtility.DefaultThreatPointsNow` |
| Fire incidents immediately (not queued) | ❌ Only `TryFire(queued=true)` | ✅ `TryFire(queued=false)` for instant |
| 0-tick latency on game-state change | ❌ Up to 60k ticks | ✅ Per-tick evaluation |

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

## 6. Summary: What Changes, What Stays

| Component | Current | Proposed | Reason |
|-----------|---------|----------|--------|
| StoryDirector | 1,086 LOC GameComponent mixing 7 concerns | Split into StoryScheduler + RimconemyStorytellerComp + IncidentDispatcher | Concern isolation, real-time hooks |
| Storyteller model | No StorytellerDef, no StorytellerComp | ADD StorytellerComp for real-time awareness | Can read vanilla incident state, 0-tick latency |
| Incident injection | `TryFire(queued:true)` only | Add `TryFire(queued:false)` option via Comp | Immediate reaction to game events |
| Dynamic mod-aware events | Not implemented | EventTemplate + DynamicResolver | Reads DefDatabase at selection time |
| Vanilla storyteller | Runs unchanged alongside us | Runs unchanged alongside us (NO change) | DECISIONS §34 — vanilla stays authoritative |
| External tracking | Foundation contracts (good) | Extend with StorytellerUtility.DefaultThreatPointsNow | Better-calibrated raid strength |

### What We Will Never Do

- ❌ Replace vanilla StorytellerDef
- ❌ Harmony-patch `Storyteller.TryFire`
- ❌ Suppress/remove vanilla incidents
- ❌ Hook into other mods' private state via Reflection

### What We Will Do

- ✅ Add `RimconemyStorytellerComp` for per-tick awareness
- ✅ Split `StoryDirector` into 3 focused classes
- ✅ Dynamic event generation from `DefDatabase` scans
- ✅ Template-based letters that resolve mod-specific names at fire time
- ✅ Keep `GameComponentTick` for daily heavy evaluation

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-08-07 | Initial analysis: vanilla storyteller architecture, injection points, alternatives, StorytellerComp recommendation | Buffy (Freebuff) |
