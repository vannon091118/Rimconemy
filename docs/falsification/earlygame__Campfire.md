# Falsifizierungsbericht: `EarlyGame/Campfire` (Phase 2.3 / 2.4)

> **Capability:** `rimconemy.scavengerinfrastructure.power` v1 + `rimconemy.scavengerinfrastructure.building` v1
> **Owner:** Scavenger Infrastructure (Package 03)
> **Stand:** 2026-08-04 · **Status:** `COMPILED (Pre-LIVE)` · **Phase-Gate:** „Die erste Nacht" — Schritt 2/5
> **Code-Anker:**
> - `mods/03/Defs/BuildingDefs/Campfire.xml` (parent `BuildingBase`, `<defName>Rimconemy_Campfire</defName>`, `CompRefuelable` + `CompGlower`)
> - `mods/03/Defs/RecipeDefs/Rimconemy_BurnSteelScraps.xml` (1:1 Stahlreste → 1 Steel)
> - `mods/03/Defs/RecipeDefs/Rimconemy_MakeCoal.xml` (3 WoodLog + 2 HempLeafy → 4 Coal)
> - `mods/03/Defs/RecipeDefs/Rimconemy_SalvageMachineParts.xml` (5 SteelScraps → 1 MachineParts)
> - `mods/03/Defs/ThingDefs/Stats/ThingCategories.xml` (`Rimconemy_CraftingStations` UI-Kategorie)
> - `mods/03/Tests/CampfireScrapsRegressionTests.cs` (Phase-2.3-Tests)
> - `mods/03/Tests/CoalChainRegressionTests.cs` (Phase-2.4 MakeCoal-Test)
> **Bezugsverträge:** `docs/vanilla-api-matrix-1.6.md` §3.8 (CompRefuelable.ConsumeFuel/Refuel), §3.9 (CompGlower.Glows/ShouldBeLitNow) · `ROADMAP.md §9.1` Phase 2.3, 2.4 · `DECISIONS.md` §24 (kein garantierter Drop, kein blockierter Arbeitstyp).
> **Owner-Checklist:** siehe `docs/falsification/README.md`.

## Kontext

Campfire ist die erste **Wärme- und Lichtquelle** des Early-Game. Die Phase-2.3-Reihe
definiert die BuildingBase-Def mit `CompRefuelable` (WoodLog-Brennstoff) und
`CompGlower` (Licht). Phase 2.4 definiert drei Rezepte für die Steel-/Machine-Parts-
Vorproduktion. Die existierende P0-Coal-Chain (`Rimconemy_Coal`,
`Rimconemy_MachineParts`, drei Recipes) deckt das Vor-Setup; die
Vertikalscheibe verlangt zusätzlich die Stage-I-Verifikation (Campfire kann
tatsächlich gebaut werden und das Rezept ist auswählbar) und die **Save/Load-
Preisstabilität** (Rezept-Output darf bei Reload nicht doppelt vergeben werden).

**Pflicht-Szenario für LIVE-Belege:** `Rimconemy_SingleSurvivor` (`mods/02/Defs/Scenarios/SingleSurvivor.xml`), Save-Slot 1. Campfire muss aus dem User-Ablauf des Survivor-Gates (Phase 1.x) erreichbar sein.

## Vertrag (Invariants)

