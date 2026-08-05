# SDD Progress — Phase A & C COMPLETE ✅

> Modus: Hybrid (Direkt-Implementer + code-reviewer-minimax-m3 zwischen Tasks, basher für Build/Test).
> Branch: main (per User-Approval, 2026-08-05).

| Phase | Tasks | Status | Commit-Range |
|---|---|---|---|
| **Phase A** Population-Ledger SSOT | T1-T8 | ✅ complete | `ef55bd1`..`2c94d2b` |
| **Phase C** Tier-Inokulation | T1-T9 | ✅ complete | `eff6fc6`..`038ca24` |
| Phase B Daily-Growth-Tick | T1-T6 | ✅ complete | `19ba8bb`..`426cd8d` |
| Phase D Horde-Overlay | T1-T? | pending | — |

## Phase A Commits (Detail)

| # | Commit | Task |
|---|---|---|
| 1 | `ef55bd1` | Profile-Multiplier-Tabelle (deterministisch, 3 Profile) |
| 2 | `dabbd34` | PopulationLedger SSOT (Scribe + ISchemaMigratable + 10 Felder) |
| 3 | `7f2d1b3` | Write-API: RegisterKill + DailyGrowth + Revenge + Reset + Overflow-Guard |
| 4 | `7f2d1b3` | DailyGrowth + Revenge-Quote |
| 5 | `d02e392` | Reconciler MapComponent |
| 6 | `d02e392` | NoteInoculation + IsInoculationCooldownElapsed |
| 7 | `5b07829` | Capability-Registrierung + Bootstrap RunAll-Hooks |
| 8 | `2c94d2b` | Bump 0.0.57 → 0.0.58 + runtime_test PASS |

## Phase C Commits (Detail)

| # | Commit | Was |
|---|---|---|
| 1+2 | `eff6fc6` | InoculationCandidate DTO + InoculationSelectorLogic (deterministisch via FNV-1a) |
| 3+6 | `0e27c9c` | InoculationConverter + GetTotalCapBudget (AnimalHalfCap: 1 Tier = 0.5 Cap-Units) |
| 4+5+7 | `0b108185` | RandomInoculationService Façade + Rimconemy_InfectedWildlife PawnKindDef + InfectedPackBehavior |
| 8+9 | `925969b` | StoryDirector Day-Tick-Hook + Bootstrap RunAll (InoculationRegressionTests + InfectedPackBehaviorRegressionTests) |
| 9/10 | `038ca24` | Bump 0.0.58 → 0.0.59 + runtime_test PASS |

## Phase C Akzeptanz-Gate

- [x] **C1** — Tests I1-I10 + P1-P5 grün im Bootstrap-Log.
- [x] **C2** — `RandomInoculationService.TryInfectRandom` statisch kompiliert, Live-Beleg von User zu erbringen.
- [x] **C3** — `Rimconemy_InfectedWildlife` PawnKindDef lädt via Defs/PawnKinds/, Branded, Humanlike=false, Animal-Original bleibt.
- [x] **C4** — `PopulationLedger.GetTotalCapBudget()` API: `Cap - (Human + floor(Animal/2))`.
- [x] **C5** — `InfectedPackBehavior` pure-Helper mit Wandering/Tracking/Dissipating States, kein Assault.
- [x] **C6** — `StoryDirector.GameComponentTick` ruft Service am Day-Tick auf.
- [x] **C7** — `./scripts/runtime_test.sh --skip-start --no-deploy` exit 0.

## What this enables

**Phase A**: Population-Daten sind SSOT-bereit. Phase B/C/D können alle darauf zugreifen.

**Phase C**: Wenn das Spiel läuft, passiert jetzt:
- **Tag-N** (Survival-Profil): wenn `Ticks - LastInoculationTick >= 420.000` (7 Tage)
- StoryDirector.GameComponentTick ruft `RandomInoculationService.TryInfectRandom(homeMap, currentTick)`
- Service: Profile-Quota > 0 ✓, Cooldown-Gate ✓, Candidate-Liste gebaut aus `map.mapPawns.AllPawnsSpawned`
- Selector: deterministisch mit FNV-1a-Seed über Profile+MapId+DayIndex+CapBudget
- Wirft einen Wolf/Bear/Caribou als Tier aus (Wildtier auf Map)
- Faction-Switch → `Rimconemy_HiddenInfectedFaction`, payback: original-kindDef (Wolf) bleibt oder branding zu `Rimconemy_InfectedWildlife` (Def in Defs/)
- PopulationLedger.NoteInoculation stamps: `LastInoculationTick + CumulativeInoculations++`
- InfectedPackBehavior-AI startet: Wandering → (offset 15..25 Tiles) → Tracking → Dissipating

## Nächste Schritte

- **Phase B**: Daily-Growth-Tick-Integration in `StoryDirector` (PopulationLedger.ApplyDailyGrowthTick(), Reset, Revenge-Quote-Aufruf an `InfectedRaidSpawnService`).
- **Phase D**: Horde-SectionLayer-Overlay (>150 Infizierte = pulsierender roter Kreis auf Home-Map).
- **Live-Test**: User-Pflicht: `./scripts/deploy.sh 05` + runtime_test ohne Skip.

## Recap: Commits Total für beide Phasen

```
038ca24 chore(05): bump 0.0.58 → 0.0.59 final Phase C Tier-Inokulation
925969b feat(05/inoculation): StoryDirector Day-Tick Hook + Bootstrap RunAll
0b108185 feat(05/inoculation): RandomInoculationService + InfectedWildlife KindDef + InfectedPackBehavior
0e27c9c feat(05/inoculation): Converter + GetTotalCapBudget (AnimalHalfCap I6-I10)
eff6fc6 feat(05/inoculation): InoculationCandidate DTO + deterministic Selector
2c94d2b chore(05): bump 0.0.57 → 0.0.58 after Phase A Population-Ledger
5b07829 feat(05+01): register population capability v1 + Bootstrap RunAll hooks for Phase A
d02e392 feat(05/population): Reconciler MapComponent + NoteInoculation + Tests T11-T16
7f2d1b3 feat(05/population): Write-API (RegisterKill + DailyGrowth + Revenge + Reset) with overflow guard
dabbd34 feat(05/population): PopulationLedger SSOT (Phase A data layer + Scribe + ISchemaMigratable)
ef55bd1 feat(05/population): deterministic profile multiplier table
```

Recover-Anker: `git log --oneline -- mods/05-Rimconemy-Infected-Automation/{Source/Inoculation,Source/Population,Source/World/InfectedPackBehavior.cs,Defs/PawnKinds/Rimconemy_InfectedWildlife.xml}`
