# Falsifizierungsberichte — Rimconemy Berichte-Index

> **Owner:** Buffy (Agent) + User
> **Stand:** 2026-08-04
> **Lifecycle:** `UNVERIFIED` → `COMPILED` → `LOADED` → `OBSERVED` → `SURVIVED`
> **Quelle:** `ROADMAP.md §8.2`

Dieses Dokument verlinkt alle Falsifizierungsberichte. Aktueller Stand:
22 Stammsberichte (Foundation/Survival/Scavenger/Economy/Infected) + 5 Vertical-Slice-Early-Game-Berichte = **27 Berichte**. Jeder Bericht
folgt einem einheitlichen A–G-Beleg-Schema (siehe Vorlage). Status wird
mit jeder Patch-Stage zusammen aktualisiert.

## Legende

- `UNVERIFIED`  — Bericht angelegt, kein Beleg
- `COMPILED`    — Berichts-Code kompiliert
- `LOADED`      — Bericht lädt im Spiel (Def+Class sichtbar)
- `OBSERVED`    — User hat den Bericht in einem echten Lauf gesehen
- `SURVIVED`    — `A–G` vollständig belegt, merge-fähig

## Berichts-Liste

### Foundation (2)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 1 | `Servicebus` | [`foundation__Servicebus.md`](foundation__Servicebus.md) | `mods/01-Rimconemy-Foundation/Source/Catalog/` | `COMPILED` |
| 2 | `BootstrapLogDedup` | [`foundation__BootstrapLogDedup.md`](foundation__BootstrapLogDedup.md) | `mods/01-Rimconemy-Foundation/Source/Profile/ProfileDetector.cs` | `COMPILED` |

### Survival &amp; Progression (5)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 3 | `Needs` | [`survival__Needs.md`](survival__Needs.md) | `NeedMappingService` | `COMPILED` |
| 4 | `WorkXp` | [`survival__WorkXp.md`](survival__WorkXp.md) | `BuildingProgressionAdapter` | `COMPILED` |
| 5 | `ExperienceUnlocks` (vormals `Research (Legacy-Read-Model)`) | [`survival__Research.md`](survival__Research.md) | `ProgressionGameComponent.ResearchCapabilities` | `COMPILED` |
| 6 | `GameOver` | [`survival__GameOver.md`](survival__GameOver.md) | `GameOverDetector` | `COMPILED` |
| 7 | `SaveMigration` | [`survival__SaveMigration.md`](survival__SaveMigration.md) | `CharacterSetupState.MigrateIfNeeded` (ISchemaMigratable) | `COMPILED` |

### Scavenger Infrastructure (5)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 8 | `ConstructionDebris` | [`scavenger__ConstructionDebris.md`](scavenger__ConstructionDebris.md) | `BauschuttRemapService` | `LOADED` |
| 9 | `FoodAndHemp` | [`scavenger__FoodAndHemp.md`](scavenger__FoodAndHemp.md) | `FoodHarvestCycleService` | `LOADED` |
| 10 | `WaterPowerArrowTurret` | [`scavenger__WaterPowerArrowTurret.md`](scavenger__WaterPowerArrowTurret.md) | `FueledGeneratorService` + `ArrowTurretPowerGate` | `LOADED` |
| 11 | `ExecutePhysicalTransfer` | [`scavenger__ExecutePhysicalTransfer.md`](scavenger__ExecutePhysicalTransfer.md) | `StorageQuery` + `CaravanStorageEnumerator` | `COMPILED` |
| 12 | `ReservePhysicalTransfer` | [`scavenger__ReservePhysicalTransfer.md`](scavenger__ReservePhysicalTransfer.md) | `StorageScope.SpecificMap` | `COMPILED` |

### Economy (5)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 13 | `WalletCredits` | [`economy__WalletCredits.md`](economy__WalletCredits.md) | `CreditsLedger` | `COMPILED` |
| 14 | `Market` | [`economy__Market.md`](economy__Market.md) | `MarketService` | `COMPILED` |
| 15 | `ReservePhysicalTransfer` | [`economy__ReservePhysicalTransfer.md`](economy__ReservePhysicalTransfer.md) | `OutpostProxyGraph` | `COMPILED` |
| 16 | `OutpostProduction` | [`economy__OutpostProduction.md`](economy__OutpostProduction.md) | `OutpostProxyGraph` | `COMPILED` |
| 17 | `TerritoryCountdown` | [`economy__TerritoryCountdown.md`](economy__TerritoryCountdown.md) | `OutpostProxyGraph.MaxReportIntervalTicks` | `COMPILED` |

