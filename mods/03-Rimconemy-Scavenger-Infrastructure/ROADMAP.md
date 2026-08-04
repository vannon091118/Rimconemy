# Roadmap 03 – Rimconemy Scavenger Infrastructure

> Eigenständige Paketaufgabe 3 von 5  
> Standalone zuerst, Full-Overhaul-Integration danach  
> Zielplattform: RimWorld 1.6 mit Royalty, Ideology, Biotech, Anomaly und Odyssey

## 1. Paketauftrag

Scavenger Infrastructure definiert den geplanten Basebuilding- und Automationskern. Im aktuellen Code sind vor allem Storage-/Power-Read-Models, ThingDefs/Marker und Bootstrap-/Regression-Gates belegt; der vollständige Standalone-Gameplay-Loop ist noch nicht als `LIVE` nachgewiesen.

```text
Bauschutt
→ Wand/Tür
→ Farm/Nahrung/Hanf
→ Wasser + Holz/Kohle
→ Generator
→ Stromnetz
→ strombetriebener Pfeilturm
```

Im Full Overhaul werden diese physischen Waren, Gebäude und Produktionsketten mit Needs/XP/Forschung, Infizierten-Druck, Economy, Outposts und Mechadroids verbunden.

## 2. Standalone-Ziel

Mit Vanilla-Needs, Vanilla-Forschung, Vanilla-Fraktionen und Vanilla-Raids bietet das Paket:

- Bauschutt als neue physische Ressource,
- Bauschutt für Wände und Türen,
- getrennte Nahrungspflanzen und Hanf,
- Wasser- und Brennstoffversorgung,
- Generator und Stromverbrauch,
- stromabhängigen Pfeilturm,
- sichtbare Produktions- und Energieengpässe.

Das Paket ist ohne Economy, Territory, Infected oder Survival & Progression build-/bootstrap-fähig. Ein vollständiger eigenständiger Gameplay-Loop ohne diese Pakete bleibt ein offenes Live-Gate.

## 3. Full-Overhaul-Ziel

Im Full Overhaul:

- Bauschutt ist das frühe Wand-/Türmaterial und regional handelbar.
- Farmfläche erzeugt Nahrung, aber auch sichtbaren Infizierten-Druck.
- Arbeit an Farmen, Generatoren, Bau- und Verteidigungsanlagen gibt XP über Paket 2.
- Forschung aus Paket 2 schaltet die Infrastruktur stufenweise frei.
- Economy verwendet physische Waren statt Credits als Item.
- Outposts verbrauchen Bauschutt, Nahrung und Technikmaterial für Gründung, Proxies und Verteidigung.
- Mechadroids können Farm-, Energie- und Wartungsaufgaben übernehmen.

## 4. Ressourcenverträge

### Bauschutt

Bauschutt besitzt eine eigene stabile Def-/Ressourcen-ID und darf nicht nur als umbenanntes Holz erscheinen. Zu prüfen sind:

- Baukosten,
- Rezepte,
- Lagerfilter,
- Handelslisten,
- Beute,
- Startausrüstung,
- Quests,
- Events,
- Outpost-Investitionen,
- Preis-/Marktzuordnung.

### SteelScraps

Stahlreste sind das erste Scavenging-Produkt. Sie werden in Ruinen gefunden und im Campfire zu nutzbarem Stahl verarbeitet.

- Quelle: Ruinen-Abbruch, Events
- Verarbeitung: Campfire → `Rimconemy_BurnSteelScraps` (3 → 2 Steel)
- Lagerfilter: `Rimconemy_Scraps`
- Stack: 200

### Coal

Kohle ist der Pyrolysis-Output aus Holz und Hanf. Sie verbrennt im Generator 1.5× effizienter als rohes Holz.

- Quelle: Campfire → `Rimconemy_MakeCoal` (3 WoodLog + 2 HempLeafy → 4 Coal)
- Verbrauch: WoodCoalGenerator (dedizierter Refuelable, fuelConsumptionRate=0.67)
- Lagerfilter: `Rimconemy_GeneratorInputs`
- Stack: 200

### MachineParts

Maschinenteile sind Präzisionskomponenten für fortgeschrittene Fertigung (Edelstahl, Türme, Automation).

- Quelle: Campfire → `Rimconemy_SalvageMachineParts` (5 SteelScraps → 1 MachineParts)
- Geplante Quellen: Ruinen-Events, Mechanoid-Abbau, Supply Drops
- Lagerfilter: `Rimconemy_Scraps`
- Stack: 150

### Pflanzen

Mindestens zwei funktionale Gruppen:

```text
Nahrungspflanzen → Nahrung
Hanf             → Hanf-Ressource
```

