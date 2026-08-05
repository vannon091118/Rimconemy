# Falsifizierungsbericht: `EarlyGame/InfectedSpawn-Skip` (Root Cause „Tag 3 ohne Infizierte & Event")

> **Capability:** `rimconemy.infectedautomation.threat` v1 (+ `rimconemy.survivalprogression.progression` Lese-Seite via Servicebus)
> **Owner:** Infected & Automation (Package 05), Root-Cause-Dokumentation: Buffy + User
> **Stand:** 2026-08-05 · **Status:** `COMPILED` (Code-verifiziert; D-Block wartet auf User-Log)
> **Zweck:** Negative-Evidence-Bericht. Belegt anhand konkreter `Player.log`-Marker + UI-Read-Model, **warum** bis Tag 3 weder ein Infizierter noch ein Story-Event erscheint. Er enthält die **zwei identifizierten Skip-Gates** und ihre nachweisbaren Marker.
> **Code-Anker:**
> - `mods/05/Source/Scenarios/ScenPart_RimconemyStartEnemies.cs` (Starter-Spawn, `PostMapGenerate`; Faction-Gate Zeile 77–84, `starter-spawn returned 0` Zeile 66)
> - `mods/05/Source/Story/StoryDirector.cs` (Pressure-Gate `EvaluateWithSnapshot` Zeile 819–821; Profil-Log Zeile 118; Queue-Log Zeile 406; Sättigungskonstante Zeile 42)
> - `mods/05/Source/Story/SettingProfile.cs` (MinThreatLevel: Refuge 0.0 / Survival 0.2 / Collapse 0.15; `AllowedEventFamilies` Zeile 78/102/126)
> - `mods/05/Source/Incidents/InfectedRaidWorker.cs` (Spawn-Bridge-Skips, `SpawnHostileRavagers`; live-Faction-Gate Zeile 209)
> - `mods/05/Source/UI/ThreatDashboard.cs` (UI-Read-Model: `LastSelectionReason` Zeilen 128–132; Dev-Knopf Zeile 66–72)
> **Bezugsverträge:** `docs/falsification/README.md` (A–G-Schema) · `ROADMAP.md §8.2` · `docs/CODE_STATUS.md §4` (Open Gate „echter Raid-Spawn").

## Kontext

User-Lauf: **Tag 3, keine Rimconemy-Events, kein Infizierter gesehen.** Die Analyse ergibt
zwei unabhängige Skip-Gates, die sich gegenseitig verriegeln:

1. **Pressure-Gate (Events):** `StoryDirector.EvaluateWithSnapshot` bricht jede
   Tages-Evaluation ab, solange `ThreatPressure < ActiveProfile.MinThreatLevel`.
   Pressure = `Σ WealthTotal / 700_000` (`WealthFullPressureThreshold`, StoryDirector.cs Zeile 42, Formel Zeile 470).
   → Survival braucht **140 000 Wealth**, Collapse **105 000**. Eine Tag-3-Kolonie
   liegt typisch bei 10–60k → Pressure ≈ 1–9 % → **jede Evaluation endet vor der
   Event-Auswahl.** Dieser Skip ist **log-still** (nur UI-Read-Model, s. Block D.4).
2. **Live-Faction-Gate (Spawns):** Der Starter-Infizierte (Tag 0) und der
   Raid-Spawn (`InfectedRaidWorker`) setzen eine **Live-Instanz** von
   `Rimconemy_HiddenInfectedFaction` im `FactionManager` voraus
   (`ScenPart_RimconemyStartEnemies.cs:77–84`, `InfectedRaidWorker.cs:204–209`).
   Da `Rimconemy_HiddenInfectedFaction` `hidden=true` ist (`HiddenInfected.xml:15`),
   ist die Instanziierung nicht garantiert → Spawn skippt mit **Log.Warning**.

**Pflicht-Szenario für LIVE-Belege:** `Rimconemy_SingleSurvivor`
(`mods/02/Defs/Scenarios/SingleSurvivor.xml`) — nur dort ist der Starter-ScenPart
registriert (Zeile 72). Ohne dieses Szenario existiert **kein** Tag-0-Infizierter.

## Vertrag (Invariants)

| # | Invariant | Mechanik |
|---|---|---|
| **I1** | Kein Story-Event, solange `ThreatPressure < Profil-Minimum`. | `StoryDirector.EvaluateWithSnapshot`: `if (snapshot.ThreatPressure < ActiveProfile.MinThreatLevel) { LastSelectionReason = "Bedrohungspegel … kein Event ausgelöst."; return; }` (StoryDirector.cs:819–821). |
| **I2** | Raid-Familie ist pro Profil freigeschaltet. | `SettingProfile`: Refuge banned `Raid` (Zeile 79); Survival/Collapse allowed (Zeile 102/126). |
| **I3** | Starter-Spawn (Tag 0) braucht eine erreichbare Live-Faction-Instanz. | `ScenPart_RimconemyStartEnemies.SpawnStarterInfected` ruft `InfectedFactionUtility.EnsureHiddenInfectedFaction()` auf. Die Utility gibt eine vorhandene Instanz zurück oder materialisiert sie aus `Rimconemy_HiddenInfectedFaction`; bei fehlendem Def ⇒ `return 0` + Error/Warning. |
| **I4** | Raid-Spawn braucht eine erreichbare Live-Faction-Instanz. | `InfectedRaidWorker.SpawnHostileRavagers` verwendet dieselbe `InfectedFactionUtility.EnsureHiddenInfectedFaction()`-Quelle; bei fehlendem Def/Materialisierung ⇒ Skip + Error/Warning. |
| **I5** | Raid-Spawn hängt an einem selektierten Event. | `InfectedRaidWorker.TryExecuteWorker` feuert nur, wenn `StoryDirector.PendingIncidentDefName == "Rimconemy_InfectedRaidIncident"` gesetzt wurde (Zeile 88–95). |
| **I6** | Starter-Spawn ist idempotent über Save/Load. | `RimconemyStartEnemiesLedger` (Schema v1, Key `mapID:starter-infected-spawn`). |

## A — Def-Liste (XML-Defs)

| DefName | Datei | Rolle | Status |
|---|---|---|---|
| `Rimconemy_HiddenInfectedFaction` | `mods/05/Defs/Factions/HiddenInfected.xml` | hidden-Faction für Ravager-Spawns (`hidden=true` Zeile 15, `humanlikeFaction=false`) | 🟢 Def vorhanden; ⏳ Live-Faction-Instanz nicht garantiert |
| `Rimconemy_InfectedRavager` | `mods/05/Defs/PawnKinds/InfectedRavager.xml` | PawnKind (combatPower 80) | 🟢 Def vorhanden |
| `Rimconemy_InfectedRaidIncident` | `mods/05/Defs/Incidents/InfectedRaid.xml` | IncidentDef, `workerClass=InfectedRaidWorker`, `baseChance 0.0` | 🟢 Def vorhanden |
| `Rimconemy_StartEnemiesPart` | `mods/05/Defs/Scenarios/Rimconemy_StartEnemiesPart.xml` | ScenPartDef (`scenPartClass=ScenPart_RimconemyStartEnemies`) | 🟢 Def vorhanden |
| `Rimconemy_SingleSurvivor` | `mods/02/Defs/Scenarios/SingleSurvivor.xml` | Szenario; registriert Starter-ScenPart (Zeile 72) | 🟢 Def vorhanden; ⏳ User muss Szenario wählen |

**Stub-Eintrag (Live-Beleg):**

```text
[LIVE-Pflicht, A] Def-Load in Player.log erwartet:
  "[Loading] Def loaded: Rimconemy_HiddenInfectedFaction in defs/Factions"
  "[Loading] Def loaded: Rimconemy_InfectedRavager in defs/PawnKinds"
  "[Loading] Def loaded: Rimconemy_InfectedRaidIncident in defs/Incidents"
  "[Loading] Def loaded: Rimconemy_StartEnemiesPart in defs/Scenarios"
```

## B — Code-Pfad (Build + Boot)

- `Bootstrap.cs` (05) registriert Tests: `StartEnemiesRegressionTests` (Zeile 55),
  `IncidentClassifierRegressionTests` (Zeile 64), `RevengeQuotaFlowRegressionTests`
  (Zeile 104), `HordeRegressionTests` (Zeile 108) u. a.
- `StoryDirector.GameComponentTick` (Zeile 166) evaluiert alle 60 000 Ticks;
  Wipe-Check alle 250 Ticks; Pressure-Gate in `EvaluateWithSnapshot` (Zeile 819).
- Der **Root-Cause-Pfad endet log-still** beim Pressure-Gate: kein
  `Log.Message`, nur `LastSelectionReason` (UI). Erwarteter Bootstrap-Log der
  Test-Gates (05):

```text
[Rimconemy.InfectedAutomation] StartEnemies regression tests: N passed, 0 failed.
[Rimconemy.InfectedAutomation] Revenge-quota flow tests: N passed, 0 failed.
[Rimconemy.InfectedAutomation] Horde regression tests: N passed, 0 failed.
```

## C — Selbsttest (RunAll)

| # | Test | Was er beweist |
|---|---|---|
| 1 | `StartEnemiesRegressionTests.RunAll` | Ledger-Schema v1 + Key-Idempotenz (`mapID:starter-infected-spawn`) — deckt I6 ab |
| 2 | `IncidentClassifierRegressionTests.RunAll` | Genau **eine** `Rimconemy_InfectedRaidIncident`-Quelle im Def-Stream |
| 3 | `RevengeQuotaFlowRegressionTests.RunAll` | SpawnPlan-Revenge-Merge (StubDirector), ohne Live-Game |

**Nicht abgedeckt (Lücke):** Kein Test prüft, ob `Rimconemy_HiddenInfectedFaction`
als **Live-Faction** instanziiert wird (I3/I4) — genau der Code-Find dieser Root Cause.

## D — Runtime-Boot (User Live-Test erforderlich) — ⭐ Die Skip-Marker

> **Wichtigste Erkenntnis dieses Berichts:** „Bedrohungspegel …" ist **keine**
> `Player.log`-Zeile! Der Pressure-Skip (Ursache 1) schreibt NUR in das
> UI-Read-Model (`LastSelectionReason`). Der `Player.log`-Nachweis ist daher
> **Negativ-Evidence** (fehlende Erfolgs-Marker) + UI-Block D.4.

### D.1 — `grep`-Reproduktion (ein Befehl pro Pfad)

```bash
# ── Skip-Marker (Ursache 2 — Starter-Faction nicht live) ─────────
grep "InfectedFactionUtility\|ScenPart_RimconemyStartEnemies: faction" Player.log
#   → Erfolg: "Materialized hidden faction 'Rimconemy_HiddenInfectedFaction'"
#   → Fehler: "FactionDef 'Rimconemy_HiddenInfectedFaction' missing" oder "... not available."
grep "starter-spawn returned 0" Player.log
#   → "[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: starter-spawn returned 0 on map=<id>; the survivor is alone."

# ── Skip-Marker (Ursache 2 — Raid-Spawn-Faction nicht live) ──────
grep "InfectedFactionUtility\|Spawn-bridge: live faction missing" Player.log
#   → Erfolg: "Materialized hidden faction 'Rimconemy_HiddenInfectedFaction'"
#   → Fehler: "Spawn-bridge: live faction missing even after materialize; skipping spawn."
grep "Spawn-bridge: PawnKind\|Spawn-bridge: FactionDef" Player.log

# ── Negativ-Evidence (Ursache 1 — kein Event selektiert/gequeued) ─
grep "StoryDirector: queued incident" Player.log
#   → FEHLT, wenn das Pressure-Gate greift (kein Event → kein Queue)
grep "StoryDirector: Selected" Player.log
#   → FEHLT bei Pressure-Skip (nur bei selektiertem Event geloggt, StoryDirector.cs:871)
grep "InfectedRaidWorker executed" Player.log
#   → FEHLT (kein Event → kein Worker-Lauf)

# ── Positiv-Kontrolle (Erfolgs-Marker, falls System funktioniert) ─
grep "spawned 1 starter infected" Player.log
#   → "[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: spawned 1 starter infected on map=<id>."
grep "StoryDirector: profile=" Player.log
#   → zeigt, welches Profil aktiv ist (Refuge ⇒ Raid-Familie gebannt, I2)
```

### D.2 — Marker-Matrix

| Marker | Quelle (Datei:Zeile) | Level | Bedeutung |
|---|---|---|---|
| `InfectedFactionUtility: Materialized hidden faction …` | InfectedFactionUtility.cs | ℹ Message | Hidden-Faction wurde vor dem Starter-Spawn live materialisiert |
| `starter-spawn returned 0 on map=<id>; the survivor is alone.` | ScenPart_RimconemyStartEnemies.cs:66 | ⚠ Warning | Folge-Marker des Faction-Skips (oder PawnKind/CellFinder-Fehler) |
| `Spawn-bridge: live faction missing even after materialize; skipping spawn.` | InfectedRaidWorker.cs | ⚠ Warning | Auch nach Materialisierungsversuch keine Faction-Instanz → kein Pawn gespawnt |
| `Spawn-bridge: PawnKind 'Rimconemy_InfectedRavager' missing` | InfectedRaidWorker.cs:193 | ⚠ Warning | KindDef fehlt im Def-Stream |
| `Spawn-bridge: FactionDef 'Rimconemy_HiddenInfectedFaction' missing` | InfectedRaidWorker.cs:200 | ⚠ Warning | FactionDef fehlt im Def-Stream |
| *(fehlend)* `StoryDirector: Selected '…'` | StoryDirector.cs:871 | ℹ Message | **Negativ-Beleg Ursache 1**: kein Event jemals selektiert |
| *(fehlend)* `StoryDirector: queued incident=Rimconemy_InfectedRaidIncident …` | StoryDirector.cs:406 | ℹ Message | **Negativ-Beleg Ursache 1**: nichts auf die IncidentQueue |
| *(fehlend)* `InfectedRaidWorker executed: … spawned=N …` | InfectedRaidWorker.cs:169 | ℹ Message | **Negativ-Beleg**: kein Worker-Lauf → 0 Pawns |

### D.3 — Erwartetes Marker-Set im Fehlerfall (Tag-3-Lauf, Survival-Profil)

```text
[Rimconemy.InfectedAutomation] StoryDirector: profile=Rimconemy_Survival (difficulty=Medium)   ← Profil-Kontrolle (StoryDirector.cs:118)
[Rimconemy.InfectedAutomation] InfectedFactionUtility: Materialized hidden faction 'Rimconemy_HiddenInfectedFaction' (...)   ← erwarteter Erfolgsmarker
(oder: ... FactionDef 'Rimconemy_HiddenInfectedFaction' missing / ... not available.)   ← Faction-/Def-Fehler
[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: starter-spawn returned 0 on map=7; the survivor is alone.    ← nur wenn Faction nicht live
(keine Zeile) [Rimconemy.InfectedAutomation] StoryDirector: Selected '…'
(keine Zeile) [Rimconemy.InfectedAutomation] StoryDirector: queued incident=…
(keine Zeile) [Rimconemy.InfectedAutomation] InfectedRaidWorker executed: …
```

### D.4 — UI-only-Nachweis („Bedrohungspegel …" — NICHT im Log!)

Der Pressure-Skip hinterlässt **keine Log-Zeile**. Nachweis ausschließlich über
das **Bedrohungs-&Story-Tab** (`ThreatDashboard`, RimconemyMainTabWindow):

1. Tab „Rimconemy · Bedrohung & Story" öffnen.
2. Sektion „§8.3 UI-Read-Model: Letzter Auswahlgrund" (ThreatDashboard.cs:128–132):
   - `Bedrohungspegel 5 % < Profil-Minimum 20 % — kein Event ausgelöst.`
   - (Zahlen variieren; Format `:P0` aus `StoryDirector.cs:821`, Gate Zeile 819)
3. Zusätzlich: Gauge „Bedrohungspegel" + „Events gesamt = 0" + „Letztes Event: Noch kein Event".
4. **Dev-Modus:** Knopf „⚡ Dev: Story-Auswertung jetzt ausführen" (`EvaluateNow`,
   ThreatDashboard.cs:66–72) erzwingt eine sofortige Evaluation — ein Klick ohne
   neues Event reproduziert denselben `LastSelectionReason`.

