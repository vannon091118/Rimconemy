# Falsifizierungsbericht: `Economy/Market`

> **Capability:** `rimconemy.economyterritory` v1 · **Owner:** Economy · **Stand:** 2026-08-04
> **Status:** `COMPILED`
> **Code-Anker:** `Source/Market/Market.cs` · **Test:** `Tests/MarketPersistenceTests.cs`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `Source/Economy/Source/Market/Market.cs`

- Kompiliert: ✅
- Bootstrap-Klasse: `Economy.Bootstrap.RunAll` ruft `Tests/MarketPersistenceTests.cs` auf
- Patch-Klassen: `mods/economy/Source/Economy/*.cs`

## C — Selbsttest (RunAll)

`Tests.MarketPersistenceTests.RunAll()` ist in `Bootstrap` aufgerufen.

## D — Runtime-Boot (User Live-Test erforderlich)

<!-- FoLgender Log-Auszug beweist den Boot:
```
[Rimconemy.Economy] ... Loaded
```
-->

## E — Save/Load Roundtrip

<!-- Step: Spielstand speichern → neu laden → STATE pruefen → Marker: SURVIVED-PREFIX -->

## F — Cross-Package READ

<!-- Step: anderes Paket liest via Capability `rimconemy....` aus -->
<!-- z. B. `CapabilityAudit.HasCapabilityOrWarn(rimconemy.rimconemy.economyterritory)` -->

## G — Performance-Kennzahl

<!-- Step: Spielstart in 30s belegen; Sample 5 Zyklen -->

## User-Aktion Pflicht

1. `./scripts/runtime_test.sh --require-scenario-tests` ausfuehren
2. Log-Auszug in Block D einsetzen
3. Save/Load-Test fuer Block E erstellen
4. Cross-Read-Test fuer Block F
5. Performance-Zahl fuer Block G

Sobald alle 4 User-Bloecke befuellt sind, gilt der Bericht als `SURVIVED`.