### Infected &amp; Automation (5)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 18 | `ThreatPressure` | [`infected__ThreatPressure.md`](infected__ThreatPressure.md) | `ThreatAggregator` + `StoryDirector` | `COMPILED` |
| 19 | `InfectedRaid` | [`infected__InfectedRaid.md`](infected__InfectedRaid.md) | `InfectedRaidSpawnService` + `InfectedRaidWorker` | `UNVERIFIED` |
| 20 | `MechadroidJob` | [`infected__MechadroidJob.md`](infected__MechadroidJob.md) | `MechadroidJobRegistry` | `COMPILED` |
| 21 | `ManualRaid` | [`infected__ManualRaid.md`](infected__ManualRaid.md) | `IncidentClassifier` | `COMPILED` |
| 22 | `AutoResolve` | [`infected__AutoResolve.md`](infected__AutoResolve.md) | `WorldRaidCoordinator` + `ThreatAggregator` | `COMPILED` |

### Vertical Slice — Early Game (5)

Diese Berichte bündeln die 5 Phasen-Gates aus
`docs/superpowers/plans/2026-08-04-early-game-vertical-slice.md` (Survivor → Campfire → Barrikade → 1. Nacht → Save/Load). Sie sind **Stand-Berichte vor dem LIVE-Lauf**: A–C-Stubs pre-LIVE, D–G warten auf `runtime_test.sh`. Erst nach vollständigem A–G darf der Phase-Übergang freigegeben werden.

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 23 | `Early Game: Survivor` | [`earlygame__Survivor.md`](earlygame__Survivor.md) | `ScenPart_RimconemyStart` + `ScenPart_RimconemyStartEnemies` + `Rimconemy_ScrapRifle` + `Rimconemy_SteelScraps` | `COMPILED` (Pre-LIVE) |
| 24 | `Early Game: Campfire` | [`earlygame__Campfire.md`](earlygame__Campfire.md) | `Rimconemy_Campfire` + `Rimconemy_MakeCoal` + `Rimconemy_SalvageMachineParts` | `COMPILED` (Pre-LIVE) |
| 25 | `Early Game: Barricade` | [`earlygame__Barricade.md`](earlygame__Barricade.md) | `Rimconemy_Tier1Barricade` + `Rimconemy_Shelter` | `UNVERIFIED` |
| 26 | `Early Game: First Night` | [`earlygame__FirstNight.md`](earlygame__FirstNight.md) | `RimconemyNightComponent` + `NightSpawnFormula` + `IncidentWorker_NightInfected` | `UNVERIFIED` |
| 27 | `Early Game: Save/Load` | [`earlygame__SaveLoad.md`](earlygame__SaveLoad.md) | `RimconemyStartState` + `RimconemyStartEnemiesLedger` + `ISchemaMigratable` | `COMPILED` (Pre-LIVE) |

> **Akzeptanz-Reihenfolge:** Erst `Early Game: Survivor`+`SavLoad` grün (echte Phase-1-Belege), dann Campfire → Barrikade → First Night. Save/Load ist die Brücke, die jeden Übergang absichert.

## Beleg-Strategie

Jeder Bericht hat einen `Belegabschnitt A–G`:

| Block | Inhalt |
|---|---|
| A | Def-Liste (Welche XMLs sind geladen?) |
| B | Code-Pfad (Welche `.cs`-Klassen sind im Bootkey enthalten?) |
| C | Selbsttest (welche `RunAll` Tests gibt es?) |
| D | Runtime-Boot (welche `static constructor`-Linien erscheinen in `Player.log`) |
| E | Save/Load (Snapshot-Roundtrip) |
| F | Lateralität (cross-package READ) |
| G | Performance-Kennzahl (Bootspeed, Tick-Last) |

`A–C` sind aus dem Code ableitbar — beim Merge `COMPILED`/`LOADED` sind
sie klickbar. `D–G` verlangen deine Ingame-Beobachtung.

## User-Aktionen (Pflicht)

Du startest mit `./scripts/runtime_test.sh --require-scenario-tests`.
Nach Lauf postest du:
- 1 Log-Auszug pro Bericht (im Berichts-File unter `## Belegblock D`)
- 1 Save-Roundtrip-Test (Belegblock E)
- 1 Cross-Read-Test (Belegblock F)
- 1 Performance-Zahl (Belegblock G)

Sobald alle 4 User-Blöcke im Bericht stehen, gilt der Bericht als `SURVIVED`.
