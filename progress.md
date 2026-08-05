# Progress — Rimconemy

## 2026-08-05 — Dead-Code-Sprint A (ausgeführt, committet, gepusht)

- Dead-Code-Audit `docs/falsification/deadcode-audit-2026-08-05.md` — 16 Kandidaten, 3 Root-Cause-Muster (Stub-first ohne Consumer, LogMarker als Lebenserhaltung, Alias ohne Cleanup), Delete-vs-Wire-Matrix, 5-Schritt-Extraktions-Pattern für Klassen-Monolithen + priorisierter Backlog (StoryState-Core, StoryEventCatalog→XML).
- Sprint A umgesetzt (Delete-Gate vom User freigegeben): entfernt `IncidentStub` (05), `MechadroidUnit` + `MechadroidJobRegistry` (05; `MechadroidJobs.cs`/`MechadroidJobLedger` bleibt), `TerritoryNode` (04), `SilverMaterial.cs`/`SilverLedger`/`SilverService`/`SilverGameComponent` (04, Doku-Widerspruch „never silver"), `OutpostStub`-Alias (04), `tmp-scribe-inspect/`, `.runtime-reports/` aus Git (41 Dateien) + gitignore.
- Bootstrap-LogMarker auf gelebte Klassen umgestellt (`Outpost.LogMarker`, InfectedRaidWorker/MechadroidJobLedger-Strings).
- SSOT-Doku nachgezogen: ROADMAP §6, CODE_STATUS, P6-PROGRESS Task 13, DECISIONS, CANONICAL_VANILLA_DOMAIN_MAP, falsification/README + infected__MechadroidJob.md.
- Versionen: 04 → 0.0.28, 05 → 0.0.41. Gates: Build 5/5 grün, `runtime_test.sh --skip-start` PASS (5/5).
- Hinweis: Worktree wurde nach der ersten Ausführung neu erstellt → Sprint A am 2026-08-05 erneut ausgeführt (identische pre-Hashes im Delete-Log §7.1).

## 2026-08-04 (early)

- Persisted implementation plan in `ROADMAP.md §9.4` (integriert, Plan-Datei gelöscht).
- Added shared tokens: `DangerSoft`, `PanelInk`, `DividerInk`.
- Added toolkit helpers: stat cards, sparklines, inline tabs, countdowns, pressure gauges.
- Migrated Survival dashboard and ProgressionPawnTab to Foundation UI bases/tokens.
- First build: Foundation passed; Survival failed because local RimWorld 1.6 `GameFont` has no `Large` member.
- Resolution: mapped H1 presentation to supported `GameFont.Medium`; rerun Survival build next.

## 2026-08-04 (late) — Schema-Migration als First-Class-Domain

- `ISchemaMigratable`-Interface in `Foundation/Source/Save/` definiert — löst Open-Coded-Switch ab.
- `SchemaStep` (Action-Closure, FromVersion<ToVersion-Validation), `MigrationStepWalker` (OHNE try/catch, propagiert Exceptions), `MigrationRegistry` (string-keyed, idempotent Register, sortiertes MigrateAll, Clear am Save-Start), `SchemaMigratableExtensions.RunMigration` (DRY-Orchestration).
- 4 Migrators implementieren das Interface: `FoundationSaveData` (01), `CharacterSetupState` (02), `StoryState` (05), `CreditsLedger` (04).
- `FoundationDashboard.cs` stale `LoadedSchemaVersion`-Reference auf `saveData.SchemaVersion` gefixt.
- `ScribeRoundTripHelper.RoundTrip<T>(IExposable)` in `Foundation/Tests/` — echter Scribe-Save→Load-PostLoadInit-Cycle via MemoryStream.
- `Tests/CharacterSetupStateSchemaBumpTests.RunAll` mit 6 T1–T6-Assertions plus `Tests/StoryStateSchemaBumpTests`, `Tests/CreditsLedgerSchemaBumpTests`.
- `docs/falsification/survival__SaveMigration.md` (236 Z.) — standalone Falsifizierungsbericht für Phase-2.8 im Foundation-7-Sektion-Layout.
- Audit `§J rows 23–27` dokumentieren die Liefer-Bestandteile (Interface, Walker, Registry, Extension, Clear-Trigger, Stale-Reference-Fix). F2/F4 um First-Class-Domain-Verweis erweitert.
- `MigrationRegistry.GetMigrationLog()` ist vorbereitet für Dashboard-Integration (unified Cross-Package-Save/Load-Report).