| # | Invariant | Mechanik |
|---|---|---|
| **I1** | Campfire ist **baubar** und produziert Glow + Wärme. | `ParentName="BuildingBase"` mit `<comps>` für `CompRefuelable` und `CompGlower`. Glow-Radius + Glow-Color werden während des Belegs aus dem XML gelesen. |
| **I2** | WoodLog-Brennstoff wird real verbraucht. | `CompRefuelable.ConsumeFuel(Single amount)` (vanilla-api-matrix §3.8 bestätigt: 4 Überladungen mit `int amount` und `List<Thing> fuelThings`). Bei `Tick` wird `fuelConsumptionRate=1.0` angewendet, sodass 1 WoodLog für ~100 Ticks reicht. |
| **I3** | Rezepte sind nur an Campfire verfügbar. | `<recipeUsers><li>Rimconemy_Campfire</li></recipeUsers>` in jeder RecipeDef. Campfire erscheint in der `Rimconemy_CraftingStations`-UI-Kategorie für Spieler-Recipe-Auswahl. |
| **I4** | MakeCoal: 3 WoodLog + 2 HempLeafy → 4 Coal (1.5× Effizienz gg\u00fc. rohem Holz-Verbrennen). | `<ingredients>` mit den zwei Defs und counts; `<products>` mit `Coal 4`. Effizienz-Beleg: pro Tick verbraucht WoodCoalGenerator mit `fuelConsumptionRate=0.67` WoodLog-Ersatz. |
| **I5** | SalvageMachineParts: 5 SteelScraps → 1 MachineParts (P0 Coal Chain Realität). | Eingehende SteelScraps-ID ist `Rimconemy_SteelScraps` (siehe `earlygame__Survivor.md` Invariant I3). Output `Rimconemy_MachineParts`. |
| **I6** | BurnSteelScraps: 1 SteelScraps → 1 Steel (Vorstufe zur Stahl-Herstellung). | Phase-2.3-Rezept als Bindeglied zu `Steel` für Vanilla-Wände. |
| **I7** | Kein Blockieren anderer Arbeitstypen (Anti-Softlock). | Vanilla-Hauling- und Bauberufe (Hauler, Builder, Smith, Cook) bleiben verfügbar — `recipeUsers` ist Add-Only, kein `replace`. |
| **I8** | Save/Load: Campfire-Snapshot ist `BuildingSnapshot.Preserved`. | `BuildingSnapshotService` speichert `BuildingConstructionState` und `BuildingPowerState`. `PostExposeData` von `CompRefuelable` setzt `Fuel`-Wert zurück. Dieselbe Doppel-Stelle-Prophylaxe wie `@Slop-Audit-Fix-F4` auf `BuildingProgressionAwards`. |

## A — Def-Liste (XML-Defs)

| DefName | Datei | Rolle | Status |
|---|---|---|---|
| `Rimconemy_Campfire` | `mods/03/Defs/BuildingDefs/Campfire.xml` (Datei heißt `Campfire.xml`, `<defName>` ist `Rimconemy_Campfire`) | BuildingBase + Refuelable + Glower | 🟢 Def vorhanden; ⏳ Live-Beleg (Phase 2.3 Spawn-Test) |
| `Rimconemy_BurnSteelScraps` | `mods/03/Defs/RecipeDefs/Rimconemy_BurnSteelScraps.xml` | 1 SteelScraps → 1 Steel @ Campfire | ✅ In P0 Coal Chain |
| `Rimconemy_MakeCoal` | `mods/03/Defs/RecipeDefs/Rimconemy_MakeCoal.xml` | 3 WoodLog + 2 HempLeafy → 4 Coal @ Campfire | ✅ In P0 Coal Chain (Falsifizierungsbericht `scavenger__FoodAndHemp.md`) |
| `Rimconemy_SalvageMachineParts` | `mods/03/Defs/RecipeDefs/Rimconemy_SalvageMachineParts.xml` | 5 SteelScraps → 1 MachineParts @ Campfire | ✅ In P0 Coal Chain |
| `Rimconemy_Coal` | `mods/03/Defs/ThingDefs/Resources/Coal.xml` | Pyrolyse-Output (ResourceBase) | ✅ In P0 Coal Chain |
| `Rimconemy_MachineParts` | `mods/03/Defs/ThingDefs/Resources/MachineParts.xml` | Präzisionskomponenten (ResourceBase) | ✅ In P0 Coal Chain |
| `Rimconemy_CraftingStations` | `mods/03/Defs/ThingDefs/Stats/ThingCategories.xml` | ThingCategoryDef UI | ✅ In P0 Coal Chain |

**Stub-Eintrag (Live-Beleg):**

```text
[LIVE-PFlicht, D] Def-Load-Logs in Player.log: expected
  "[Loading] Def loaded: Rimconemy_Campfire in defs/BuildingDefs" +
  "[Loading] Def loaded: Rimconemy_BurnSteelScraps in defs/RecipeDefs" +
  ...
```

## B — Code-Pfad (Build + Boot)

**Campfire ist als XML-Def vorhanden, Code-Konsumenten sind Vanilla (`Building_WorkTable`, `CompRefuelable`-Tick, `CompGlower`-GlowRenderer).** Kein Rimconemy-spezifischer C#-Code-Pfad erforderlich. Bootstrap-Test-Pfade:

- `CampfireScrapsRegressionTests.RunAll()` füttert deterministische Inputs in den Campfire-Snapshot-Sevice und prüft `BuildingConstructionState.Built`.
- `CoalChainRegressionTests.RunAll()` prüft MakeCoal- und SalvageMachineParts-Rezept-Stabilität über Save-Replay.

