# Runtime Test Extension Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the ad-hoc RimWorld start/log check with one repeatable script that deploys the current mods, starts RimWorld for a bounded interval, and evaluates fresh runtime gates.

**Architecture:** `scripts/runtime_test.sh` wraps the canonical `scripts/deploy.sh`, records the Player.log signature before and after startup, and evaluates absence/presence gates against the newly written current log. Static installed-mod checks run independently; optional flags allow no-deploy, no-build, skip-start, custom paths, and stricter runtime-test requirements.

**Tech Stack:** Bash, coreutils (`timeout`, `stat`, `find`), ripgrep when available, existing `scripts/deploy.sh`, RimWorld 1.6 Linux installation.

## Global Constraints

- Do not modify RimWorld Core or DLC files.
- Use `scripts/deploy.sh` as the only deployment implementation.
- Never treat an old Player.log as fresh runtime evidence.
- Exit non-zero on failed mandatory gates.
- Keep optional game-state gates explicit because a boot-only run cannot prove Save/Load or FinalizeInit behavior.

---

### Task 1: Add the canonical runtime script

**Files:**
- Create: `scripts/runtime_test.sh`

- [x] Define safe defaults for project root, RimWorld base, Mods path, Player.log path, timeout, and report directory.
- [x] Support `--no-deploy`, `--no-build`, `--skip-start`, `--timeout SEC`, `--game PATH`, `--log PATH`, `--report-dir PATH`, `--require-scenario-tests`, and `--help`.
- [x] Validate required commands and paths before deployment/start.
- [x] Run `scripts/deploy.sh` unless `--no-deploy` is selected.
- [x] Verify all five installed Rimconemy `About.xml` files, package IDs, supported versions, and DLLs after deployment.
- [x] Record the current log inode/size/mtime before startup and require a changed current log after startup.
- [x] Run RimWorld with `timeout`; accept timeout termination as expected, but fail on immediate startup errors.
- [x] Fail on abstract Need, Sandbox Scenario config, invalid PatchOperationTest, Market Scribe, Rimconemy exception/error/failure patterns.
- [x] Require FullOverhaul, five registered packages, and all boot regression summaries with zero failures.
- [x] Keep ScenarioContract/FinalizeInit checks optional via `--require-scenario-tests`.
- [x] Write a compact timestamped report and return a meaningful exit status.

### Task 2: Validate the script without a game mutation

**Commands:**

```bash
bash -n scripts/runtime_test.sh
bash scripts/runtime_test.sh --help
bash scripts/runtime_test.sh --skip-start --no-deploy --report-dir /tmp/rimconemy-runtime-test
```

Expected: syntax/help succeed; static installed-mod gates pass or report the exact missing artifact without starting RimWorld.

### Task 3: Review and runtime verification

- [x] Run the code reviewer over `scripts/runtime_test.sh`.
- [x] Fix concrete shell/runtime issues only, including deploy-before-static-gate ordering.
- [ ] Run the script with deployment and bounded startup when the local runtime gate is requested.
- [x] Report separately which boot gates passed and which Save/Load/gameplay gates were not exercised.
