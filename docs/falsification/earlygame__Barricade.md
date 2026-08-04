# Falsifizierungs-Stand-Bericht: Barrikade (Phase 4.1 / 4.2)

**Status:** 🔴 **NICHT BELegt** — Template-Stand, wartet auf `SURVIVED`-Lauf.
**Gate-Zuordnung:** Vertical-Slice-Plan §Phase 4 (`docs/superpowers/plans/2026-08-04-early-game-vertical-slice.md`).
**Letzter belegter Code-Stand:** siehe unten A–E.
**Erforderliche Beweise:** siehe §7 Akzeptanz-Gate.

---

## 1. Ziel des Gates

Der Spieler muss nach Sammeln von Holz und Stahlresten eine **definierte Holz-Stahl-Tier-1-Barrikade (1 Holz + 1 Stahlrest)** in der Architect-Kategorie **Zuflucht** platzieren und fertigstellen können, ohne dass Vanilla-Barrikaden, Reparaturpfade oder andere Bills die fehlenden Materialien umgehen. Die Materialreservierung muss echt sein, Bauabbruch muss Materialien korrekt zurückgeben, und Save/Load muss den Bauzustand erhalten.

Phase 4.1/4.2 folgt direkt auf Phase 2.1/2.2 (Stahlreste in Startzone) und vor Phase 7 (1. Nacht).

**Pflicht-Szenario für LIVE-Belege:** `Rimconemy_SingleSurvivor` (`mods/02/Defs/Scenarios/SingleSurvivor.xml`), Save-Slot 1. Vorbedingung: Scout-Phase-1-Gate (Survivor + Stahlreste-Streuung) muss grün sein, sonst fehlt das Input-Material.

---

## 2. Vanilla-/Architektur-Anker

| Hook | 1.6-Status | Quelle |
|---|---|---|
| Eigenes `ThingDef` als Building | ✅ bestätigt | `docs/vanilla-api-matrix-1.6.md` §ThingComp |
| `costList` Mischkosten | ⚠️ spike-pflicht (genaues costList-Schema in 1.6) | Vanilla-`ThingDef`-Def-Schema |
| `designationCategory` → `Rimconemy_Shelter` | ⚠️ spike-pflicht (Eigenkategorie erlaubt?) | Vanilla-`DesignationCategoryDef` |
| Bauabschluss-Hook (XP-Vergabe folgt erst in Phase 8.3) | ❌ **OFFEN** | Phase 8.3 in Vertical-Slice-Plan |
| Reservierungs-/Abbruch-Verhalten (Materials zurück) | ⚠️ spike-pflicht | Vanilla-`Blueprint_Build`-Pfad |

> **Spike-Pflicht:** Vor Implementierung muss ein 1-Zeilen-Test gegen die lokale 1.6-`Assembly-CSharp.dll` zeigen, dass `costList` mit zwei ThingDefs funktioniert und dass ein eigener `DesignationCategoryDef` im Architect sichtbar wird.

---

## 3. CODE — vorläufige Stubs

| Pfad | Zustand |
|---|---|
| `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/BuildingDefs/Rimconemy_Tier1Barricade.xml` | 🔴 offen |
| `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/DesignationCategoryDefs/Rimconemy_Shelter.xml` | 🔴 offen |
| `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Building/Rimconemy_Tier1BarricadeComp.cs` (optional, falls HP-Override nötig) | 🔴 offen |

Aktuelle Stubs (Referenz — Vertical-Slice-Plan §Phase 4.1):

