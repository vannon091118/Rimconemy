# VANILLA EARLY-BLUEPRINT MATRIX 1.6 — Phase-1 Vanilla-Anker für Rimconemy

> **Stand:** 2026-08-05
> **Quelle:** Lokale RimWorld-1.6.4566-Installation `/home/vannon/GOG Games/RimWorld/game/Data/`.
> **Rolle:** Phasen-Blueprint-Audit für Phase 1 (EarlySurvival) → Phase 2 (Production). Vor jedem Patch MUSS diese Matrix den Vanilla-Def-Beleg liefern. Stille Stufe-3-Patches (Replace ganze ThingDefs) sind explizit verboten.

---

## §1 Audit-Konvention

| Spalte | Bedeutung |
|---|---|
| **Def** | `defName` der Vanilla-Definition |
| **Pfad** | Lokaler XML-Pfad in der Core-Installation |
| **Z-Linie** | Ungefähre Zeilennummer (Versatz möglich) |
| **CostList** | Vanilla-Materialkosten (`<costList>` Block) |
| **StuffCategories** | Erlaubte `<stuffCategories>` |
| **Comps** | Vanilla-Component-Liste |
| **PlaceWorker** | Vanilla-Placement-Worker |
| **Research/Skill** | Vanilla-Voraussetzung |
| **Rimconemy-Phase** | Zugeordnete Phase aus PHASE_PROGRESSION_CONTRACT.md §2 |
| **PatchStrategy** | Stufe 1 (Add) / Stufe 2 (Replace-Skalar) / Stufe 3 (Replace-List) |
| **Empfehlung** | KEEP / ADD_RECIPE / ADD_STUFF / NO_TOUCH |

---

## §2 Phase-1 Blueprints (EarlySurvival)

| Def | Pfad | CostList | StuffCategories | Comps | PlaceWorker | Research/Skill | Phase | Stufe | Empfehlung |
|---|---|---|---|---|---|---|---|---|---|
| `Wall` | `Core/Defs/ThingDefs_Buildings/Buildings_Structure.xml` | `WoodLog 5` | `Woody` | (keine) | (keine) | (keine) | EarlySurvival | 3 | NO_TOUCH — Vanilla-Wand bleibt; Stuff-Option nur patchen, wenn Phase-Contract es verlangt |
| `Door` | `Buildings_Structure.xml` | `WoodLog 4` | `Woody` | (keine) | (keine) | (keine) | EarlySurvival | 3 | NO_TOUCH — Verhalten bleibt Vanilla |
| `Autodoor` | `Buildings_Structure.xml` | `Steel 6` | `Metallic` | `CompProperties_Power`, `CompProperties_Refuelable` (?) | (keine) | Smithing | Production+ | 3 | NO_TOUCH — Early-Setup erfährt keine Patch |
| `AnimalFlap` | `Buildings_Structure.xml` | `WoodLog 4` | `Woody` | (keine) | (keine) | (keine) | EarlySurvival | 3 | NO_TOUCH |
| `Sandbags` | `Buildings_Security.xml` | `Fabric 4` | `Fabric` | (keine) | (keine) | (keine) | EarlySurvival | 2 | OPTIONAL — Debris als zusätzliche Stuff-Option ist sinnvoll; Stufe-2-Patch |
| `Barricade` | `Buildings_Security.xml` | `WoodLog 8` | `Woody` | (keine) | (keine) | (keine) | EarlySurvival | 2 | OPTIONAL — `Rimconemy_ConstructionDebris` als Stuff-Option via Stufe-2-Patch |
| `TrapSpike` | `Buildings_Security.xml` | `Steel 4` | `Metallic` | (keine) | (keine) | Construction 3 | Production | 3 | NO_TOUCH — Stahl nicht aufblasen |
| `CraftingSpot` | `Buildings_Production.xml` | (keins) | (keine) | (keine) | (keine) | (keine) | EarlySurvival | 1 | ADD_RECIPE (`Rimconemy_BurnSteelScraps`) hinter Research-Gate Early-Survival-Cookery |
| `HandTailoringBench` | `Buildings_Production.xml` | `WoodLog 25` | `Woody` | (keine) | (keine) | ComplexClothing | Production | 1 | ADD_RECIPE (Hemp-Cloth) |
| `FueledStove` | `Buildings_Production.xml` | `WoodLog 30` | `Woody` | `CompProperties_Refuelable (WoodLog)`, `PlaceWorker_Heater` | `PlaceWorker_Heater` | Electricity (Nope — ist Cooking-Station) | EarlySurvival | 1 | NO_TOUCH — Vanilla-Food-Chain reicht |
| `SimpleResearchBench` | `Buildings_Production.xml` | `WoodLog 50` | `Woody` | `ResearchBench`-tag | (keine) | (keine) | EarlySurvival | 3 | NO_TOUCH |
| `TorchLamp` | `Buildings_Furniture.xml` | `WoodLog 20` | `Woody` | `CompProperties_Glower` | `PlaceWorker_PreventInteractionSpotOverlap` | (keine) | EarlySurvival | 3 | NO_TOUCH |
| `Campfire` | `Buildings_Temperature.xml` | `WoodLog 20` | `Woody` | `CompProperties_Refuelable (WoodLog)`, `CompProperties_Glower`, `CompProperties_HeatPusher` | `PlaceWorker_PreventInteractionSpotOverlap`, `PlaceWorker_Heater`, `PlaceWorker_GlowRadius` | (keine) | EarlySurvival | 3 | NO_TOUCH — siehe `campfire-parity-1.6.md` |

---

## §3 Phase-2 Blueprints (Production)

