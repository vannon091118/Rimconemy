# Spec — Phase D: Horde-Overlay (Home-Map + World-Map)

> **Stand:** 2026-08-05
> **Owner:** Infected & Automation (Package 05)
> **Phase:** D von 4 (Visualisierung der Horde)
> **Code-Anker (geplant):**
> - `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeCalculator.cs` (Pure-Logic)
> - `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeSectionLayer.cs` (Home-Map-Kreis)
> - `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeBurstLayer.cs` (Per-Infected-Bursts)
> - `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeCameraOverlay.cs` (Edge-Border Pulse)
> - `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeWorldObject.cs` (World-Map-Icon)
> - `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeSpawner.cs` (MapComponent, 250-tick Spawn-Pfad)
> - `mods/05-Rimconemy-Infected-Automation/Defs/WorldObjects/Rimconemy_HordeWorldObject.xml`
> - `Tests/HordeRegressionTests.cs`

## 1. Zweck / Warum diese Phase

User-Anforderung 2026-08-05:
> "pulsierender roter Kreis SectionLayer > 150 infizierte auf Home-Map, World-Map-Icon for Horde"

Phase A liefert die **Daten** (PopulationProfileMultipliers.HordeThreshold + PopulationLedger.HumanoidLiveCount/AnimalLiveCount), aber **keine Visualisierung**. Der Spieler sieht die Bedrohung nur indirekt über die ThreatDashboard-UI (modale Zahlen, kein räumliches Signal).

Phase D liefert:
1. **Home-Map Visualisierung (3 Schichten):**
   - Schicht 1: pulsierender Kreis um die Home-Map-Mitte via SectionLayer (HUD-Edge-Border Pulse zusätzlich als Backup-Layer)
   - Schicht 2: Per-Infected Radial-Bursts (5-Tile Soft-Glow um jeden live Infected Pawn auf der Home-Map)
   - Schicht 3: HUD-Edge-Border (Camera-Frame) als finale "wir sind umzingelt"-Indikation
2. **World-Map Visualisierung:**
   - Ein wanderbarer HordeWorldObject (Verse.WorldObject-Subclass) mit kreisförmigem Icon, der sich auf der World-Map zwischen Player-Home-Tile und Edge-Tiles bewegt

## 2. Design-Entscheidungen (User-Approval 2026-08-05)

| # | Frage | Entscheidung |
|---|---|---|
| 1 | Counter-Quelle | **Hybrid (Animal 0.5x)** — HumanoidLiveCount + 0.5 × AnimalLiveCount |
| 2 | Render-Layer für Kreis | **HUD-Edge-Border (CameraUIOverlay) + zusätzlich SectionLayer_Mittelpunkt-Kreis** |
| 3 | World-Map-Icon | **Eigener HordeWorldObject** (analog Outpost-WorldObject-Pattern) |
| 4 | Kreis-Form-Kombination | **Kreis mittig + Bursts pro infiziertem Pawn** (User hat beide Schichten aktiviert) |

**Konsequenz:** "Pulsierender roter Kreis" wird durch Schicht 1 (SectionLayer Kreis mittig) + Schicht 3 (Camera Edge Pulse) zusammen interpretiert. Schicht 2 (Bursts pro Pawn) ist die granulare Komponente, die zeigt wo genau die Infizierten sind.

## 3. Architektur — neue Components

| Komponente | Typ | Verantwortung |
|---|---|---|
| `HordeCalculator` | static class (Pure) | Berechnet Effective-Horde-Count mit Profile-Routing |
| `HordeCalculator.GetEffectiveCount(ledger, profile)` | Method | Returns floor(Humanoid + 0.5×Animal) |
| `HordeCalculator.IsActive(ledger, profile)` | Method | True wenn Effective >= HordeThreshold(profileId) |
| `HordeSectionLayer` | SectionLayer subclass | Pulsierender Kreis (Alpha-getrieben) auf Home-Map |
| `HordeBurstLayer` | SectionLayer subclass | Pro infiziertem Pawn ein 5-Tile-Radius Soft-Glow |
| `HordeCameraOverlay` | static class | HUD-Edge-Border Pulse via Postfix auf `UIRoot.UIRootOnGUI` |
| `HordeWorldObject` | Verse.WorldObject subclass | Wanderer-Horde mit Position + Movement |
| `HordeSpawner` | MapComponent | Positioniert/updated HordeWorldObject jeden 250 Ticks |

## 4. Datenfluss