Jede Pflanze braucht:

- definierte Terrain-/Biomeignung,
- Wachstumszeit,
- Ertrag,
- Arbeitsanforderung,
- Ernte-/Zerstörungsregeln,
- Drop-/Handelsverhalten,
- Full-Overhaul-Druckfaktor.

### Erze und Silber

- Erz-Namen bleiben erhalten und erhalten Upgrade-Funktionen.
- Eisen dient zunächst technischen/defensiven Upgrades.
- Silber wird im Full Overhaul nicht als allgemeines Geld verwendet, sondern als physisches Mechadroid-Upgrade-Material.
- Im Standalone-Modus darf Silber nicht stillschweigend umgedeutet werden, wenn Economy/Automation fehlen; die README muss den aktiven Modus anzeigen.

## 5. Sequenzielle Arbeitsschritte

### Task 3.1 – Defs- und Ressourcen-Spike

- Bauschutt-ThingDef anlegen.
- Kategorien, Stack, MarketValue, Flammability, Hauling und Storage festlegen.
- Vanilla-Textur nur als temporäre Grafik verwenden und dokumentieren.
- Bauschutt in einem Minimaltest erzeugen, lagern und bewegen.

**Gate:** Bauschutt ist im Spiel sichtbar, transportierbar, lagerbar und eindeutig von Silber/Credits getrennt.

### Task 3.2 – Bauschutt für Wände und Türen

- Wand- und Türkosten auf Bauschutt remappen.
- Bauplatzierung und Designator-UI prüfen.
- Reparatur, Abriss, Wiederverwendung und Baufortschritt testen.
- andere Vanilla-Def-/Patchquellen auf alte Holz-/Eisenkosten prüfen.

**Blindspot-Gate:** Nur die zwei sichtbaren Gebäude zu ändern reicht nicht. Recipes, Quest-Belohnungen, Start-Szenarien, Trader, Loot, WorkGivers und DLC-Gebäude dürfen keinen unbeabsichtigten alten Baumaterialpfad öffnen.

**Exit-Test:** Eine neue Standalone-Kampagne kann nur mit Bauschutt eine Wand und eine Tür errichten; UI erklärt fehlende Ressourcen.

### Task 3.3 – Nahrung, Farmen und Hanf

- Nahrungspflanzen und Hanfpflanzen getrennt definieren.
- Farm-WorkGiver, Plant lifecycle, Ernte, Verderb und Lagerung prüfen.
- Expeditionen und Drops als spätere Nahrungseingänge vorbereiten, ohne doppelte Quellen zu erzeugen.
- Farmfläche als messbaren Output- und Schutzfaktor exponieren.

**Blindspot-Gate:** PlantDefs, Terrain-/Biome-Regeln, Seasons, Snow/Sand-Attribute, Designator-Zeichenregeln und neue 1.6-Pflanzenfelder prüfen.

**Exit-Test:** Nahrung kann angebaut/gelagert werden; Hanf landet nicht im Nahrungspool; jeder Output ist im UI nachvollziehbar.

### Task 3.4 – Wasser- und Brennstoffmodell

Definiere im Standalone-Modus:

- Wasserquelle,
- Wassergewinnung,
- Wasserlagerung oder Versorgung,
- Holzverbrennung,
- Kohleförderung/-verarbeitung,
- Brennstoffverbrauch,
- Blockade bei fehlendem Input.

Keine globale Wasserzahl ohne physische Logistik: Der Spieler muss erkennen, wo Wasser entsteht, gelagert und verbraucht wird.

**Exit-Test:** Generator kann ohne Wasser oder Brennstoff nicht dauerhaft laufen; Blockadegrund ist sichtbar.

### Task 3.4.1 – P0 Coal Chain (ABGESCHLOSSEN)

Implementiert: 2026-08-04

- `Rimconemy_Coal` ThingDef: Pyrolysis-Produkt aus Holz + Hanf
- `Rimconemy_MachineParts` ThingDef: Präzisionskomponenten aus SteelScraps
- `Rimconemy_CraftingStations` ThingCategoryDef: UI-Kategorie für Campfire
- `Rimconemy_MakeCoal` Recipe: 3 WoodLog + 2 HempLeafy → 4 Coal @ Campfire
- `Rimconemy_SalvageMachineParts` Recipe: 5 SteelScraps → 1 MachineParts @ Campfire
- WoodCoalGenerator: Separater Refuelable für Coal (fuelConsumptionRate=0.67 = 1.5× Effizienz)
- Campfire: 3 Rezepte wired (BurnSteelScraps, MakeCoal, SalvageMachineParts)

