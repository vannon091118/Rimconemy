# Blueprint 02 – Rimconemy Survival & Progression

## API-Hinweis

Die genannten Needs-, Job-, Skill- und Research-Typen sind Planungsanker. Exakte 1.6-Signaturen werden über `API-NEED-01`, `API-JOB-01` und die Research-Spikes bestätigt (Spike-/Baseline-Dokumente archiviert in `docs/archive-md-2026-08-04.tar.gz`).

## Ziel

Das Paket definiert den Character-/Progression-Rahmen für den einzelnen Überlebenden. Im aktuellen Code sind Need-Mapping, Progression-Read-Model, Character-Setup-Logik, Sandbox-/Game-Over-Anker und Regression-Gates belegt; vollständige Need-, Job-/Output-XP-, Research- und Save/Load-Live-Schichten bleiben offen.

## Standalone-Spielwert

```text
Bedarf erkennen → Arbeit wählen → XP/Spezialisierung → Forschung → bessere Überlebenschance
```

Vanilla-Ressourcen, Gebäude und Storyteller bleiben im Standalone erhalten.

## Vanilla-/DLC-Anker

| Bereich | Anker | Entscheidung | Spike |
|---|---|---|---|
| Pawn-Start | Szenario-/Pawn-Generator | ein Pawn, sichtbare Individualisierung | genaue 1.6-Startsignaturen |
| Needs | `Pawn_NeedsTracker`-/Need-/Mood-Pfade | adaptieren, nicht unklar doppeln | eigene Need-Def vs. Adapter gegen lokale Assembly |
| Arbeit | WorkGiver/JobDriver/Reservation/Jobabschluss | Vanilla-Jobs behalten, XP bei validiertem Output | Hookpunkt für genau einmaligen XP-Commit |
| Skills | `Pawn_SkillTracker`/SkillRecord | Vanilla-Skill als Effizienzanker oder klarer Adapter | keine zweite konkurrierende Skillstufe |
| Research | `ResearchProjectDef`/ResearchManager | Defs + Capability-Ausgabe | Zyklus-/DLC-Graph lokal prüfen |
| Mood/Mental Break | Vanilla-Mood-/Break-Pfade | sichtbare Einflussmatrix | keine pauschale globale Unterdrückung |
| DLC | Genes, Ideology, Psycasts, Anomaly, Odyssey | koexistieren mit Adapterregeln | jede geheime Need-/Jobänderung messen |

## Artefaktziele und Q13-Scaffoldstatus

Q13 legte die Paket-02-Blueprint-/Build-/Falsifizierungsbasis an. Im aktuellen Code sind Need-Mapping, Progression-Read-Model, Character-Setup-Logik sowie Sandbox-/Game-Over-Anker und Regressionstests vorhanden. Vollständige Need-/Job-/Output-XP-/Research-Gameplay-Schichten, ein eigener Character-Setup-Save-State und Live-Belege bleiben bis Q14 beziehungsweise den Runtime-Gates offen; die vier Berichte bleiben `UNVERIFIED`.

| Scaffold-Artefakt | Status |
|---|---|
| `Rimconemy.SurvivalProgression.csproj` | vorhanden; isoliert, nur RimWorld-Managed-Assemblies und Harmony |
| `Source/Bootstrap.cs` | vorhanden; Standalone-Startupmarker ohne Foundation-Compile-Referenz |
| `Patches/` | vorhanden; Patch-Anker dokumentiert (Scaffold-README archiviert) |
| `FALSIFICATION_REPORTS/rimconemy.survivalprogression__Needs.md` | vorhanden; `UNVERIFIED` |
| `FALSIFICATION_REPORTS/rimconemy.survivalprogression__WorkXp.md` | vorhanden; `UNVERIFIED` |
| `FALSIFICATION_REPORTS/rimconemy.survivalprogression__Research.md` | vorhanden; `UNVERIFIED` |
| `FALSIFICATION_REPORTS/rimconemy.survivalprogression__GameOver.md` | vorhanden; `UNVERIFIED` |

## Artefaktziele (geplante Zielpfade)

| Task | Dateien/Artefakte | Test-IDs |
|---|---|---|
| P1 | `Defs/Scenarios/`, `Source/Start/`, `Tests/SingleSurvivorGameOver.md` | `NEW_GAME`, `UI_REASON` |
| P2 | `Source/Needs/`, `Source/MoodAdapter/`, `Tests/NeedInfluenceMatrix.md` | `NEW_GAME`, `UI_REASON`, `DLC_SCOPE` |
| P3 | `Source/Progression/`, `Source/Jobs/`, `Tests/JobXpIdempotency.md` | `JOB_RESERVATION`, `MAP_CHANGE`, `DETERMINISM` |
| P4 | `Defs/Research/`, `Source/Research/`, `Tests/CapabilityUnlocks.md` | `NEW_GAME`, `SAVE_LOAD`, `DLC_SCOPE` |
| P5 | `Source/Save/`, `Source/UI/`, `Tests/DlcProgressionMatrix.md`, vier Falsifizierungsberichte | `SAVE_LOAD`, `MAP_CHANGE`, `UI_REASON`, `DLC_SCOPE` |

## Fünf Build-Tasks

### P1 – Einzelstarter und Game-Over-Anker

- Szenario mit einem individualisierbaren Start-Pawn definieren.
- kontrollierbare Spielerbewohner über stabile Identität zählen.
- Outpost-Population/Mechadroids explizit ausschließen.

**Gate:** neue Kampagne startet mit genau einem gültigen Spieler-Pawn und dokumentiertem Besitz.

