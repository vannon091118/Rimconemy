# CAMPFIRE PARITY 1.6 — Vergleich Vanilla vs. Rimconemy_Campfire

> **Stand:** 2026-08-05
> **Rolle:** Migrationsentscheidung `Rimconemy_Campfire` vs. Vanilla `Campfire`. Vor jeder Löschung MUSS diese Parität nachgewiesen werden. Default-Empfehlung: **KEEP_DISTINCT** bis eine Live-Paritätsprüfung anders entscheidet.

---

## §1 Vanilla Campfire (Core 1.6.4566)

Quelle: `game/Data/Core/Defs/ThingDefs_Buildings/Buildings_Temperature.xml` (Zeile 4+).

| Property | Wert |
|---|---|
| `defName` | `Campfire` |
| `ParentName` | (keine direkte; basiert auf `FilthProducer`-Chain) |
| `thingClass` | `Building_WorkTable` (Workstation, brandet auf Work-Bill) |
| `graphicData.texPath` | `Things/Building/Misc/Campfire` |
| `graphicData.graphicClass` | `Graphic_Single` |
| `tickerType` | `Normal` |
| `passability` | `PassThroughOnly` |
| `interactionCellOffset` | (Vanilla-Default `0,0,-1`) |
| `size` | `(1,1)` |
| `costList` | `WoodLog 20` |
| `MaxHitPoints` | `80` |
| `WorkToBuild` | `200` |
| `MarketValue` | (Vanilla-Default) |
| `WorkTableWorkSpeedFactor` | `0.5` (Workstations sind Faktor-0.5) |
| **Comps:** `CompProperties_Refuelable` | `fuelFilter: { thingDefs: [WoodLog] }`, `fuelCapacity: 20`, `fuelConsumptionRate: 10`, **einziger Refuelable** |
| **Comps:** `CompProperties_Glower` | `glowRadius: 10`, `glowColor: (252,187,113,0)` |
| **Comps:** `CompProperties_HeatPusher` | (Heat-Push an Nachbarzellen) |
| **Comps:** `CompProperties_Flickable` | (An/Aus-Schalter) |
| **Comps:** `CompProperties_Forbiddable` | (Zonen-Verbots-Flag) |
| **PlaceWorkers** | `PlaceWorker_PreventInteractionSpotOverlap`, `PlaceWorker_Heater`, `PlaceWorker_GlowRadius` |
| `designationCategory` | (Vanilla: Production) |
| `researchPrerequisites` | (keine — Vanilla ist frei platzierbar) |
| Rezeptliste | (leer — Vanilla hat keine Rezept-Liste am Campfire; ist reine Feuer-Stelle) |

**Wichtig:** Vanilla `Campfire` ist primär **Heiz- und Licht-Stelle**, **NICHT** als Workstation-Rezept-Stelle gedacht. Vanilla-Rezepte nutzen den Campfire nicht als `recipeUser`. Das ist ein wichtiger Architektur-Hinweis für Rimconemy.

---

## §2 Rimconemy_Campfire (Mod 03, Build-Stand)

Quelle: `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/BuildingDefs/Campfire.xml`.

| Property | Wert | vs. Vanilla |
|---|---|---|
| `defName` | `Rimconemy_Campfire` | abweichend |
| `ParentName` | `BuildingBase` | Vanilla erbt anders |
| `thingClass` | `Building_WorkTable` | identisch |
| `graphicData.texPath` | `Things/Building/Misc/Campfire` | **identisch** — gleiche Textur |
| `tickerType` | `Normal` | identisch |
| `costList` | (nicht gesetzt) | Vanilla: `WoodLog 20` → hier fehlt |
| `MaxHitPoints` | `120` | Vanilla: `80` (Rimconemy ist +50%) |
| `WorkToBuild` | `600` | Vanilla: `200` (Rimconemy ist 3× höher) |
| `WorkTableWorkSpeedFactor` | `1.0` | Vanilla: `0.5` (Rimconemy ist 2× schneller) |
| **Rezepte explizit** | `Rimconemy_BurnSteelScraps`, `Rimconemy_MakeCoal`, `Rimconemy_SalvageMachineParts`, `Rimconemy_MakeStainlessSteel` | **Vanilla hat keine Rezept-Liste am Campfire!** |
| **Comps** | nur `CompProperties_Glower` (Radius 5, Color(255,200,100,200)) | Vanilla hat zusätzlich Refuelable, HeatPusher, Flickable, Forbiddable |
| **PlaceWorkers** | (keine) | Vanilla hat 3 PlaceWorkers |
| `thingCategories` | `Rimconemy_CraftingStations` | (Custom-Kategorie, kein Vanilla-Anker) |
| `recipes` | s. o. | Vanilla-Campfire hat keine eingebauten Rezepte |

