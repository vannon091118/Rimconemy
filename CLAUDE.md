# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Rimconemy is a **modular RimWorld 1.6.4566 overhaul** split into 5 independent packages. Each package compiles independently with no project references between them. Cross-package communication uses a Capability system registered in Foundation (01).

**Packages:**
| Num | Package | Responsibility |
|-----|---------|---------------|
| 01 | Foundation | Registry, Diagnostics, Capabilities, Save-Metadata, UI Toolkit |
| 02 | Survival & Progression | Pawn setup, Needs, XP/Progression, Character Setup, Game-Over |
| 03 | Scavenger Infrastructure | Physical storage, Power chains, Building snapshots, Bauschutt |
| 04 | Economy & Territory | Credits wallet, Markets, Outposts, Territory |
| 05 | Infected & Automation | Story events, Threat, Infected, Mechadroids, Ideology adapters |

## Build & Development Commands

### Canonical Build & Deploy
```bash
# Build & deploy all 5 packages to RimWorld Mods folder
./scripts/deploy.sh

# Build & deploy single package (01-05)
./scripts/deploy.sh 03

# Deploy only (skip build)
./scripts/deploy.sh --no-build
./scripts/deploy.sh 02 --no-build
```

### Runtime Verification
```bash
# Full runtime gate: build, deploy, start RimWorld, verify fresh Player.log
./scripts/runtime_test.sh

# Static check only (no game start) - verifies installed mod artifacts
./scripts/runtime_test.sh --skip-start --no-deploy

# Require scenario contract tests in log
./scripts/runtime_test.sh --require-scenario-tests
```

### Development Helpers
```bash
# Bump VERSION file for a package (run after any code/def/XML change)
./scripts/bump_version.sh 03

# Quick dev test (build + static check)
./scripts/dev_quick_test.sh
```

### Local Paths
- RimWorld install (default): `/home/vannon/GOG Games/RimWorld/game/`  
  (Check `scripts/deploy.sh` and adjust if your installation differs)
- Mods source: `/home/vannon/Schreibtisch/Rimconemy/mods/`
- Runtime reports: `/home/vannon/Schreibtisch/Rimconemy/.runtime-reports/`

## Architecture Principles

### Package Isolation
- **No project references** between packages 01-05
- Cross-package communication via **Capability Registry** (Foundation)
- DLL references only from dependent packages → Foundation (`Assemblies/Rimconemy.Foundation.dll`)
- `About.xml` of dependent packages declares `<loadAfter>rimconemy.foundation</loadAfter>`

### Capabilities (from INTERFACE_CONTRACT.md §2)
Each package registers versioned capabilities. Other packages call `CapabilityAudit.HasCapabilityOrWarn(...)` before reading.

| Capability ID | Owner | Consumers |
|---------------|-------|-----------|
| `rimconemy.foundation.profile` | 01 | all |
| `rimconemy.foundation.colonials` | 01 | 02, 03, 05 |
| `rimconemy.survivalprogression.needs` | 02 | 05 |
| `rimconemy.survivalprogression.progression` | 02 | 04, 05 |
| `rimconemy.survivalprogression.gameover` | 02 (sole) | 05 (write pending only) |
| `rimconemy.scavengerinfrastructure.resources` | 03 | 04, 05 |
| `rimconemy.scavengerinfrastructure.power` | 03 | 02 |
| `rimconemy.economyterritory.wallet` | 04 | 02, 05 |
| `rimconemy.economyterritory.market` | 04 | 02 |
| `rimconemy.infectedautomation.threat` | 05 | 02, 03 |
| `rimconemy.infectedautomation.automation` | 05 | 02 |

### DLL Reference Topology (INTERFACE_CONTRACT §9.3)
```
01 (Foundation)
   ↑ (DLL-Ref)
   ├── 02 (SurvivalProgression)
   ├── 03 (ScavengerInfrastructure)
   └── 05 (InfectedAutomation) 
            ↑ (DLL-Ref, via F-V3)
            └── 03 (StorageQuery)
(Economy 04: late-bound reflection only, no DLL-Ref)
```

### Harmony Strategy (INTERFACE_CONTRACT §10)
**Minimize Harmony** — prefer native anchors:
1. `Defs` / `PatchOperation-XML` (highest)
2. `[StaticConstructorOnStartup]` 
3. `GameComponent` / `WorldComponent`
4. `[HarmonyPrefix]` / `[HarmonyPostfix]` (only when 1-3 insufficient)
5. Transpiler (last resort)

**Active patch:** `Page_ConfigureStartingPawnsBioPatch` (02, Postfix) — only way to intercept pre-first-UI-render.

