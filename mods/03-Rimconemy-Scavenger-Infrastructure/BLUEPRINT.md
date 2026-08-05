# Blueprint 03 – Rimconemy Scavenger Infrastructure

## API-Hinweis

Die genannten ThingDef-, Plant-, Power-, Fuel- und Turret-Anker sind Planungsanker. Exakte 1.6-Def-Felder und Signaturen werden über `API-POWER-01` und lokale Def-/Assembly-Spikes bestätigt (Spike-/Baseline-Dokumente archiviert in `docs/archive-md-2026-08-04.tar.gz`).

## Ziel

Dieses Paket beschreibt den geplanten ersten Rimconemy-Moment: Bauschutt soll die Basis schützen, Farmen Nahrung liefern, Hanf getrennt bleiben, Wasser und Holz/Kohle den Generator versorgen und Strom den Pfeilturm aktivieren. Im aktuellen Code sind Storage-/Power-Read-Models, Defs/Marker und Bootstrap-Gates belegt; der vollständige Loop bleibt ein offenes Live-Gate.

## Standalone-Spielwert

```text
Bauschutt → Wand/Tür → Nahrung/Hanf → Wasser + Brennstoff → Power → Pfeilturm
```

Vanilla-Needs und Vanilla-Raids bleiben aktiv, solange die optionalen Pakete fehlen.

## P0 Coal Chain (IMPLEMENTIERT 2026-08-04)

```text
WoodLog + HempLeafy → Campfire (MakeCoal) → Coal → WoodCoalGenerator (1.5× Effizienz)
SteelScraps → Campfire (SalvageMachineParts) → MachineParts → Edelstahl/Turm (P1)
```

Artefakte:
- `Rimconemy_Coal` ThingDef (ResourceBase, StackCount, GeneratorInputs)
- `Rimconemy_MachineParts` ThingDef (ResourceBase, StackCount, Scraps)
- `Rimconemy_CraftingStations` ThingCategoryDef (Buildings parent)
- `Rimconemy_MakeCoal` Recipe (Campfire, 3 WoodLog + 2 HempLeafy → 4 Coal)
- `Rimconemy_SalvageMachineParts` Recipe (Campfire, 5 SteelScraps → 1 MachineParts)
- WoodCoalGenerator: 2× Refuelable (WoodLog/Chemfuel @ 1.0, Coal @ 0.67)
- Campfire: 3 Rezepte wired

Nicht belegt (Live-Gates): MakeCoal-Durchlauf, Salvage-Ausbeute, Save/Load

### Geplanter T2-Strom-Schritt: elektrischer Hochofen

Der elektrische Hochofen folgt erst nach dem stabilen Survival-/Power-Fundament:

```text
Stahl → elektrischer Hochofen → Munition

Kohle → Ofen-Refuelable für ausgewählte Rezepte und Generator-Refuelable für das PowerNet
```

- Tier: `T2 Energy`.
- Geplanter Bauinput: Kalkstein, Sandstein oder Granit sowie Eisen/Stahl.
- Stahl ist der physische Rezeptinput; ausgewählte Rezepte verbrauchen Kohle über die Ofen-Refuelable-Mechanik. Der Generator verbraucht Kohle separat für das PowerNet; Credits sind kein Ersatz.
- Keine garantierte Munitionsquelle durch Gegner-Drops. Ruinenfunde bleiben zufällig.
- Combat Extended bleibt optional; der Core muss ohne CE funktionieren.
- Vor `LIVE` bleiben Hochofen, Munitions-ThingDef, Recipe, Energieverbrauch und Save/Load ausdrücklich `OPEN`.

## Standalone-Spielwert

```text
Bauschutt → Wand/Tür → Nahrung/Hanf → Wasser + Brennstoff → Power → Pfeilturm
```

Vanilla-Needs und Vanilla-Raids bleiben aktiv, solange die optionalen Pakete fehlen.

## Vanilla-/DLC-Anker

