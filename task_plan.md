# Rimconemy UI P0–P4

## Goal
Implement the approved Rimconemy Visual Language across toolkit, Survival, Infected, Economy, Scavenger, and Foundation UI while keeping UI read-only over existing runtime state.

## Current Phase

**UI-Toolkit-Phasen 1–6 abgeschlossen** (Toolkit / Survival-P0 / Threat-UI / Economy-Hub / Scavenger-UI / Foundation-Polish).

Die hier gelisteten 6 UI-Phasen beziehen sich ausschließlich auf den
beauftragten Rimconemy-Visual-Language-Sprint und **nicht** auf die
Gameplay-Phasen aus dem Vertical-Slice-Plan
(`ROADMAP.md §9.1`).
Gameplay-Phasen (insbesondere Vertical-Slice 7+ mit echten Raids,
Save/Load-LIVE-Belegen, Build-Completions und Mechanic-Vollständigkeit)
sind weiterhin `OFFEN` per `docs/falsification/audit-fixes-2026-08-04.md`.

Aussage "alle Phasen fertig" gilt **nur** für den oben umrissenen
UI-Toolkit-Scope. Kein impliziter Gameplay-Vollständigkeitsanspruch.

## Next Step
Runtime verification by starting RimWorld (`./start.sh`).

> **SSOT-Hinweis:** Diese Datei dokumentiert nur den UI-Toolkit-Sprint. Für alle anderen Themen (Owner-Matrix, Save/Load, Storage-Query, Story-Profil, Ideology, DLC, Tech/Experience) siehe [docs/INDEX.md §1](docs/INDEX.md) für die SSOT-Landkarte.

## Phases
- [x] 1. Shared toolkit + Survival P0
- [x] 2. Infected threat UI
- [x] 3. Economy hub
- [x] 4. Scavenger infrastructure UI
- [x] 5. Foundation dashboard polish
- [x] 6. Full builds, review, and runtime-gate report

## Constraints
- RimWorld 1.6 / Unity IMGUI only.
- No third-party dependencies.
- UI reads existing snapshots/services; no duplicate simulation state.
- No claim of runtime rendering without a fresh game run.

## Errors Encountered
- `GameFont.Large` missing in RimWorld 1.6 Unity assembly -> Resolved by mapping H1 titles to `GameFont.Medium`.
- Missing `using System;` directive in `StoryStateRegressionTests.cs` -> Fixed.
