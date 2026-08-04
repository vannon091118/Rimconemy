# H1 — RimWorld 1.6 API- und Def-Gate

> **SSOT-Hinweis:** Detail-Topic dieser Datei ist im Orient-Index [ARCHITECTURE.md §1](ARCHITECTURE.md). Topic-Landkarte: [INDEX.md §1](INDEX.md).
> **Owner:** Research/Design (kein Code)
> **Status:** `PLANNED` — Symboltabelle erstellt, Runtime-Verifikation durch User erforderlich
> **Basis:** Lokale Installation RimWorld 1.6.4566, `Assembly-CSharp.dll`-String-Scan, Core-/DLC-Def-XML, RimWorld-Wiki
> **Referenz:** [ROADMAP.md §8.1](../ROADMAP.md#81-offene-api-spikes-gates), [ROADMAP.md §1](../ROADMAP.md#1-verbindliche-priorität). Empirische DLL-String-Belege der lokalen RimWorld-1.6-Installation (u.a. `IncidentWorker_RaidEnemy`, `FixedBiologicalAge`, `GameEnder`) im Archiv `docs/archive-md-2026-08-04.tar.gz` (ehem. GESAMTREPORT §3).

## Zweck

Vor jedem Story-, Ideology-, Character- oder Storage-Code muss klar sein, welche RimWorld-1.6-Signaturen **lokal belegt** sind — nicht nur als Assembly-String, sondern als nutzbare API. Diese Tabelle trennt bewusst zwischen statischem Fund, Kompilierbarkeitsprobe und Runtime-Beleg.

## Statuswortschatz

| Kürzel | Bedeutung |
|---|---|
| `STRING` | Symbol in `Assembly-CSharp.dll` als String gefunden (keine Signatur-Garantie) |
| `COMPILES` | Code kompiliert gegen das Symbol (lokale Build-Probe bestanden) |
| `DEF-LOAD` | Def/XML lädt im Spiel ohne Fehler |
| `RUNTIME` | Verhalten im Spiel beobachtet (User-Test) |
| `READY` | Alle drei Stufen (COMPILES + DEF-LOAD + RUNTIME) bestätigt |
| `OPEN` | Symbol-Existenz noch nicht lokal geprüft |
| `REFUTED` | Symbol existiert nicht oder Signatur abweichend |

---

## Sektion A: Storyteller / Incident / Difficulty

| # | Symbol | Namespace | Lokaler String-Treffer | COMPILES | DEF-LOAD | RUNTIME | Status | Anmerkung |
|---|---|---|---:|---|---|---|---|
| A1 | `IncidentWorker` | `RimWorld` | 3 (Basisklasse) | ✅ via `InfectedRaidWorker` | ❌ | ❌ | `COMPILES` | Abstract-Klasse; Subklassen instanziierbar |
| A2 | `IncidentWorker_RaidEnemy` | `RimWorld` | 3 | ❌ | ❌ | ❌ | `STRING` | Erbungsziel für A2-Raid |
| A3 | `TryResolveRaidFaction` | `RimWorld` | 4 | ❌ | ❌ | ❌ | `STRING` | Methode in `IncidentWorker_RaidEnemy` |
| A4 | `ResolveRaidPoints` | `RimWorld` | 1 | ❌ | ❌ | ❌ | `STRING` | Raidpunkt-Berechnung |
| A5 | `ResolveRaidStrategy` | `RimWorld` | 4 | ❌ | ❌ | ❌ | `STRING` | Strategie-Auswahl |
| A6 | `GetLetterDef` | `RimWorld` | 1 | ❌ | ❌ | ❌ | `STRING` | Letter-Def für Incident |
| A7 | `IncidentDef` | `RimWorld` | via `IncidentDef` | ✅ via `InfectedRaid.xml` | ❌ | ❌ | `COMPILES` | XML-Def lädt; `workerClass` Feld in 1.6 |
| A8 | `IncidentParms` | `RimWorld` | via `IncidentWorker` | ✅ | ❌ | ❌ | `COMPILES` | Parameter-Struct |
| A9 | `IncidentCategoryDef` | `RimWorld` | via `ThreatBig` | ✅ via `category` | ❌ | ❌ | `COMPILES` | `ThreatBig` als Kategorie vergeben |
| A10 | `StorytellerDef` | `RimWorld` | ❌ | ❌ | ❌ | ❌ | `OPEN` | Kein eigener Storyteller im Repo; Feld existiert laut Wiki |
| A11 | `StorytellerComp` | `RimWorld` | ❌ | ❌ | ❌ | ❌ | `OPEN` | Basisklasse für Storyteller-Komponenten |
| A12 | `StorytellerUtility` | `RimWorld` | 6 | ❌ | ❌ | ❌ | `STRING` | Utility-Klasse |
| A13 | `DefaultThreatPointsNow` | `RimWorld` | 1 | ❌ | ❌ | ❌ | `STRING` | Property auf `Storyteller` |
| A14 | `threatScale` | `DifficultyDef` | 1 | ❌ | ❌ | ❌ | `STRING` | Difficulty-Feld |
| A15 | `cropYieldFactor` | `DifficultyDef` | 1 | ❌ | ❌ | ❌ | `STRING` | Difficulty-Feld |
| A16 | `researchSpeedFactor` | `DifficultyDef` | 1 | ❌ | ❌ | ❌ | `STRING` | Difficulty-Feld |
| A17 | `colonistMoodOffset` | `DifficultyDef` | 1 | ❌ | ❌ | ❌ | `STRING` | Difficulty-Feld |
| A18 | `diseaseIntervalFactor` | `DifficultyDef` | 1 | ❌ | ❌ | ❌ | `STRING` | Difficulty-Feld |
| A19 | `isExtreme` | `DifficultyDef` | 1 | ❌ | ❌ | ❌ | `STRING` | Difficulty-Feld |

**Bewertung Sektion A:** Storyteller/Incident-Pfad ist **solide per String-Scan** belegt. Ein eigener `IncidentWorker` kompiliert bereits. Die Difficulty-Felder sind einzeln bestätigt. Für einen direkten `StorytellerDef`/`StorytellerComp` fehlt der Runtime-Beleg.

**Nächster Schritt (User):** `IncidentWorker_RaidEnemy`-Erbung kompilieren, `ThreatBig`-Incident im Spiel feuern lassen, Letter/Message prüfen.

---

## Sektion B: Ideology (DLC — DLC-Assembly lokal nicht verfügbar)

| # | Symbol | Namespace | Lokaler String-Treffer | COMPILES | DEF-LOAD | RUNTIME | Status | Anmerkung |
|---|---|---|---:|---|---|---|---|
| B1 | `PreceptDef` | `RimWorld` | 19 | ❌ | ❌ | ❌ | `STRING` | In Core-Assembly; Ideology-DLC separat |
| B2 | `RitualDef` | `RimWorld` | 73 | ❌ | ❌ | ❌ | `STRING` | Hohe Trefferzahl → stabil |
| B3 | `RoleDef` | `RimWorld` | 17 | ❌ | ❌ | ❌ | `STRING` | Rollen-Def |
| B4 | `IdeoManager` | `RimWorld` | 6 | ❌ | ❌ | ❌ | `STRING` | Manager-Klasse |
| B5 | `IdeoDef` | `RimWorld` | ❌ | ❌ | ❌ | ❌ | `OPEN` | **In DLC-Assembly, lokal nicht prüfbar** |
| B6 | `ThoughtDef` | `RimWorld` | via `ThoughtDef` | ✅ via `FoundationDefInventory` | ❌ | ❌ | `COMPILES` | Bereits im Inventory enumeriert |
| B7 | `ThoughtWorker` | `RimWorld` | via `ThoughtWorker_*` | ❌ | ❌ | ❌ | `STRING` | Situational/Memory-Thoughts indirekt |
| B8 | `TraitDef` | `RimWorld` | via `TraitDef` | ✅ via `FoundationDefInventory` | ❌ | ❌ | `COMPILES` | Bereits im Inventory enumeriert |
| B9 | `NeedDef` | `RimWorld` | via `NeedDef` | ✅ via `FoundationDefInventory` | ❌ | ❌ | `COMPILES` | Bereits im Inventory enumeriert |

**Bewertung Sektion B:** Ideology-Anker sind **teilweise im Core bestätigt** (`PreceptDef`, `RitualDef`, `RoleDef` per String). `IdeoDef` fehlt lokal, da in DLC-Assembly. Dies ist der **kritischste offene Spike** — ohne `IdeoDef`-Signatur kann die Einflussmatrix nur spezifikativ sein.

**Nächster Schritt (User):** DLC-Assembly lokal verfügbar machen (Ideology-DLC installiert → `Data/Ideology/Assemblies/`), `IdeoDef`-Signatur per Decompile/Reflection prüfen.

---

## Sektion C: Pawn / Character / Generation

| # | Symbol | Namespace | Lokaler String-Treffer | COMPILES | DEF-LOAD | RUNTIME | Status | Anmerkung |
|---|---|---|---:|---|---|---|---|
| C1 | `PawnGenerationRequest` | `Verse` | 3 | ❌ | ❌ | ❌ | `STRING` | Generator-Parameter |
| C2 | `FixedBiologicalAge` | `PawnGenerationRequest` | 3 | ❌ | ❌ | ❌ | `STRING` | **Startalter 18 direkt setzbar** |
| C3 | `FixedChronologicalAge` | `PawnGenerationRequest` | 3 | ❌ | ❌ | ❌ | `STRING` | **Chronologisches Alter setzbar** |
| C4 | `PawnGenerator` | `Verse` | via `GeneratePawn` | ❌ | ❌ | ❌ | `STRING` | Static-Klasse |
| C5 | `GenerateTraits` | `PawnGenerator` | ❌ | ❌ | ❌ | ❌ | `OPEN` | Trait-Generierung — Spike nötig |
| C6 | `SkillRecord` | `RimWorld` | via `Pawn_SkillTracker` | ❌ | ❌ | ❌ | `STRING` | Skill-Daten |
| C7 | `Pawn_SkillTracker` | `RimWorld` | via `skills` | ❌ | ❌ | ❌ | `STRING` | Tracker auf Pawn |
| C8 | `SetInitialLevel` | `Need` | 2 | ❌ | ❌ | ❌ | `STRING` | Need-Initialisierung |
| C9 | `CurLevelPercentage` | `Need` | 2 | ❌ | ❌ | ❌ | `STRING` | Need-Abfrage |
| C10 | `NeedInterval` | `Need` | 2 | ❌ | ❌ | ❌ | `STRING` | Need-Tick-Intervall |
| C11 | `ScenarioDef` | `RimWorld` | via `ScenarioDef` | ✅ via `SingleSurvivor.xml` | ❌ | ❌ | `COMPILES` | Szenario-Def |
| C12 | `ScenPart_ConfigPage_ConfigureStartingPawns` | `RimWorld` | via `ScenPart_*` | ✅ via XML | ❌ | ❌ | `COMPILES` | Pawn-Auswahl-UI |
| C13 | `BackstoryDef` | `RimWorld` | via `Backstory` | ❌ | ❌ | ❌ | `STRING` | Backstory-Defs |

**Bewertung Sektion C:** Die kritischen Anker für Character Setup sind **positiv**: `FixedBiologicalAge` und `FixedChronologicalAge` sind per String bestätigt. Szenario-Def und Pawn-Auswahl kompilieren bereits. `GenerateTraits`-Signatur ist noch offen.

**Nächster Schritt (User):** `PawnGenerationRequest` mit `FixedBiologicalAge=18` + `FixedChronologicalAge=18` instanziieren und kompilieren.

---

## Sektion D: Storage / Map / Resources

| # | Symbol | Namespace | Lokaler String-Treffer | COMPILES | DEF-LOAD | RUNTIME | Status | Anmerkung |
|---|---|---|---:|---|---|---|---|
| D1 | `ThingDef` | `Verse` | via `ThingDef` | ✅ via `FoundationDefInventory` | ❌ | ❌ | `COMPILES` | Bereits enumeriert |
| D2 | `DefDatabase<T>.AllDefsListForReading` | `Verse` | ❌ (generisch) | ✅ via `FoundationDefInventory` | ❌ | ❌ | `COMPILES` | In Verwendung |
| D3 | `Map.mapPawns` | `Verse` | via `mapPawns` | ✅ via `ProgressionGameComponent` | ❌ | ❌ | `COMPILES` | In Verwendung |
| D4 | `Map.listerThings` | `Verse` | via `ListerThings` | ❌ | ❌ | ❌ | `STRING` | Thing-Enumeration |
| D5 | `SlotGroup` / `StorageGroup` | `RimWorld` | via `SlotGroup` | ❌ | ❌ | ❌ | `STRING` | Lagerzonen |
| D6 | `Thing.stackCount` | `Verse` | via `stackCount` | ❌ | ❌ | ❌ | `STRING` | Stack-Größe |
| D7 | `Thing.Position` / `Map` | `Verse` | via `Position` | ❌ | ❌ | ❌ | `STRING` | Position |
| D8 | `Thing.HitPoints` | `Verse` | via `HitPoints` | ❌ | ❌ | ❌ | `STRING` | Zustand/Qualität |
| D9 | `CompQuality` | `RimWorld` | via `CompQuality` | ❌ | ❌ | ❌ | `STRING` | Qualitäts-Comp |
| D10 | `CompRottable` | `RimWorld` | via `CompRottable` | ❌ | ❌ | ❌ | `STRING` | Verderbs-Comp |
| D11 | `PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists` | `Verse` | 1 | ❌ | ❌ | ❌ | `STRING` | **Name korrekt bestätigt** (F6-Fix) |

**Bewertung Sektion D:** Storage-Enumeration ist **machbar**: `DefDatabase<T>`, `Map.listerThings`, `SlotGroup`, `Thing.stackCount` sind per String bestätigt. Die Foundation nutzt bereits `AllDefsListForReading`. `CompQuality`/`CompRottable` für Qualität/Verderb bestätigt.

**Nächster Schritt (User):** `Map.listerThings.AllThings` + `SlotGroup`-Enumeration kompilieren und gegen lokale Assembly prüfen.

---

## Sektion E: Save / Scribe

| # | Symbol | Namespace | Lokaler String-Treffer | COMPILES | DEF-LOAD | RUNTIME | Status | Anmerkung |
|---|---|---|---:|---|---|---|---|
| E1 | `Scribe_Values.Look` | `Verse` | via `Scribe_Values` | ✅ via `ProgressionGameComponent` | ❌ | ❌ | `COMPILES` | In Verwendung |
| E2 | `Scribe_Collections.Look` | `Verse` | 3 | ✅ via `ProgressionGameComponent` | ❌ | ❌ | `COMPILES` | In Verwendung |
| E3 | `GameComponent` | `Verse` | via `GameComponent` | ✅ via `ProgressionGameComponent` | ❌ | ❌ | `COMPILES` | In Verwendung |
| E4 | `WorldComponent` | `Verse` | via `WorldComponent` | ❌ | ❌ | ❌ | `STRING` | World-Save |
| E5 | `LoadedModManager.RunningMods` | `Verse` | via `RunningMods` | ✅ via `FoundationDefInventory` | ❌ | ❌ | `COMPILES` | In Verwendung |

---

## Sektion F: Sonstige verifizierte Anker

| # | Symbol | Status | Fundstelle |
|---|---|---|---|
| F1 | `CompPowerTrader` (4) | `STRING` | Power-Comp |
| F2 | `CompPowerPlant` (27) | `STRING` | Power-Plant |
| F3 | `CompRefuelable` (5) | `STRING` | Brennstoff-Comp |
| F4 | `WorldObjectComp` (71) | `STRING` | Outpost-Basis |
| F5 | `GameEnder` (3), `CheckOrUpdateGameOver` (1) | `STRING` | Game-Over-API existiert |
| F6 | `TicksPerDay` (1) | `STRING` | **1 Tag = 60.000 Ticks** → F1-Befund bestätigt |
| F7 | `Need_Food`, `Need_Rest`, `Need_Joy` | `STRING` | Need-Unterklassen |
| F8 | DLC-PackageIds: `Ludeon.RimWorld.Royalty` (PascalCase) | `DEF-LOAD` | F5 widerlegt |
| F9 | `Flammability` als StatDef | `DEF-LOAD` | F7 widerlegt |
| F10 | `Turret_MiniTurret` nutzt `Gun_MiniTurret` | `DEF-LOAD` | Turret-Befund widerlegt |

---

## Zusammenfassung

| Sektion | COMPILES | STRING | OPEN | Bewertung |
|---|---|---|---|---|
| A — Storyteller/Incident | 3 | 12 | 2 | 🟡 solide, Runtime fehlt |
| B — Ideology | 3 | 5 | 1 (`IdeoDef`) | 🔴 kritisch: `IdeoDef` fehlt lokal |
| C — Pawn/Character | 3 | 8 | 2 | 🟢 Startalter-Anker bestätigt |
| D — Storage/Map | 2 | 8 | 0 | 🟢 machbar, Needs Enumeration-Spike |
| E — Save/Scribe | 4 | 1 | 0 | 🟢 Scribe in Verwendung |
| F — Sonstige | — | — | — | 🟢 10 Anker bestätigt/widerlegt |

**Nächster logischer Schritt für User:** `IdeoDef`-Signatur in der DLC-Assembly prüfen (H3-Blocker), dann StorytellerComp-Spike kompilieren.
