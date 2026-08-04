# Falsifizierungsbericht: `Foundation/Servicebus`

> **Capability:** `rimconemy.foundation` v1 · **Owner:** Foundation · **Stand:** 2026-08-04
> **Status:** `COMPILED`
> **Code-Anker:** `mods/01-Rimconemy-Foundation/Source/CrossPackage/CrossPackageState.cs` · **Test:** `Tests/FoundationProfileRefreshTests.cs`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `Source/Foundation/mods/01-Rimconemy-Foundation/Source/CrossPackage/CrossPackageState.cs`

- Kompiliert: ✅
- Bootstrap-Klasse: `Foundation.Bootstrap.RunAll` ruft `Tests/FoundationProfileRefreshTests.cs` auf
- Patch-Klassen: `mods/foundation/Source/Foundation/*.cs`

## C — Selbsttest (RunAll)

`Tests.FoundationProfileRefreshTests.RunAll()` ist in `Bootstrap` aufgerufen.

## D — Runtime-Boot (User Live-Test erforderlich)

<!-- FoLgender Log-Auszug beweist den Boot:
```
[Rimconemy.Foundation] ... Loaded
```
-->

## E — Save/Load Roundtrip

<!-- Step: Spielstand speichern → neu laden → STATE pruefen → Marker: SURVIVED-PREFIX -->

## F — Cross-Package READ

<!-- Step: anderes Paket liest via Capability `rimconemy....` aus -->
<!-- z. B. `CapabilityAudit.HasCapabilityOrWarn(rimconemy.rimconemy.foundation)` -->

## G — Performance-Kennzahl

<!-- Step: Spielstart in 30s belegen; Sample 5 Zyklen -->

## User-Aktion Pflicht

1. `./scripts/runtime_test.sh --require-scenario-tests` ausfuehren
2. Log-Auszug in Block D einsetzen
3. Save/Load-Test fuer Block E erstellen
4. Cross-Read-Test fuer Block F
5. Performance-Zahl fuer Block G

Sobald alle 4 User-Bloecke befuellt sind, gilt der Bericht als `SURVIVED`.
