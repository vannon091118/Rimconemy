# Rimconemy — Technical Debt Catalog

> **Zweck:** Konsolidierte Liste aller visuell aktiven Systeme mit
> "not yet functional"-Disclaimer oder bekannten Live-Bugs.
> Pro Eintrag: Symptom, Root-Code-Spur, Owner-Paket, Status, Abarbeitungs-Ticket.
>
> **Stand:** 2026-08-07 (aktualisiert für StorytellerDef + RimPad-Verkabelung) · **Quelle:** Live-Tests (Player.log) + Code-Audit
> **Roadmap-Referenz:** ROADMAP.md §4-§9
> **Operativer Plan:** → [WORKPLAN.md](WORKPLAN.md) (DELETE → WIRE → NEW)
> **Parser:** `scripts/parse_runtime_log.py` für automatische Debug-Übersicht

---

## §1 Visuelle Layer & UI-Dashboards (TD-01–TD-06)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-01 | RimPad Survival-Tab | `Rimconemy.RimPad.Tab.Todo` — Callback `SurvivalTabDrawer` bereit, noch nicht registriert | `mods/01/Source/UI/RimPadWindow.cs:93` | 01→02 | READY (Callback existiert) |
| TD-02 | RimPad Infrastructure-Tab | `Tab.Todo` — Callback `InfrastructureTabDrawer` bereit, noch nicht registriert | `RimPadWindow.cs:94` | 01→03 | READY (Callback existiert) |
| TD-03 | RimPad Economy-Tab | `Tab.Todo` — Callback `EconomyTabDrawer` bereit, noch nicht registriert | `RimPadWindow.cs:95` | 01→04 | READY (Callback existiert) |
| TD-04 | RimPad Threat-Tab | ✅ Verkabelt → `ThreatDashboard.DrawRimPadContent` | `RimPadWindow.cs:96` + `Bootstrap.cs` (Pkg 05) | 01→05 | **FIXED 2026-08-07** |
| TD-05 | RimPad Diagnostics-Tab | ✅ Verkabelt → `RimconemyUi.DrawDiagnosticsContent` | `RimPadWindow.cs:97` + `RimconemyUi.cs` | 01→01 | **FIXED 2026-08-07** |
| TD-06 | RimPad Guide-Tab | Guide-Tab registriert (Drawer-Callback aktiv), Inhalt noch statisch | `RimPadWindow.cs:83-89` + `RimPadTab.Guide` | 01→05 | IMPROVED (W-05: enum+SelectTab) |

## §2 Spawn- und Map-Lifecycle-Bugs (TD-08–TD-10)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-08 | IntroFlowWindow Horde-Spawn-NRE | 4× NullReferenceException → behoben mit try/catch | `mods/01/Source/UI/IntroFlowWindow.cs` — `5422daf` | 01 | **FIXED 2026-08-06** |
| TD-09 | Infected initial spawn | 0 Infected im Start-Tag nach TD-08-Fix | `mods/05/Source/Scenarios/ScenPart_RimconemyStartEnemies.cs` | 05 | NEEDS RE-EVAL |
| TD-10 | Wildlife-Dichte Endgame | Vanilla-Density, kein Rimconemy-Tuning | kein Hebel im Code | 05 | OPEN |

## §3 Cross-Package-Bridge (TD-07, TD-14)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-07 | ResourceCollector Vanilla-Integration | kein Harmony-Postfix auf HauledToInventory | `mods/02/Source/Survival/ResourceCollector.cs` | 02 | OPEN |
| TD-14 | SurvivalTutorialBridge.Initialize() | Survival-Events erreichten nie TutorialDirector | `mods/02/Source/Bootstrap.cs` — `dcc7fd8` | 02 | **FIXED 2026-08-06** |
| TD-15 | TutorialStepDef Namespace | XML löste Typ nicht auf → `<Rimconemy...Story.TutorialStepDef>` Fix | `TutorialSteps.xml` + `TutorialStepDef.cs` | 05 | **FIXED 2026-08-06** |
| TD-16 | Logging-Konkretheit | Test-Failure-Logs ohne Datei/Erwartung → verbessert | 4 Test-Suiten + `scripts/parse_runtime_log.py` | 05 | IMPROVED 2026-08-06 |

## §4 Architektur-Drift (TD-11–TD-13)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-11 | TutorialState re-entry nach Load | Dialog bleibt offen nach Save/Load | `mods/05/Source/Story/TutorialDirector.ReopenCurrentStepIfAny` | 05 | CODE vorhanden, LIVE unverifiziert |
| TD-13 | Doppeltes CapabilityAudit | gelöscht: Bridge-Dateien + FoundationInitializer auf PackageRegistry umgelenkt | `79407fc` + `e8a02c5` | 01 | **FIXED 2026-08-06** |

## §5 Funktionaler-Drift: Vision vs. Realität (TD-17–TD-19)

| ID | Vision | Realität | Diskrepanz | Owner |
|----|--------|---------|------------|-------|
| TD-17 | "Endzeit: jeder Schritt ist Threat" | nur 60k-Tick-Wave-Trigger | StoryDirector-Hochfrequenz + permanente Bedrohung offen | 05 |
| TD-18 | "Sichtweite begrenzt + Zombies überall" | `DarknessSectionLayer` rendert Layer ohne echte Fog-Modifikation | Harmony-Patch auf FogGrid oder eigener SightRange-Hook nötig | 05 |
| TD-19 | "90% weniger Tiere + 90% mehr Infected" | keine Wildlife-Spawn-Reduktion | Harmony-Patch auf WildAnimalSpawner (Spike-Pflicht) | 05 |

## §6 Bekannte Test-Failures (TD-20–TD-23)

> Entdeckt via `scripts/parse_runtime_log.py` am 2026-08-06.
> Vorbestehende Fehler in Mod 05, nicht durch aktuelle Änderungen verursacht.

| ID | Test-Suite | Fehler | Datei |
|----|-----------|--------|-------|
| TD-20 | TutorialDirector | TD14: unlockDefs nicht aufgelöst (Cross-Ref) | `TutorialDirectorRegressionTests.cs` |
| TD-21 | PopulationLedger | T7+T8: Growth/Revenge-Quote | `PopulationLedgerRegressionTests.cs` |
| TD-22 | RevengeQuotaFlow | T7+T9+T10+T11: Recomputation/Plan | `RevengeQuotaFlowRegressionTests.cs` |
| TD-23 | HordeManifest | T20: AdvanceTileFsmStageDown | `HordeManifestTests.cs` |

## §7 Abarbeitungs-Plan (Roadmap-konform)

| Phase | Tickets | Aufwand | ROADMAP-Bezug |
|-------|---------|---------|---------------|
| **α** (erledigt) | TD-08 ✅, TD-13 ✅, TD-14 ✅, TD-15 ✅ | — | Phase F-V4 |
| **β** (aktuell) | TD-09, TD-10, TD-11, TD-16, TD-20–TD-23 | 3-5 Tage | P6 Gameplay-Schichten |
| **γ** (später) | TD-01–TD-07, TD-17–TD-19 | 5-10 Tage | P6 + Vision-Spec |

## §8 Änderungsregel

Jeder Eintrag muss einen `git log`-baren Commit-Hash oder Phase-N-Verweis haben.
Status-Wechsel (OPEN → FIXED) nur mit Build- + Runtime-Beleg.
Neue Tickets: zuerst hier eintragen, dann in ROADMAP.md spiegeln.