**Erwarteter Bootstrap-Log (Scavenger 03):**

```text
[Rimconemy.ScavengerInfrastructure] Campfire scrapping tests: N passed, 0 failed.
[Rimconemy.ScavengerInfrastructure] Coal-chain regression tests: N passed, 0 failed.
```

**Stand (2026-08-04):** Tests vorhanden (`mods/03/Tests/CoalChainRegressionTests.cs`, `CampfireScrapsRegressionTests.cs`); Bootstrap-Aufruf im aktuellen Code prüft bestehende Marker. Reihenfolge im `Bootstrap.cs` ist zu verifizieren.

## C — Selbsttest (RunAll)

| # | Test | Was er beweist |
|---|---|---|
| 1 | `CampfireScrapsRegressionTests.RunAll` | Campfire-Drop-Invariante: 1 Stahlreste im Survivor-Inventar + Campfire-Verfügbarkeit. |
| 2 | `CoalChainRegressionTests.RunAll` | MakeCoal-Determinismus (3 WoodLog + 2 HempLeafy → 4 Coal bei jedem Replay); Salvage-Output (5 → 1) deterministisch. |

**Stub-Eintrag (Live-Beleg):**

```text
[LIVE-PFlicht, D] Nach Build: Expected
  "[Rimconemy.ScavengerInfrastructure] Campfire scrapping tests: N passed, 0 failed (expected=N)."
  "[Rimconemy.ScavengerInfrastructure] Coal-chain regression tests: N passed, 0 failed."
```

## D — Runtime-Boot (User Live-Test erforderlich)

**Reproduktions-Sequenz:**

1. Single-Survivor-Kampagne starten (siehe `earlygame__Survivor.md`).
2. Campsite finden, Materialien sammeln.
3. Campfire platzieren (Architect → Rimconemy Shelter / Rimconemy CraftingStations). Verbrauchte Ressourcen: 1 (?) Holz (in Q-14 noch zu spezifizieren).
4. Recipe-Auswahl testen: Campfire anklicken → Recipe-Dropdown zeigt 3 Einträge.
5. Recipe „BurnSteelScraps" wählen und bei vorhandenen 5 Stahlresten 1 Steel erstellen.
6. `Player.log` filter:
```bash
grep "Rimconemy_Campfire\|Rimconemy_BurnSteelScraps\|Rimconemy_MakeCoal\|CompRefuelable\|CompGlower\|Campfire scrapping\|Coal-chain" Player.log
```

**Erwartetes Marker-Set:**

```text
[Rimconemy.ScavengerInfrastructure] Campfire constructed. fuelConsumptionRate=1.0, glowColor=(1,0.65,0.3), glowRadius=6.
[Rimconemy.ScavengerInfrastructure] Recipe 'Rimconemy_BurnSteelScraps' selected. 1 SteelScraps → 1 Steel.
[Rimconemy.ScavengerInfrastructure] Recipe completion: "burn-steel-scraps" committed (output Hash = ...).
```

**Einzusetzender Live-Beleg:**

```text
<!--
TODO: nach `./scripts/runtime_test.sh --require-scenario-tests` und Ingame-Architect-Test
den Player.log-Auszug mit den Campfire-/Recipe-Markern hier einfuegen.
-->
```

## E — Save/Load Roundtrip (User Live-Test erforderlich)

### E.1 — Statischer Build-Beleg

`BuildingSnapshot.SnapshotContentHash` muss Campfire-Snapshots identisch
erhalten über Re-load. Statisch deterministisch (siehe `BuildingSnapshotService`
in `mods/03/Source/Building/`). Die Berechnung nutzt `Fuel/FuelPercentOfMax`,
`GlowColor/GlowRadius` und `ConstructionState`.

### E.2 — Live-Test-Roundtrip

1. Campfire bauen und mit WoodLog befeuern.
2. Save → Quit → Re-load.
3. **Erwartet:** Campfire erscheint mit identischem Fuel-Stand, Glow, und Recipe-Dropdown. Kein „Recipe-Doppel-Ausf\u00fchren"-Effekt.
4. Rezept-Bill-Output ist über `BuildingProgressionAdapter`-Keying (vorbereitet, Phase 8.4 verifiziert).

### E.3 — Idempotenz-Beleg