**Nicht belegt (Live-Gates):**
- MakeCoal → Generator-Effizienz im echten Spiel
- SalvageMachineParts-Ausbeute aus Ruinen-Abbruch
- Coal vs. WoodLog Brenndauer-Vergleich
- Save/Load der neuen Ressourcen

### Task 3.5 – Stromnetz und Generator

- Generator/Boiler als Gebäude oder klar definierte Anlage.
- Stromproduktion und -verbrauch messen.
- Prioritäten und Ausfallverhalten definieren.
- Stromnetz mit temporärer Vanilla-Energie kompatibel halten, bis Full Profile aktiv ist.
- keine versteckten kostenlosen Strompunkte.

**Exit-Test:** Produktion, Verbrauch, Speicher und Ausfall werden im UI angezeigt und deterministisch berechnet.

### Task 3.6 – Pfeilturm-Meilenstein

- strombetriebener Pfeilturm als eigene Verteidigungsanlage.
- Strombedarf als harter Betriebszustand.
- Reichweite, Zielwahl, Feuerrate, Schaden und Wartung festlegen.
- Pfeilturm ohne Strom sichtbar als `Blocked` statt als funktionierend.
- Eisen-Upgrades als spätere Progressions-Capability vorbereiten.

**Exit-Test:** Der erste funktionierende Pfeilturm benötigt alle definierten Inputs und verteidigt eine Testbasis automatisch.

### Task 3.7 – XP-/Research-Adapter

Optional prüfen:

- `Scavenging` für Bauschuttgewinnung,
- `Farming` für Pflanzenarbeit,
- `Power`/`Engineering` für Generator und Reparatur,
- `Building` für Wände/Türen,
- `Combat` für Turm-/Verteidigungsarbeit.

Ohne Paket 2 laufen diese Arbeiten mit Vanilla-Verhalten. Mit Paket 2 werden nur standardisierte Capability-/Arbeitstyp-IDs aktiviert.

**Gate:** Keine doppelte XP-Vergabe durch Vanilla-Job plus Rimconemy-Adapter.

### Task 3.8 – Economy-/Threat-Adapter

- Economy erkennt Bauschutt, Nahrung, Hanf, Holz, Kohle, Wasser und Erze als physische Waren.
- Infected erkennt aktive Farmfläche, Bewohner, Generatoren, Produktion und Verteidigung als Druckfaktoren.
- Fehlende Pakete dürfen keine Phantom-Wallets oder Phantom-Bedrohungswerte erzeugen.

**Exit-Test:** Ein Full-Profile-Test zeigt für einen Ressourcenbestand sowohl Marktstatus als auch Bedrohungsbeitrag mit derselben Quelle.

### Task 3.9 – DLC- und Vanilla-Kompatibilität

Prüfe besonders:

- Royalty-Bau-/Quest-/Trader-Belohnungen,
- Ideology-Pflanzen- und Rollenregeln,
- Biotech-Pollution, Xenotypen und Mechanoid-Baupfade,
- Anomaly-Pit-/Hediff-/Event-Inhalte,
- Odyssey-Weltkarten-, Transporter-, Gravship- und Expeditionspfade.

Zusätzlich prüfen:

- Rezepte und `ThingCategory`-Filter,
- Trading und MarketValue,
- Wealth-Berechnung,
- Storyteller-Raidstärke,
- Terrain-/Biome-Generierung,
- Caravan-Camp-Maps.

### Task 3.10 – Save-Migration

- Remapping-Version speichern.
- alte Bauschutt-/Vanilla-Ressourcen nicht still umdeuten.
- bestehende Bauwerke beim Laden validieren.
- nicht mehr gültige Rezepte, Pflanzen oder Kosten kontrolliert migrieren.
- den verbindlichen Fall aus `../../docs/SAVE_CONTRACT.md` ausführen: alter Ressourcen-/Remapping-Stand wird bis zur Validierung eingefroren oder kontrolliert abgelehnt.
- Warnung vor semantisch verändertem Save.

**Exit-Test:** Ein Save mit altem Remapping wird geladen, validiert und mit Ergebnis `Migrated`, `FrozenWithWarning` oder `LoadRejectedWithReason` protokolliert.

## 6. Blindspots und Gegenmaßnahmen