| Def | Pfad | CostList | StuffCategories | Comps | Research/Skill | Phase | Stufe | Empfehlung |
|---|---|---|---|---|---|---|---|---|
| `FueledSmithy` | `Buildings_Production.xml` | `Steel 30 + WoodLog 50` | `Woody` (?), `Metallic (?)` | `CompProperties_Refuelable (WoodLog)`, `CompProperties_HeatPusher`, `CompProperties_Flickable` | Smithing | Production | 1 | ADD_RECIPE (`Rimconemy_BurnSteelScraps`) — Stufe-1-PatchOperationAdd auf `<recipes>` |
| `TableStonecutter` | `Buildings_Production.xml` | `WoodLog 25 + Steel 30` | (keine) | (keine) | Machining (?) | Production | 3 | NO_TOUCH |
| `TableMachining` | `Buildings_Production.xml` | `Steel 50 + WoodLog 30 + ComponentIndustrial 4` | (keine) | (keine) | Machining | Automation | 1 | ADD_RECIPE (`Rimconemy_SalvageMachineParts`) |
| `ElectricTailoringBench` | `Buildings_Production.xml` | `Steel 50 + Cloth 30 + ComponentIndustrial 4` | (keine) | `CompProperties_Power` | ComplexClothing + Electricity | Automation | 1 | ADD_RECIPE (Industrial-Cloth) |

---

## §4 Phase-3 Blueprints (Automation)

| Def | Pfad | CostList | Comps | Research/Skill | Phase | Stufe | Empfehlung |
|---|---|---|---|---|---|---|---|
| `WoodFiredGenerator` | `Buildings_Power.xml` | `WoodLog 100 + Steel 20` | `CompProperties_Refuelable (WoodLog)`, `CompProperties_Power`, `CompProperties_HeatPusher`, `CompProperties_Breakdownable`, `CompProperties_Flickable` | Electricity | Automation | 1 | OPTIONAL — Coal als zusätzliche `fuelFilter` Option nach Phase-Gate-`MakeCoal` |
| `WindTurbine` | `Buildings_Power.xml` | `Steel 50 + ComponentIndustrial 4` | `CompProperties_Power`, `CompProperties_Breakdownable` | Electricity + Construction 4 | Automation | 3 | NO_TOUCH |
| `Battery` | `Buildings_Power.xml` | `Steel 50 + ComponentIndustrial 4` | `CompProperties_PowerTrader`, `CompProperties_Breakdownable`, `CompProperties_Forbiddable` | Batteries research | Automation | 3 | NO_TOUCH |
| `SolarGenerator` | `Buildings_Power.xml` | `Steel 50 + ComponentIndustrial 4` | `CompProperties_Power`, `CompProperties_Breakdownable` | SolarPanels + Construction 6 | Automation | 3 | NO_TOUCH |
| `Turret_MiniTurret` | `Buildings_Security_Turrets.xml` | `Steel 35 + ComponentIndustrial 2` | `CompProperties_TurretGun`, `CompProperties_Power`, `CompProperties_Mannable` | GunTurrets + Construction 5 | Production+ | 3 | NO_TOUCH — Hochwert-Material nicht aufblasen |

---

## §5 Phase-4 Blueprints (Trade)

| Def | Pfad | CostList | Comps | Research | Phase | Stufe | Empfehlung |
|---|---|---|---|---|---|---|---|
| `CommsConsole` | `Buildings_Production.xml` | `Steel 50 + ComponentIndustrial 4 + ComponentSpacer 2 (Anomaly?)` | `CompProperties_Power` | Communications | Trade | 3 | NO_TOUCH |
| `OrbitalRelay` | `Anomaly/...` | (DLC-gebunden) | (DLC-gebunden) | (DLC-gebunden) | Trade | n/a | DLC-only |

---

## §6 Patch-Stufe-Rubrik

**Stufe 1 (Default):**

- `PatchOperationAdd` für Recipe-Lists (`<recipes>` oder `<recipeUsers>`) auf existierenden Def.
- `PatchOperationConditional` für PATH-Existenz-Checks.
- `PatchOperationSequence` für geordnete Multi-Patches.
- `PatchOperationFindMod` für DLC-gating.

**Stufe 2 (Review erforderlich):**

- Ersetzen **eines skalaren Werts** (z. B. `costStuffCount`, einzelnes Ingredient-Count).
- Hinzufügen einer StuffCategory zu einer existierenden Stuff-Liste.
- Hinzufügen einer Fuel-Option zur `CompProperties_Refuelable.fuelFilter` (kein Replace!).

**Stufe 3 (Verboten ohne Live-Test):**

- Vollständiges Ersetzen von `costList`, `compProperties`, `graphicData`, `placeWorkers`, `recipes`-Listen.
- Whole-Def-Ersetzung via entferntem Original (Anti-Pattern).
- Whole-Comps-Replacement auf einem Building mit aktiven Comps.

**Regel:** Wer Stufe 3 anwendet, muss in demselben Patch einen Live-Test-Beleg im `verify_bootstrap_log.sh` und eine Falsifikations-Datei in `docs/falsification/` erzeugen.

---

## §7 Akzeptanz-Gate

- Jeder Eintrag in §2–§5 hat einen Vanilla-Pfad-Beleg.
- Keine `Empfehlung` ist `REPLACE_DEF` ohne Stufe-3-Begründung + Live-Test.
- Phase-1-Blueprints bleiben überwiegend `NO_TOUCH` — Rimconemy macht EarlySurvival besser durch zusätzliche Vanilla-Recipes, nicht durch Override.
- Coal erscheint nur als optionaler Fuel-Filter-Add (Stufe 1), nicht als Vanilla-Force-Replace auf `WoodFiredGenerator`.
- Kein Stufe-2/3-Patch ohne Commits in `docs/falsification/`.