### Save & Migration (SAVE_CONTRACT.md)
All persistent state implements `ISchemaMigratable` (Foundation). MigrationRegistry + MigrationStepWalker orchestrate migrations.

| Package | Save Class | Schema Version |
|---------|------------|----------------|
| 01 | `FoundationSaveData` | 1 |
| 02 | `CharacterSetupState`, `ProgressionGameComponent` | 1 |
| 03 | `StorageSnapshot` (reconstructed from maps, no own save) | 1 |
| 04 | `CreditsLedger`, `Market`/`MapMarketComponent` | 1 |
| 05 | `StoryState` | 1 |

**Stop-gates:** No silent data loss. Migration or controlled rejection with log message.

### Determinism Rules (ROADMAP.md)
- Explicit seed or deterministic selection ID
- Stable sorting of candidates
- No system time as game input
- No background threads for game state
- Store selection reason + input snapshot
- Idempotency key per event execution

## Key Documentation

| File | Purpose |
|------|---------|
| `README.md` | Player & developer entry point |
| `ROADMAP.md` | Master plan, phase hierarchy, backlog |
| `docs/CODE_STATUS.md` | Evidence boundary: CODE/DEF/COMPILES/BOOT/LIVE per package |
| `docs/INTERFACE_CONTRACT.md` | Capabilities, package boundaries, cross-package contracts |
| `docs/SAVE_CONTRACT.md` | `ISchemaMigratable`, migration patterns, envelopes |
| `docs/DECISIONS.md` | Architectural decisions with rationale |
| `docs/COMPATIBILITY_MATRIX.md` | RimWorld/DLC compatibility |
| `docs/ARCHITECTURE.md` | SSOT orient linking all spec docs (H1-H6) |
| `docs/H1-api-def-gate.md` | Vanilla API signatures & status |
| `docs/H2-story-contract.md` | Story writer, events, selection |
| `docs/H3-ideology-influence-matrix.md` | Setting rules → Ideology adapters |
| `docs/H4-storage-query-contract.md` | `StorageSnapshot` / `StorageQuery` |
| `docs/H5-character-setup-formula.md` | Skill budget, trait assignment, bio remap |
| `docs/H6-pawn-generator-api-spike.md` | PawnGenerator API verification |
| `mods/*/BLUEPRINT.md` | Per-package technical boundaries & ownership |
| `docs/falsification/` | Falsification reports (A-G evidence blocks) |

## Current Status (2026-08-04)

**All 5 packages:** COMPILES, BOOT ✅  
**Not yet LIVE:** Save/Load roundtrip, full raid resolution, complete gameplay loops

**Verified gates (`runtime_test.sh`):**
- RimWorld 1.6 runtime marker
- Foundation bootstrap complete
- FullOverhaul profile detected
- 5 packages registered
- 30+ regression test summaries (CapabilityGate, ColonialReader, CrossPackageState, EventLog, Profile, BioRemap, NeedMapping, CreditsLedger, Market, StorySelector, StoryState, Building progression/persistence/core/input, Physical transfer, Outpost investment, Threat, Mechadroid, Campfire, Bauschutt, ArrowTurret, CoalChain, StainlessSteel)

**Open Live Gates (CODE_STATUS.md §4):**
1. Save → quit → reload with unchanged state
2. Event selection → queue → worker/letter → actual raid
3. Map change, caravan/temporary maps, unloaded storage
4. Full infected spawn (not letter-only)
5. Full ideology influence matrix
6. Complete scavenger build/farm/water/tower mechanics
7. Economy WorldObject/transfer/territory lifecycle

## Common Development Workflow

1. **Make changes** within package boundaries (respect ownership)
2. **Bump version**: `./scripts/bump_version.sh <pkg>`
3. **Build & deploy**: `./scripts/deploy.sh <pkg>` or `./scripts/deploy.sh --all`
4. **Run static check**: `./scripts/runtime_test.sh --skip-start --no-deploy`
5. **Run runtime gate**: `./scripts/runtime_test.sh`
6. **Check report** in `.runtime-reports/runtime-<timestamp>.txt`

## Important Rules

- **Never commit** generated assemblies, local logs, or save files
- **Package boundaries** are strict: new logic goes in the owning package
- **Public APIs** need INTERFACE_CONTRACT.md entry
- **Save state** needs schema version + migration path + Scribe roundtrip test
- **API assumptions** must be verified against local RimWorld 1.6 assemblies (vanilla-api-matrix-1.6.md)
- **No parallel truths**: physical resources read from StorageQuery; credits separate wallet; UI/story/economy share snapshot sources