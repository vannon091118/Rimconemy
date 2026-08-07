# Rimconemy — Crystal Workplan (Master-Arbeitsplan)

> **Zweck:** Operationalisierter, abhängigkeitsfreier Master-Plan.
> Jeder Task hat **maximal 1 Vorgänger**. Keine 4-stufigen Abhängigkeitsketten.
> Priorität: **DELETE → WIRE → NEW**. Erst aufräumen, dann verdrahten, dann Neues bauen.
>
> **Prinzip:** Alle Funktionen die `CODE`/`DEF`/`COMPILES` sind, müssen `LIVE` werden.
> Keine neuen Systeme, solange existierende nicht laufen.
>
> **Stand:** 2026-08-07 (aktualisiert: StorytellerDef + RimPad + Tutorial) · **Basis:** Live-Test (Player.log) + Dead-Code-Audit + CODE_STATUS.md
> **Letzter Runtime-Test:** PASS (failures=0, warnings=1) · 44/44 Test-Suiten 0/0
> **Session-Doku:** → [SESSION_2026-08-07.md](SESSION_2026-08-07.md) (14 Commits: StorytellerDef, Migration, Decompile, RimPad, Tutorial)
> **Parser-Preflight:** `scripts/test_parser_preflight.py` + `scripts/parser_config.json`

---

## §1 Datenstrom (Cross-Package-Flows)

```
                    ┌──────────────────────────────────────────┐
                    │           01 Foundation                   │
                    │  PackageRegistry · CapabilityAudit        │
                    │  ProfileDetector · ColonialReader         │
                    │  RimPadWindow · IntroFlowWindow           │
                    └────┬──────┬──────┬──────┬────────────────┘
                         │      │      │      │
              DLL-Ref ───┤      │      │      ├── DLL-Ref
                         │      │      │      │
      ┌──────────────────┘      │      │      └──────────────────┐
      ▼                         ▼      ▼                         ▼
┌───────────┐          ┌───────────┐  ┌───────────┐    ┌───────────┐
│ 02 Survival│          │03 Scavenger│  │04 Economy │    │05 Infected │
│           │          │           │  │           │    │           │
│ Needs     │          │ StorageQ. │  │ CreditsL. │    │ StoryDir. │
│ Progression│         │ PowerChain│  │ Market    │    │ ThreatAggr│
│ GameOver  │          │ CoalChain │  │ Outpost   │    │ TutorialD.│
│ CampfireM.│          │           │  │           │    │ Horde     │
│ WallBuild.│          │           │  │           │    │ AnimalInf.│
│ TutorialB.│          │           │  │           │    │ DarknessL.│
└───────────┘          └───────────┘  └───────────┘    └───────────┘

Reflection-Bridges (kein Compile-Ref):
  02→05: SurvivalTutorialBridge → TutorialTriggerBridge (via Type.GetType)
  03→05: StorageQuery → StoryDirector.AssignStorageHash (via CapabilityAudit)
  05→02: StoryState.MarkGameOverPending → CrossPackageState (Foundation Servicebus)
```

---

## §2 Priorisierte Task-Liste (DELETE → WIRE → NEW)

### 🗑️ DELETE: Dead Code + Altlasten (10 Tasks, 0 Abhängigkeiten)

| ID | Was | Wo | Größe | Status |
|----|-----|----|-------|--------|
| ~~D-01~~ | `Bridge/CapabilityAudit.cs` löschen | `mods/01/Source/Bridge/` | 50 LOC | ✅ `79407fc` |
| ~~D-02~~ | `Bridge/EventBridge.cs` löschen | `mods/01/Source/Bridge/` | 62 LOC | ✅ `79407fc` |
| ~~D-03~~ | `Source/Tutorial/*.cs` (5 Files) löschen | `mods/05/Source/Tutorial/` | 319 LOC | ✅ `e8a02c5` |
| ~~D-04~~ | `TutorialStepDefs.xml` (alt) löschen | `mods/05/Defs/TutorialSteps/` | 52 LOC | ✅ `e8a02c5` |
| ~~D-05~~ | `qa_findings.md` löschen | Repo-Root | ~4 LOC | ✅ `e8a02c5` |
| ~~D-06~~ | `log_watcher.py` löschen | Repo-Root | ~16 LOC | ✅ `e8a02c5` |
| ~~D-07~~ | `Ship-Learn-Next Plan…` löschen | Repo-Root | ~200 LOC | ✅ lokal gelöscht (nicht committed) |
| ~~D-08~~ | `docs/CHAT_PROTOCOL_2026-08-05.md` löschen | `docs/` | 86 LOC | ✅ `e8a02c5` |
| ~~D-09~~ | `docs/P6-PROGRESS.md` löschen | `docs/` | 55 LOC | ✅ `e8a02c5` |
| ~~D-10~~ | `RimPadToggle.xml` (KeyBinding) löschen | `mods/01/Defs/KeyBindingDefs/` | ~17 LOC | ✅ `e8a02c5` |

