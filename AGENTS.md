# AGENTS.md — Rimconemy (RimWorld 1.6 Mod Suite)

Rimconemy is a 5-package RimWorld 1.6 mod suite (Foundation, Survival, Scavenger, Economy, Infected) built with C# (.NET Standard 2.1) and XML Defs. The project uses Harmony for patches and enforces package isolation via capability contracts. All build/test commands are scripted in `scripts/`.

> **Vision**: Total Overhaul → Survival RPG + Base Builder mit **Infected Horde** (Zombies sammeln sich über Zeit, bilden Horden, infizieren Tiere, jagen) + **Faction Territory War** (Überlebende Fraktionen kämpfen um Rohstoffe, die einem helfen, sich besser zu verteidigen) + **Growth = Attention** (Wachstum zieht Verteidigung nach sich).
>
> **Status**: Phase 0-4 CODE/DEF/COMPILES/BOOT belegt. Phase 5-6 (Gameplay-Schichten) OPEN. `runtime_test.sh`: PASS (35+ Summaries, 0 Failures).

---

## Dev Environment

- **Runtime**: RimWorld 1.6.4566 (GOG install at `/home/vannon/GOG Games/RimWorld/game/`)
- **SDK**: .NET SDK (netstandard2.1 target)
- **Dependencies**: Assembly-CSharp.dll, UnityEngine.* DLLs, 0Harmony.dll from RimWorld Managed + Harmony Mod folders
- **Key env vars for build**: `RimWorldManagedPath`, `HarmonyAssembliesPath` (passed via `-p:` to `dotnet build`)

---

## Build & Test Commands

| Task | Command |
|------|---------|
| Build & deploy all 5 packages | `./scripts/deploy.sh` |
| Build & deploy single package | `./scripts/deploy.sh 03` |
| Deploy only (no build) | `./scripts/deploy.sh --no-build` |
| Static gate check (no game start) | `./scripts/runtime_test.sh --skip-start --no-deploy` |
| Full runtime gate (build + deploy + 90s game + log verify) | `./scripts/runtime_test.sh` |
| Bump package version (+0.0.1) | `./scripts/bump_version.sh 01` |
| Bump all 5 packages at once | `./scripts/bump_version.sh --all` |
| Fast static check (XML, no game start) | `./scripts/dev_quick_test.sh [--strict]` |
| Verify bootstrap log invariants | `./scripts/verify_bootstrap_log.sh <Player.log>` |

**Build details**: Each package has a `.csproj` referencing RimWorld assemblies via `-p:RimWorldManagedPath=.../RimWorldLinux_Data/Managed -p:HarmonyAssembliesPath=.../Harmony/Current/Assemblies`. Output goes to `Assemblies/`.

---

## Project Structure

```
/home/vannon/Schreibtisch/Rimconemy/
├── mods/
│   ├── 01-Rimconemy-Foundation/
│   ├── 02-Rimconemy-Survival-Progression/
│   ├── 03-Rimconemy-Scavenger-Infrastructure/
│   ├── 04-Rimconemy-Economy-Territory/
│   └── 05-Rimconemy-Infected-Automation/
├── scripts/
│   ├── deploy.sh           # canonical build+deploy (rsync --delete)
│   ├── runtime_test.sh     # bounded runtime gate (90s default)
│   ├── dev_quick_test.sh   # fast static check (~30s)
│   ├── verify_bootstrap_log.sh
│   └── bump_version.sh
├── docs/
│   ├── INDEX.md            # entry point — links everything below
│   ├── ARCHITECTURE.md     # top-level architecture
│   ├── CODE_STATUS.md      # evidence tiers: CODE/DEF/COMPILES/BOOT/LIVE
│   ├── DECISIONS.md        # architecture decisions (large, growing log)
│   ├── INTERFACE_CONTRACT.md
│   ├── SAVE_CONTRACT.md
│   ├── COMPATIBILITY_MATRIX.md
│   ├── PHASE_PROGRESSION_CONTRACT.md
│   ├── CANONICAL_VANILLA_DOMAIN_MAP.md
│   ├── H1–H6 *.md           # API / contract spike notes
│   ├── vanilla-api-matrix-1.6.md
│   ├── vanilla-early-blueprint-matrix-1.6.md
│   ├── campfire-parity-1.6.md
│   └── falsification/      # 33 domain falsification reports (A–G evidence)
├── tools/
│   └── inspect/            # Mono.Cecil API scanner (TypeScanner, FinalSpike, Phase8Construction)
├── .learnings/             # Agent self-improvement log (LRN/ERR/FR IDs)
├── ROADMAP.md              # canonical plan + backlog
├── CONTRIBUTING.md
├── README.md
└── AGENTS.md               # this file
```

