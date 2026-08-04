# Falsifizierungsbericht: `Infected/InfectedRaid`

> **Capability:** `rimconemy.infectedautomation` v1 · **Owner:** Infected · **Stand:** 2026-08-04
> **Status:** `UNVERIFIED` — begrenzte Letter→Spawn-Bridge als CODE/DEF vorhanden; für den aktuellen uncommitteten Arbeitsstand liegt kein frischer Compile-/Runtime-Beleg vor
> **Code-Anker:** `mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidSpawnService.cs + InfectedRaidWorker.cs` · **Tests:** vorhandene Story-/Threat-/Incident-Classifier-Regressionstests in `mods/05-Rimconemy-Infected-Automation/Tests/`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidSpawnService.cs + mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidWorker.cs`

- Code vorhanden: ✅; aktueller uncommitteter Spawn-Arbeitsstand ist nicht frisch kompiliert.
- Bootstrap-Klasse: `Rimconemy.InfectedAutomation.Bootstrap` registriert die vorhandenen Story-, Threat-, Mechadroid- und Incident-Classifier-Regressionstests; eine Datei `IncidentClassifierRegressionTests.cs` existiert aktuell nicht.
- Patch-Klassen: keine erforderlich für den Worker-Spawn-Pfad; die aktive Logik liegt unter `mods/05-Rimconemy-Infected-Automation/Source/Incidents/`
- `InfectedRaidWorker` sendet die Letter und fordert danach aus `InfectedRaidSpawnService` maximal einen `Rimconemy_InfectedRavager` an; `HiddenInfected.xml` und `InfectedRavager.xml` sind die minimale DEF-Basis.
- Der Worker-/Plan-Pfad ist kein Beleg für einen erfolgreichen Live-Spawn oder einen vollständigen Raid-/Kampflifecycle.

## C — Selbsttest (RunAll)

Die im aktuellen Bootstrap registrierten Regressionstests decken Story-Auswahl/-State, Threat, Mechadroid-Jobs und Incident-Klassifikation ab. Ein eigener `IncidentClassifierRegressionTests.RunAll()`-Aufruf ist aktuell nicht vorhanden.

## D — Runtime-Boot (User Live-Test erforderlich)

<!-- FoLgender Log-Auszug beweist den Boot:
```
[Rimconemy.Infected] ... Loaded
```
-->

## E — Save/Load Roundtrip

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

Sobald alle 4 User-Bloecke befuellt sind, gilt der Bericht als `SURVIVED`.
