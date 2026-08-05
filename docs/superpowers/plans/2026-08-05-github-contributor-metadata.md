# GitHub Contributor and Metadata Documentation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the contributor entry point German-first with a complete collapsed English translation and publish copy-paste-ready GitHub description, topics, and bilingual release notes.

**Architecture:** Keep `CONTRIBUTING.md` as the canonical contributor guide with German visible by default and English inside one `<details>` block. Add `docs/GITHUB_RELEASE_METADATA.md` as a non-authoritative copy deck for repository settings and releases; it must link back to the README, roadmap, and evidence boundary without changing GitHub remotely.

**Tech Stack:** GitHub-flavored Markdown, existing Bash scripts, existing Rimconemy documentation.

## Global Constraints

- RimWorld target remains `1.6.4566`.
- `COMPILES` and `BOOT` must never be presented as `LIVE`.
- German is the visible default; English is complete and collapsible.
- No GitHub API, remote settings, commit, or push is performed by this task.
- No generated assemblies, logs, save files, or build output are added.

---

### Task 1: GitHub metadata and release copy

**Files:**
- Create: `docs/GITHUB_RELEASE_METADATA.md`

- [ ] Add a concise German and English repository description.
- [ ] Add a restrained topic list covering RimWorld, C#, modding, survival, economy, automation, and the project’s modular architecture.
- [ ] Add short German and English pre-alpha release text with evidence-boundary wording and links.
- [ ] Add instructions explaining that the file is copy-paste metadata, not a second status source.

### Task 2: Bilingual contributor guide

**Files:**
- Modify: `CONTRIBUTING.md`

- [ ] Preserve the existing German contributor rules as the visible primary guide.
- [ ] Refine the build, architecture, save, determinism, DLC, and pull-request sections without changing their factual scope.
- [ ] Add a complete English translation inside one collapsed `<details>` block.
- [ ] Give English navigation unique manual anchors so it does not collide with German headings.
- [ ] Link the metadata copy deck from both language sections.

### Task 3: Validation

**Files:**
- Validate: `CONTRIBUTING.md`, `docs/GITHUB_RELEASE_METADATA.md`

- [ ] Run `git diff --check`.
- [ ] Verify exactly one balanced English `<details>` block in `CONTRIBUTING.md`.
- [ ] Verify German content precedes English content.
- [ ] Verify all local Markdown links used by the changed docs exist.
- [ ] Check that no changed documentation claims `LIVE` for an open gate.