---

## §3 Paritäts-Bewertung

| Aspekt | Parität | Drift |
|---|---|---|
| **ThingClass** | ✅ Identisch (`Building_WorkTable`) | Keine |
| **TexPath** | ✅ Identisch | Keine |
| **Glower** | 🟡 Drift | Rimconemy-Radius 5 vs. Vanilla 10; Farbwert (255,200,100,200) vs. Vanilla (252,187,113,0) |
| **HeatPusher** | ❌ Fehlt | Vanilla hat es; Rimconemy fehlt — keine Heizleistung |
| **Refuelable** | ❌ Fehlt | Vanilla hat `fuelFilter=WoodLog, capacity=20, consumeRate=10`; Rimconemy hat keinen Refuel-Comp → brennt nicht auf, kühlt nicht ab |
| **Flickable** | ❌ Fehlt | Vanilla an/aus; Rimconemy fehlt |
| **Forbiddable** | ❌ Fehlt | Vanilla hat's; Rimconemy hat's nicht |
| **PlaceWorkers** | ❌ Fehlt | Vanilla: 3 PlaceWorkers; Rimconemy: keine |
| **Rezepte** | 🟢 Bewusst abweichend | Rimconemy hat Rezept-Liste, Vanilla nicht |
| **StatBases** | 🟡 Drift | Rimconemy ist 2× robuster, 3× teurer, 2× schneller |
| **costList** | ❌ Rimconemy fehlt | Vanilla verlangt 20 WoodLog zum Bau; Rimconemy hat keinen Cost-Block → muss durch parent-Layer gesetzt sein |
| **Research/Designation** | 🟡 Drift | Vanilla frei platzierbar in Production-Category; Rimconemy in eigener `Rimconemy_CraftingStations` |

**Gesamtbild:** Rimconemy_Campfire ist **kein Drop-in-Patch** auf Vanilla_Campfire, sondern eine **eigenständige Workstation-Variante mit anderer Spielmechanik**. Es ist KEIN visueller oder physikalischer 1:1-Ersatz.

---

## §4 Entscheidungsmatrix

| Frage | Antwort |
|---|---|
| Ist `Rimconemy_Campfire` visuell identisch zu Vanilla? | **Ja** (gleiche `texPath`) |
| Ist `Rimconemy_Campfire` mechanisch identisch zu Vanilla? | **Nein** (Rezepte, Refuelable, Flickable fehlen) |
| Würde ein Löschen + Vanilla-Patchen das Verhalten erhalten? | **Nein** — Rimconemy braucht die Rezept-Liste und das veränderte Work-Table-Verhalten |
| Sind Rimconemy-spezifische Rezept-Listen ersetzbar durch `<PatchOperationAdd>` auf Vanilla? | **Ja**, ID-stabil |
| Sind Rimconemy-spezifische StatOffsets (MaxHitPoints 120, WorkToBuild 600) ersetzbar durch Stufe-2-Patches? | **Ja**, aber pro Vanilla-Def nur ein Eintrag; pro Campfire-Aufruf keine Doppel-Stat-Override |
| Sind Rimconemy-spezifische Rezept-Bindungen (`recipeUser`-Patches) sauber? | **Bedingt** — Vanilla-Campfire hat keine `<recipes>` im ParentName; ein Add-Patch auf XPath `/Defs/ThingDef[defName="Campfire"]/recipes` ist nicht trivial, weil es dort kein `recipes`-Element gibt |

