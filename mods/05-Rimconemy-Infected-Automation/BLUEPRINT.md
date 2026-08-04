# Blueprint 05 – Rimconemy Infected & Automation

## API-Hinweis

Die genannten Incident-, Storyteller-, WorldMap-, Pawn- und Biotech-Mech-Anker sind Planungsanker. Exakte 1.6-Semantik wird über `API-INCIDENT-01`, `API-MECH-01`, `API-WORLD-01` und die lokale Baseline bestätigt (Spike-/Baseline-Dokumente archiviert in `docs/archive-md-2026-08-04.tar.gz`).

## Ziel

Das Paket macht die zentrale Fantasie sichtbar: Wachstum erzeugt Aufmerksamkeit. Ein einziger Bedrohungsbesitzer liest bestehende Snapshots, plant erklärbare Infizierten-Raids und bietet später begrenzte Automation statt kostenloser Arbeitskraft.

## Standalone-Spielwert (Planungsziel; Live-Beleg offen)

Deterministischer Story-Layer, StoryState, Storage-Bridge und letter-basierter Incident-Pfad sind im Code vorhanden. Vollständiger Infizierten-Spawn, World-Map-Raids und Mechadroid-Gameplay bleiben offen.

```text
Druck verstehen → Verteidigung priorisieren → lokale Automation → Raidrisiko entscheiden
```

Ohne Economy/Territory bleiben lokale Ziele und generische Weltkartenpfade aktiv; ohne Scavenger werden Vanilla-Inputs verwendet.

## Vanilla-/DLC-Anker

| Bereich | Anker | Entscheidung | Spike |
|---|---|---|---|
| Gegner | Faction/PawnKind/Hediff/Incident | eigene Domäne, Vanilla-Assets vorläufig möglich | Shambler/Human/Scaria-Semantik lokal prüfen |
| Storyteller | Storyteller/StorytellerComp/IncidentWorker | ein Infizierten-Provider im Full Profile | keine globale Raiddeaktivierung ohne Questmatrix |
| Wealth-Raids | WealthWatcher/Threat-Punkte | getrennte Policy; nicht nur MarketValue nullen | Vanilla-Wealth-Doppelbetrieb messen |
| Raids | Incident-/Raidpfade/WorldObject | Druck → Incident → Stärke → Auflösung | idempotente Save-/Mapwechsel |
| Mechadroids | Pawn-/Job-/Comp-Anker | eigene Einheit-/Auftragsdomäne | `IsMechanoid`/Mechanitor-Semantik lokal beweisen |
| WorldMap | WorldObjects/Caravans/temporary maps | sichtbare Raidobjekte | Odyssey-/Anomaly-Lifecycle |
| DLC | Anomaly Entities/Hediffs, Biotech Mechs/Genes, Odyssey travel | Adapter/Koexistenz nach Test | parallele Bedrohungen und Ownership |

## Artefaktziele (geplante Zielpfade, noch keine vorhandenen Belege)

| Task | Dateien/Artefakte | Test-IDs |
|---|---|---|
| A1 | `Defs/Factions/`, `Defs/PawnKinds/`, `Defs/Incidents/`, `Tests/MinimalInfected.md` | `NEW_GAME`, `DLC_SCOPE`, `UI_REASON` |
| A2 | `Source/Threat/`, `Source/Snapshots/`, `Tests/ThreatDeterminism.md` | `DETERMINISM`, `WORLD_STEP`, `UI_REASON` |
| A3 | `Source/Storyteller/`, `Source/Incidents/`, `Tests/IncidentOncePolicy.md` | `INCIDENT_ONCE`, `DLC_SCOPE`, `DETERMINISM` |
| A4 | `Source/WorldRaids/`, `Source/Mechadroids/`, `Tests/MechadroidJob.md` | `SAVE_LOAD`, `MAP_CHANGE`, `JOB_RESERVATION`, `UI_REASON` |
| A5 | `Source/AutoResolve/`, `Source/ManualRaid/`, `Tests/RaidRecovery.md`, fünf Infected-Berichte | `SAVE_LOAD`, `TEMP_MAP`, `INCIDENT_ONCE`, `DETERMINISM` |

## Fünf Build-Tasks

### A1 – Minimaler Infizierten-Gegner

- eigene Faction-/PawnKind-/Incident-Identität definieren.
- Vanilla-Texturen/Archetypen nur als markierten Prototyp verwenden.
- Infizierte von Survivor, Tieren und Vanilla-Mechanoids getrennt klassifizieren.

**Gate:** sichtbarer, eigener Spawn-/Raidpfad; keine reine Label-Umbenennung.

### A2 – Ein Bedrohungsaggregator

- Farm-, Population-, Production-, Power-, Defense-, Combat- und Regionalinputs aus vorhandenen Snapshots lesen.
- Druck und Trend deterministisch berechnen.
- Eskalationsstufen, Ruhephasen und Obergrenzen definieren.
- ThreatSnapshot mit Faktoren und Prognose veröffentlichen.

