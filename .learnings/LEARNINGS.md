# LEARNINGS (continued)

## LRN-20260806-005 — Skill_Seekers generiert generische Codebase-Skills, keine Domain-Skills
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P1
- **Status**: resolved
- **Area**: tool
- **Description**: `skill-seekers create --directory .` extrahiert Patterns (Adapter 10×, Observer 9×), Service Layer, Unity-Framework, aber kennt **keine** Rimconemy-Domain: keine 5-Pakete-Isolation, keine Phase-First-Architektur, keine Evidence-Tiers, keine Infected Horde, kein Growth=Attention.
- **Remediation**: Skill_Seekers-Output als `references/skill_seekers_scan.md` archivieren. Domain-Skill **manuell kuratieren** (SKILL.md) mit Vision, Commands, Conventions, Pitfalls, Live-Gates.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/SKILL.md`, `/home/vannon/AGENTS.md`

## LRN-20260806-006 — Repo hat zwei "Exploration Tools" die für API-Verifikation kritisch sind
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P2
- **Status**: open
- **Area**: doc
- **Description**: `tools/inspect/` enthält 3 Spike-Projekte (TypeScanner, FinalSpike, Phase8Construction) + `TypeScanner.cs` (Mono.Cecil). Diese scannen `Assembly-CSharp.dll` für Vanilla-API-Signaturen. Wird in H1–H6 API-Spikes referenziert. `live-test-monitor-workspace/` war in früherem ls sichtbar, scheint aber gelöscht/verschoben.
- **Remediation**: In Skill + AGENTS.md unter "Exploration Tools" dokumentieren. Für neue Vanilla-Hooks: erst TypeScanner laufen lassen, dann H-Spike schreiben.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/SKILL.md` (Exploration Tools section), `/home/vannon/AGENTS.md` (Project Structure)

## LRN-20260806-007 — Phase-First ≠ Feature-First ist die zentrale Architektur-Entscheidung
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P0
- **Status**: open
- **Area**: architecture
- **Description**: ROADMAP.md + PHASE_PROGRESSION_CONTRACT.md definieren **wann** Systeme spielerisch relevant werden (6 Phasen), nicht **was** sie tun. Ressourcen-Matrix (§2) ist SSOT für Verfügbarkeit. DLCs = Adapter hinter `DLCFilter`. Negative-Regeln (§5) verbieten z.B. Coal-Rezepte in EarlySurvival.
- **Remediation**: Jede neue Feature-Implementierung **muss** Phase-Zuordnung haben und Availability-Stage (Visible/Lootable/Producible/Strategic) einhalten. In Skill + AGENTS.md prominent machen.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/SKILL.md` (Key Conventions #9), `/home/vannon/AGENTS.md` (Vision block)

## LRN-20260806-008 — Evidence Tiers sind Non-Negotiable: CODE ≠ LIVE
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P0
- **Status**: open
- **Area**: process
- **Description**: CODE_STATUS.md definiert 6 Stufen. Kein Dokument darf `CODE`/`DEF`/`COMPILES` als `LIVE` verkaufen. 33 Falsifikationsberichte fordern A–G Beweisblöcke. Ohne `SURVIVED` mit A–G = keine Übergabe.
- **Remediation**: Bei jeder Behauptung "Feature X fertig" → Evidence Tier nennen. In Skill + AGENTS.md als Conventions #7 + Pitfall.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/SKILL.md`, `/home/vannon/AGENTS.md`

## LRN-20260806-009 — Vertikale Scheibe "Die erste Nacht" ist der kritische Pfad
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P0
- **Status**: open
- **Area**: planning
- **Description**: ROADMAP.md §9 + CODE_STATUS.md §4: 12 Phasen, 37 Subtasks. Erfordert: Single Survivor → Campfire → Tier-1 Barricade → 1 Nacht → Save/Load ohne Drift. Erst **danach** Kohle, T2-Hochofen, Automation. Horizontales Fertigstellen einzelner Pakete ist **falsch**.
- **Remediation**: Nächster Sprint = Fog-of-War (Sprint 2) + Save/Load-Roundtrip + Story-Event-Feuerung live. In Skill unter "Empfohlener Nächster Sprint" + Live Gates.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/SKILL.md`, `/home/vannon/AGENTS.md`

## LRN-20260806-010 — Package-Isolation via CapabilityRegistry + Reflection ist das einzige Cross-Package-Pattern
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P1
- **Status**: open
- **Area**: architecture
- **Description**: Package 01 kennt 02-05 zur Compile-Time NICHTS. Runtime: `CapabilityRegistry.GetCapability<T>()`, `DefDatabase<ThingDef>.GetNamedSilentFail()`. Defs: Owner-Paket besitzt kanonische Def-Datei. Andere patchen **nur** additiv via `<PatchOperationFindMod>`. Save: Jedes Paket eigenes `ISchemaMigratable`.
- **Remediation**: In Skill unter "Package Ownership & Interface Contract" + "Cross-Package Rules" detailliert. AGENTS.md Conventions #1.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/references/package_ownership.md`, `/home/vannon/.hermes/skills/rimconemy/SKILL.md`

## LRN-20260806-011 — Infected Raid Worker ist Loop-Closure Exception (Phase-1 MVP)
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P2
- **Status**: open
- **Area**: code
- **Description**: `InfectedRaidWorker.cs` Zeile 35-49: Mod 05 dokumentiert als "read-only" für Mod-03/-04 Reads. **Pawn-Spawning auf Colony Map ist bewusste Phase-1 Loop-Closure Exception**. Capability-Gate `rimconemy.infectedautomation.incident.spawn` für Phase-2-Verstärkung vorbereitet.
- **Remediation**: Nicht revertieren ohne Phase-2-Design. In Skill unter Infected Package Ownership notieren.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/references/package_ownership.md`

## LRN-20260806-012 — HordeCalculator ist Pure-Logic mit einziger Live-Exception
- **Timestamp**: 2026-08-06T03:25
- **Priority**: P2
- **Status**: open
- **Area**: code
- **Description**: `HordeCalculator.cs`: `GetEffectiveCount`, `IsActive`, `ComputePulsePhase` sind **Pure** (deterministisch aus Inputs). Nur `IsActiveNow()` liest Live-Ledger + Profil — shared gate für 3 Render-Paths (SectionLayer, BurstLayer, CameraOverlay). PulseCycleTicks = 120 (2 in-game Sekunden).
- **Remediation**: Bei Horde-Änderungen: Pure-Logik nicht mit State vermischen. Render-Paths nutzen nur `ComputePulsePhase(currentTick)`.
- **Promoted to**: `/home/vannon/.hermes/skills/rimconemy/SKILL.md` (Infected Package details)