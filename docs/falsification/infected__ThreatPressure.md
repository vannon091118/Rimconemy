# Falsifizierungsbericht: `Infected/ThreatPressure`

> **Capability:** `rimconemy.infectedautomation.automation` v1 · **Owner:** Infected · **Stand:** 2026-08-05
> **Status:** `COMPILED, BOOT, REGRESSION` · `LIVE`: pending user verification
> **Code-Anker:**
> - `Source/Horde/HordeCalculator.cs` (Pure Effective-Count + IsActive + PulsePhase)
> - `Source/Horde/HordeUpdateLogic.cs` (Pure Spawn/Move/Despawn)
> - `Source/Horde/HordeSpawner.cs` (MapComponent, 250-tick Spawner)
> - `Source/Horde/HordeWorldObject.cs` + `Defs/WorldObjects/Rimconemy_HordeWorldObject.xml` (Wanderer)
> - `Source/Horde/HordeSectionLayer.cs` (Pulsierender Kreis mittig, 3 Ringe)
> - `Source/Horde/HordeBurstLayer.cs` (Per-Infected Radial-Bursts)
> - `Source/Horde/HordeCameraOverlay.cs` (UIRoot Postfix, 4 Edge-Borders)
> - `Tests/HordeRegressionTests.cs` (D1–D12)

## A — Def-Liste (XML-Defs)

- `Defs/WorldObjects/Rimconemy_HordeWorldObject.xml`:
  - `<defName>Rimconemy_HordeWorldObject</defName>`
  - `<worldObjectClass>Rimconemy.InfectedAutomation.Horde.HordeWorldObject</worldObjectClass>`
  - `<drawerType>MapMeshAndFoam</drawerType>`
  - `<color>(0.85, 0.15, 0.15)</color>`

## B — Code-Pfad (Build + Boot)

`Source/Horde/HordeCameraOverlay.cs` wird EXPLIZIT via `HordeCameraOverlay.Install()`
(harmony.Patch auf `UIRoot.UIRootOnGUI`) aus `Bootstrap` registriert. Package 05 hat
kein `Harmony.PatchAll` — ein nacktes `[HarmonyPatch]`-Attribut wäre inert (verifiziert
an Assembly-CSharp: einziger Patch-Mechanismus in 05 ist `DarknessSectionLayerLifecycle`
mit explizitem `harmony.Patch(...)`).

- Bootstrap: `Tests.HordeRegressionTests.RunAll()` + `Horde.HordeCameraOverlay.Install()`, dann log: `[Rimconemy.InfectedAutomation] Phase D: Horde overlay wired (…).`
- Patch-Klassen: `Source/Horde/HordeCameraOverlay.cs` (Postfix auf `UIRoot.UIRootOnGUI`)
- Layer-Regen-Driver: `HordeSpawner.MapComponentTick` ruft alle 15 Ticks
  `map.mapDrawer.RegenerateLayerNow(HordeSectionLayer)` + `RegenerateLayerNow(HordeBurstLayer)`.
  Custom SectionLayer werden zwar auto-instantiiert (`GenTypes.AllSubclassesNonAbstract`),
  aber VANILLA regeneriert nur dirty Layers — ohne den expliziten Driver bliebe der Kreis leer.
  (60-Tick-Regen wäre falsch: |sin| bei θ und θ+π ist gleich → Puls friert ein.)

## C — Selbsttest (RunAll)

`Tests.HordeRegressionTests.RunAll()` ist in `Bootstrap` aufgerufen. 12 Tests (D1–D12).

- D1–D6: HordeCalculator (Effective-Count, IsActive-Surface, PulsePhase two-breath sinusoid)
- D7–D10: HordeUpdateLogic.ComputeHordeTile (Spawn + 5 / Drift 1 pro 250 / Clamp bei home / deterministisch)
- D11: Hybrid-Count-Route (AnimalHalfCap) bei Refuge-Threshold
- D12: `Rimconemy_HordeWorldObject` Def lädt + worldObjectClass == typeof(HordeWorldObject)

## D — Phase-D Live-Beleg (User Live-Test erforderlich)

**Erwartet im Player.log nach Phase D (2026-08-05):**

