# Audit: Dead Code & nutzloser Code — Root Cause, Löschen vs. Verkabeln, Extraktions-Pattern

> **Stand:** 2026-08-05 · **Owner:** Buffy (Agent) + User
> **Methodik:** Verwendungsgraphen per `code_searcher` (rg) über alle 5 Pakete;
> jeder Kandidat mit 0 Produktions-Callern einzeln verifiziert. Kein Treffer in
> `Source/**` = toter Code (Tests/Bootstrap-LogMarker zählen nicht als Verkabelung).
> **Scope:** `mods/01-05`, `scripts/`, `tools/`, `tmp-scribe-inspect/`, Wurzel-Dateien.
> **Kein Löschen ohne [DELETE-GATE]** — dieser Report ist die Find-Phase; Gates folgen separat.
>
> **Legende:** 🟢 DELETE (sicher) · 🟡 WIRE (verkabeln lohnt) · 🔴 VERDÄCHTIG (prüfen) · ⚪ KEIN DEAD CODE
>
> **Stand Sprint A (2026-08-05, ausgeführt):** Alle 🟢-Kandidaten entfernt und verifiziert —
> Build aller 5 Pakete grün, `./scripts/runtime_test.sh --skip-start` PASS (5/5 Pakete),
> Stale-Doku-Verweise nachgezogen. Siehe §7.1 Delete-Log.

---

## 1. Executive Summary — Top-Kandidaten

| # | Fund | Paket | Root-Cause | Entscheidung | Aufwand |
|---|------|-------|-----------|--------------|---------|
| 1 | `.runtime-reports/` (41 Dateien, 172 KB, **git-getrackt**) | Repo | Runtime-Logs wurden committet, obwohl CLAUDE.md „Never commit logs" verbietet | 🟢 DELETE aus Git + gitignore | 5 min |
| 2 | `tmp-scribe-inspect/` (Spike-Projekt) | Repo | Einmal-Spike zum Scribe-IL-Dump, nie entfernt, git-getrackt | 🟢 DELETE | 5 min |
| 3 | `IncidentStub` | 05 | Pre-Phase-Datencontainer, vom echten `InfectedRaidWorker` abgelöst; lebt nur via Bootstrap-LogMarker | 🟢 DELETE | 10 min |
| 4 | `OutpostStub` | 04 | Legacy-Alias nach Umbenennung `OutpostStub → Outpost`; `PowerChainStub`/`MarketStub` bereits entfernt (Präzedenz!) | 🟢 DELETE | 10 min |
| 5 | `TerritoryNode` | 04 | Territory-Graph nie umgesetzt (OPEN); lebt nur via Bootstrap-LogMarker | 🟢 DELETE | 10 min |
| 6 | `MechadroidJobRegistry` / `MechadroidUnit` | 05 | Mechadroid-Aufträge gated für Milestone B; lebt nur via Bootstrap-LogMarker | 🟢 DELETE (Markierung in Bootstrap ersetzen) | 10 min |
| 7 | `WorldRaidCoordinator` | 05 | Phase-6-Stub für P6 Task 15; **null Produktions-Caller** | 🟢 DELETE oder 🟡 WIRE in StoryDirector | 20 min |
| 8 | `OutpostProxyGraph` | 04 | Phase-6-Stub P6 Task 14; **null Caller überhaupt** | 🟢 DELETE | 10 min |
| 9 | `FoodHarvestCycleService` | 03 | Phase-6-Stub P6 Task 9; `ReadTotals()` hat **keinen Caller**, nicht mal UI | 🟡 WIRE in InfrastructureDashboard (Food/Hemp-Karte) | 30 min |
| 10 | `FueledGeneratorService` | 03 | Phase-6-Stub P6 Task 10; `CurrentFuelInventory()` hat **keinen Caller** | 🟡 WIRE in ArrowTurretPowerGate / PowerChain (Fuel-Gate) | 1–2 h |
| 11 | `SilverMaterial.cs` (SilverLedger/SilverService/SilverGameComponent) | 04 | „currency=credits, never silver" (Bootstrap-Log!) — Silver-Klasse widerspricht der eigenen Doku; kein Caller | 🟢 DELETE (Doku-Widerspruch) | 15 min |
| 12 | `PhaseProgressWindow` | 02 | Nur via Reflection-FindType in FoundationDashboard erreichbar — **kein MainTabDef/Button registriert**; UI unerreichbar | 🔴 VERDÄCHTIG — Wire in MainTabDef oder DELETE | 30 min |
| 13 | `MarketStub` / `PowerChainStub` | 03/04 | Bereits entfernt (2026-08-05) — Präzedenz für alle Alias-Entfernungen | ✅ erledigt | — |
| 14 | `banner.html` / `banner.svg` | Repo | GitHub-Presence-Deliverable (ROADMAP §9.7) — **kein** Dead Code | ⚪ KEEP | — |
| 15 | `tools/inspect/` | Repo | Referenziert in ROADMAP (Reproduktion: `dotnet tools/inspect/...`) — lebendes Spike-Tool | ⚪ KEEP | — |
| 16 | `--scan` (Wurzel) | Repo | Spike-Rohdaten-Datei, nicht git-getrackt | ⚪ lokal aufräumen, nicht Teil des Repos | — |

