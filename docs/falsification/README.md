# Falsifizierungsberichte — Rimconemy 20-Berichte-Index

> **Owner:** Buffy (Agent) + User
> **Stand:** 2026-08-04
> **Lifecycle:** `UNVERIFIED` → `COMPILED` → `LOADED` → `OBSERVED` → `SURVIVED`
> **Quelle:** `ROADMAP.md §8.2`

Dieses Dokument verlinkt die 20 Falsifizierungsberichte. Jeder Bericht
folgt einem einheitlichen A–G-Beleg-Schema (siehe Vorlage). Status wird
mit jeder Patch-Stage zusammen aktualisiert.

## Legende

- `UNVERIFIED`  — Bericht angelegt, kein Beleg
- `COMPILED`    — Berichts-Code kompiliert
- `LOADED`      — Bericht lädt im Spiel (Def+Class sichtbar)
- `OBSERVED`    — User hat den Bericht in einem echten Lauf gesehen
- `SURVIVED`    — `A–G` vollständig belegt, merge-fähig

## Berichts-Liste

### Foundation (1)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 1 | `Servicebus` | [`foundation__Servicebus.md`](foundation__Servicebus.md) | `mods/01-Rimconemy-Foundation/Source/Catalog/` | `COMPILED` |

### Survival &amp; Progression (4)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 2 | `Needs` | [`survival__Needs.md`](survival__Needs.md) | `NeedMappingService` | `COMPILED` |
| 3 | `WorkXp` | [`survival__WorkXp.md`](survival__WorkXp.md) | `BuildingProgressionAdapter` | `COMPILED` |
| 4 | `Research` | [`survival__Research.md`](survival__Research.md) | `ProgressionGameComponent.ResearchCapabilities` | `COMPILED` |
| 5 | `GameOver` | [`survival__GameOver.md`](survival__GameOver.md) | `GameOverDetector` | `COMPILED` |

### Scavenger Infrastructure (5)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 6 | `ConstructionDebris` | [`scavenger__ConstructionDebris.md`](scavenger__ConstructionDebris.md) | `BauschuttRemapService` | `LOADED` |
| 7 | `FoodAndHemp` | [`scavenger__FoodAndHemp.md`](scavenger__FoodAndHemp.md) | `FoodHarvestCycleService` | `LOADED` |
| 8 | `WaterPowerArrowTurret` | [`scavenger__WaterPowerArrowTurret.md`](scavenger__WaterPowerArrowTurret.md) | `FueledGeneratorService` + `ArrowTurretPowerGate` | `LOADED` |
| 9 | `ExecutePhysicalTransfer` | [`scavenger__ExecutePhysicalTransfer.md`](scavenger__ExecutePhysicalTransfer.md) | `StorageQuery` + `CaravanStorageEnumerator` | `COMPILED` |
| 10 | `ReservePhysicalTransfer` | [`scavenger__ReservePhysicalTransfer.md`](scavenger__ReservePhysicalTransfer.md) | `StorageScope.SpecificMap` | `COMPILED` |

### Economy (5)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 11 | `WalletCredits` | [`economy__WalletCredits.md`](economy__WalletCredits.md) | `CreditsLedger` | `COMPILED` |
| 12 | `Market` | [`economy__Market.md`](economy__Market.md) | `MarketService` | `COMPILED` |
| 13 | `ReservePhysicalTransfer` | [`economy__ReservePhysicalTransfer.md`](economy__ReservePhysicalTransfer.md) | `OutpostProxyGraph` | `COMPILED` |
| 14 | `OutpostProduction` | [`economy__OutpostProduction.md`](economy__OutpostProduction.md) | `OutpostProxyGraph` | `COMPILED` |
| 15 | `TerritoryCountdown` | [`economy__TerritoryCountdown.md`](economy__TerritoryCountdown.md) | `OutpostProxyGraph.MaxReportIntervalTicks` | `COMPILED` |

### Infected &amp; Automation (5)

| # | Bericht | Datei | Code-Anker | Status |
|---|---|---|---|---|
| 16 | `ThreatPressure` | [`infected__ThreatPressure.md`](infected__ThreatPressure.md) | `ThreatAggregator` + `StoryDirector` | `COMPILED` |
| 17 | `InfectedRaid` | [`infected__InfectedRaid.md`](infected__InfectedRaid.md) | `InfectedRaidSpawnService` + `InfectedRaidWorker` | `LOADED` |
| 18 | `MechadroidJob` | [`infected__MechadroidJob.md`](infected__MechadroidJob.md) | `MechadroidJobRegistry` | `COMPILED` |
| 19 | `ManualRaid` | [`infected__ManualRaid.md`](infected__ManualRaid.md) | `IncidentClassifier` | `COMPILED` |
| 20 | `AutoResolve` | [`infected__AutoResolve.md`](infected__AutoResolve.md) | `WorldRaidCoordinator` + `ThreatAggregator` | `COMPILED` |

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