**Einzusetzender Live-Beleg:**

```text
<!--
TODO: nach Ingame-Lauf den ThreatDashboard-Screenshot (Sektion „Letzter Auswahlgrund")
+ den Player.log-Auszug der D.3-Marker hier einfuegen.
-->
```

## E — Save/Load Roundtrip

Starter-Spawn-Idempotenz ist durch `RimconemyStartEnemiesLedger` (Schema v1,
`EventKey_OneInfectedSpawn`, Key `mapID:starter-infected-spawn`) gesichert —
statisch verifiziert in `StartEnemiesRegressionTests` (C1). Der Skip hat keine
Save-Auswirkung (nichts wird committet, wenn `spawned == 0`).

## F — Cross-Package READ

- `ScenPart_RimconemyStartEnemies` liest **nur eigenes** Ledger (05) — kein
  P02-Zugriff (INTERFACE_CONTRACT §0/§9).
- `StoryDirector` liest Colony-Wealth über `MapRegistry` (Foundation) +
  `ColonialReader` (Foundation) für `SurvivorCount` — read-only, capability-gated.
- **Kein** Schreibpfad über P02/P03/P04 im Root-Cause-Pfad.

## G — Performance-Kennzahl

| Metrik | Budget | Heutiger Stand |
|---|---|---|
| Eval-Zyklus (60k Ticks) | ≤ 1 ms | Vanilla-Bound; Pressure-Skip ist der *schnelle* Pfad (kein Event → kein Queue) |
| Pressure-Skip Kosten | O(Wealth-Watch) | `wealthWatcher.WealthTotal` ist gecached (StoryDirector.cs:557–559) |
| ThreatDashboard UI | O(Cooldowns) | Unkritisch |

