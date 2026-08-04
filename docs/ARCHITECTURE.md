# ARCHITECTURE — Rimconemy Architecture SSOT-Orient

> **Stand:** 2026-08-04
> **Rolle:** SSOT-Orient-Entry — bündelt die Topic-Verdicts der Vanilla-API, Story-, Ideology-, Storage-, Character- und Pawn-Generator-Specs an einer Stelle. Detail-Tiefe liegt weiterhin in den H1..H6- und `vanilla-api-matrix-1.6.md`-Dateien.
> **Lese-Pfad:** Für jede architektonische Frage hier das Topic finden, dann in den verlinkten Spec-Detail-Doc einsteigen.
> **Pflicht:** Wer ein Topic added/moved, aktualisiert diese Datei **und** den Eintrag in [INDEX.md §1](INDEX.md).

## Sektion 1 — Vanilla-API-Matrix (Detail: vanilla-api-matrix-1.6.md)

**Verdict:** Solide Scope-Belegung der lokalen RimWorld-1.6-Assembly. 15 Vanilla-Anker (ScenarioBase, ScenPart, GameComponent, MapComponent, IncidentWorker, RecipeWorker, Designator, GenSight, CompRefuelable, CompGlower, PawnGenerator, ResearchManager, …) sind klassifiziert nach Owner-Paket und Belegstufe (STRING/COMPILES/DEF-LOAD/RUNTIME/READY).

- **Detail:** [vanilla-api-matrix-1.6.md](vanilla-api-matrix-1.6.md) (Phase-0-Task-0.1-Spike)
- **Sub-Sektionen:**
  - §3.1 — `ScenPart`-Ableitungen für Szenario-Setup (Owner: **05** Incident-Raid, **02** Survivor)
  - §3.5 — `RecipeWorker`-Hook (Owner: **02** Experience-Completion)
  - §3.6 — `Designator`-Hooks (Owner: **03** Architect)
  - §3.8 — `CompRefuelable.ConsumeFuel/Refuel` (Owner: **03** Campfire/Generator)
  - §3.9 — `CompGlower.Glows/ShouldBeLitNow` (Owner: **03** Shelter-Snapshot)
  - §3.10 — `PawnGenerator` (Owner: **02** Survivor, **04** Carrier)
  - §3.11 — `ResearchManager` als Read-Model (Owner: **02** Experience als primäre Freischaltung)
- **Status-Wortschatz:** siehe [H1 §Statuswortschatz](H1-api-def-gate.md) (STRING/COMPILES/DEF-LOAD/RUNTIME/READY/OPEN/REFUTED)
- **Aufgaben:** Bei jedem Code-Task mit Vanilla-API-Nutzung die Signatur in Matrix cross-prüfen; SPIKE-PFLICHT für `⚠`-markierte Hooks.

## Sektion 2 — Story-Vertrag (Detail: H2-story-contract.md)

**Verdict:** Profile-Code und Event-Catalog sind im Code vorhanden, deterministische Auswahl ist implementiert und per Regressionstest abgedeckt. Konkrete Balancewerte, vollständige Choice-Effekte und Live-Save-/Event-Gates bleiben offen.

- **Detail:** [H2-story-contract.md](H2-story-contract.md)
- **Sub-Sektionen:**
  - §1 — Difficulty-Profile (`Rimconemy_Refuge`, `Rimconemy_Survival`, `Rimconemy_Collapse`)
  - §2 — Drei MVP-Events (`SupplyShortage`, `IdeologyConflict`, `ExternalThreat`)
  - §3 — Acht Eventfamilien + 4↔8-Familien-Cross-Walk (`Code: Supply, Social, Raid, Collapse`)
  - §4 — Selection-Algorithmus
  - §5 — Save-Schema `StoryState`
  - §6 — Vanilla/DLC-Policy (kein eigener Storyteller, kein Quest-Suppress)
  - §7 — Def-Preset-Ordner (Modulpfad `mods/05/Defs/StoryEvents/`)