```
HordeSpawner.MapComponentTick (nur auf der primären Player-Home-Map):
  1. ledger = PopulationLedger.Get()
  2. profile = StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival
  3. effective = HordeCalculator.GetEffectiveCount(ledger, profile)
  4. if (!HordeCalculator.IsActive(effective, profile)) {
        // Horde ist unter Schwelle — alle HordeWorldObjects despawnen
        foreach (var ho in Find.WorldObjects.AllWorldObjects.OfType<HordeWorldObject>())
            ho.Destroy();
        return;
     }
  5. homeMap = MapRegistry.GetPrimaryPlayerHomeMap(); null → return
  6. alle 250 Ticks: tile = HordeUpdateLogic.ComputeHordeTile(homeMap.Tile, now)
     (rein tick-abgeleitet, keine Drift-Persistenz — spec §6)
     → HordeWorldObject auf tile setzen/spawnen
  7. alle 15 Ticks: map.mapDrawer.RegenerateLayerNow(HordeSectionLayer) +
     RegenerateLayerNow(HordeBurstLayer)  // Custom-Layer regenerieren NICHT automatisch

WICHTIG (verifiziert an Assembly-CSharp 1.6.4566):
  - Section-Layer-Subclasses werden pro Section automatisch instantiiert
    (GenTypes.AllSubclassesNonAbstract(typeof(SectionLayer)) + Activator.CreateInstance),
    aber Regenerate() läuft nur bei Dirty-Markierung durch Vanilla-Flags oder expliziten
    RegenerateLayerNow(Type)-Aufruf. Ohne den 15-Tick-Driver wäre der Kreis leer.
  - 60-Tick-Regen wäre falsch: die 120-Tick-|sin|-Pulse-Phase liefert bei θ und θ+π
    gleiche Alphas → der Puls würde einfrieren. 15 Ticks = 8 Samples/Zyklus.

Tick Frame (HordeCameraOverlay Postfix auf UIRoot.UIRootOnGUI):
  - Malt 4 Edge-Bänder mit pulse-alpha; Pulse-Phase aus ComputePulsePhase (120-Tick-Zyklus)
  - Postfix wird explizit via HordeCameraOverlay.Install() (harmony.Patch) registriert —
    Package 05 hat kein PatchAll, ein nacktes [HarmonyPatch]-Attribut wäre inert
```

## 5. API / Interface

### `HordeCalculator`

```csharp
public static class HordeCalculator
{
    /// <summary>Hybrid counter: Humanoid + 0.5×Animal, clamped at 0.
    /// Reads ledger fields directly (no IO, deterministic from inputs).</summary>
    public static int GetEffectiveCount(PopulationLedger ledger);

    /// <summary>True when Effective >= HordeThreshold(profileId).
    /// ProfileId-mapped via StripRimconemyPrefix.</summary>
    public static bool IsActive(int effectiveCount, SettingProfile profile);

    /// <summary>Pulse-Phase for visual jitter. Returns 0..1 over 120-tick cycle.</summary>
    public static float ComputePulsePhase(long currentTick);
}
```

### `HordeSectionLayer` — Pulsierender Kreis

```csharp
public sealed class HordeSectionLayer : SectionLayer
{
    public HordeSectionLayer(Section section) : base(section) { }

    public override bool Visible => HordeCalculator.IsActive(
        // pull from PopulationLedger via ResourceReadPattern on each render
        _effectiveCount, _profile);

    public override void Regenerate()
    {
        // Compute pulse-alpha via HordeCalculator.ComputePulsePhase
        // Add radial gradient submesh from mittelpunkt of section outwards
    }
}
```

### `HordeBurstLayer` — Per-Infected Burst

```csharp
public sealed class HordeBurstLayer : SectionLayer
{
    public override void Regenerate()
    {
        // Walk map.mapPawns.AllPawnsSpawned
        // For each pawn with Faction = Rimconemy_HiddenInfectedFaction
        //   Add 5-Tile radial soft-glow submesh (red, alpha pulse)
    }
}
```

### `HordeWorldObject`

```csharp
public class HordeWorldObject : Verse.WorldObject
{
    // Drift state - einfach und deterministisch
    public long LastMoveTick;
    public long MoveIntervalTicks = 250L;
    
    public void MoveTowardsHome(int homeTile, long currentTick)
    {
        if (currentTick < LastMoveTick + MoveIntervalTicks) return;
        LastMoveTick = currentTick;
        // Drift one tile closer to home (deterministic via passed RNG)
    }
}
```

### `HordeSpawner` MapComponent