| Blindspot | Gegenmaßnahme |
|---|---|
| nur Wand-XML geändert | globale Def-/Recipe-/Trader-/Loot-/Quest-Inventur |
| Nahrungspflanze funktioniert, aber Hanf nicht | getrennte Plant-/Recipe-/Storage-/Trade-Tests |
| Farmen erzeugen Nahrung ohne Druck | aktiver Output und Druck aus derselben Snapshotquelle |
| Wasser ist nur eine UI-Zahl | Quelle, Lager, Verbrauch und Blockade als physischer Pfad |
| Generator läuft kostenlos | harte Input-/Wartungsregeln und UI-Prognose |
| Turm funktioniert ohne Strom | Betriebszustand muss von Power-Netz abhängen |
| Vanilla-Wealth skaliert weiter unpassend | späterer Threat-Adapter muss Wealth-Raid-Pfad explizit behandeln |
| neue 1.6-Plant-/Designator-Felder fehlen | 1.6-Def-Review und Platzierungstests |
| DLC-Rezepte umgehen Bauschutt | Def-/Recipe-/Quest-Inventur pro DLC |
| Arbeits-XP doppelt | Job-/XP-Trace mit eindeutiger Transaktions-ID |

## 7. Gemeinsamer Interface-Vertrag

- Kanonischer Besitzer von Ressourcen, Pflanzen, Wasser, Power und Turmstatus ist dieses Paket.
- Andere Pakete lesen `ResourceSnapshot` und `PowerSnapshot`; sie schreiben keine physischen Bestände direkt.
- Capability- und Snapshot-Regeln stehen verbindlich in `../../docs/INTERFACE_CONTRACT.md`.

## 8. Kompatibilitätsregeln

- Nur Harmony und RimWorld 1.6 als Build-Grundlage.
- Keine Pflichtabhängigkeit auf Pakete 1, 2, 4 oder 5.
- Foundation-/Survival-/Economy-/Infected-Integrationen laufen im Full Profile über den Foundation-Servicebus und versionierte Capabilities; ohne Foundation bleibt der Standalone-Modus.
- Native PatchOperations für reine Datenänderungen.
- Harmony nur für Verhalten, das nicht über eigene Defs/Komponenten lösbar ist.
- Temporäre Vanilla-Textur bleibt als bewusst markierter Prototyp.
- Jede Ressource besitzt eine stabile ID; Anzeigename darf sich ändern.

## 9. Performance-Gate

Ressourcen-, Pflanzen- und Power-Updates werden aggregiert; kein Outpost- oder Mechadroid-Placeholder darf im Standalone-Modus einen permanenten Pawn-Tick erzeugen. Produktionsdaten werden nicht von Economy und Infected jeweils neu berechnet.

**Messbares Exit-Kriterium:** P1, P2 und P3 aus `../../docs/INTERFACE_CONTRACT.md` laufen zehn Ingame-Tage mit höchstens 2 ms durchschnittlicher und 5 ms 99.-Perzentil-Updatezeit pro 60-Tick-Update, höchstens 1 MiB Netto-Speicherwachstum pro Ingame-Tag, ohne doppelte Produktion und mit höchstens 20 deduplizierten Diagnoseeinträgen pro Ingame-Tag; Kartenwechsel darf keine anderen Outputwerte erzeugen.

## 10. Falsifizierungs-Gate

Vor Übergabe müssen die drei Berichte `../../docs/FALSIFICATION_REPORTS/rimconemy.scavengerinfrastructure__ConstructionDebris.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.scavengerinfrastructure__FoodAndHemp.md` und `../../docs/FALSIFICATION_REPORTS/rimconemy.scavengerinfrastructure__WaterPowerArrowTurret.md` jeweils `SURVIVED` erreichen. Zusätzlich müssen `../../docs/FALSIFICATION_REPORTS/rimconemy.scavengerinfrastructure__ExecutePhysicalTransfer.md` und `../../docs/FALSIFICATION_REPORTS/rimconemy.economyterritory__ReservePhysicalTransfer.md` im Full Profile jeweils `SURVIVED` erreichen. Jeder Bericht braucht A–G mit eigenem Test, Ergebnis und Beleg; Vanilla-Bau-/Plant-/Powerpfade sowie Wasser-, Combat-, Def-/Recipe- und Load-Order-Fremdmodfälle werden nach `../../docs/COMPATIBILITY_MATRIX.md` klassifiziert.

## 11. Exit-Kriterien für Übergabe an Paket 4

- Standalone-Basebuilding-Loop ist im echten Spielablauf belegt.
- Bauschutt ersetzt Wände/Türen wirklich und nicht nur im UI.
- Nahrung/Hanf sind getrennt.
- Wasser/Brennstoff/Strom/Pfeilturm funktionieren und sind sichtbar.
- XP-/Research-Adapter sind doppelfrei.
- Markt- und Bedrohungsadapter liefern dieselben physischen Quellen.
- Vanilla-/DLC-/Save-Blindspots aus Abschnitt 6 sind geprüft.