### 🔌 WIRE: Existierende Funktionen verdrahten (11 Tasks, max. 1 Vorgänger)

| ID | Was | Code schon da? | Was fehlt? | Vorgänger |
|----|-----|----------------|------------|-----------|
| ~~W-01~~ | CampfireManager → TutorialDirector | ✅ `FrameCompletionPatch` → `CampfireManager.TryBuildCampfire` | ✅ `SurvivalTutorialBridge.Initialize()` in Bootstrap 02 — `dcc7fd8` | — |
| ~~W-02~~ | WallBuilder → TutorialDirector | ✅ Gleicher Pfad wie W-01 | selbe Initialisierung (via W-01) | W-01 |
| ~~W-03~~ | IntroFlowWindow → Horde-Spawn | ✅ `ScenPart_IntroSequence` + `FlashHorde`-Callback | ✅ try/catch — `5422daf` | — |
| W-04 | ScenPart_RimconemyStartEnemies | ✅ Difficulty×MapSize-Multiplier + `GenSpawn.Spawn` | Live-Test | W-03 |
| ~~W-05~~ | RimPad Guide-Tab | ✅ `GuideTabDrawer`-Callback von 05 registriert | ✅ `RimPadTab.Guide` enum + `SelectTab` — `dcc87cd` | — |
| W-06 | Tutorial-Schritte erscheinen | ✅ `TutorialDirector` + `Dialog_TutorialStep` | W-01 + W-02 + W-05 kombinieren | W-01, W-02, W-05 |
| W-07 | StoryDirector feuert Event | ✅ `StoryDirector.GameComponentTick` (60k-Tick) + `InfectedRaidWorker` | Live-Test: nach 1 Tag sichtbar? | — |
| W-08 | AnimalInfection Auto-Conversion | ✅ `AnimalInfectionDriver.TryFireOnce` + `PopulationProfileMultipliers` | Live-Test: Tier wird infiziert? | — |
| W-09 | DarknessSectionLayer (Sicht-Effekt) | ✅ `ComputeOverlayAlpha` + 14 Regression-Tests | Live-Test: visuell sichtbar? | — |
| W-10 | Horde-Overlay (Weltkarte) | ✅ `HordeSpawner` + `HordeSectionLayer` + `HordeCameraOverlay` | Live-Test: sichtbar? | — |
| W-11 | Coal-Kette (MakeCoal → Generator) | ✅ `Rimconemy_MakeCoal` Recipe + `WoodCoalGenerator` | Live-Test: Spieler kann Coal herstellen + verbrennen? | — |

### 🆕 NEW: Echt neue Features (5 Tasks, nachdem WIRE fertig ist)

| ID | Was | Abhängigkeit |
|----|-----|-------------|
| N-01 | ResourceCollector Vanilla-Hook (HauledToInventory) | W-06 (Tutorial-Pipeline muss laufen) |
| N-02 | Wildlife-Dichte Tuning (90% weniger Tiere, Harmony-Patch auf WildAnimalSpawner) | W-07 (StoryDirector muss feuern) |
| N-03 | Fog-of-War Verschärfung (DarknessSectionLayer → echte Fog-Mod) | W-09 |
| N-04 | Endzeit-Hochfrequenz-StoryDirector | W-07, W-08 |
| N-05 | RimPad Tab-Daten (NeedMapping, Storage, Credits) | W-05 — Threat+Diagnostics ✅, Survival/Infrastructure/Economy: Callbacks bereit |

---

## §3 Doku-Widersprüche (zu korrigieren)

