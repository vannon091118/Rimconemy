# UI Honest Character Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make Character Setup a real, persistent, user-visible vertical slice and clearly label every dashboard feature that is still read-only, scaffolded, or not live.

**Architecture:** Keep simulation ownership in existing package services. Add a small Foundation UI helper for semantic feature-status banners, wire Character Setup completion into the existing `CharacterSetupState`, and expose a truthful persisted summary in the Skill Budget window. Do not turn read-only dashboards into fake gameplay controls.

**Tech Stack:** RimWorld 1.6.4566, Unity IMGUI, C# netstandard2.1, existing Foundation UI toolkit, package-local bootstrap regression tests.

## Global Constraints

- Preserve RimWorld 1.6 API compatibility and package load boundaries.
- Do not introduce third-party dependencies or unverified generator/incident APIs.
- Do not overwrite unrelated existing working-tree changes.
- Character Setup must remain deterministic, idempotent, and cost-capped at 30 points.
- Save state must use the existing `CharacterSetupState` / `PawnSetupRecord` contract.
- UI must distinguish `LIVE`, `READ-ONLY`, `PREVIEW`, and `UNAVAILABLE` states with text, not color alone.
- Do not claim a runtime Save/Load or in-game rendering proof without running the relevant gate.

---

### Task 1: Add shared truthful feature-status rendering

**Files:**
- Modify: `mods/01-Rimconemy-Foundation/Source/UI/RimconemyUi.cs`
- Modify: `mods/01-Rimconemy-Foundation/Source/UI/RimconemyTheme.cs` only if a missing semantic token is required

**Produces:** `RimconemyUi.DrawFeatureStatus(Rect rect, string label, string detail, StatusLevel level)` that always renders a textual state label and explanatory detail, restoring GUI state afterward.

- Use existing theme colors and `StatusLevel`; do not add package-specific colors.
- The helper must be safe when translated strings are missing and accept plain fallback text.
- Add no simulation behavior.

### Task 2: Make Character Setup completion persistent and inspectable

**Files:**
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetupState.cs`
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/Character/SkillBudgetWindow.cs`
- Modify: `mods/02-Rimconemy-Survival-Progression/Tests/CharacterSetupStateRegressionTests.cs`
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/Bootstrap.cs` only if the new regression runner must be registered

**Produces:**
- A null-safe `CharacterSetupState.RecordAppliedPawns(IEnumerable<Pawn> pawns)` method.
- The method records each pawn through `new PawnSetupRecord(pawn)`, sets `Applied=true`, and is idempotent for repeated application.
- `SkillBudgetWindow` records the affected starting pawns after both Apply and implicit close/default application, then shows a clear status line before applying: `Entwurf — noch nicht gespeichert`.
- A persisted state line after reload is available through `CharacterSetupState.Get()` and can be tested without a live Pawn.

**Test invariants:**
- Empty/null pawn collections do not throw and do not mark state applied.
- Recording the same pawn twice replaces its record without duplicating dictionary entries.
- A valid record stores age, skills, traits, and `Applied=true`.

### Task 3: Mark non-live package surfaces honestly

**Files:**
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/UI/SurvivalProgressionDashboard.cs`
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Source/UI/InfrastructureDashboard.cs`
- Modify: `mods/04-Rimconemy-Economy-Territory/Source/Wallet/EconomyHub.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/UI/ThreatDashboard.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/UI/SettingRulesInspector.cs`

**Produces:** Each screen displays a visible, textual capability banner near its header:
- Survival: `LIVE: Bedürfnisse/Progression-Read-Model` plus `OPEN: vollständige Gameplay-Loops und Save/Load-Live-Gate`.
- Infrastructure: `READ-ONLY: Status-Snapshot` plus `OPEN: echte Verbrauchs-/Bau-/Power-Mutationen`.
- Economy: `PARTIAL: Wallet/Markt-Daten` plus `OPEN: physische Transfers und Weltkartenlogistik`.
- Threat: `READ-ONLY: Story-/Threat-Snapshot` plus `OPEN: echter Raid-Spawn und vollständige Eventauflösung`.
- Setting Rules: `READ-ONLY: Regelkatalog` plus `OPEN: vollständige native Ideology-Verhaltensbindung`.

Do not add buttons to these screens unless an existing method already performs a real, persisted action. Existing demo trade actions remain explicitly labeled as demo/fallback behavior rather than being presented as a complete economy.

### Task 4: Validate, review, and document limits

**Files:**
- Modify: `docs/CODE_STATUS.md` only if the implemented Character Setup persistence evidence changes its stated status
- Modify: `progress.md` only with concise progress evidence if appropriate

- Run `git diff --check`.
- Build Foundation and Survival with local RimWorld/Harmony references; build other changed packages if references are available.
- Run package-local regression tests through the established bootstrap/test mechanism where possible.
- Spawn code-reviewer-luna after edits and resolve critical/important findings.
- Report that interactive rendering and real Save/Load remain runtime gates unless actually executed.
