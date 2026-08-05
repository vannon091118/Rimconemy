# P6 — Gameplay Layers: Multi-Task Progress Sheet (Phase 6)

> **Stand:** 2026-08-04
> **Owner:** Survival (02), Scavenger (03), Economy (04), Infected (05) — jedes Task behält seinen Paketeigentümer
> **Status:** Strukturscaffold gelegt, sichtbare Mechanik durch Live-Lauf zu belegen

Dieses Dokument sammelt Eingangs- und Nachweispunkte für die P6-Multi-Task-Strecke:

| # | Subtask | Paket | Code-Anker | Live-Beleg-Pflicht |
|---|---|---|---|---|
| 8 | Bauschutt → Wand/Tür-Remap | 03 | `Patches/Bauschutt_Remap_Patches.xml` +  `Source/Building/BauschuttRemapService.cs` | Bauschutt-Drop auf Wand-Spot → Wall-stub |
| 9 | Nahrung/Hanf getrennt (WorkGiver + Ernte + Verderb) | 03 | `Source/Plants/{HempFoodSplitter,FoodHarvestCycleWindow}.cs` | Nahrung in Storage wächst; Hanf reift unabhängig |
| 10 | Wasser-/Brennstoff-Physikalischer Generator | 03 | `Source/Resources/{WaterFuelPath,GeneratorFuelFeeder}.cs` | Generator-Klick → WoodLog-Verbrauch sichtbar |
| 11 | Pfeilturm (Strom als harte Bedingung) | 03 | `Source/Building/ArrowTurretPowerGate.cs` | Tower ohne Strom → Offline-UI |
| 12 | Infizierten-Raids (echter Spawn) | 05 | `Source/Incidents/InfectedRaidSpawnService.cs` + `IncidentWorker_InfectedRaid.TryExecuteWorker` | Letter → Map-Region-Spawn |
| 13 | Mechadroids (Grundsystem) | 05 | `Source/Mechadroids/{MechadroidUnit,MechadroidJobRegistry}.cs` | Mechadroid RNG spawnt + Job |
| 14 | Outposts & Proxy-Graph (3-Tages-Countdown) | 04 | `Source/Outposts/{OutpostService,ProxyGraph}.cs` | Outpost-Gründung + 60.000·3 Tick-Countdown |
| 15 | Weltkarten-Endgame (World-Map-Overlay + World-Raids) | 05 | `Source/World/{WorldRaidCoordinator,WorldMapOverlay}.cs` | World-Raid-Event auf Tile-Coordinate |
| 16 | **Predicate Truthfulness Hardening** (Phase Contract v2) | 02 | `Source/Phase/PhaseProgressResolver.cs` + `Phase/PhaseContractGate.cs` | PhaseProgress-Overlay zeigt echte Phase-Progression, keine Trader-False-Positives. Truthfulness Doctrine in §10 |


## Erforderliche User-Live-Tests je Task

Da diese Mechaniken direkt mit Spielzustand arbeiten, ist jeder Subtask mit
einem **Evidence-Gate** versehen, den ich nicht ohne Spielstart belegen
kann. Du startest die Kampagne (`./scripts/runtime_test.sh --require-scenario-tests`),
führst die Szene aus und hängst den resultierenden `player.log`-Auszug an
den passenden Falsifizierungsbericht.

Vorgehen pro Task:
1. Code-Aufruf in `Bootstrap.cs` ist verkabelt (`Tests.XxxRegressionTests.RunAll()`).
2. Def-XML-Datei in `Defs/` des richtigen Pakets abgelegt.
3. `bump_version.sh` läuft, `git commit` abgesetzt.
4. Live-Test: einmal manuell im Spiel reproduzieren, Falsifizierungsbericht aktualisieren.

## Falsifizierungsbericht-Lifecycle

Wir haben 20 Berichte (`docs/superpowers/...` Vorlagen siehe frühere Archiv-Tar).
Pro Task wird EIN Bericht autopopulated mit dem kleinen Setup-Stand.
Die SURVIVED-Markierung erfolgt jeweils nach deinem Live-Run.

## Truthfulness-Hardening-Lockstep (2026-08-05)

Predicate-SSOT-Update in `docs/PHASE_PROGRESSION_CONTRACT.md §10` + `PhaseProgressResolver.cs`:
- `first-cooked-meal` — Counter × Building-OR-Campfire-Stove-Electric (kein Trader-False-Positive; Campfire als Phase-1-Kochstation muss gezählt werden)
- `first-steel-smelted` — Research × Building × Counter Triple-Conjunction (engstmögliche Wahrhaftigkeit ohne RecipeWorker-Postfix)
- `machine-parts-built` — Counter × Building-OR-Smithy-Table-Machining (kein Trader-Dump-Trigger)
- Build-Fix: `ResearchProjectDef.CostAmount` (1.6-fremd) → `bool IsFinished` (kanonisch)
- `runtime_test.sh` verifiziert die SSOT-Indices weiterhin über `PhaseProgressResolverTests.RunAll()`.

## Owner-Notizen

Die Pipeline-Marker P1–P5 + P6 sind in dieser Datei zentralisiert. So bleibt
ROADMAP.md §8.6 referenzierbar ohne dass jede Patch-Stufe ihren eigenen
Tracker pflegen muss.