---

## 2. Detail-Findings

### 🟢 DELETE — Kategorie A: Legacy-Aliase & LogMarker-Waisen

**A1. `IncidentStub.cs` (05)**
- `mods/05-Rimconemy-Infected-Automation/Source/Incidents/IncidentStub.cs:13-23`
- Verwendung: **nur** `Bootstrap.cs:30` → `var _raidLog = Incidents.IncidentStub.LogMarker;`
- Root-Cause: Datencontainer aus der Pre-Phase („SPIKE: API-INCIDENT-01"). Der echte Worker (`InfectedRaidWorker` mit Letter + Spawn-Bridge) existiert längst. Die Klasse ist eine LogMarker-Waise: Sie lebt, weil Bootstrap ihren Marker loggt.
- Entscheidung: 🟢 DELETE — Zeile in Bootstrap auf `IncidentStub`-String oder `InfectedRaidWorker`-Markierung umstellen. Kein externer Code referenziert `IncidentStub` (nur `LogMarker`-Konstante).

**A2. `OutpostStub` (04)**
- `mods/04-Rimconemy-Economy-Territory/Source/Outposts/Outpost.cs:341-348` — „Backward-compat alias for the historical OutpostStub name."
- Verwendung: **nur** `Bootstrap.cs:28` → `var _outposts = Outposts.OutpostStub.LogMarker;`
- Root-Cause: Nach der Umbenennung `OutpostStub → Outpost` wurde ein Alias stehen gelassen „so legacy callers / deploy scripts continue to compile". Es gibt aber **keinen** Legacy-Caller (0 Treffer außer Bootstrap).
- Präzedenz: `PowerChainStub` und `MarketStub` wurden am 2026-08-05 exakt nach diesem Muster entfernt (siehe `PowerChainService.cs:14`: „PowerChainStub alias (removed 2026-08-05)"). `OutpostStub` ist die letzte verbliebene Alias-Waise.
- Entscheidung: 🟢 DELETE — Bootstrap-Zeile auf `Outposts.Outpost.LogMarker` umstellen.

**A3. `TerritoryNode.cs` (04)**
- `mods/04-Rimconemy-Economy-Territory/Source/Territory/TerritoryNode.cs:13-18` — enum `TerritoryNodeType` + sealed class.
- Verwendung: **nur** `Bootstrap.cs:29` → `var _nodes = Territory.TerritoryNode.LogMarker;`
- Root-Cause: Territory-Graph ist explizit OPEN („Economy WorldObject/transfer/territory lifecycle" = offenes Live-Gate). Die Klasse wurde als Platzhalter angelegt, der Graph nie gebaut.
- Entscheidung: 🟢 DELETE — LogMarker-Zeile aus Bootstrap entfernen oder durch String-Konstante ersetzen. Wieder-Einführung bei realem Territory-Feature.

**A4. `MechadroidJobRegistry.cs` / `MechadroidUnit` (05)**
- `mods/05-Rimconemy-Infected-Automation/Source/Mechadroids/MechadroidJobRegistry.cs:19` — Phase-6-Stub, 0 Caller.
- `MechadroidUnit.cs:17` — LogMarker `v0`, **nur** in `Bootstrap.cs:31` (`var _mechLog = Mechadroids.MechadroidUnit.LogMarker;`).
- Root-Cause: „Mechadroid job contracts are gated for Milestone B" (Bootstrap-Log). Registry + Unit sind reine Marker-Träger ohne Verhalten.
- Entscheidung: 🟢 DELETE beide Klassen; Bootstrap-Marker auf String umstellen. Bei Milestone-B-Implementierung neu schreiben (dann mit echtem Def-Bezug).

**A5. `SilverMaterial.cs` — SilverLedger/SilverService/SilverGameComponent (04)**
- `mods/04-Rimconemy-Economy-Territory/Source/Upgrades/SilverMaterial.cs:1-160`
- Verwendung: **nur selbstreferenziell** (GetComponent<SilverGameComponent> intern). Kein Caller in `EconomyHub`, `TradePanel`, Bootstrap, Tests.
- Root-Cause: E-T4-Spec („Silber als Upgrade-Material") wurde als vollständige Wallet-Parallele implementiert, **bevor** die Credits-Entscheidung fiel. `Bootstrap.cs:22` loggt explizit: „Wallet, Markets, Outposts and Territory stubs registered as data records (**currency=credits, never silver**)." → Die Silver-Klasse widerspricht der eigenen Doku-Zeile.
- Zusätzlich: `SilverGameComponent` ist eine `GameComponent`-Subklasse und wird von RimWorld automatisch instanziiert → sie läuft sogar mit, ohne dass sie jemand nutzt (toter State, der gespeichert wird!).
- Entscheidung: 🟢 DELETE — Doku-Widerspruch beheben. Falls Silber später kommt, bewusst wieder einführen.

### 🟢 DELETE — Kategorie B: Phase-6-Stubs ohne Caller

**B1. `WorldRaidCoordinator.cs` (05)**
- `mods/05-Rimconemy-Infected-Automation/Source/World/WorldRaidCoordinator.cs:28-78` — `PlanWorldRaids(long)`.
- Verwendung: **0 Produktions-Caller**. Nur Doc-Referenzen in `ThreatSnapshotBridge.cs:12` und `InfectedRaidSpawnService.cs:24` (Kommentare) sowie `Bootstrap.cs:61` (Kommentar).
- Root-Cause: P6 Task 15 (Weltkarten-Endgame). Der Stub wurde vor dem Trigger geschrieben; WorldObject-Erzeugung/Ankunft/Auflösung sind OPEN. Der Stub hat nie einen Aufrufer bekommen, weil der Live-Loop nicht existiert.
- Entscheidung: 🟢 DELETE (Spec bleibt in `docs/P6-PROGRESS.md` Task 15 und `ROADMAP`) **oder** 🟡 WIRE: `StoryDirector.GameComponentTick` könnte `PlanWorldRaids` als Druck-Vorschau in die UI einbinden. Da kein Live-Pfad existiert, ist DELETE ehrlicher — WIRE würde nur tote Zahlen produzieren.

**B2. `OutpostProxyGraph.cs` (04)**
- `mods/04-Rimconemy-Economy-Territory/Source/Outposts/OutpostProxyGraph.cs:19-70` — statischer `_edges`-Listen-State.
- Verwendung: **0 Caller überhaupt** (nicht mal Bootstrap). `EstablishEdge`/`RecordReport`/`GetOverdueOutposts`/`Clear` nie aufgerufen.
- Root-Cause: P6 Task 14 (Drei-Tage-Countdown). Konzept „parent ↔ outpost edges" nie an die `Outpost`-State-Machine (die existiert und tickt!) angeschlossen.
- Entscheidung: 🟢 DELETE — kein einziger Caller, kein UI, kein Persistenz-Envelope. Spec in `P6-PROGRESS.md` Task 14 + ROADMAP 4.6. Alternative WIRE: `OutpostService.Tick` → `EstablishEdge`/`GetOverdueOutposts`; das ist aber Milestone-B-Arbeit.

### 🟡 WIRE — Kategorie C: Stubs, deren Verkabelung echten Spielwert liefert

**C1. `FoodHarvestCycleService.cs` (03)**
- `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Plants/FoodHarvestCycleService.cs:20-57` — `ReadTotals()` zählt Food/Hemp/Straw/Rotten aus StorageSnapshot.
- Verwendung: **0 Caller** — auch `InfrastructureDashboard` zeigt die Zahlen nicht.
- Root-Cause: P6 Task 9 („Nahrung/Hanf getrennt"). Read-Model wurde geschrieben, die UI-Karte nie angebunden.
- Entscheidung: 🟡 WIRE — 3-Zeilen-Dashboard-Anbindung (Food vs. Hemp StatCard) macht das Feature live und gibt dem Storage-Read-Model einen echten Konsumenten. Kosten ~30 min, Test-Seam vorhanden (StorageQuery).

**C2. `FueledGeneratorService.cs` (03)**
- `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Power/FueledGeneratorService.cs:19-52` — `CurrentFuelInventory()` aggregiert WoodLog/Coal/Water.
- Verwendung: **0 Caller**.
- Root-Cause: P6 Task 10 („Wasser/Brennstoff als physischer Pfad zum Generator"). Der Fuel-Gate („generator power gate can refuse to come Online when no fuel is present") wurde nie an `ArrowTurretPowerGate` oder `PowerChainService` angeschlossen.
- Entscheidung: 🟡 WIRE — `ArrowTurretPowerGate.ClassifyState` + PowerChain-Online-Entscheidung mit `HasAnyCombustibleFuel` koppeln. Das schließt die erste echte Gameplay-Schleife (Brennstoff → Strom). 1–2 h, da PowerChainService existiert.

### 🔴 VERDÄCHTIG — Kategorie D: erreicht, aber unerreichbar

**D1. `PhaseProgressWindow` (02)**
- `mods/02-Rimconemy-Survival-Progression/Source/Phase/PhaseProgressWindow.cs:15`
- Verwendung: `FoundationDashboard.cs:96` findet den Typ per `FindType(...)` und instanziiert ihn als Sub-Window (Reflection). **Aber**: kein `MainButtonDef`/`MainTabDef`-XML referenziert `Rimconemy.SurvivalProgression.Phase.PhaseProgressWindow` (Suche: 0 Treffer in XML). RimWorld MainTabWindows brauchen ein `MainButtonDef` mit `tabWindowClass` — sonst ist das Window **nie über die UI erreichbar**.
- Root-Cause: P6/16 PhaseProgress-Overlay. Dashboard-Sub-Tab gebaut, aber der RimWorld-Erreichbarkeitsvertrag (MainButtonDef) fehlt.
- Entscheidung: 🔴 VERDÄCHTIG — prüfen, ob `FoundationDashboard` als einziges MainTab registriert ist und Sub-Windows per `GetSubWindow(tabIndex)` erreicht. Falls ja: `PhaseProgressWindow` ist tot aus User-Sicht → entweder MainButtonDef ergänzen (WIRE) oder DELETE. **Anmerkung:** Auch `SurvivalProgressionDashboard`, `InfrastructureDashboard`, `EconomyHub`, `ThreatDashboard` erben `RimconemyMainTabWindow` — gleiche Frage gilt für alle: Wer registriert den Haupt-Tab? (Vermutlich `FoundationTab.xml`, Prüfung offen.)

**D2. `MiningHookPatch_Bootstrap.cs` (02)**
- `mods/02-Rimconemy-Survival-Progression/Source/HarmonyPatches/MiningHookPatch_Bootstrap.cs:11-15` — nur eine `const string MiningGateBlockedKey`.
- Verwendung: die Konstante wird von `MiningHookPatch` selbst als Class-Referenz gehalten? Prüfung: `MiningGateExt.cs` referenziert `MiningHookPatch` (Doc). Der `[HarmonyPatch]`-Klassen-Mechanismus in `Bootstrap.cs:54` (`harmony.PatchAll`) patcht `MiningHookPatch` automatisch → **das Patch ist LIVE**. `MiningHookPatch_Bootstrap` selbst ist nur ein Translation-Key-Container.
- Entscheidung: ⚪ KEEP (Translation-Key ist Owner-Vertrag), aber als Verdacht markiert: falls `MiningGateBlockedKey` nirgends gelesen wird, Container in `MiningHookPatch.cs` mergen.

---

## 3. Root-Cause-Muster (warum entsteht Dead Code hier?)

Über alle 10 Fälle lassen sich **drei wiederkehrende Wurzeln** identifizieren:

### RC-1: „Stub-first ohne Übergabevertrag" (dominant, 6/10 Fälle)
Der Repo-Stil ist: Phase-6-Stubs mit Kommentar „…is owned by a User Live-Test phase" schreiben, **bevor** der Trigger/Consumer existiert. Dadurch entstehen Read-Modelle ohne Konsumenten:
- `FoodHarvestCycleService` (UI fehlt), `FueledGeneratorService` (Gate fehlt), `WorldRaidCoordinator` (Trigger fehlt), `OutpostProxyGraph` (State-Machine-Anbindung fehlt), `MechadroidJobRegistry` (Milestone B), `TerritoryNode` (Graph OPEN).
- **Heilmittel:** Stub erst schreiben, wenn der Consumer im selben Commit verdrahtet wird („No Stub Without Consumer"-Regel). Oder: Stub als Spec-only markieren und im selben PR löschen, wenn der Consumer verschoben wird.

### RC-2: LogMarker als Lebenserhaltung (4/10 Fälle)
`Bootstrap.cs` liest pro Paket `X.LogMarker`-Konstanten, um „Domain stubs ready" zu loggen. Jede Klasse, deren einziger Haltepunkt dieser LogMarker ist, wird dadurch künstlich am Leben gehalten (`IncidentStub`, `OutpostStub`, `TerritoryNode`, `MechadroidUnit`).
- **Heilmittel:** Bootstrap-LogMarker auf **echte, gelebte** Klassen umstellen (`Outpost.LogMarker`, `InfectedRaidWorker`-Markierung) oder Strings. Regel: „Ein LogMarker rechtfertigt keine Klasse."

### RC-3: Umbenennung ohne Alias-Aufräumung (2/10 Fälle)
`OutpostStub → Outpost`, `PowerChainStub → PowerChainService`, `MarketStub → Market`, `IncidentStub → InfectedRaidWorker`: Die Umbenennung passierte, der Alias blieb „für Deploy-Skripte" — die es nie gab (0 Treffer).
- **Heilmittel:** Bei Umbenennungen Alias **im selben Commit** entfernen und `git grep` als Gate nutzen. Präzedenz `PowerChainStub`/`MarketStub` (bereits gelöscht 2026-08-05) zeigt: funktioniert gefahrlos.

### Sekundär: Doku-Widerspruch (SilverMaterial) & Committed-Artifacts (runtime-reports, tmp-scribe-inspect)
- `SilverMaterial` wurde implementiert, obwohl die Credits-Entscheidung („never silver") bereits gefallen war → Doku und Code widersprechen sich, der Report hat einen ganzen Abschnitt dafür.
- `runtime_test.sh` schreibt Reports nach `.runtime-reports/`, und sie wurden committet, obwohl `CLAUDE.md` „Never commit generated assemblies, local logs, or save files" vorschreibt → die `.gitignore` hat keinen `.runtime-reports/`-Eintrag.

---

## 4. Delete-vs-Wire-Entscheidungsmatrix (generalisierbar)

| Frage | Wenn JA → | Wenn NEIN → |
|-------|-----------|-------------|
| Hat die Klasse einen Produktions-Caller? | KEEP / prüfen | weiter |
| Ist ein **echter** Consumer im selben Milestone geplant (nicht „irgendwann")? | 🟡 WIRE (mit Ticket) | 🟢 DELETE |
| Lebt die Klasse nur über Bootstrap-LogMarker? | 🟢 DELETE | KEEP |
| Ist es ein Legacy-Alias nach Umbenennung? | 🟢 DELETE (Präzedenz!) | KEEP |
| Widerspricht sie einer dokumentierten Entscheidung („never silver")? | 🟢 DELETE | KEEP |
| Ist sie per Reflection/Def erreichbar (UI)? | ⚪ KEEP, Erreichbarkeit prüfen | 🟢 DELETE oder WIRE |

**Kosten-Nutzen:** WIRE lohnt nur, wenn der Consumer ≤ 1 Milestone entfernt ist und das Feature echten Spielwert liefert (Fuel-Gate, Food/Hemp-UI). Alles andere ist DELETE — der Code lebt in Git-Historie, die Spec in `P6-PROGRESS.md`.

---

## 5. Extraktions-Pattern: Monolithen systematisch abbauen

Größte Dateien aktuell (LOC, Stand 2026-08-05): `StoryEventCatalog` (891→1687 nach Expansion), `StoryState` (817), `StoryDirector` (761→996 nach StorytellerComp-Migration 2026-08-07), RimconemyStorytellerComp (558, neu), `FoundationDashboard` (695), `ProgressionGameComponent` (603), `BauschuttRemapApply` (480), `StorageQuery` (471), `CreditsLedger` (468), `FoundationSaveData` (463), `StorySelector` (399), `Market` (388), `ArrowTurretPowerGate` (381).

**Wichtig vorab:** Die Pakete sind bereits sauber modular (keine Projekt-Referenzen, Capability-Gate). „Monolith" heißt hier **Klassen-Monolith** (God-Classes), nicht Paket-Monolith. Das Pattern zielt auf Klassen-Extraktion.

### 5.1 Das 5-Schritt-Seam-Pattern „Extract-to-Service"

Jede Extraktion folgt festen Schritten, angelehnt an die bereits etablierten Muster im Repo (Read-Model-Trennung, ISchemaMigratable, Capability-Gate):

1. **Seam identifizieren** — Eine Verantwortung im God-Class isolieren, die (a) eigenständig testbar ist, (b) einen klaren Datenfluss hat. Kandidaten aus dem Repo:
   - `StoryState` (817): Scribe-Persistenz ↔ Selektions-Logik ↔ Cooldown-Verwaltung sind 3 Seams.
   - `StoryDirector` (761): Tick-Orchestrierung ↔ Event-Auswahl ↔ Letter-Emission.
   - `FoundationDashboard` (695): Tab-Routing ↔ StatCards ↔ Reflection-Instanziierung.
   - `ProgressionGameComponent` (603): XP-Aggregation ↔ GameOver ↔ Need-Amplifier.
2. **Pure-Core-Extraktion** — Den deterministischen, testbaren Kern in eine **statische, def-freie Klasse** ziehen (Vorbild: `DeterministicRng`, `PhaseProgressResolver`, `ThreatAggregator` — bereits nach diesem Muster gebaut). Kein `Current.Game`, kein Scribe, keine Harmony. → sofort unit-testbar.
3. **Read-Model vor Mutation** — Die gelesenen Zustände als immutable Snapshot extrahieren (Vorbild: `StorageSnapshot`, `PowerChainSnapshot`, `SituationSnapshot`). Mutation bleibt am GameComponent, Read geht an den Service.
4. **Envelope-Separation** — Scribe-Persistenz in eigene `IExposable`-Envelope-Klassen ziehen (Vorbild: `FoundationSaveData`-Envelope, `marketSnapshot`, `CreditsLedger`). GameComponent wird dünner Host.
5. **Facade zurücklassen** — Die alte öffentliche API bleibt 1 Release als Delegat (`public void Old() => _service.New();`), damit UI/Cross-Package-Consumer ohne Kaskaden-Migration überleben. Danach Facade löschen (DELETE-Gate).

### 5.2 Definition of Done pro Extraktion

- [ ] Neue Klasse hat ≥ 1 `Tests/*RegressionTests.cs` mit `RunAll()` im Paket-Bootstrap.
- [ ] Alt-API lebt 1 Release als Facade, dann [DELETE-GATE].
- [ ] Capability-Registry/INTERFACE_CONTRACT-Eintrag bei neuer Cross-Package-API.
- [ ] `CODE_STATUS.md`-Zeile aktualisiert (Beleg-Ebene).
- [ ] Kein `NotImplementedException`-Pfad, kein `catch {}`.

### 5.3 Priorisierte Extraktions-Backlog (nach LOC × Wiederverwendbarkeit)

| Prio | Klasse | Seam | Ziel | Nutzen |
|------|--------|------|------|--------|
| 1 | `StoryState` (817) | Cooldown/Selektion → `StoryStateCore` | Def-freier, deterministischer Kern | Save-Logik von Rechen-Logik trennen; T6-Tests erweitern |
| 2 | `StoryDirector` (761) | Event-Auswahl → `StoryScheduler` | Tick-GC schrumpft auf Orchestrierung | Letter-Flood-Disziplin testbar |
| 3 | `FoundationDashboard` (695) | StatCard-Rendering → `StatCardRenderer` | Tab-Routing bleibt, Rendering delegiert | UI-Logik wiederverwendbar (auch für 02-05) |
| 4 | `ProgressionGameComponent` (603) | XP-Aggregation → `XpAggregator` | GameComponent schrumpft um ~40 % | XP-Regeln ohne Game-Kontext testbar |
| 5 | `StoryEventCatalog` (891) | Hardcoded-Specs → **XML-Defs** | Katalog wird reine Merge-Logik | Content nach XML, nicht Code (Repo-Präferenz!) |

**Bonus-Pattern für `StoryEventCatalog` (891 LOC, größte Datei):** Die 12 Events sind bereits als `StoryEventDef`-XML-Overlays gedacht (`MergeFromDefDatabase` existiert!). Der Hardcoded-Katalog kann **vollständig nach XML wandern** — die C#-Datei schrumpft auf ~120 LOC Merge-Logik. Das ist „Daten aus Code extrahieren", das wirkungsvollste Pattern überhaupt in RimWorld-Mods (Defs sind der native Content-Kanal).

### 5.4 Regeln gegen Neu-Dead-Code (Prävention)

1. **No Stub Without Consumer** — Stub + erster Caller in einem Commit.
2. **LogMarker ≠ Lebensrecht** — Bootstrap-Marker nur auf gelebte Klassen.
3. **Alias-Removal im Umbenennungs-Commit** — `git grep` als Gate.
4. **`.runtime-reports/`, `tmp-*/`, Spike-Projekte → gitignore** — kein `git add` von Logs (CLAUDE.md-Regel 1).

---

## 6. Empfohlene Umsetzungsreihenfolge

**Sprint A — Sofort-Cleanup (0 Risiko, ~1 h):**
1. `.runtime-reports/` aus Git entfernen + gitignore (41 Dateien, 172 KB Ballast).
2. `tmp-scribe-inspect/` löschen (git-getrackter Spike).
3. `OutpostStub` + `IncidentStub` + `TerritoryNode` + `MechadroidUnit`/`MechadroidJobRegistry` + `SilverMaterial` löschen, Bootstrap-LogMarker auf Strings/gelebte Klassen umstellen.
4. Build + `./scripts/runtime_test.sh --skip-start --no-deploy` als Gate.

**Sprint B — WIRE mit Spielwert (1 Tag):**
5. `FoodHarvestCycleService.ReadTotals()` in `InfrastructureDashboard` (Food-vs-Hemp-Karte).
6. `FueledGeneratorService.CurrentFuelInventory()` in `ArrowTurretPowerGate`/`PowerChain` (Fuel-Gate).
7. `PhaseProgressWindow`-Erreichbarkeit prüfen → MainButtonDef ergänzen oder DELETE.

**Sprint C — Monolith-Extraktion (2–3 Tage, nach Sprint A/B):**
8. `StoryState`-Core-Extraktion (Prio 1) als Pilot für das 5-Schritt-Pattern.
9. `StoryEventCatalog` → XML-Wanderung (größter LOC-Gewinn pro Stunde).

---

## 7. Reproduktion

```bash
# Kandidat "0 Produktions-Caller" verifizieren:
rg -l "FoodHarvestCycleService|FueledGeneratorService" mods/*/Source --glob '*.cs'   # nur eigene Datei
rg -l "WorldRaidCoordinator" mods/*/Source --glob '*.cs'                              # nur Kommentare
rg -l "OutpostProxyGraph" mods/04-Rimconemy-Economy-Territory --glob '*.cs'           # nur eigene Datei

# LogMarker-Waisen finden:
rg "LogMarker" mods/*/Source/Bootstrap.cs

# Git-getrackte Artefakte:
git ls-files .runtime-reports | wc -l   # → 41
git ls-files tmp-scribe-inspect
```

Jeder Befund oben wurde am 2026-08-05 gegen den aktuellen Source-Stand verifiziert. Pfade relativ zum Projekt-Root.

---

## 7.1 Delete-Log Sprint A (2026-08-05, User-freigegeben)

> R2-konform: Jede Löschung einzeln, mit pre-Hash, post-test und Gate-Verdikt.
> Pre-Hashes vor Löschung aufgenommen; post-test `test -e` ✅ gone.
> Hinweis: Das Worktree wurde nach der ersten Ausführung neu erstellt (Änderungen
> verloren); die Löschungen wurden am 2026-08-05 erneut ausgeführt — identische
> pre-Hashes bestätigen identische Dateien.

| # | Pfad | SHA-256 (pre) | Verdict | User-Choice | Post-Check |
|---|---|---|---|---|---|
| 1 | `mods/05-.../Incidents/IncidentStub.cs` | `dd4a6ad5…dac5` | LogMarker-Waise | DELETE | ✅ gone, Build grün |
| 2 | `mods/05-.../Mechadroids/MechadroidUnit.cs` | `8b419751…f619` | LogMarker-Waise | DELETE | ✅ gone, Build grün |
| 3 | `mods/05-.../Mechadroids/MechadroidJobRegistry.cs` | `6e9b6c23…7582` | 0 Caller | DELETE | ✅ gone, Build grün |
| 4 | `mods/04-.../Territory/TerritoryNode.cs` | `a3d37c5b…f86f3` | Graph OPEN, Marker-only | DELETE | ✅ gone, Build grün |
| 5 | `mods/04-.../Upgrades/SilverMaterial.cs` | `0bfc5f2a…4387` | Doku-Widerspruch „never silver" | DELETE | ✅ gone, Build grün |
| 6 | `tmp-scribe-inspect/` (Projekt) | — (2 Dateien) | Einmal-Spike | DELETE | ✅ gone |
| 7 | `.runtime-reports/` (41 Dateien, 172 KB) | — | Committed-Logs | aus Git entfernt | ✅ untracked (lokal erhalten) |

**Nicht entfernt (bewusst):** `MechadroidJobs.cs` (`MechadroidJobLedger` — lebendig, Tests laufen),
`Outpost.cs` (`Outpost` — lebendige State-Machine), `WorldRaidCoordinator`/`OutpostProxyGraph`
(Sprint B/C offen, nicht freigegeben), `banner.html`/`banner.svg` (GitHub-Presence), `tools/inspect/` (lebendes Spike-Tool).

**Mitgeführte Code-Änderungen:**
- `mods/05-.../Source/Bootstrap.cs` — LogMarker-Zeilen ersetzt (IncidentStub/MechadroidUnit → Kommentar + gelebte Marker-Referenzen).
- `mods/04-.../Source/Bootstrap.cs` — `OutpostStub.LogMarker` → `Outpost.LogMarker`; TerritoryNode-Zeile entfernt.
- `mods/04-.../Source/Outposts/Outpost.cs` — Alias-Klasse `OutpostStub` entfernt (341-348).
- `.gitignore` — `.runtime-reports/` + `/tmp-scribe-inspect/` ergänzt.

**Mitgeführte Doku-Updates (SSOT „keine parallelen Wahrheiten"):** `ROADMAP.md §6` (Silber-Zeile),
`docs/CODE_STATUS.md` (MechadroidUnit-Absatz), `docs/P6-PROGRESS.md` Task 13, `docs/falsification/README.md`
Zeile 20, `docs/falsification/infected__MechadroidJob.md` Code-Anker, `docs/DECISIONS.md` GameComponent-Tabelle,
`docs/CANONICAL_VANILLA_DOMAIN_MAP.md` IncidentStub-Empfehlung.

**Gates:** `dotnet build -c Release` (5/5 Pakete, 0 Fehler) · `./scripts/runtime_test.sh --skip-start` PASS
(5/5 Pakete). Hinweis: Der initiale `--skip-start --no-deploy`-Lauf zeigte einen vorbestehenden
02-DLL-Fehler (installierter Ordner enthielt nur `.deps.json`); durch den Deploy im kanonischen
`--skip-start`-Lauf behoben — nicht durch die Dead-Code-Entfernung verursacht.
