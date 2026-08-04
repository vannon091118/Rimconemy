# Rimconemy UI P0–P4 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the approved Rimconemy Visual Language across all five packages: shared toolkit additions, Survival P0 migration, Infected threat UI, Economy hub, Scavenger status tab, and Foundation dashboard polish.

**Architecture:** UI remains read-only over existing GameComponent/read-model APIs. All drawing is stateless and uses `RimconemyTheme`/`RimconemyUi`; no gameplay, persistence, or simulation rules change. New package screens degrade to explicit empty states when their runtime data is unavailable.

**Tech Stack:** RimWorld 1.6, Unity IMGUI, C# netstandard2.1, existing Rimconemy Foundation UI toolkit.

## Global Constraints

- Preserve RimWorld 1.6 API compatibility and existing package load boundaries.
- Do not introduce new third-party dependencies.
- Do not duplicate simulation state in UI classes.
- Keep all UI interaction tooltip-backed and keyboard/mouse safe.
- Build each affected package against the local RimWorld managed assemblies.
- Runtime game launch/deploy remains a separate explicit gate.

---

### Task 1: Map existing UI contracts and runtime read models

**Files:** Read-only inspection of existing Theme, Ui, base classes, dashboards, panels, and package data services.

- [x] Identify existing tokens and helper signatures.
- [x] Identify concrete runtime data available to each screen.
- [x] Record RimWorld 1.6 API hazards, especially inspect-tab registration.

### Task 2: Extend the shared UI toolkit

**Files:**
- Modify: `mods/01-Rimconemy-Foundation/Source/UI/RimconemyTheme.cs`
- Modify: `mods/01-Rimconemy-Foundation/Source/UI/RimconemyUi.cs`

**Produces:** additive `DangerSoft`, `PanelInk`, `DividerInk`, `DrawStatCard`, `DrawSparkline`, `DrawTabs`, `DrawCountdown`, and `DrawPressureGauge` helpers. Helpers restore GUI state and accept deterministic rectangles/data.

- [ ] Add tokens and documented typography/contrast conventions.
- [ ] Add stat-card, sparkline, tabs, countdown, and pressure helpers.
- [ ] Build Foundation.

### Task 3: Migrate Survival P0 dashboard

**Files:**
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/UI/SurvivalProgressionDashboard.cs`
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/UI/ProgressionPawnTab.cs`
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/Bootstrap.cs` only if registration is already supported by existing conventions.

**Produces:** toolkit-based token UI, pawn-card grid, 3-bar inspect layout, explicit empty/error states, localized keys where existing localization infrastructure supports them.

- [ ] Replace direct `MainTabWindow`/`Color.*`/magic layout values with Foundation base/tokens.
- [ ] Render snapshot cards and game-over banner.
- [ ] Render inspect tab with three need bars and progression details.
- [ ] Build Foundation + Survival.

### Task 4: Add Infected threat screen (P1)

**Files:**
- Create/modify: package-05 UI folder and `Source/Bootstrap.cs` as needed.
- Read: `StoryDirector`, `ThreatAggregator`, `SituationSnapshot`, `StoryState`.

**Produces:** a read-only MainTabWindow or safe floating window showing threat pressure, next incident, target/path metadata when available, and explicit unavailable states. No raid behavior changes.

- [ ] Add adapter/read-only extraction from existing StoryDirector state.
- [ ] Add threat UI with pressure gauge and countdown.
- [ ] Register only through a verified RimWorld 1.6 MainTab convention.
- [ ] Build package 05.

### Task 5: Add Economy hub (P2)

**Files:**
- Modify: `mods/04-Rimconemy-Economy-Territory/Source/Wallet/TradePanel.cs` or add focused UI/data adapter files.
- Read: `CreditsLedger`, `Market`, `Outpost`.

**Produces:** Wallet/Markets/Outposts tabs, balance/status header, transaction list, deterministic price visualization, and outpost status/countdown. Existing transaction behavior remains unchanged.

- [ ] Add tab state and token-based rendering.
- [ ] Add sparkline from existing prices only; no synthetic persistence.
- [ ] Add outpost empty/active/blocked states and countdown.
- [ ] Build package 04.

### Task 6: Add Scavenger infrastructure tab (P3)

**Files:**
- Create: package-03 UI file(s).
- Read: `PowerChainService`, `PlantHelper`, existing DefDatabase-backed state.
- Modify: package-03 `Source/Bootstrap.cs` only if existing registration convention supports it.

**Produces:** compact infrastructure screen for power/fuel/farm/turret availability, with warnings represented by text and badges as well as color.

- [ ] Add read-only adapter for available service snapshots.
- [ ] Render status rows and empty state.
- [ ] Register via verified MainTab convention.
- [ ] Build package 03.

### Task 7: Polish Foundation dashboard (P4)

**Files:**
- Modify: `mods/01-Rimconemy-Foundation/Source/UI/FoundationDashboard.cs`

**Produces:** header/profile badge, stat-card row, collapsible sections, tokenized event chips, and preserved existing diagnosis/detail behavior.

- [ ] Add deterministic section state and card layout.
- [ ] Preserve all existing information and fallbacks.
- [ ] Build Foundation.

### Task 8: Add UI regression/build gates and review

**Files:**
- Add focused UI contract tests only where they can run through existing bootstrap conventions.
- Update relevant docs/roadmap status if needed.

- [ ] Run `git diff --check` from the actual repository root if available.
- [ ] Build all five packages against RimWorld 1.6 managed assemblies.
- [ ] Run code review and fix concrete findings.
- [ ] Report runtime limitations honestly; do not claim in-game rendering without a fresh RimWorld run.