- **Code-Schnittstelle:** `Source/Story/{StoryDirector,StorySelector,StoryState,StoryEventCatalog}.cs`
- **Öffene Gates:** Live-Save/Load-Gate, vollständige deklarative Effects-Engine, StorytellerDef-Eigenbau.

## Sektion 3 — Ideology-Influence-Matrix (Detail: H3-ideology-influence-matrix.md)

**Verdict:** Drei Setting-Regeln spezifiziert; Regel 2 (`CollectiveDefense`) Code-implementiert mit `Pawn_PostApplyDamage_CollectiveDefense`-Harmony-Postfix. Precepte sind live-def-ladefähig; `IdeoDef`-Signatur ist der einzige `OPEN`-Spike (DLC-Assembly lokal nicht verfügbar).

- **Detail:** [H3-ideology-influence-matrix.md](H3-ideology-influence-matrix.md)
- **Sub-Sektionen:**
  - Regel 1 `ResourceFairness` — `PreceptDef + ThoughtDef + ThoughtWorker`
  - Regel 2 `CollectiveDefense` — `RoleDef + ThoughtDef + RitualDef` (Ideology-DLC)
  - Regel 3 `Transparency` — `PreceptDef + ThoughtDef + ThoughtWorker`
  - Vanilla-Precept-Policy (`Cannibalism`, `Execution`, `Slavery` neutralisieren)
- **Code-Schnittstelle:** `Source/Ideology/{CollectiveDefenseTracker,CollectiveDefensePostCombatPatch,TransparencyTracker,ResourceFairnessThoughtWorker}.cs`
- **Aufgaben:** DLC-Assembly-Lokalisierung für `IdeoDef`-Signatur-Verifikation.

## Sektion 4 — Storage-Query-Vertrag (Detail: H4-storage-query-contract.md)

**Verdict:** `StorageSnapshot` und `StorageQuery.ReadStorage()` als Read-only-Layer sind implementiert. Caravan-/Temporary-Map-, Save/Load- und vollständige Konsumenten-Gates befinden sich in Scope.

- **Detail:** [H4-storage-query-contract.md](H4-storage-query-contract.md)
- **Sub-Sektionen:**
  - §1 `ReadStorage(scope, filter, tick)` als einzige Read-only-Quelle
  - §2 Datenstrukturen (`StorageSnapshot`, `StorageEntry`, `StorageScope`, `ResourceFilter`)
  - §3 Code-Abgleich + 250-Tick-Cache + `InvalidateCache()`/`ReadStorageFresh()`
  - §4 Verbraucher (StoryDirector, UI, Economy)
- **Code-Schnittstelle:** `mods/03/Source/Storage/{StorageQuery,StorageSnapshot,StorageEntry,StorageScope,ResourceFilter}.cs`
- **Aufgaben:** Caravan-Inclusion (`AllMapsIncludingCaravans`), Save/Load-Roundtrip, Konsumenten-Reads gegen `ContentHash`-Drift.

## Sektion 5 — Character-Setup-Formel (Detail: H5-character-setup-formula.md)

**Verdict:** Hybrid-Schnitt (2026-08-04) ist implementiert: SkillBudgetCalculator mit progressiver Kostenfunktion, TraitAssigner mit deterministischer Pure-Auswahl, Light/Strong-Polarity. Combat-Skills freigeschaltet; Bio-Remap-Pfad ist offen (FixAge-Fallback aktiv).

- **Detail:** [H5-character-setup-formula.md](H5-character-setup-formula.md)
- **Sub-Sektionen:**
  - §1 Startalter 18 (Reflection-Fallback via `pawn.ageTracker.AgeBiologicalTicks`)
  - §2 Skillbudget 30 (progressive Kosten 1..10 ab Skill > 10)
  - §3 Neutralzone [-5, +3] und Trait-Pools
  - §4 Bio-Remap (offen in Phase A-2; aktuell Post-Generation-Fallback)
  - §5 Reproduzierbarkeits-Gate (Seed-Determinismus)
  - §6 Save-Schema `CharacterSetupState`