### P2 – Kernbedürfnisse und Einflussmatrix

- Nahrung, Sicherheit und Soziales als sichtbare Zustände modellieren.
- Gesundheit, Verletzung, Temperatur, Erschöpfung und Krankheiten getrennt behalten.
- Vanilla Traits, Genes, Ideology, Psycasts und Hediffs in der Einflussmatrix klassifizieren.

**Gate:** UI zeigt Wert, Trend, Ursache und Konsequenz; `Unavailable` ist kein Nullwert.

### P3 – Arbeit, XP und Effizienz

- Arbeitstypen `Building`, `Farming`, `Scavenging`, `Power`, `Engineering`, `Combat`, `Social/Trade`, `Expedition`, `Automation` registrieren.
- XP erst nach validiertem Job-/Outputereignis vergeben.
- Diminishing/Abklingregel und Effizienzformel dokumentieren.
- eine Idempotency-ID pro Joboutput verwenden.

**Gate:** 50-Pawn-Stresstest liefert keine Idle-/Doppel-XP und keine Reservation-Loops.

### P4 – Forschung als Capability-Graph

- Tier 0 Grundversorgung, Tier 1 Stabilisierung, Tier 2 Spezialisierung, Tier 3 Automation.
- jedes Projekt hat Zweck, Kosten/Zeit, Capability und sichtbaren Output.
- optionale Paket-Capabilities bleiben gesperrt statt Phantomdefs zu erzeugen.

**Gate:** jede getestete Forschung verändert eine zugängliche Aktion, ein Gebäude, eine Arbeit oder die UI.

### P5 – UI, Save, DLC und Falsifizierung

- ProgressionSnapshot, Game-Over-Event und Migration implementieren.
- Full-DLC-Testmatrix aus Baseline ausführen.
- Standalone ohne Foundation und Teilprofil mit Foundation prüfen.

**Exit:** `Needs`, `WorkXp`, `Research`, `GameOver` bleiben bis zu realen A–G-Belegen `UNVERIFIED`.

## Schnittstellen

- liest Scavenger-Arbeit/Outputs, schreibt keine Ressourcen.
- veröffentlicht `ProgressionSnapshot` und Capability-Freischaltungen.
- liefert Sicherheit/Game-Over-Anker für Paket 5.
- Foundation liest nur Snapshots/Events.

## UI-Minimum

Kernbedürfnisse, Arbeitsstatus, XP je Bereich, Effizienzursache, Forschungspfad, Startprofil und Game-Over-Status.

## Save-/Performance-Gates

Pawn-ID, Need-/XP-/Research-Schema versionieren. Kartenwechsel darf keine Veränderung der Progression erzeugen. Updates an echte Zustandsänderungen oder definierte Intervalle binden; keine Tick-XP.

## Offene Spikes und Q14-Grenze

- Q14/T1: exakte 1.6-Need-Aktivierung und Custom-Need-Anker lokal prüfen (`API-NEED-01`).
- Q14/T1: `causesNeed`-Aussagen nicht übernehmen, ohne lokale Def-/Assembly-Bestätigung.
- Q14/T1–T2: direkte Vanilla-Skillmodifikation gegen additive Effizienzkomponente testen (`API-JOB-01` für den Job-/Outputpfad).
- Q14/T1–T4: Quest-/DLC-Belohnungen auf Forschung/Needs prüfen.
- Q14/T1–T4: Research-Manager-/Forschungsgraph-Lifecycle und Game-Over-Lifecycle gegen die vorhandene Research-Baseline prüfen; hierfür werden vor Implementierung nur bestätigte Spike-IDs aus `docs/H1-api-def-gate.md` verwendet.
- Diese Research-/Game-Over-Lifecycle-Prüfungen sind keine Q13-Scaffold-Belege und bleiben bis Q14 ausdrücklich offen.

## Decision-Status (Track 2-C, 2026-08-04)

- **F-T1 ColonialReader**: DONE (Phase-B-Sprint, Migration in `ProgressionGameComponent.UpdateRuntimeState`).
- **F-T2 GameOverMode Enum + SaveData.IsSandboxMode**: DONE (`Source/GameOver/GameOverMode.cs`, `FoundationSaveData`).
- **S-T1 NeedDefs**: DECIDED — Hybrid-Ansatz: eigene `NeedDef`-/`Need_SettingIdentity`-Marker beschreiben die Setting-Kategorien; sie werden absichtlich nicht an Pawns angehängt. Vanilla-Needs liefern die Runtime-Daten.
- **S-T2 GameOverDetector Sandbox-Awareness**: DONE (`Source/GameOver/GameOverDetector.cs` + Branch in `ProgressionGameComponent`).
- **S-T3 Sandbox-Modus per ScenPart**: DONE (`Source/Scenarios/ScenPart_StartInSandbox.cs` + XML `Defs/Scenarios/Rimconemy_SandboxScenario.xml`).
- **S-T4 ThoughtWorker → Mod 05**: DECIDED, **noch nicht migriert**. Heute capability-gated in Mod 02. Move ausstehend (Track 2-C weiterer Sprint).
- **S-T5 ClassifyJob FIXME**: DONE (WorkTypeDef + WorkTags via `pawn.workSettings.GetPriority`).
- **X-T3 Blueprint-Status**: DONE (hier).
- F-V2 Sole-Owner GameOver + Reflection-Bridge (Phase B): DONE.
- F-V4 Capability-Gate (Phase B): DONE — `CapabilityAudit.HasCapabilityOrWarn` in 4× aktiv genutzt.