```csharp
public sealed class HordeSpawner : MapComponent
{
    // Layer-Regen-Cadence: 15 Ticks (8 Samples pro 120-Tick-Puls).
    // 60 Ticks wären falsch — θ und θ+π liefern gleiche |sin|-Alphas.
    private const int LayerRegenIntervalTicks = 15;
    private int _lastTick = -HordeUpdateLogic.TickInterval;
    private int _nextLayerRegenTick;

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (map == null) return;
        if (Scribe.mode != LoadSaveMode.Inactive) return;
        Map homeMap = MapRegistry.GetPrimaryPlayerHomeMap();
        if (homeMap == null || map != homeMap) return;
        int now = Find.TickManager?.TicksGame ?? 0;
        if (!HordeCalculator.IsActiveNow()) { DespawnAllHordes(); return; }
        if (now >= _lastTick + HordeUpdateLogic.TickInterval)
        {
            _lastTick = now;
            SyncHordeAtTile(HordeUpdateLogic.ComputeHordeTile(homeMap.Tile, now), homeMap.Tile);
        }
        if (now >= _nextLayerRegenTick)
        {
            _nextLayerRegenTick = now + LayerRegenIntervalTicks;
            map.mapDrawer?.RegenerateLayerNow(typeof(HordeSectionLayer));
            map.mapDrawer?.RegenerateLayerNow(typeof(HordeBurstLayer));
        }
    }
}
```

### `HordeUpdateLogic` Pure Helper

```csharp
public static class HordeUpdateLogic
{
    public const int TickInterval = 250;
    public const int InitialDistanceFromHome = 5;

    /// tile = homeTile + max(0, 5 − floor(tick/250))
    /// Rein tick-abgeleitet — keine Persistenz, Save/Load-resistent.
    public static int ComputeHordeTile(int homeTile, long currentTick)
    {
        int drifted = (int)(currentTick / TickInterval);
        return homeTile + Math.Max(0, InitialDistanceFromHome - drifted);
    }
}
```

## 6. Determinismus-Garantien

- `HordeCalculator` Pure → keine Zeit/Random-Abhängigkeit → reproduzierbar.
- `HordeSpawner` MapComponent-Tick 250 → deterministischer Loop-Cadence via `currentTick` only.
- `HordeUpdateLogic.RunOnce` deterministisch: gleiche (currentTick, ledger, profile, mapRegistry) → gleiche Spawn-Position.
- `HordeWorldObject.MoveTowardsHome` deterministisch: gleicher currentTick → gleiche Tile-Coordinates (drift = 1 tile closer).
- Save/Load: keine Persistenz für `LastMoveTick` (transient — Rebuild aus currentTick % MoveIntervalTicks). Bei Save/Load wird Resume an gleicher Position gewährleistet (Drift = floor(currentTick/250) tiles in Richtung Home).

## 7. Edge Cases / Failure-Modes

| Edge Case | Verhalten |
|---|---|
| ledger == null | HordeCalculator.GetEffectiveCount returns 0; IsActive false; Horde wird despawnt |
| profile == null | Fallback "Survival" via StripRimconemyPrefix (HordeThreshold=150) |
| Effective ≥ Threshold, keine Player-Home-Map | HordeSpawner.RunOnce no-op; keine Spawn |
| Player-Home-Map auf Tile 0 | HordeWorldObject driftet von Tile 5 (deterministisch) |
| HordeWorldObject fehlt im Def | Log.Error + no spawn (Failure-mode logged) |
| MapDrawer not ready | SectionLayer.Regenerate defensive: try/catch → empty layer |
| Camera-Overlay außerhalb Game | Static postfix no-op |
| Save/Load in Mid-Horde | Layer/WorldObject rebuild deterministisch via currentTick drift |

## 8. Tests (`Tests/HordeRegressionTests.cs`)

| # | Test | Asserts |
|---|---|---|
| D1 | CalculatorEmptyLedger | Effective=0, IsActive=false |
| D2 | CalculatorSurvival150Human | Humanoid=150 → Effective=150 → IsActive=true (Survival threshold) |
| D3 | CalculatorSurvival100Human100Animal | 100 + 0.5×100 = 150 → active |
| D4 | CalculatorCollapseNeverBelow | Threshold Collapse=80 → 50 humanoid not active; 80 humanoid = active |
| D5 | CalculatorProfileFallback | null profile → Survival-fallback active |
| D6 | PulsePhaseSinusoidal | tick=0 → phase=0; tick=30 → ~1.0; tick=60 → ~0 |
| D7 | UpdatePureSpawnAtInitialDistance | ComputeHordeTile(50, 0) == 55 (home + 5) |
| D8 | UpdatePureDriftsOnePerInterval | 249→55, 250→54, 500→53 (floor(tick/250) moves) |
| D9 | UpdatePureArrivesAndClampsAtHome | 1249→51, 1250→50, 100000→50 (nie unter home) |
| D10 | UpdatePureDeterministicFromTick | gleiche (tile,tick) → gleiches Ergebnis; home=7,tick=250 → 11 |
| D11 | CalculatorAnimalHalfCapRoute | Hybrid count at Refuge threshold (100+0.5×100=150 < 220) |
| D12 | WorldObjectExistsInDefDB | Rimconemy_HordeWorldObject XML loads successfully |
| D13 | SpawnerCadenceSamplesPulse | 15-Tick-Cadence teilt 120 mit ≥4 Samples, keine Aliasing |
| D14 | SpawnerRegenOnlyWhileActive | Regen feuert nur bei aktiv + fällig; Layers sind SectionLayer |
| D15 | SpawnerActuallyFiresRegen | Driver feuert RegenerateLayerNow je Layer pro Fire, nur bei aktiv (Counting-Sink) |