Negative-Pfad auf `MakeCoal`: zweimal `\u201cMake 4 Coal\u201d` aufrufbar auf demselben Campfire, aber **nur mit neuen Inputs**. Die Bill-Verfolgung via `Bill_Production.Notify_IterationCompleted` (vanilla-api-matrix §4.5 bestätigt) ist der zentrale Hook. Wenn der Hook korrekt blockiert, wird die XP-Bridge den BurnSteelScraps-Bill nicht doppelt zählen.

## F — Cross-Package READ

**KEIN CROSS-PAKAGE-READ** in Phase 2.3/2.4. `Rimconemy_Campfire` wird
vom Survivor-Pawn gebaut (Vanilla-Job `JobDriver_BuildStandingWork`), und das
Rezept wird von `JobDriver_DoBill` durchgeführt (Phase 8.4 Hook-Stelle).

**StoryDirector liest `BuildingSnapshot` über `StoryDirector.AssignStorageHashFromCapability`
(siehe vorhandene Code-Pfade in `mods/05/Source/Story/`).** Die Capability-Gates
`s/rimconemy.scavengerinfrastructure.power` und `s/rimconemy.scavengerinfrastructure.building`
sind bereits in der OWNER-Map (INTERFACE_CONTRACT §9.1) registriert.

## G — Performance-Kennzahl

| Metrik | Phase 2.3 / 2.4 Budget | Heutiger Stand |
|---|---|---|
| Campfire-Tick (Refuelable + Glower) | ≤ 0,5 ms | Vanilla-Bound, keine Messung nötig |
| Recipe-Ausf\u00fchrung Wall-Time | ≤ 600 workAmount / 1,0 workSpeedStat = 150 ticks ≈ 2,5 s | identisch |
| Bill_Production.Notify_IterationCompleted Hook | O(1) | vanilla-api-matrix §4.5 bestätigt |
| Save/Load-Campfire-Roundtrip | ≤ 50 ms | entspricht `BuildingSnapshotService.Save` (Sample-Messung in Phase 2 ergänzen) |

**Phase 2.3/2.4 ist nicht-gate-blockierend** bezüglich Performance. Volle Gates
kommen in Phase 2 mit `ProgressionGameComponent.UpdateRuntimeState`-Sampling.

## Reproduktion

```bash
./scripts/runtime_test.sh --require-scenario-tests
```

Anschlie\u00dfend (interaktiv — User-Pflicht):

1. Starte Single-Survivor-Kampagne aus `earlygame__Survivor.md`-Reproduktion.
2. Baue Campfire wie oben.
3. F\u00chre MakeCoal-Bill aus.
4. Speichere und lade neu.
5. Beleg im Block D + E als Live-Log-Auszug.

## Negative-Test (manuell)

**Belegbruch 1 — Doppel-Rezept-Verbrauch (sollte nicht passieren):**
1. Patch: `MakeCoal`-Rezept in Save-Reload erlaubt erneute Anwendung auf gleichen Bill.
2. Folge: 1 Save mit 10 Coal wird zu 20 Coal nach Save/Load (Doppelverbrauch gleicher Inputs).
3. Marker: `grep "Recipe completion: \\\\\"make-coal\\\\\" committed" Player.log | wc -l > 1`.

**Belegbruch 2 — Fuel-Stand-Verlust:**
1. Patch: `CompRefuelable.PostExposeData` wird leer gelassen.
2. Folge: Campfire wird nach Save/Load leer (Fuel=0), Wärme verschwindet.
3. Marker: Snapshot-Service berichtet `Fuel=0` nach Reload trotz Voll-Lager.

Beide Br\u00fcche sind im `## D`-Block mit erwarteten Log-Mustern festgehalten; nach
erfolgreichem Live-Boot wird der Bericht auf `SURVIVED` gehoben.

## Siehe auch

- `docs/vanilla-api-matrix-1.6.md` §3.8 (CompRefuelable), §3.9 (CompGlower), §4.5 (Bill_Production.Notify_IterationCompleted).
- `ROADMAP.md §9.1` Phase 2.3, 2.4.
- `DECISIONS.md` §24.
- `docs/falsification/earlygame__Survivor.md` (Single-Survivor-Voraussetzung).
- `docs/falsification/earlygame__Barricade.md` (nächste Stufe: Tier-1-Barrikade mit gemischter Holz+Stahlrest-Kosten).
- `docs/falsification/scavenger__ConstructionDebris.md` (Phase-2.1-Vorstufe).
- `docs/falsification/scavenger__ReservePhysicalTransfer.md` (Phase-2.2-Vorstufe).