| Bereich | Anker | MVP-Entscheidung | Spike |
|---|---|---|---|
| Bauschutt | eigene `ThingDef`, Baukosten, Storage/Trade | neue Def mit vorhandener Vanilla-Textur | vollständige Def-/Recipe-/Loot-Inventur |
| Wand/Tür | vorhandene Build-/Designator-/Cost-Pfade | gezielte PatchOperations | Reparatur/Abriss/DLC-Gebäude |
| Nahrung | Plant-/Harvest-/Spoil-/Food-Filter | Vanilla-Pflanzenpipeline adaptieren | 1.6-Plant-/Designator-Felder lokal |
| Hanf | eigene Plant-/Harvest-/ThingCategory | ausdrücklich nicht essbar | FoodPolicy-/Trader-/Drop-Test |
| Wasser | physischer Input, kein globaler Zähler | MVP als definierter Item-/Consumerpfad | echte Quelle/Lager/Verbrauch gegen DLC-Wassermods |
| Generator/Power | Vanilla-Fuel-/Power-Comps | Vanilla PowerNet wiederverwenden | genaue Fuel-/Comp-Signaturen |
| Pfeilturm | Vanilla-Turret-/Power-Anker | stromabhängige vorhandene Turretbasis | CE-/Combat-Adapter später |
| Erze/Silber | Vanilla-ThingDefs | Upgrades statt allgemeiner Baukosten | DLC-Loot-/Quest-/Traderpfade |

## Artefaktziele (geplante Zielpfade, noch keine vorhandenen Belege)

| Task | Dateien/Artefakte | Test-IDs |
|---|---|---|
| I1 | `Defs/ThingDefs/ConstructionDebris.xml`, `Defs/ThingDefs/Hemp.xml`, `Patches/StorageAndTrade.xml`, `Tests/ResourceDefs.md` | `NEW_GAME`, `UI_REASON`, `DLC_SCOPE` |
| **I1.1 (P0)** | `Defs/ThingDefs/Resources/Coal.xml`, `Defs/ThingDefs/Resources/MachineParts.xml`, `Defs/ThingDefs/Stats/ThingCategories.xml` (CraftingStations), `Defs/RecipeDefs/MakeCoal.xml`, `Defs/RecipeDefs/SalvageMachineParts.xml`, `Defs/BuildingDefs/Campfire.xml`, `Defs/BuildingDefs/PowerPlants.xml` | `NEW_GAME`, `BOOT`, `UI_REASON` |
| I2 | `Patches/Bauschutt_Remap_Patches.xml` (angelegt 2026-08-04), `Defs/PlantDefs/` | `NEW_GAME`, `UI_REASON`, `DETERMINISM` |
| I3 | `Defs/ThingDefs/Water.xml`, `Defs/BuildingDefs/`, `Source/Power/` nur falls nötig, `Tests/PowerChain.md` | `NEW_GAME`, `SAVE_LOAD`, `UI_REASON` |
| I4 | `Defs/BuildingDefs/ArrowTurret.xml`, `Patches/UpgradeCosts.xml`, `Tests/ArrowTurretPower.md` | `SAVE_LOAD`, `UI_REASON`, `DLC_SCOPE` |
| I5 | `Source/Integration/`, `Tests/DlcDefInventory.md`, drei Kernberichte plus Transferberichte | `SAVE_LOAD`, `MAP_CHANGE`, `DLC_SCOPE`, `DETERMINISM` |

## Fünf Build-Tasks

### I1 – Ressourcen-Defs und prototypische Grafik

- `ConstructionDebris` als eigene stabile Ressource anlegen.
- vorhandene Vanilla-Textur referenzieren, keine neue Grafik erzeugen.
- Stack, Kategorie, Flammability, MarketValue und Handel bewusst definieren.
- Hanf-Output in eigene Kategorie ohne Food-Flags führen.

**Gate:** Bauschutt und Hanf sind sichtbar, lagerbar, transportierbar und semantisch getrennt.

### I1.1 – P0 Coal Chain Resources (ABGESCHLOSSEN 2026-08-04)

