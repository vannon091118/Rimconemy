# Falsifizierungsbericht: `Survival/Research`

> **Capability:** `rimconemy.survivalprogression` v1 · **Owner:** Survival · **Stand:** 2026-08-04
> **Status:** `COMPILED` — Legacy-/Kompatibilitäts-Read-Model, nicht der neue organische Freischaltpfad
> **Code-Anker:** `Source/Progression/ProgressionGameComponent.cs (ResearchCapabilities)` · **Test:** `Tests/BuildingProgressionPersistenceRegressionTests.cs`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `Source/Survival/Source/Progression/ProgressionGameComponent.cs (ResearchCapabilities)`

- Kompiliert: ✅
- Einordnung: Der aktuelle Pfad sammelt abgeschlossene Vanilla-`ResearchProjectDef`s als Read-Model. Er beweist weder Forschungstisch-Freigaben noch den beschlossenen Erfahrungsbaum.
- Neuer Zielpfad: bestätigte Action-Completion → genau eine Bereichserfahrung → organische Architektenfreigabe.
- Bootstrap-Klasse: `Survival.Bootstrap.RunAll` ruft `Tests/BuildingProgressionPersistenceRegressionTests.cs` auf
- Patch-Klassen: `mods/survival/Source/Survival/*.cs`

## C — Selbsttest (RunAll)

`Tests.BuildingProgressionPersistenceRegressionTests.RunAll()` ist in `Bootstrap` aufgerufen.

## D — Runtime-Boot (User Live-Test erforderlich)

<!-- FoLgender Log-Auszug beweist den Boot:
```
[Rimconemy.Survival] ... Loaded
```
-->

## E — Save/Load Roundtrip

<!-- Step: Spielstand speichern → neu laden → STATE pruefen → Marker: SURVIVED-PREFIX -->

## F — Cross-Package READ

<!-- Step: anderes Paket liest via Capability `rimconemy....` aus -->
<!-- z. B. `CapabilityAudit.HasCapabilityOrWarn(rimconemy.rimconemy.survivalprogression)` -->

## G — Performance-Kennzahl

<!-- Step: Spielstart in 30s belegen; Sample 5 Zyklen -->

## Neue Design-Grenze

Dieser Bericht darf erst auf einen organischen Wissens-/Freigabepfad erweitert werden, wenn getrennt belegt ist:

1. eine physisch abgeschlossene Handlung,
2. genau ein idempotenter Erfahrungsgewinn,
3. eine gespeicherte Freigabe im Architektenmenü,
4. kein Fortschritt durch Platzieren, Abbrechen, Menüöffnen oder Spam,
5. Save/Load ohne Phantomfreigabe oder Verlust.

Vanilla-ResearchProjectDefs bleiben für DLC-/Mod-Kompatibilität bestehen, sind aber nicht automatisch ein Rimconemy-Unlock.

## User-Aktion Pflicht

1. `./scripts/runtime_test.sh --require-scenario-tests` ausfuehren
2. Log-Auszug in Block D einsetzen
3. Save/Load-Test fuer Block E erstellen
4. Cross-Read-Test fuer Block F
5. Performance-Zahl fuer Block G

Sobald alle 4 User-Bloecke befuellt sind, gilt der Bericht als `SURVIVED`.