**Gate:** gleiche Inputs liefern denselben Druck; keine zweite Farm-/Produktionszählung.

### A3 – Storyteller-/Incident-Policy

- Druckanstieg, Incidentwahl, Raidstärke und Auflösung trennen.
- Vanilla-Wealth-Raids und DLC-/Quest-Incidents in einer Policy klassifizieren.
- quest-erzwungene Ereignisse nicht pauschal unterdrücken.
- genau einen Infizierten-Raidprovider im Full Profile aktivieren.

**Gate:** ein Druckereignis erzeugt höchstens einen vorgesehenen, geloggten Raid.

### A4 – World-Raids und Mechadroid-MVP

- Raidobjekt mit Symbol, Nummer, Anzahl, Ziel, Pfad, Richtung, ETA, Seed und Prognose.
- Mechadroid mit ID, Besitzer, Energie, Wartung, Auftrag, Status und Upgradehistorie.
- Silber als physisches Upgrade-Material; Credits bleiben Wallet.
- Aufträge: Farm, Ressourcen, Generatorwartung, Reparatur, Verteidigung.

**Gate:** Mechadroid benötigt Input/Energie/Wartung, fällt sichtbar aus und verhindert kein Game Over.

### A5 – Auto-Resolve, manuelle Raids und Endgame-Spike

- manuelle Expedition und Auto-Resolve getrennt darstellen.
- Kosten, Risiko, Verluste, Beute, Seed und Ergebnis vorab anzeigen.
- Save-/Load-/Crash-Recovery idempotent prüfen.
- Hauptstädte erst nach allen vorherigen Gates definieren.

**Exit:** Threat, InfectedRaid, MechadroidJob, ManualRaid und AutoResolve bleiben bis zu A–G-Belegen `UNVERIFIED`.

## Schnittstellen

- besitzt ThreatSnapshot, Infizierten-Raidstatus, Mechadroid- und Automationstatus.
- liest Progression-, Resource-, Power- und Territory-Snapshots.
- fordert Territory-Änderungen/Walletkosten über Commands an, schreibt sie nicht direkt.
- veröffentlicht Raid-/Automation-Events und Game-Over-relevante Bedrohungsdaten.

## UI-Minimum

Druckfaktoren, Trend, Prognose, Raidursache/-stärke/-ziel, WorldMap-Symbol/Pfad/ETA, Auto-Resolve-Kosten/Risiko, Mechadroid-Auftrag/Status/Blockade/Wartung.

## Save-/Performance-Gates

Threat-/Raid-/Unit-/Job-IDs und Seeds versionieren. Raid-Auflösung genau einmal. Aggregierte World-Step-Updates statt Pawn-Tick-Simulation für Outposts. Unterbrochene/unloaded Mapzustände als `InactiveMap`, `Frozen` oder `CatchUpPending` anzeigen.

## Offene Spikes

- Mechadroid `IsMechanoid`/Mechanitor-Integration nicht aus Annahmen entscheiden; lokal testen.
- Shambler-/Scaria-/Anomaly-Hediff nicht als fertige Infektionssemantik übernehmen.
- StorytellerComps nicht pauschal aus Vanilla-Defs entfernen; Quest-/DLC-Matrix zuerst.
- Gravship und mobile MainBase nicht vor statischem Threat-/Territory-Loop.
- Hauptstädte erst nach stabilem Auto-Resolve und manueller Raidkampagne.

## Decision-Status (Track 2-C, 2026-08-04)

- **F-V3 Storage-Bridge (Phase B)**: DONE — `StoryDirector.AssignStorageHashFromCapability` liest echten `StorageQuery.ContentHash` wenn Mod 03 aktiv.
- **F-V2 Sole-Owner GameOver (Phase B)**: DONE — `StoryState.MarkGameOverPending` + Reflection-Bridge, Mod 02 triggert `CheckOrUpdateGameOver()` alleinig.
- **I-T1 StoryDirector echter StorageHash**: DONE (siehe F-V3 oben).
- **I-T3 IdeologyAssigner Runtime-Auto-Assignment**: PARTIAL — `AssignForProfile` existiert in `Source/Ideology/IdeologyAssigner.cs`. Runtime-Confirm-API `ModsConfig.IdeologyActive` ausstehend für Final-Triggger.
- **I-T4 ThoughtWorker von Mod 02 übernehmen**: DECIDED, noch nicht migriert. Heute capability-gated in Mod 02 (`Source/Ideology/ThoughtWorker_ResourceFairness.cs`).
- **I-T2 StoryEventCatalog XML-erweiterbar**: DECIDED — XML-Defs als Quelle statt hardcoded; Loader-Implementation ausstehend.
- Story Director tick-Frequenz (60.000 = 1 Tag) bleibt; Game-Trigger über `EmptyColonistGraceIntervals` (Mod 02, 12×250 = 50s).
