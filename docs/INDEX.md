# INDEX — SSOT-Topics-Map

> **Stand:** 2026-08-05
> **Rolle:** Diese Map zeigt, welche Datei welches Topic final besitzt. Wer ein Topic moved/edits, aktualisiert die Map (und idealerweise gleichzeitig [ARCHITECTURE.md](ARCHITECTURE.md)).

## §1 Phasen-First Architecture (2026-08-05)

| Topic | SSOT | Owner | Notes |
|---|---|---|---|
| Gameplay-Progression Phasen | [docs/PHASE_PROGRESSION_CONTRACT.md](PHASE_PROGRESSION_CONTRACT.md) | Mod 03 Owner | Phase-First-SSOT; sechs Phasen EarlySurvival→Empire |
| Vanilla Blueprint Audit Phase 1/2 | [docs/vanilla-early-blueprint-matrix-1.6.md](vanilla-early-blueprint-matrix-1.6.md) | Mod 03 Owner | Stufe-1/2/3 Patch-Rubrik |
| Campfire Parity Decision | [docs/campfire-parity-1.6.md](campfire-parity-1.6.md) | Mod 03 Owner | `KEEP_DISTINCT` Empfehlung bis Live-Test |
| Resource SSOT Owner-Map | [docs/PHASE_PROGRESSION_CONTRACT.md §4](PHASE_PROGRESSION_CONTRACT.md) | All Owners | Single-owner pro Resource |

## §2 Vanilla + DLC Anchors

| Topic | SSOT | Owner | Notes |
|---|---|---|---|
| Vanilla API Spike Evidence | [docs/H1-api-def-gate.md](H1-api-def-gate.md) + [docs/vanilla-api-matrix-1.6.md](vanilla-api-matrix-1.6.md) | Mod 01 Foundation | Statuswortschatz STRING/COMPILES/DEF-LOAD/RUNTIME/READY |
| Storage Query | [docs/H4-storage-query-contract.md](H4-storage-query-contract.md) | Mod 03 Scavenger | Read-only Layer |
| Story-Vertrag | [docs/H2-story-contract.md](H2-story-contract.md) | Mod 05 InfectedAutomation | Profile + Event-Catalog |
| Ideology-Influence | [docs/H3-ideology-influence-matrix.md](H3-ideology-influence-matrix.md) | Mod 05 | drei Setting-Regeln |
| Character-Setup | [docs/H5-character-setup-formula.md](H5-character-setup-formula.md) | Mod 02 Survival | Hybrid-Schnitt |
| Pawn-Generator Spike | [docs/H6-pawn-generator-api-spike.md](H6-pawn-generator-api-spike.md) | Mod 02 | Reflection-Fallback-Pfad |

## §3 Cross-Package & Save

| Topic | SSOT | Owner |
|---|---|---|
| Cross-Package Boundaries | [docs/INTERFACE_CONTRACT.md](INTERFACE_CONTRACT.md) | All |
| Save-Schema + Migration | [docs/SAVE_CONTRACT.md](SAVE_CONTRACT.md) | Mod 01 (ISchemaMigratable) |
| Architecture SSOT-Orient | [docs/ARCHITECTURE.md](ARCHITECTURE.md) | Mod 01 |
| Implementation Plan | [docs/superpowers/plans/2026-08-05-phase-first-gameplay-implementation-plan.md](superpowers/plans/2026-08-05-phase-first-gameplay-implementation-plan.md) | Mod 03 |
| Live-Status Vocabulary | [docs/CODE_STATUS.md](CODE_STATUS.md) | Mod 01 |

## §4 Decisions & Compatibility

| Topic | SSOT | Notes |
|---|---|---|
| Architektur-Entscheidungen | [docs/DECISIONS.md](DECISIONS.md) | mit Begründung |
| Vanilla + DLC Compat | [docs/COMPATIBILITY_MATRIX.md](COMPATIBILITY_MATRIX.md) | RimWorld 1.6 + DLC-Tabelle |

## §5 Falsifikation (Anti-Confirm-Bias)

| Topic | SSOT-Ordner | Owner |
|---|---|---|
| Falsifikations-Berichte | [docs/falsification/](falsification/) | per-Topic-Owner |

## §6 Phasen-First Tasks (2026-08-05)

19 Tasks ausgeführt (Stand 2026-08-05):

| # | Task | Status | Output |
|---|---|---|---|
| 0  | Baseline | ✅ | Plan-Datei |
| 1  | Phase-Contract | ✅ | docs/PHASE_PROGRESSION_CONTRACT.md + ARCHITECTURE.md §7 |
| 2  | SteelScraps-SSOT | ✅ | Mod-02-Duplikat gelöscht; SSOT-Marker in Mod-03 |
| 3  | Early-Scatter Phase-Gate | ✅ | ScenPart_RimconemyStart.cs |
| 4  | Vanilla Blueprint Audit | ✅ | docs/vanilla-early-blueprint-matrix-1.6.md |
| 5  | Campfire Parität | ✅ | docs/campfire-parity-1.6.md (KEEP_DISTINCT) |
| 6  | Recipe Phase-Gating | ✅ | 5:1 + Smithing-Research + Smithing>=3 |
| 7  | Vanilla Patches (Stufe 1) | ✅ | FueledSmithy/TableMachining additive idempotent |
| 8  | Mining-API Spike + Reader | ✅ | MiningGateExt.cs + MiningHookPatch.cs (2 Postfix) + Vanilla DefModExt |
| 9  | Fuel-Modell | ✅ | PowerPlants.xml duplicate-Refuelable entfernt |
| 10 | Core-only Vertical Slice | ✅ | Recipe + Patch + SSOT wired |
| 11 | Ideology-Adapter | ✅ | Ideology_Precept_ResourceFairness.xml |
| 12 | Biotech-Mechanitor-Adapter | ✅ | Biotech_Mechanitor_Gene_Mining.xml |
| 13 | Anomaly/Odyssey-Ruinenadapter | ✅ | AnomalyOdyssey_Ruins_ConstructionDebris.xml |
| 14 | Royalty-Handelsadapter | ✅ | Royalty_Empire_TributeTrader_StainlessTrade.xml |
| 15 | Mod-04 BuildingInputAdapter | ✅ | Refactor zu Def.costList |
| 16 | Phasen + Falsifikations-Tests | ✅ | Tests für 5:1 + Mining-Gate + Recipe-Phase |
| 17 | Validierungsmatrix | ✅ | dev_quick_test.sh --strict → PASS exit 0 |
| 18 | Doku-Crosswalk | ✅ | INDEX.md (dieses Doc), ARCHITECTURE.md, CODE_STATUS.md aktualisiert |