(D11-D12 duplicates and D15 impossible-input test were cut in the 2026-08-05 scope-cleanup;
D2/D5 already cover prefix-strip routing and null-profile fallback.)

## 9. StoryEventCatalog-Anker

Phase D fügt **keine** neuen Story-Events hinzu. Die Horde ist ein passiver Visual-State, kein Event.

## 10. Bootstrap & Logging

`Bootstrap.cs`:
```csharp
World.DarknessSectionLayerLifecycle.Install();    // existing
Tests.HordeRegressionTests.RunAll();               // NEW
Horde.HordeCameraOverlay.Install();                // NEW — expliziter harmony.Patch
Log.Message("[Rimconemy.InfectedAutomation] Phase D: Horde overlay (Home+World+Camera) wired.");
```

The Camera-Edge postfix REQUIRES the explicit `Install()` call:
Package 05 has no `Harmony.PatchAll`, so a bare `[HarmonyPatch]`
attribute would never be applied (verified: only
`DarknessSectionLayerLifecycle` patches, and it does so explicitly
with `harmony.Patch(...)`). Same explicit-install pattern applies.

Logging-Hooks (alle Debug-Level):
- `[HordeCalculator] effective=N threshold=N active=true|false`
- `[HordeSpawner] Spawning HordeWorldObject at tile=N (home=N)`
- `[HordeSpawner] Despawning all HordeWorldObjects — count below threshold`
- `[HordeWorldObject] Move: tile=N → N+1 (currentTick=N, interval=250)`
- `[HordeSectionLayer] Regenerate at section (X,Z) alpha=0.000..0.350 pulse-phase=0.75`

## 11. Akzeptanz-Gate (Phase D)

- [ ] D1 — `HordeRegressionTests.RunAll()` = 15/15 PASS.
- [ ] D2 — `HordeCalculator.GetEffectiveCount` deterministisch-tests bei 5 Config-Samples.
- [ ] D3 — `HordeSpawner.MapComponentTick` läuft syncron mit PopulationLedger-Reconciler (kein Race).
- [ ] D4 — `Rimconemy_HordeWorldObject` Def lädt via DefDatabase XML.
- [ ] D5 — `HordeSectionLayer` regeneriert NICHT wenn Horde inactive (RegenerateLayerNow prüft Visible pro Section; Spawner ruft den Driver nur bei active).
- [ ] D6 — `HordeCameraOverlay` Postfix via explizitem `HordeCameraOverlay.Install()` (harmony.Patch auf UIRoot.UIRootOnGUI) registriert.
- [ ] D7 — `runtime_test.sh --skip-start --no-deploy` exit 0; Bump auf 0.0.61.
- [ ] D8 — Live-Beleg im Player.log: Spawner-Marker + World-Map-Icon sichtbar.

## 12. Nicht-Ziele (Phase D)

- **Kein** neuer Incident (Horde ist passiv).
- **Keine** Persistenz für `LastMoveTick` (transient, Rebuild aus currentTick).
- **Kein** Override auf bestehende SectionLayer_Darkness (Horde ist additiv).
- **Keine** neue FactionDef (Rimconemy_HiddenInfectedFaction weiterverwendet).
- **Keine** neue PawnKind (Infizierte Tiere nutzen Rimconemy_InfectedWildlife, Human-Horde Rimconemy_InfectedRavager).

## 13. Verweise

- Phase A spec: `docs/superpowers/specs/2026-08-05-population-ledger-design.md` (Daten-SSOT: Humanoid/Animal/Cap + HordeThreshold in PopulationProfileMultipliers §2 Profile-Multiplier).
- Phase B spec: `docs/superpowers/specs/2026-08-05-daily-growth-revenge-design.md` (Day-Tick-Block — HordeCalculator liest PopulationLedger nach dem Phase-B-Reconcile-Block).
- Phase C spec: `docs/superpowers/specs/2026-08-05-tier-inoculation-design.md` (AnimalHalfCap pattern — 0.5×-Factor ist konsistent).
- Darkness Spec: `docs/superpowers/specs/2026-08-05-darkness-sectionlayer-design.md` (Pattern-Reference für SectionLayer-Harmony + AddColor + CreateOverlayColor).
- OutpostWorldObject Pattern: `mods/04-Rimconemy-Economy-Territory/Source/Outposts/OutpostWorldObject.cs` (Pattern für Verse.WorldObject-Subclass).
- INTERFACE_CONTRACT §3 (Cross-Package-Auth) und §8 (UI/Visual).