- **Code-Schnittstelle:** `mods/02/Source/Character/{SkillBudgetCalculator,TraitAssigner,CharacterSetup,CharacterSetupState}.cs`
- **Aufgaben:** `PawnGenerationRequest.FixedBiologicalAge`-Direktpfad (API-START-01), Specialization-Passion-Schreiben (Reflection/Harmony A-3).

## Sektion 6 — Pawn-Generator-API-Spike (Detail: H6-pawn-generator-api-spike.md)

**Verdict:** Reflection-Fallback-Pfad in `Page_ConfigureStartingPawnsBioPatch` aktiv und gegen lokales 1.6.4566 validiert. Direkt-Pfad via `PawnGenerator.GeneratePawn(...)` mit `PawnGenerationRequest` ist Spike offen.

- **Detail:** [H6-pawn-generator-api-spike.md](H6-pawn-generator-api-spike.md)
- **Reflection-Fallback-Tabelle:** siehe [H6 §Reflection-Fallback-Pfad](H6-pawn-generator-api-spike.md)
- **Pflicht-Tests:** drei Runtime-Belege A/B/C dokumentiert in H6 für Falsifikations-Gate
- **Code-Schnittstelle:** `mods/02/Source/Patches/Page_ConfigureStartingPawnsBioPatch.cs`
- **Aufgaben:** Direkt-Pfad-Variante ohne Reflection.

## Drift-Status-Matrix (Stand 2026-08-04)

| Sektion | Status | Haupt-Drift | Quelle |
|---|---|---|---|
| 1 Vanilla-API | 🟡 mostly-loaded | `IdeoDef` Spike offen (DLC-Assembly lokal nicht vorhanden) | H1 Sektion B |
| 2 Story-Vertrag | 🟢 Code-implementiert; StorytellerDef fehlt | Eigener Storyteller macht keinen Sinn | H2 §6; DECISIONS §2.3 |
| 3 Ideology | 🟢 Code-implementiert | Ideology-DLC nicht installiert → Regel 2 inaktiv | H3 Sektion B; COMPATIBILITY |
| 4 Storage | 🟡 Code-implementiert | Caravan-/Temporary-Map noch nicht abgedeckt | H4 §3 |
| 5 Character-Setup | 🟡 Hybrid-Schnitt; Direkt-Pfad offen | Reflection-Fallback deckt Verhalten semantisch ab | H5; H6 |
| 6 Pawn-Generator | 🔴 Spike offen | Direkt-Pfad via `FixedBiologicalAge` nicht reproduzierbar | H6 Tabelle |

## Cross-Walk: Canvas ↔ Spec

Diese Datei **gruppiert nach Topic** und verweist auf Detail-Tiefe in H1..H6 + `vanilla-api-matrix`. Die H-Docs selbst bleiben mit eigener Audit-Spur (Datum/Owner-Header) und unverändertem Inhalt als Quellen erhalten — sie werden nicht gelöscht oder zusammengeführt, nur von dieser Orient konsolidiert referenziert.

## Siehe auch

- [INDEX.md §1](INDEX.md) — SSOT-Topics-Map (welche Datei was final besitzt)
- [INTERFACE_CONTRACT.md](INTERFACE_CONTRACT.md) — Paket-Eigentumsgrenzen
- [SAVE_CONTRACT.md](SAVE_CONTRACT.md) — `ISchemaMigratable`-Pattern
- [CANONICAL_VANILLA_DOMAIN_MAP.md](CANONICAL_VANILLA_DOMAIN_MAP.md) — Vanilla↔Rimconemy-Mapping
- [COMPATIBILITY_MATRIX.md](COMPATIBILITY_MATRIX.md) — DLC-Tabelle
- [DECISIONS.md](DECISIONS.md) — Architektur-Entscheidungen mit Begründung
- [CODE_STATUS.md](CODE_STATUS.md) — `COMPILED`/`LOADED`/`RUNNING`-Status je Paket