Each package contains: `Source/` (C#), `Defs/` (XML), `Patches/` (Harmony patch classes), `Languages/` (translation keyfiles), `Textures/` (PNG assets), `Assemblies/` (build output), `About/About.xml`, `BLUEPRINT.md`, `ROADMAP.md`, `Tests/`.

---

## Conventions

- **Package isolation**: Package 01 (Foundation) knows nothing of 02–05 at compile time. Cross-package communication via `CapabilityRegistry` (Foundation), reflection loading, and interface contracts (`docs/INTERFACE_CONTRACT.md`).
- **Versioning**: Every package has a `VERSION` file. Bump via `scripts/bump_version.sh <num>` after any code/def/XML change.
- **No parallel truths**: Physical resources read only from `StorageSnapshot` (Scavenger). Credits are a separate wallet (Economy). UI/Story/Economy share one source.
- **Determinism**: Explicit seeds, stable sort, no system time as game input, idempotency keys per event execution, selection reason + input snapshot stored.
- **Save contracts**: Every persistent state needs schema version, migration path or controlled rejection, Scribe roundtrip tests. Never silently drop data.
- **API verification**: No Harmony patches or Def assumptions from strings alone. Verify signatures against local assemblies (`API-*` spikes in `docs/`).
- **Evidence tiers**: `CODE` (written) → `DEF` (defined) → `COMPILES` → `BOOT` (loads) → `LIVE` (runtime verified). Claim only what's proven.
- **XML Defs**: RimWorld 1.6 structure. Invalid 1.6 fields blocked: `surfacePosition`, `defaultIngredientCount`, `showInInterface`.
- **Load order**: Core → DLCs → Harmony → 01 Foundation → 02 Survival → 03 Scavenger → 04 Economy → 05 Infected

---

## Pitfalls

- **Deploy uses `rsync --delete`**: Target Mods folder (`/home/vannon/GOG Games/RimWorld/game/Mods/`) is mirrored to source. Wrong path = data loss.
- **Hardcoded RimWorld path** in `deploy.sh` and `runtime_test.sh`. If your install differs, edit scripts or use `--no-deploy` + manual copy.
- **Build requires env vars**: `RimWorldManagedPath` and `HarmonyAssembliesPath` must point to valid DLLs. `dotnet build` fails fast with clear errors if missing.
- **Runtime test expects fresh Player.log**: It signatures the log before/after start. If log doesn't change, gate fails.
- **Forbidden log patterns**: `Config error in Rimconemy_Campfire`, XML errors for removed 1.6 fields, `CA9011A3` (abstract Need), Sandbox/Market/Patch errors — all cause runtime gate failure.
- **Required regression summaries**: 35+ specific "X tests: N passed, 0 failed" lines must appear in Player.log for runtime PASS.
- **No prebuilt DLLs in repo**: Must build locally before testing.
- **Harmony Mod required** in RimWorld Mods folder for both build and runtime.
- **Anomaly + Odyssey DLCs required** for Full Overhaul profile detection.
- **PEP 668 (externally-managed Python)**: `pip install` blocked. Use `uv tool install` or `pip install --break-system-packages`.

---

## Hermes Skill

A dedicated Hermes skill lives at `~/.hermes/skills/rimconemy/` with:
- `SKILL.md` — Full workflow, architecture, commands, conventions, pitfalls
- `references/` — Skill_Seekers scan, code status, phase matrix, package ownership, falsification index, live gates

Load with: `skill_view(name='rimconemy')`