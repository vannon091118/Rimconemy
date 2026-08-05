# Falsifizierungsbericht: `Infected/ManualRaid`

> **Capability:** `rimconemy.infectedautomation` v1 · **Owner:** Infected · **Stand:** 2026-08-04
> **Status:** `COMPILED`
> **Code-Anker:** `Source/Incidents/IncidentClassifier.cs` · **Test:** `Tests/IncidentClassifierRegressionTests.cs`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `Source/Infected/Source/Incidents/IncidentClassifier.cs`

- Kompiliert: ✅
- Bootstrap-Klasse: `Infected.Bootstrap.RunAll` ruft `Tests/IncidentClassifierRegressionTests.cs` auf
- Patch-Klassen: `mods/infected/Source/Infected/*.cs`

## C — Selbsttest (RunAll)

`Tests.IncidentClassifierRegressionTests.RunAll()` ist in `Bootstrap` aufgerufen.

## D — Runtime-Boot (User Live-Test erforderlich)

<!-- FoLgender Log-Auszug beweist den Boot:
```
[Rimconemy.Infected] ... Loaded
```
-->

## E — Phase B Live-Beleg (StoryDirector.Revenge × InfectedRaidWorker)

**Erwartet im Player.log nach Phase B (2026-08-05) — Daily-Growth+Reset+Revenge coupling:**

```
[Rimconemy.InfectedAutomation] StoryDirector: [Tick 180000] Selected 'rimconemy.revenge.lesser' (roll=…, weight=0.7, candidates=…, seed=…)
                              (StoryDirector.LastPendingRevenge=7 auf Survival-Profil nach 10 Kills gestern.)
```

**Verifikation (User-Pflicht):**
1. Start Survival-Kolonie (difficulty=Medium/Rough).
2. Lass 5+ Colonisten bis Tag 7+ leben.
3. Töte 10 Infizierte über mehrere Tage (PopulationLedger.RecentKillsToday trackt).
4. Warte nächste Eval-Tick (= T-stamp + 60000 nach LastEvaluationTick).
5. Suche im Player.log nach `StoryDirector.LastPendingRevenge` (oder nach einer Log-Zeile, die die Quote ausweist).
6. Erwarteter Wert: 7 (= floor(10 × PopulationProfileMultipliers.RevengeRatio["Survival"]=0.7)).
7. Optional: Force-evaluate via Dev-Mode-Dashboard, prüfe dass BuildPlanForTick einen Plan mit PawnCount >= 5 liefert.

**Akzeptanz-Gate:**
- [ ] Live-Beleg im Player.log vor Bump auf 0.0.61 (Block-Release bis Live-Beleg vorhanden).
- [ ] Slot-Decrement: nach Spawn-Workerat `LastPendingRevenge` um `actuallySpawned` reduziert.
- [ ] Save/Load: Slot transient → nach Reload recompute aus ledger.RecentKillsToday (Wert 0, dann re-füllt sich nach erneutem Kill-Tag).

## F — Save/Load Roundtrip (Phase A+B)

<!-- Step: Spielstand speichern → neu laden → STATE pruefen → Marker: SURVIVED-PREFIX -->

## F — Cross-Package READ

<!-- Step: anderes Paket liest via Capability `rimconemy....` aus -->
<!-- z. B. `CapabilityAudit.HasCapabilityOrWarn(rimconemy.rimconemy.infectedautomation)` -->

## G — Performance-Kennzahl

<!-- Step: Spielstart in 30s belegen; Sample 5 Zyklen -->

## User-Aktion Pflicht

1. `./scripts/runtime_test.sh --require-scenario-tests` ausfuehren
2. Log-Auszug in Block D einsetzen
3. Save/Load-Test fuer Block E erstellen
4. Cross-Read-Test fuer Block F
5. Performance-Zahl fuer Block G

Sobald alle User-Bloecke (D, E, F, G, H) befuellt sind, gilt der Bericht als `SURVIVED`.