| # | Widerspruch | Quelle A | Quelle B | Korrektur |
|---|-------------|----------|----------|-----------|
| C-01 | ✅ Behoben (2026-08-06) | CODE_STATUS §2 aktualisiert |
| C-02 | ✅ Behoben — D-03+D-04 + Working-Tree-Commit `804f2e5` |
| C-03 | ✅ Behoben — Working-Tree-Commit `804f2e5` |
| C-04 | ✅ Behoben — D-01+D-02 + FoundationInitializer `79407fc` |
| C-05 | ✅ Behoben (2026-08-06) — TECH_DEBT.md + CODE_STATUS.md synchronisiert, TD-14/TD-13/TD-08 alle FIXED |
| C-06 | ✅ Behoben (2026-08-06) — WORKPLAN.md ist SSOT; ROADMAP.md + TECH_DEBT.md referenzieren hierher |

---

## §4 Ausführungs-Reihenfolge (einmal durch, keine Schleifen)

```
SCHRITT 0 — Hygiene (30 min)
  ├─ Alle D-01 bis D-10 ausführen (git rm / Datei löschen)
  ├─ Build verifizieren (alle 5 Module)
  └─ Commit: chore(cleanup): dead code removal sprint

SCHRITT 1 — Build-Fixes (15 min)
  ├─ W-05: RimPadTab.Guide enum + SelectTab Methode
  ├─ W-03: IntroFlowWindow try/catch
  ├─ Build verifizieren
  └─ Commit: fix(build): resolve compile errors

SCHRITT 2 — Bootstrap-Wiring (15 min)
  ├─ W-01: SurvivalTutorialBridge.Initialize() in mods/02/Bootstrap.cs
  ├─ Build + Runtime-Test
  └─ Commit: fix(bridge): wire TutorialBridge

SCHRITT 3 — Working-Tree-Files committen (20 min)
  ├─ Neue Tutorial-Files in 05/Source/Story/
  ├─ IntroFlowWindow nach 01/Source/UI/
  ├─ Survival-Stubs + Bridge
  ├─ Neue Defs (GameConditionDefs, WeatherDefs, TutorialSteps)
  └─ Commit: feat(tutorial): complete tutorial pipeline + survival stubs

SCHRITT 4 — Doku synchronisieren (20 min)
  ├─ CODE_STATUS.md: Widersprüche C-01 bis C-06 korrigieren
  ├─ TECH_DEBT.md: Status-Spalte aktualisieren
  ├─ ROADMAP.md: auf WORKPLAN.md verweisen
  └─ Commit: docs(sync): resolve documentation conflicts

SCHRITT 5 — Live-Tests (2-4 h)
  ├─ W-07: StoryDirector feuert? (1 Tag warten)
  ├─ W-08: AnimalInfection sichtbar?
  ├─ W-09: Darkness-Layer sichtbar?
  ├─ W-10: Horde-Overlay sichtbar?
  ├─ W-11: Coal-Kette funktioniert?
  └─ W-04: Infected-Start-Spawn nach W-03-Fix?

SCHRITT 6 — Neue Features (nach Schritt 5)
  └─ N-01 bis N-05 in beliebiger Reihenfolge (keine Abhängigkeiten untereinander)
```

---

## §5 Erfolgskriterien pro Schritt

| Schritt | Gate |
|---------|------|
| 0 | `find mods/ -name "*.cs" | wc -l` < vorher (Dead Code entfernt). Build 0/0. |
| 1 | `dotnet build` Mod 01+02+05 = 0/0. RimPad öffnet ohne Crash. |
| 2 | `grep "SurvivalTutorialBridge" Player.log` zeigt "Tutorial trigger"-Lines nach Campfire-Bau. |
| 3 | `git status` zeigt 0 untracked wichtige Source-Files. |
| 4 | `grep "C-01\|C-02\|C-03\|C-04\|C-05\|C-06" docs/` zeigt 0 Treffer (alle Widersprüche behoben). |
| 5 | Jeder W-Task: 1 Live-Beleg-Screenshot oder Player.log-Zeile. |
| 6 | Neue Features: eigene Spec + Test + Falsifizierungsbericht. |

---

## §6 Änderungsregel

- WORKPLAN.md ist **SSOT für Tasks**. ROADMAP.md und TECH_DEBT.md referenzieren hierher.
- Neue Tasks: zuerst hier eintragen, dann in TECH_DEBT spiegeln.
- Task-IDs sind stabil (D-01, W-01, N-01). Kein Re-Indexing.
- Wenn ein Task erledigt ist: `~~durchgestrichen~~` mit Datum + Commit-Hash.
