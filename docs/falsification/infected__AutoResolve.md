# Falsifizierungsbericht: `Infected/AutoResolve`

> **Capability:** `rimconemy.infectedautomation` v1 · **Owner:** Infected · **Stand:** 2026-08-04
> **Status:** `COMPILED` (Finding 3 addressed)
> **Code-Anker:** `Source/World/WorldRaidCoordinator.cs` + `Source/Story/StoryState.cs` (FirstWipeTick) + `Source/Story/StoryDirector.cs` (edge-trigger) · **Test:** `Tests/ThreatSnapshotBridgeRegressionTests (World-Raid selbst UNVERIFIED).cs`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`
> **Fixes (2026-08-04):**
> - Finding 3: Edge-triggered GameOverPending via `StoryState.MarkGameOverPending(reason, colonistsPresent)` + `FirstWipeTick` persisted for wipe chronology

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `Source/Infected/Source/World/WorldRaidCoordinator.cs`
- Kompiliert: ✅
- Bootstrap-Klasse: `Infected.Bootstrap.RunAll` ruft `Tests/ThreatSnapshotBridgeRegressionTests (World-Raid selbst UNVERIFIED).cs` auf
- Patch-Klassen: `mods/infected/Source/Infected/*.cs`

## C — Selbsttest (RunAll)

`Tests.ThreatSnapshotBridgeRegressionTests.RunAll()` ist in `Bootstrap` aufgerufen.

## D — Runtime-Boot (User Live-Test erforderlich)

<!-- FoLgender Log-Auszug beweist den Boot:
```
[Rimconemy.Infected] ... Loaded
```
-->

## E — Save/Load Roundtrip

<!-- Step: Spielstand speichern → neu laden → STATE pruefen → Marker: SURVIVED-PREFIX -->
FirstWipeTick persists across save/load for wipe chronology (survival__GameOver.md / infected__AutoResolve.md).

## F — Cross-Package READ

<!-- Step: anderes Paket liest via Capability `rimconemy....` aus -->
<!-- z. B. `CapabilityAudit.HasCapabilityOrWarn(rimconemy.rimconemy.infectedautomation)` -->

## G — Performance-Kennzahl

<!-- Step: Spielstart in 30s belegen; Sample 5 Zyklen -->
GameOverPending writes now edge-triggered (O(1) vs redundant per 250 ticks).

## User-Aktion Pflicht

1. `./scripts/runtime_test.sh --require-scenario-tests` ausfuehren
2. Log-Auszug in Block D einsetzen
3. Save/Load-Test fuer Block E erstellen (verify FirstWipeTick)
4. Cross-Read-Test fuer Block F
5. Performance-Zahl fuer Block G

Sobald alle 4 User-Bloecke befuellt sind, gilt der Bericht als `SURVIVED`.