- `Rimconemy_Coal` — Pyrolysis-Output, Category `Rimconemy_GeneratorInputs`, Stack 200, MarketValue 6
- `Rimconemy_MachineParts` — Precision Components, Category `Rimconemy_Scraps`, Stack 150, MarketValue 15
- `Rimconemy_CraftingStations` — ThingCategoryDef für Campfire UI-Platzierung

**Gate:** Alle drei Defs laden fehlerfrei, Campfire erscheint in CraftingStations-Kategorie.

### I2 – Bau- und Farmloop

- Wände und Tür gezielt auf Bauschutt umstellen.
- Reparatur, Abriss, Baufortschritt und Lagerfilter testen.
- Nahrungspflanze, Hanf, Verderb, Seasons, Terrain, Ernte und Drops prüfen.

**Gate:** neue Kampagne baut nur mit Bauschutt Wand/Tür; Hanf kann nicht als Nahrung enden.

### I3 – Wasser/Brennstoff/Power

- Wasserquelle, Lager/Verbrauch und Blockadezustand definieren.
- Holz und Kohle als Brennstoffpfade definieren.
- vorhandenes PowerNet/Generator/Fuel-Verhalten wiederverwenden.
- UI zeigt Quelle, Bestand, Verbrauch und Offline-Grund.

**Gate:** fehlendes Wasser oder Brennstoff verhindert dauerhaften Generatorbetrieb; keine kostenlose globale Energie.

### I4 – Pfeilturm und Upgradepfad

- Vanilla-Turret-/Power-Anker adaptieren.
- Strom ist harte Betriebsbedingung.
- Zielwahl, Reichweite, Feuerrate, Wartung und Eisen-Upgrades dokumentieren.
- Pfeilturmstatus `Active`, `Blocked`, `Offline`, `Damaged` anzeigen.

**Gate:** Turm verursacht ohne Strom keinen Schaden; der erste aktive Turm ist der definierte Phase-1-Meilenstein.

### I5 – Integration, Save und DLC-Inventur

- ResourceSnapshot/PowerSnapshot veröffentlichen.
- Survival-Erfahrung nur über physisch bestätigte Command-/Output-Abschlüsse melden; keine Meldung für Preview, Platzierung oder Abbruch.
- Economy liest physische Waren; Infected liest Aktivität, nicht eigene Farmkopien.
- Core + alle fünf DLC-Defs/Recipes/Trader/Loot/Questpfade durchsuchen.

**Exit:** drei Scavenger-Berichte plus physischer Transfer bleiben bis zu Laufzeitbelegen `UNVERIFIED`.

## Schnittstellen

- besitzt Ressourcen, Pflanzen, Wasser, Power und Turmstatus.
- veröffentlicht `ResourceSnapshot`, `PowerSnapshot`, `PhysicalTransferReserved/Committed`.
- liest Progression-Capabilities, schreibt keine XP direkt.
- Economy/Threat lesen denselben Produktionsbestand.

## UI-Minimum

Bauschuttbestand und Baukosten, Farm-/Hanfstatus, Wasser-/Brennstoffprognose, PowerNet, Turmstatus und eindeutiger Blockadegrund.

## Save-/Performance-Gates

Remapping-Version und lokale Infrastrukturzustände versionieren. Keine stillen Holz→Bauschutt-Umdeutungen in alten Saves. Pflanzen-/Powerupdates aggregieren, keine zweite Berechnung für UI/Economy/Threat.

## Offene Spikes

- exakte 1.6-Def-Felder für Plant-/Designator-/Comp-Pfade gegen lokale Daten prüfen.
- `MarketValue=0` nicht als Wealth-Raid-Lösung übernehmen; Wealth-/Threat-Policy separat testen.
- Wasser nicht automatisch aus Odyssey-Fishing ableiten.
- Caravan Camp, Gravship und temporäre Maps erst nach Vertical Slice.
- Erfahrungsbaum-/Architektenfreigaben bleiben Eigentum von Paket 02; keine parallele Forschungslogik in Paket 03.
