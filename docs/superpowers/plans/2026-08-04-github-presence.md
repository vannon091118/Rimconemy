# GitHub Presence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Rimconemy's GitHub landing page useful for players and developers while keeping its dry, ironic author voice and its verified status boundaries.

**Architecture:** Keep `README.md` as the public landing page with a short player-first path followed by a developer path. Move detailed contribution workflow into a focused `CONTRIBUTING.md`; update the HTML and SVG banner copy without changing the visual system or any mod code.

**Tech Stack:** GitHub-flavored Markdown, static HTML/CSS, static SVG, existing Bash scripts and repository documentation.

## Global Constraints

- Do not modify C#, XML defs, project files, or gameplay behavior.
- Use German as the primary public language.
- Preserve dry, self-aware RimWorld humor without hiding technical limitations.
- Treat `CODE`, `DEF`, `COMPILES`, `BOOT`, and `LIVE` as distinct evidence levels.
- Do not claim a complete or fully verified playable overhaul.
- Do not add invented releases, Workshop links, screenshots, videos, or license claims.
- Do not overwrite unrelated working-tree changes.

---

### Task 1: Replace the README with a player-first, developer-second landing page

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: `ROADMAP.md`, `docs/CODE_STATUS.md`, `docs/COMPATIBILITY_MATRIX.md`, package `About.xml` files, `scripts/deploy.sh`, `scripts/runtime_test.sh`.
- Produces: public sections linking to the existing canonical technical documents and commands.

- [ ] **Step 1: Write the public structure and copy**

Use this section order:

1. Banner, title, status callout, short author-voice pitch.
2. `Was ist Rimconemy?`
3. `Was erwartet dich?`
4. `Aktueller Stand: ehrlich, weil die Kolonie schon genug lügt`.
5. `Installation für Spieler` with requirements, load order, and compatibility link.
6. `Die fünf Pakete` with short player-facing descriptions and evidence caveats.
7. `Roadmap` with `Jetzt`, `Als Nächstes`, and `Später, wenn die Infizierten zustimmen`.
8. `Für Entwickler` with a concise workflow and links.
9. `Dokumentation` and a dry closing line.

Use verified public facts:

- Target RimWorld `1.6.4566`.
- Harmony is required by every package.
- Foundation declares Anomaly and Odyssey as hard dependencies.
- Royalty, Ideology, and Biotech are not hard requirements.
- Five packages exist and the local build/runtime boot gates are evidenced.
- Full Save/Load, map changes, complete event/raid execution, and complete gameplay loops remain open according to `docs/CODE_STATUS.md` and `ROADMAP.md`.

Use concise status tables rather than reproducing the internal backlog. Link to the detailed documents instead of copying them.

- [ ] **Step 2: Check links and claims against repository files**

Confirm every command, path, package name, dependency, and status statement against the current files. Remove any sentence that cannot be supported by a repository source.

- [ ] **Step 3: Review the README for tone and audience**

Check that the first screen serves players, that developer details are below the player path, that humor is dry rather than noisy, and that no joke obscures a requirement or open gate.

### Task 2: Add focused contribution guidance

**Files:**
- Create: `CONTRIBUTING.md`

**Interfaces:**
- Consumes: `ROADMAP.md`, `docs/CODE_STATUS.md`, `docs/DECISIONS.md`, `docs/INTERFACE_CONTRACT.md`, `docs/SAVE_CONTRACT.md`, `docs/COMPATIBILITY_MATRIX.md`, `scripts/deploy.sh`, `scripts/runtime_test.sh`.
- Produces: developer setup, validation commands, architectural guardrails, and a PR checklist.

- [ ] **Step 1: Document setup and validation commands**

Include the local RimWorld 1.6.4566 requirement, .NET SDK, Harmony assemblies, the canonical build/deploy command, the static runtime gate, and the full runtime gate caveat. Explain that a successful compile is not a live gameplay proof.

- [ ] **Step 2: Document project guardrails**

Cover package ownership, Foundation service contracts, real physical storage as the resource source, deterministic state transitions, save schema/versioning, DLC policy, and the rule against duplicating ledgers or inventing unverified APIs.

- [ ] **Step 3: Add a concise contribution checklist**

Require focused changes, relevant regression tests, build validation, status updates for public claims, and explicit documentation of remaining runtime gaps. Keep the tone welcoming and lightly ironic.

### Task 3: Update public banner copy without redesigning it

**Files:**
- Modify: `banner.html`
- Modify: `banner.svg`

**Interfaces:**
- Consumes: current five-package scope and target platform from package metadata.
- Produces: consistent static/HTML banner copy for the README and linked banner page.

- [ ] **Step 1: Update HTML copy**

Change the label to `RIMWORLD 1.6 · PRE-ALPHA`, use a concise ironic tagline about growth creating attention, keep all five module chips, and change the version line to a status-oriented label rather than a stale hard-coded package version.

- [ ] **Step 2: Mirror the copy in SVG**

Make the README image agree with the HTML banner. Preserve geometry, colors, and decoration. Do not introduce external assets.

### Task 4: Validate and review the documentation changes

**Files:**
- Validate: `README.md`, `CONTRIBUTING.md`, `banner.html`, `banner.svg`, `docs/superpowers/specs/2026-08-04-github-presence-design.md`, `docs/superpowers/plans/2026-08-04-github-presence.md`

- [ ] **Step 1: Run Markdown/link and shell sanity checks available in the repository**

Run:

```bash
bash -n scripts/deploy.sh scripts/runtime_test.sh
python3 - <<'PY'
from pathlib import Path
for name in ("README.md", "CONTRIBUTING.md"):
    text = Path(name).read_text()
    assert text.startswith("# "), name
    assert "Pre-Alpha" in text or "PRE-ALPHA" in text, name
print("documentation sanity: PASS")
PY
```

Expected: shell syntax succeeds and the documentation sanity script prints `documentation sanity: PASS`.

- [ ] **Step 2: Inspect the diff for scope safety**

Run `git diff -- README.md CONTRIBUTING.md banner.html banner.svg` and confirm no code, defs, unrelated local changes, or generated artifacts are included.

- [ ] **Step 3: Run code review and address findings**

Request review of the documentation changes. Resolve factual, link, tone, and scope issues before reporting completion.