```xml
<!-- mods/03-Rimconemy-Scavenger-Infrastructure/Defs/BuildingDefs/Rimconemy_Tier1Barricade.xml -->
<ThingDef ParentName="BuildingBase">
  <defName>Rimconemy_Tier1Barricade</defName>
  <label>Holz-Stahl-Barrikade</label>
  <thingClass>Building</thingClass>
  <category>Building</category>
  <altitudeLayer>Building</altitudeLayer>
  <fillPercent>0.8</fillPercent>
  <blockLight>true</blockLight>
  <blockWind>true</blockWind>
  <passability>Impassable</passability>
  <statBases>
    <MaxHitPoints>80</MaxHitPoints>
    <WorkToBuild>80</WorkToBuild>
    <Flammability>0.8</Flammability>
  </statBases>
  <costList>
    <WoodLog>1</WoodLog>
    <Rimconemy_SteelScraps>1</Rimconemy_SteelScraps>
  </costList>
  <designationCategory>Rimconemy_Shelter</designationCategory>
</ThingDef>
```

```xml
<!-- mods/03-Rimconemy-Scavenger-Infrastructure/Defs/DesignationCategoryDefs/Rimconemy_Shelter.xml -->
<DesignationCategoryDef>
  <defName>Rimconemy_Shelter</defName>
  <label>Zuflucht</label>
  <order>10</order>
</DesignationCategoryDef>
```

---

## 4. TESTS — vorläufige Stubs

| Pfad | Zustand |
|---|---|
| `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/Tier1BarricadeCostListTests.cs` | 🔴 offen |
| `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/ShelterCategoryVisibilityTests.cs` | 🔴 offen |

> **Hinweis:** Solange kein LIVE-Lauf vorliegt, sind Tests nur Compile- und Schema-Checks. Sie beweisen nicht, dass die Barrikade im Architect erscheint oder Materialien korrekt reserviert werden.

---

## 5. Bausteine / externe Verträge

| Vertrag | Quelle |
|---|---|
| Eigenes `costList`-Schema (statt Stuff-Wand) | Vertical-Slice-Plan §Phase 4.1 |
| Eigene DesignationCategory statt Vanilla-Misc | Vertical-Slice-Plan §Phase 4.2 |
| Reservierung gegen `Rimconemy_SteelScraps` + `WoodLog` | Vertical-Slice-Plan §Phase 2.1 |
| Reparatur-/Rebuild-Gate (Phase 9.2) | Vertical-Slice-Plan §Phase 9 |

---

## 6. Was fehlt bis `SURVIVED`

- [ ] A — Spike-Beweis: 1.6-`costList` mit zwei ThingDefs kompiliert
- [ ] B — Spike-Beweis: `DesignationCategoryDef` erscheint im Architect
- [ ] C — Barrikade platzierbar mit korrekter Materialreservierung
- [ ] D — Bauabschluss verbraucht exakt 1 Holz + 1 Stahlrest
- [ ] E — Bauabbruch gibt beide Materialien korrekt in die Storage-Group zurück
- [ ] F — Save/Load nach teilgebauter Barrikade erhält Baufortschritt ohne Drifth
- [ ] G — Tag-/Nachtwechsel überlebt Barrikade als BuildingSnapShot-Eintrag (siehe `BuildingSnapshotService`)

> Hinweis A–G entspricht den Akzeptanz-Punkten aus Vertical-Slice-Plan §Phase 4.1/4.2.

---

## 7. Akzeptanz-Gate

| Punkt | Beleg-Typ | Quelle |
|---|---|---|
| Materialkosten | Companion decompile + XML-Read | `Assembly-CSharp.dll` Def-Schema |
| Architect-Sichtbarkeit | In-Game Screenshots | Laufzeit-Reports |
| Reservierungs-Effekt | Designer-Reports / Save-Diff | Save-Inspect |
| Abbruch-Verhalten | Designer-Reports | Save-Inspect |

---

## 8. Verweise

- `docs/superpowers/plans/2026-08-04-early-game-vertical-slice.md` §Phase 4
- `docs/vanilla-api-matrix-1.6.md` §ThingComp
- `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/BuildingCoreRegressionTests.cs` (Blueprint-Vorbild)
- `docs/falsification/scavenger__ConstructionDebris.md` (Schwesterbericht, Bauschutt-Remap)