---

## §5 Default-Empfehlung: **KEEP_DISTINCT**

**Begründung:**

1. Rimconemy_Campfire ist eine **Workstation-Variante** mit Rezept-Liste; Vanilla-Campfire ist **reine Feuer-Stelle ohne Rezept-Liste**. Der Funktionsumfang ist anders.
2. Eine Migration zu Vanilla bräuchte **drei aufeinanderfolgende Stufe-1-Patches** (Add `<recipes>`, Add Refuelable Replacements, Add PlaceWorkers-Replace). Kombiniert ist das faktisch Stufe 3 (Listen-Replace). Stufe 3 ist verboten ohne Live-Test.
3. Eine Migrations-Liveprüfung bedeutet: Campfire bauen, Rezept auswählen, Fuel laden, Anzünden, Work-Speed messen. Solange diese Live-Test-Pipeline fehlt, ist Migration **technisches Vabanque**.
4. KEEP_DISTINCT erhält die **Ownership-Klarheit** und macht Mod-03-Owner-Map stabil.

**Migrations-Voraussetzungen (für späteren Wechsel):**

1. Live-Test-Skript存在, das Vanilla + Patches vs. Rimconemy_Campfire funktional vergleicht (Work-Speed, Glower-Radius, Rezept-Availability, Game-Boot).
2. Eine Patch-Suite existiert, die in `verify_bootstrap_log.sh` Regression-Testet.
3. Eine Falsifikations-Datei unter `docs/falsification/campfire__VanillaMigration.md` dokumentiert das Ergebnis.

**Bis diese Voraussetzungen erfüllt sind:**

- `Rimconemy_Campfire` bleibt eigenständig.
- Phase-Gate für Rimconemy_Campfire ist `EarlySurvival` mit Research-Voraussetzung `Campfire` (so dass das Bauen dokumentiert bleibt).
- Rezepte auf Rimconemy_Campfire bleiben Phase-gated: BurnSteelScraps ab Production, MakeCoal ab Automation, SalvageMachineParts ab Automation, MakeStainlessSteel ab Late Production.

---

## §6 Pflicht-Roadmap

| Schritt | Verantwortlich | Status |
|---|---|---|
| 1. Phase-Gate in `BurnSteelScraps.xml` setzen (Recipe nur in Production) | Mod 03 | OFFEN |
| 2. `MakeCoal.xml` `<workSkill>Cooking</workSkill>` + `skillRequirements` (>=3) ergänzen | Mod 03 | OFFEN |
| 3. Rimconemy_Campfire-Rezepte: BurnSteel/MakeCoal/Salvage/MakeStainlessSteel alle phase-hinter `<researchPrerequisites>` | Mod 03 | OFFEN |
| 4. Falsifikations-Datei `docs/falsification/campfire__PhaseGating.md` schreiben | Doc-Owner | OFFEN |
| 5. Re-evaluate MIGRATE_TO_VANILLA-Entscheidung in 6 Monaten | Architektur-Owner | OFFEN |

---

## §7 Akzeptanz-Gate

- Es existiert eine dokumentierte Empfehlung (`KEEP_DISTINCT` oder `MIGRATE_TO_VANILLA`).
- Drift-Spalte zeigt pro Property einen exakten Wertvergleich.
- Begründung nennt mindestens eine Live-Prüfungs-Lücke.
- Kein Phase-1-Rezept ist auf Rimconemy_Campfire exponiert ohne Phase-Gate.

---

**Stand:** 2026-08-05 · **Quelle Vanilla:** lokale RimWorld 1.6.4566 GOG-Installation · **Empfehlung:** KEEP_DISTINCT bis Phase-Gating + Live-Test-Migrations-Suite vorliegen.