```
[Rimconemy.InfectedAutomation] HordeCameraOverlay: edge-frame postfix installed.
[Rimconemy.InfectedAutomation] HordeSpawner: Spawning HordeWorldObject at tile=N (home=N)
```

(Der Spawn-Marker ist der einzige Horde-WorldObject-Log; Drift ist über die World-Map-
Icon-Position beobachtbar — tile = home + max(0, 5 − floor(tick/250)), keine Log-Zeilen.)

**Verifikation (User-Pflicht):**

1. Start Survival-Kolonie (difficulty=Medium).
2. Töte im Dev-Mode 150+ infizierte Human-Pawns auf der Home-Map (PopulationLedger.HumanoidLiveCount ≥ 150).
3. World-Map: rotes Wanderer-Icon sichtbar auf Tile nahe Home (5 Tiles entfernt initial).
4. Warte einige 250-Tick-Intervalle: das Icon driftet deterministisch Richtung Home (floor(currentTick/250) Tiles).
5. Home-Map: pulsierender roter Kreis (3 Ringe: Inner/Mid/Outer) um Map-Mitte sichtbar; pulst mit ~2-Sek-Atem.
6. Per-Infected-Pawn: 5-Tile Radial-Burst um jeden visible HiddenInfected-Pawn sichtbar.
7. Camera-Edge: 4 dünne rote Borders am Bildschirmrand, pulsen synchron zum Map-Overlay.
8. Sinkt HumanoidLiveCount unter 150 → alle Visual-Effekte verschwinden, HordeWorldObject wird despawned.

**Akzeptanz-Gate:**

- [ ] **D-1**: 12/12 HordeRegressionTests grün im Bootstrap.log.
- [ ] **D-2**: Spell auf Survival-Profil mit ~10 PopulationLedger.RecentKillsToday löst KEIN Horde aus (Threshold erst ab 150 Humanoid).
- [ ] **D-3**: Live-Beleg im Player.log (Schritte 1–8 oben dokumentiert).
- [ ] **D-4**: Save/Load: Horde-WorldObject-Position rebuild deterministisch aus currentTick-Drift.
- [ ] **D-5**: `runtime_test.sh --skip-start --no-deploy` exit 0.

## E — Save/Load Roundtrip

<!-- Step: Spielstand speichern → neu laden → HordeWorldObject neu positioniert via currentTick-Drift-Berechnung -->
<!-- Transient: kein Scribe (kein State-Persistenz). Rebuild aus Find.WorldObjects + HordeUpdateLogic.ComputeHordeTile mit currentTick. -->

## F — Cross-Package READ

HordeCode liest:
- `PopulationLedger.Get()` (eigene Capability `rimconemy.infectedautomation.population` v1)
- `StoryDirector.Get()` (Phase-B-Pattern; Capability bereits registered)
- `Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap()` (Mod-01 Surface)

Schreibt:
- WorldObjects via `Find.WorldObjects.Add(...)` (hostile-faction-less; visible Player-Faction-agnostic).

## G — Performance-Kennzahl

- HordeSpawner: 250-tick WorldObject-Cadence + 15-tick Layer-Regen-Driver (nur bei active, nur Home-Map).
- HordeSectionLayer: 32 Segmente × 3 Ringe = 96 Triangles, gezeichnet NUR aus der einzelnen Section, die `map.Center` enthält (world-space Vertices). Keine 9×-Überlagerung/Z-Fighting mehr.
- HordeBurstLayer: 16 Segmente pro HiddenInfected-Pawn je Section. Bei 20 Bursts × 30 sections = ~600 Triangles. Akzeptabel.
- HordeCameraOverlay: 4 GUI-Draw-Aufrufe pro Frame (Top/Bottom/Left/Right), 1 Postfix-Aufruf pro Frame (early-out wenn inactive).

## User-Aktion Pflicht

1. `./scripts/deploy.sh 05` (Live-Deploy).
2. `./scripts/runtime_test.sh --require-scenario-tests` (Runtime-Beleg).
3. Live-Beleg der Schritte 1–8 dokumentieren in **Abschnitt D** hier.
4. Save/Load-Test für Abschnitt E.
5. Performance-Zahl für Abschnitt G.

Sobald alle User-Bloecke befuellt sind, gilt der Bericht als `SURVIVED`.
