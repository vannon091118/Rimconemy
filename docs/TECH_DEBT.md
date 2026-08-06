# Rimconemy — Technical Debt Catalog

> **Zweck:** Konsolidierte Liste aller visuell aktiven Systeme mit
> "not yet functional"-Disclaimer oder bekannten Live-Bugs.
> Pro Eintrag: Symptom, Root-Code-Spur, Owner-Paket, Status, Abarbeitungs-Ticket.
>
> **Stand:** 2026-08-06 · **Quelle:** Live-Tests (Player.log) + Code-Audit
> **Roadmap-Referenz:** ROADMAP.md §4-§9
> **Operativer Plan:** → [WORKPLAN.md](WORKPLAN.md) (DELETE → WIRE → NEW)

---

## §1 Visuelle Layer & UI-Dashboards (TD-01–TD-06)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-01 | RimPad Survival-Tab | `Rimconemy.RimPad.Tab.Todo` statt Bedürfnis-Werte | `mods/01/Source/UI/RimPadWindow.cs:93` | 01→02 | OPEN |
| TD-02 | RimPad Infrastructure-Tab | `Tab.Todo` statt Storage/Power | `RimPadWindow.cs:94` | 01→03 | OPEN |
| TD-03 | RimPad Economy-Tab | `Tab.Todo` statt Credits/Market | `RimPadWindow.cs:95` | 01→04 | OPEN |
| TD-04 | RimPad Threat-Tab | `Tab.Todo` statt ThreatAggregator | `RimPadWindow.cs:96` | 01→05 | OPEN |
| TD-05 | RimPad Diagnostics-Tab | `Tab.Todo` statt EventLog | `RimPadWindow.cs:97` | 01→01 | OPEN |
| TD-06 | RimPad Guide-Tab | zeigt "Empty" ohne TutorialDirector-Status | `RimPadWindow.cs:83-89` | 01→05 | OPEN |

## §2 Spawn- und Map-Lifecycle-Bugs (TD-08–TD-10)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-08 | IntroFlowWindow Horde-Spawn-NRE | 4× NullReferenceException in Player.log Z. 3195-3198 | `mods/01/Source/UI/IntroFlowWindow.cs:FlashHorde()` | 01 | **FIXED 2026-08-06** (try/catch um SpawnHordePawn-Callback) |
| TD-09 | Infected initial spawn | 0 Infected im Start-Tag nach TD-08-Fix | `mods/05/Source/Scenarios/ScenPart_RimconemyStartEnemies.cs` | 05 | NEEDS RE-EVAL nach TD-08 |
| TD-10 | Wildlife-Dichte Endgame | aktuell Vanilla-Density, kein Rimconemy-Tuning | kein Hebel im Code | 05 | OPEN |

## §3 Cross-Package-Bridge (TD-07, TD-14)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-07 | ResourceCollector Vanilla-Integration | kein Harmony-Postfix auf HauledToInventory | `mods/02/Source/Survival/ResourceCollector.cs` | 02 | OPEN |
| TD-14 | SurvivalTutorialBridge.Initialize() | Survival-Events erreichten nie TutorialDirector | `mods/02/Source/Bootstrap.cs` | 02 | **FIXED 2026-08-06** (Bridge.SurvivalTutorialBridge.Initialize() hinzugefügt) |

## §4 Architektur-Drift (TD-11–TD-13)

| ID | System | Symptom | Code-Anker | Owner | Status |
|----|--------|---------|------------|-------|--------|
| TD-11 | TutorialState re-entry nach Load | Dialog bleibt offen nach Save/Load | `mods/05/Source/Story/TutorialDirector.ReopenCurrentStepIfAny` | 05 | CODE vorhanden, LIVE unverifiziert |
| TD-13 | Doppeltes CapabilityAudit | `FoundationInitializer` registriert in DUMMY-Bridge.Dict | `mods/01/Source/Bridge/CapabilityAudit.cs` + `FoundationInitializer.cs` | 01 | PENDING (F1a-Patch vorbereitet) |

## §5 Funktionaler-Drift: Vision vs. Realität (TD-15–TD-17)

| ID | Vision | Realität | Diskrepanz | Owner |
|----|--------|---------|------------|-------|
| TD-15 | "Endzeit: jeder Schritt ist Threat" | nur 60k-Tick-Wave-Trigger | StoryDirector-Hochfrequenz + permanente Bedrohung offen | 05 |
| TD-16 | "Sichtweite begrenzt + Zombies überall" | `DarknessSectionLayer` rendert Layer ohne echte Fog-Modifikation | Harmony-Patch auf FogGrid oder eigener SightRange-Hook nötig | 05 |
| TD-17 | "90% weniger Tiere + 90% mehr Infected" | keine Wildlife-Spawn-Reduktion | Harmony-Patch auf WildAnimalSpawner (Spike-Pflicht) | 05 |

## §6 Abarbeitungs-Plan (Roadmap-konform)

| Phase | Tickets | Aufwand | ROADMAP-Bezug |
|-------|---------|---------|---------------|
| **α** (Tag 0+1) | TD-08 ✅, TD-14 ✅, TD-13 | 1-2 Tage | Phase F-V4 |
| **β** (Tag 2-5) | TD-09, TD-10, TD-11 | 3-5 Tage | P6 Gameplay-Schichten |
| **γ** (Tag 6-10) | TD-01–TD-07, TD-15–TD-17 | 5-10 Tage | P6 + Vision-Spec |

## §7 Änderungsregel

Jeder Eintrag muss einen `git log`-baren Commit-Hash oder Phase-N-Verweis haben.
Status-Wechsel (OPEN → FIXED) nur mit Build- + Runtime-Beleg.
Neue Tickets: zuerst hier eintragen, dann in ROADMAP.md spiegeln.