## Reproduktion

```bash
./scripts/runtime_test.sh --require-scenario-tests
```

Danach (interaktiv — User-Pflicht):

1. Szenario **„Rimconemy Einzelüberlebender"** starten (Tag-0-Starter-Gate I3).
2. Bis Tag 3 spielen; **kein** Rimconemy-Event abwarten.
3. `grep`-Block D.1 gegen `Player.log` laufen lassen.
4. ThreatDashboard-Tab öffnen und Sektion „Letzter Auswahlgrund" prüfen (D.4).
5. Log-Auszug + Dashboard-Screenshot als `## D`-Beleg hier einfügen → Status auf `OBSERVED`.

## Negative-Test (manuell)

**Belegbruch 1 — Event trotz Sub-Minimum-Pressure (sollte nicht passieren):**
1. Patch: Pressure-Gate in `EvaluateWithSnapshot` (StoryDirector.cs:819) entfernen.
2. Folge: `StoryDirector: Selected '…'` erscheint auch bei 1 % Pressure → Events
   fluten das Early Game (Supply-Events gewinnen die Gewichts-Lotterie).
3. Marker: `grep "StoryDirector: Selected" Player.log` mit `ThreatPressure`-Gauge < MinThreatLevel.

**Belegbruch 2 — Starter-Spawn ohne Live-Faction (sollte nicht passieren):**
1. Patch: Faction-Check in `SpawnStarterInfected` auskommentieren.
2. Folge: `PawnGenerator.GeneratePawn` wirft / erzeugt Pawn mit nicht existenter
   Faction → Log-Warning oder Crash im `PostMapGenerate`.
3. Marker: `grep "ScenPart_RimconemyStartEnemies:" Player.log`.

## Siehe auch

- `docs/falsification/earlygame__Survivor.md` (Single-Survivor-Szenario-Gate I3).
- `docs/falsification/earlygame__FirstNight.md` (`IncidentWorker_NightInfected`).
- `docs/falsification/infected__InfectedRaid.md` (Raid-Spawn-Bridge, `UNVERIFIED`).
- `docs/falsification/infected__ThreatPressure.md` (`ThreatAggregator` + `StoryDirector`).
- `docs/CODE_STATUS.md §4` (Open Gate: echter Raid-Spawn / Eventauflösung).
- `docs/H2-story-contract.md §1` (SettingProfiles, Event-Familien).
