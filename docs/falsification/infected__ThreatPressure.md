# Falsifizierungsbericht: `Infected/ThreatPressure`

> **Capability:** `rimconemy.infectedautomation` v1 · **Owner:** Infected · **Stand:** 2026-08-04
> **Status:** `COMPILED`
> **Code-Anker:** `Source/Story/StoryDirector.cs` · **Test:** `Tests/BuildingThreatRegressionTests (ThreatSnapshotBridge-Pfad).cs`
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## A — Def-Liste (XML-Defs)

<!-- Befuelle nach Ingame-Boot: Welche .xml-Defs sind im Player.log geladen? -->

## B — Code-Pfad (Build + Boot)

Quelle: `Source/Infected/Source/Story/StoryDirector.cs`

- Kompiliert: ✅
- Bootstrap-Klasse: `Infected.Bootstrap.RunAll` ruft `Tests/BuildingThreatRegressionTests (ThreatSnapshotBridge-Pfad).cs` auf
- Patch-Klassen: `mods/infected/Source/Infected/*.cs`

## C — Selbsttest (RunAll)

`Tests.BuildingThreatRegressionTests (ThreatSnapshotBridge-Pfad).RunAll()` ist in `Bootstrap` aufgerufen.

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

---

## Update 2026-08-05 — Sensing-Anbindung (User-Anforderung #3)

> **Quelle:** `docs/CHAT_PROTOCOL_2026-08-05.md` §1.3 · `ROADMAP.md §9.8` (T1/T2/T5/T6/T7, kein Code geändert).

User wünscht höhere Infizierten-Dichte mit **Licht-Hören + Radius X + begrenzter Sichtweite**. Das berührt diesen Bericht doppelt:

- `ThreatSnapshotBridge.cs:100` (`TotalPressure = d.LastSnapshot.ThreatPressure`) bleibt die Druckquelle; die **Spawn-Dichte** (T7) skaliert darüber, ohne „Druck = Raid" zu vermengen (ROADMAP 05 §4).
- Die Sensing-Formel (T2) liest Licht/Radius/Sicht — **kein zweiter Druckkopf**, keine Änderung an `SituationSnapshot.ThreatPressure` (StoryDirector).
- F-Block (Cross-Read `rimconemy.infectedautomation.threat`) bleibt die Beleg-Pflicht; T9 in der Tasklist ist die konkrete Step-Vorlage.
