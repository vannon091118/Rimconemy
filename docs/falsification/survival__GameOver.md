# Falsifizierungsbericht: `Survival/GameOver`

> **Capability:** `rimconemy.survivalprogression` v1 · **Owner:** Survival · **Stand:** 2026-08-04
> **Status:** `COMPILED` (Findings 3+5 addressed)
> **Code-Anker:** `Source/GameOver/GameOverDetector.cs` + `Source/Progression/ProgressionGameComponent.cs` (Finding 5 hoisted RecreationAvailable) · **Test:** `Tests/FoundationCrossPackageStateTests.cs`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`
> **Fixes (2026-08-04):**
> - Finding 3: Edge-triggered GameOverPending via `StoryState.MarkGameOverPending(reason, colonistsPresent)` + `FirstWipeTick` persisted
> - Finding 5: `RecreationAvailable` hoisted out of pawn loop in `UpdateRuntimeState()`

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `Source/Survival/Source/GameOver/GameOverDetector.cs`
- Kompiliert: ✅
- Bootstrap-Klasse: `Survival.Bootstrap.RunAll` ruft `Tests/FoundationCrossPackageStateTests.cs` auf
- Patch-Klassen: `mods/survival/Source/Survival/*.cs`

## C — Selbsttest (RunAll)

`Tests.FoundationCrossPackageStateTests.RunAll()` ist in `Bootstrap` aufgerufen.

## D — Runtime-Boot (User Live-Test erforderlich)

<!-- FoLgender Log-Auszug beweist den Boot:
```
[Rimconemy.Survival] ... Loaded
```
-->

## E — Save/Load Roundtrip

<!-- Step: Spielstand speichern → neu laden → STATE pruefen → Marker: SURVIVED-PREFIX -->
FirstWipeTick persists across save/load for wipe chronology.

## F — Cross-Package READ

<!-- Step: anderes Paket liest via Capability `rimconemy....` aus -->
<!-- z. B. `CapabilityAudit.HasCapabilityOrWarn(rimconemy.rimconemy.survivalprogression)` -->

## G — Performance-Kennzahl

<!-- Step: Spielstart in 30s belegen; Sample 5 Zyklen -->
RecreationAvailable hoisted from O(pawns) to O(1) per tick block.

## User-Aktion Pflicht

1. `./scripts/runtime_test.sh --require-scenario-tests` ausfuehren
2. Log-Auszug in Block D einsetzen
3. Save/Load-Test fuer Block E erstellen (verify FirstWipeTick)
4. Cross-Read-Test fuer Block F
5. Performance-Zahl fuer Block G

Sobald alle 4 User-Bloecke befuellt sind, gilt der Bericht als `SURVIVED`.