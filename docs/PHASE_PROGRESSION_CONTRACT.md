# PHASE PROGRESSION CONTRACT — Rimconemy Gameplay Phases SSOT

> **Stand:** 2026-08-05
> **Rolle:** SSOT für die Gameplay-Phasen. Definiert **wann** ein System spielerisch relevant wird.
> **Pflicht:** Wer eine Phase-Zuordnung ändert, aktualisiert diese Datei **und** den Eintrag in [ARCHITECTURE.md §7] sowie den Owner-Crosswalk in [INTERFACE_CONTRACT.md §9].
> **Detail:** Architektur-Einordnung und DLC-Leverage-Pivot stehen in [ARCHITECTURE.md](ARCHITECTURE.md); Vanilla-/DLC-Anker in [CANONICAL_VANILLA_DOMAIN_MAP.md](CANONICAL_VANILLA_DOMAIN_MAP.md); Save-Schema in [SAVE_CONTRACT.md](SAVE_CONTRACT.md); Live-Status in [CODE_STATUS.md](CODE_STATUS.md).

---

## §1 Phasen-Übersicht

Rimconemy definiert **sechs Gameplay-Phasen** in fester Reihenfolge:

| # | Phase | Hauptentscheidung | Frühe Sichtbarkeit erlaubt? |
|---|---|---|---|
| 1 | `EarlySurvival` | Brennstoff vs. Bauholz; Risiko vs. Sicherheit | Ja — `ConstructionDebris`, seltene `SteelScraps` |
| 2 | `Production` | Erste kontrollierte Stahl-Verarbeitung; Werkbank-Wahl | Ja — Rezept-Vorbereitung |
| 3 | `Automation` | Brennstoff-Wahl (Holz vs. Kohle); Stromallokation | Selten — Coal als seltene Beute möglich |
| 4 | `Trade` | Verkaufen, Horten, Tribut-Bedienen | Royalty-Fraktion als entfernt sichtbar |
| 5 | `Expansion` | Outpost vs. lokale Festigung; Expedition vs. Karawane | Odyssey-Ruinen als früh-lootbare Mini-Stash |
| 6 | `Empire` | Mehr-Standort-Wirtschaft; politischer Druck | Keine |

Eine Phase darf ein Feature **maximal eine Phase früher** sichtbar machen, niemals früher produzieren.

---

## §2 Phasen-Matrix (Ressourcen × Verfügbarkeit)

| Resource | EarlySurvival | Production | Automation | Trade | Expansion | Empire |
|---|---|---|---|---|---|---|
| `WoodLog` | Visible + Lootable + Strategic | sichtbar als Brennstoff | Brennstoff + Coal-Vorstufe | handelbar | reisend | massenhaft |
| `Stone` | Strategic | statisch | statisch | handelbar | outpost-tauglich | statisch |
| `Rimconemy_ConstructionDebris` | Visible + Lootable + Strategic | Recycling | Recycling | handelbar | gesammelt | verarbeitet |
| `Food` | Strategic | stabil | stabil | handelbar | karawanen-tauglich | verwaltet |
| `Rimconemy_SteelScraps` | Visible + Lootable (sehr selten) | Producible Eingang (5:1) | 5:1 Eingang für Steel oder MachineParts | handelbar | expeditions-loot | verarbeitet |
| `Steel` (Vanilla) | **Producible: NEIN** | Producible 5:1 aus Scraps | Strategic | handelbar | outpost | imperial |
| `Rimconemy_Coal` | **Producible: NEIN** | **Producible: NEIN** | Producible aus Wood + Hemp | handelbar | transportfähig | strategisch |
| `Rimconemy_MachineParts` | **Producible: NEIN** | **Producible: NEIN** | Producible 5:1 aus Scraps | handelbar | expeditions-loot | industrial |
| `Rimconemy_StainlessSteel` | **Producible: NEIN** | **Producible: NEIN** | **Producible: NEIN** | erste Verkäufe | Strategic (Towers) | imperial |
| `Credits` (Vanilla) | KEIN Wallet-Startwert | Mapped via Baby-Step | Mapped | Strategic | karawanen-fähig | imperial |
| `Rimconemy_HempLeafy` | Lootable + Strategic | Coal-Vorstufe | Coal-Vorstufe | handelbar | karawanen-tauglich | industrial |

**Verfügbarkeits-Stufen sind orthogonal zu Phase-Positionen:**

```text
Visible       = Spieler sieht die Ressource oder ihre Existenz (Label, Recipe-Label, Hit-Pulse).
Lootable      = Spieler findet eine begrenzte Menge als Risiko/Belohnung.
Producible    = Eine wiederholbare Bill/Recipe ist verfügbar (ganze Phase-Zeit verfügbar).
Strategic     = Die Ressource ist ein verlässlicher Senke für die aktuelle Phase.
```

Eine Ressource darf **eine Phase früher** lootable sein, aber ihre wiederholbare Produktion beginnt frühestens in ihrer zugewiesenen Phase.

---

## §3 Phasen-Übergänge (Milestones statt Tages-Counts)

```text
EarlySurvival → Production:
  stabile Behausung + erste Salvage-Station + bestätigte Nahrungsschleife

Production → Automation:
  kontrollierter Stahl-Output + erreichbarer Smithy-Weg + Brennstoff-Reserve

Automation → Trade:
  stabile Strom- und Produktionskette + Überschuss an Hochwert-Outputs + sicherer Transport

Trade → Expansion:
  Reserve-Lager + Karawanen-/Outpost-Fähigkeit + Versöhnungspfad (Royalty-Tribute)

Expansion → Empire:
  >1 strategischer Standort ODER validierter politischer/territorialer Pfad
```

Kein Übergang allein durch Tag-Count ausgelöst. Übergänge werden über beobachtete Milestones validiert.

---

## §4 Ressourcen-SSOT & Owner-Regel (Ownership-Single-Source)

| Resource | Owner-Paket | Kanonische Def-Datei | Rollen |
|---|---|---|---|
| `WoodLog` | Vanilla (Core) | `game/Data/Core/Defs/ThingDefs_Items/Items_Resource_Stuff.xml` | fuel, construction, Coal input |
| `Steel` | Vanilla (Core) | `game/Data/Core/Defs/ThingDefs_Items/Items_Resource_Stuff.xml` | construction, power, defense, trade |
| `Plasteel` | Vanilla (Core) | `game/Data/Core/Defs/ThingDefs_Items/Items_Resource_Stuff.xml` | high-end material (nicht-patch) |
| `Chemfuel` | Vanilla (Core) | `game/Data/Core/Defs/ThingDefs_Items/Items_Resource_Stuff.xml` | advanced fuel (nicht-patch) |
| `Rimconemy_ConstructionDebris` | **Mod 03 (Canon)** | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/ConstructionDebris.xml:13+` | early cover/building/scavenge role |
| `Rimconemy_SteelScraps` | **Mod 03 (Canon)** | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/SteelScraps.xml:16+` | early loot; Production/Automation 5:1 Steel **oder** 5:1 MachineParts |
| `Rimconemy_Coal` | **Mod 03 (Canon)** | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/Coal.xml:15+` | Automation-Fuel |
| `Rimconemy_MachineParts` | **Mod 03 (Canon)** | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/MachineParts.xml:16+` | Automation/Repair/High-Tier |
| `Rimconemy_StainlessSteel` | **Mod 03 (Canon)** | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/StainlessSteel.xml:16+` | Late Expansion/Empire |
| `Rimconemy_DistilledWater` | **Mod 03 (Canon)** | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/Water.xml:12+` | Optionaler Wasserpfad |
| `Rimconemy_HempLeafy` | **Mod 03 (Canon)** | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/HempLeafy.xml` | Coal-Vorstufe + Tailoring |

**Verletzungen der SSOT-Regel:**

- Eine zweite Def-Datei mit gleichem `defName` → **Verboten**. Der zweite Owner erzeugt Last/Order-Warnungen und Test-Drift.
- Eine `Def`-Datei außerhalb des Owner-Pakets → **Verboten**, außer als additive Patch via `<PatchOperationFindMod>`.
- Mod 02 C#-Code darf `DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_SteelScraps")` machen, aber **niemals** einen `<defName>Rimconemy_SteelScraps</defName>`-Block enthalten.

---

## §5 Negative-Regeln (Anti-Pattern)

| Negativ-Regel | Begründung |
|---|---|
| Kein wiederholbares Coal/MachineParts/Steel-Rezept in EarlySurvival | Zerstört sonst die Phase-Dramaturgie |
| Kein zweiter `CompProperties_Refuelable` auf einem BuildingDef | Vanilla instanziert nur den ersten; weitere sind Dead-Code |
| Kein `Rimconemy.Masonry` oder neue StuffCategory | Vanilla deckt mit `Stony`/`Metallic`/`Woody`/`Fabric`/`Leathery` ab |
| Keine Persistence für Mining-Gate ohne dokumentierten Ausnahmezustand | Skill entscheidet live, Save-Bloat vermeiden |
| Kein eigenes WorkType-/JobDriver-/TraderKind-/HediffDef-/ResearchProjectDef | Vanilla-/DLC-Anker sind robuster gegen Version-Drift |
| Kein PatchOperationReplace auf `costList`, `compProperties` oder `graphicData` | Stufe 2/3 → Kollision mit anderen Mods |
| Kein Streuen von Coal/MachineParts/StainlessSteel in Early-ScenParts | Anti-Softlock wäre damit zerstört |
| Kein manueller Tag-Count-Phasen-Übergang | Übergänge sind milestone-getrieben |

---

## §6 Phasen-DLC-Strategie (Verstärker, nicht Treiber)

| DLC | Phase-Eintritt | Verstärkung | Fehlende-DLC-Fallback |
|---|---|---|---|
| **Core** | Phase 1 (EarlySurvival) | Fundament | unverzichtbar |
| **Ideology** | Phase 1 (Early) → 2 (Mid) | Mood/Thought/HistoryEvent-Auditor | Core-only: Regel 1, 3 via Vanilla-Precepts |
| **Royalty** | Phase 4 (Trade) | Empire als Handelspartner; Titel als späte Permission | Trade ohne Royalty: Vanilla-Trader reicht |
| **Biotech** | Phase 3 (Automation) | Mechanitor, MachineParts-Senken, Genes | Core-only: MachineParts nur als Quest-Reward |
| **Anomaly** | Phase 2 (Production) → 5 (Expansion) | Entity-Beute als Scraps-Quelle | Core-only: Vanilla-Ancient-Ruins reicht |
| **Odyssey** | Phase 1 (frühe Mini-Ruins) → 5 → 6 | Loot-Stashes, Gravship, Orbit | Core-only: ohne Odyssey endet Expansion bei Vanilla-Caravan |

DLCs sind **Adapter hinter `DLCFilter`**, nie Voraussetzung. Core-only-Pfad ist vollständig.

---

## §7 Übergang in Save-/Migration-Vertrag

| Phase-Übergang | Save-Schema-Bump nötig? | Begründung |
|---|---|---|
| Early → Production | Nein | Live-Berechnung aus Skills/Bill-Verfügbarkeit |
| Production → Automation | Nein | Live-Berechnung aus Vanilla-Recipes |
| Automation → Trade | Nein | Live-Berechnung aus Wallet/Market-State |
| Trade → Expansion | Nein | Live-Berechnung aus WorldObjectDef-Anker |
| Expansion → Empire | Nein | Live-Berechnung aus Royalty-Title-Permit |

Es existiert **kein** `PhaseProgressSaveData`. Phasen sind beobachtbare Phänomene, keine persistierten Zustände. Das verhindert Migration-Kosten und Save-Bloat.

## §7.1 Live-Evidence Open Gates (OPEN)

| Gate | Status | Pfad |
|---|---|---|
| Mining-Gate UI-Side (player click) | CODE / DEF / COMPILES ✓ | `mods/02/Source/HarmonyPatches/MiningHookPatch.cs` Postfix auf `Designator_Mine.CanDesignateCell` |
| Mining-Gate AI-Side (auto-mine) | OPEN | weitere Vanilla-Hook-Evaluation nötig — `WorkGiver_Miner.HasJobOnThing` überschreibt in 1.6 nicht zuverlässig; `Mineable.DestroyMined` ist ein Kandidat |
| `BuildingInputAdapter` Stuff-Source | CODE ✓; LIVE pending | aktuell liest `def.costList`; `costStuffCount`-Pfad ist noch nicht abgedeckt (siehe Code-Reviewer-MEDIUM-Hinweis) |
| Stahl-Yield-Burst | OPEN | kein Postfix auf `Mineable.DestroyMined`; aktuell nur UI-Block. AI-Colonist kann mit Mining<8 einen kleinen Steel-Ertrag aus einem ohnehin geplanten Job mitnehmen. |

---

## §8 Cross-References (Pflicht-Verknüpfungen)

Diese Datei muss bei Änderungen synchron sein mit:

| Datei | Was dort aktualisiert wird |
|---|---|
| `docs/ARCHITECTURE.md` | Neue Sektion §7 Phasen-Index verlinkt |
| `docs/INTERFACE_CONTRACT.md` | §9 Owner-Map enthält Owner-Paket pro Phase-Ressource |
| `docs/CANONICAL_VANILLA_DOMAIN_MAP.md` | Domain-Map spiegelt Phasen-Reihenfolge wider |
| `docs/CODE_STATUS.md` | Live-Status pro Phase-Ziel-Resource |
| `mods/03-Rimconemy-Scavenger-Infrastructure/BLUEPRINT.md` | Mod-03-Owner-Matrix |

---

## §9 Akzeptanz-Gate (Definition of Done)

Der Contract gilt als **committed**, wenn:

1. Jede Row in §2 hat eine explizite Phase + Verfügbarkeits-Stufe.
2. Keine Verfügbarkeits-Stufe widerspricht einer Phase-Reihenfolge.
3. Kein Owner-Paket hat zwei Def-Dateien für denselben `defName`.
4. Kein Recipe ist gleichzeitig Early-lootable *und* massenproduzierbar.
5. Eine Vanilla-1.6-Belegstelle aus `docs/H1-api-def-gate.md` ist für jede Vanilla-Anker-Behauptung referenziert.

Jede Änderung am Contract bricht den Accept-Status bis zur erneuten Verifikation aller fünf Punkte.

---

## §10 Phase-Progress Overlay (Phase-First HUDTab-Style)

> **SSOT:** Overlay-Surface (Renderer + Resolver) lives in Mod 02 (`mods/02-Rimconemy-Survival-Progression/Source/Phase/`). Routing entry-point lives in Mod 01 (`FoundationDashboard` hub-tab slot 5) via reflection pattern (no cross-package DLL reference). Widget primitives **all** come from Foundation's existing UI toolkit (`RimconemyUi.*` + `RimconemyTheme.*`) — no forked widget API.

| Aspect | Owner / Path |
|---|---|
| Resolver (pure static) | `mods/02/Source/Phase/PhaseProgressResolver.cs` — reads RimconemyStartState + Vanilla research/building/bill state via `DefDatabase<>.GetNamedSilentFail` (never throws) |
| Window (MainTab) | `mods/02/Source/Phase/PhaseProgressWindow.cs` — extends `RimconemyMainTabWindow` (Foundation chrome parent). Renders 4 cards: header, next-milestone, overall %, tip-row. NO forked widgets. |
| Tests | `mods/02/Tests/PhaseProgressResolverTests.cs` — Def-level SSOT probes for 12 ThingDef names + 7-enum PhaseId check + null-Map honest-fallback probe. |
| Wiring | `mods/01/Source/UI/FoundationDashboard.cs` slot 5 — tabLabels extended (6 entries), `GetSubWindow(5)` reflection-routes to `Rimconemy.SurvivalProgression.Phase.PhaseProgressWindow`. Re-uses existing pattern (Survival/Infrastructure/Economy/Threat). |
| Localization | `mods/01/../Foundation.xml` (EN+DE): `Rimconemy.Hub.Tab.Phase`. `mods/02/../SurvivalProgression.xml` (EN+DE): `Rimconemy.PhaseProgress.*` (Title / Empty / NextMilestone / Phase label × 6 / Milestone label × 15 / Tip). |
| Honest fallback | `RimconemyUi.DrawFeatureStatus(state, detail, StatusLevel.Muted)` for null-map / pre-game conditions (status-vs-code-audit §A1/A2 contract). No thrown exception from render pass. |

### Phase-milestone SSOT (Phase 1—3, Phase 4—6 stub)

| Phase | Milestone Key | Predicate (SSOT) |
|---|---|---|
| 1: EarlySurvival | `single-survivor-start` | `RimconemyStartState.IsCompletedFor(map, "single-survivor")` |
| 1: EarlySurvival | `first-cooked-meal` | `Map.resourceCounter.GetCount(MealSimple) >= 1 ∧ Map.listerBuildings.Any(def ∈ {Rimconemy_Campfire \| FueledStove \| ElectricStove})` (truthful conjunction; campfires MUST be counted as a cook station to keep Phase 1 reachable pre-stovetop) |
| 1: EarlySurvival | `campfire-built` | `Map.listerBuildings.AllBuildings().Any(def == Rimconemy_Campfire)` |
| 1: EarlySurvival | `three-buildings-built` | `Map.listerBuildings.Count >= 3` |
| 2: Production | `first-coal-produced` | `Map.resourceCounter.GetCount(Rimconemy_Coal) >= 1` |
| 2: Production | `smelting-research-finished` | `Rimconemy_SmeltingCoal.IsFinished` (ResearchProjectDef bool property; no CostAmount in 1.6) |
| 2: Production | `first-steel-smelted` | `ResearchProjectDef("Smithing").IsFinished ∧ Map.listerBuildings.Any(def == FueledSmithy) ∧ Map.resourceCounter.GetCount(Steel) >= 1` (truthful conjunction; tightest possible without RecipeWorker postfix instrumentation) |
| 2: Production | `smithy-built` | `Map.listerBuildings.Any(def == FueledSmithy)` |
| 3: Automation | `machine-parts-built` | `Map.resourceCounter.GetCount(ComponentIndustrial) >= 5 ∧ Map.listerBuildings.Any(def ∈ {FueledSmithy \| TableMachining})` (truthful conjunction; counter-only falsely fires on trader side-dump) |
| 3: Automation | `stainless-smelted` | `Map.resourceCounter.GetCount(Rimconemy_StainlessSteel) >= 1` |
| 3: Automation | `stainless-tower-built` | `Map.listerBuildings.Any(def == Rimconemy_StainlessSteelTower)` |
| 3: Automation | `power-grid-online` | `Map.listerBuildings.Any(def == Rimconemy_WoodCoalGenerator)` |
| 4: Expansion | `outpost-constructed` | `RimconemyStartState.IsCompletedFor(map, "outpost-constructed")` (Mod-03 anchor; deferred) |
| 5: Trade | `credits-wallet-initialised` | `RimconemyStartState.IsCompletedFor(map, "credits-wallet-initialised")` (Mod-04 anchor; deferred) |
| 6: Empire | `empire-tribute-paid` | `RimconemyStartState.IsCompletedFor(map, "empire-tribute-paid")` (Mod-04 anchor; deferred) |

Per-milestone localisation: `Rimconemy.PhaseProgress.Milestone.<Key>` for human-readable names.

### Contract rules

1. The resolver is **pure static**, no GameComponent, no schema migration — its state inputs come from `Map` + `Current.Game` only. Per §7 above, phase progress is a **read-only observation**, not a persisted GameComponent.
2. The overlay never lies. If `EmptyReason != null`, it shows a single muted banner with the explanation. Phase-percent is computed from predicate truth; if a predicate's Def doesn't exist, it returns false (no fake completion).
3. Cross-package coupling is reflection-only (Foundation → Mod-02 `FindType`). No DLL reference. The pattern matches `FoundationDashboard.GetSubWindow(1..4)`.
4. Adding a milestone changes the overlay's contract surface. Update this §10 row + Def SSOT probe in `PhaseProgressResolverTests` + LanguageString in EN+DE.

### Truthfulness doctrine (added 2026-08-05)

Every predicate in the SSOT table above MUST be a **truthful conjunction** of distinct signals — never a single signal that admits trader-deposits or blueprint-not-yet-built edge cases. The four signals available are:

| Signal | Truth-class | Beispiel |
|---|---|---|
| **Research** | `ResearchProjectDef("Foo").IsFinished` | True iff the player has fully completed the research (not just queued); research completion is a save-persisted Vanilla fact. |
| **Counter** | `Map.resourceCounter.GetCount(Mat) >= N` | True iff ≥N units of `Mat` are present in the player-home storage; truthful only when combined with another signal. |
| **Building** | `Map.listerBuildings.allBuildingsColonist.Any(def == X)` | True iff a colonist-built building of def `X` exists; truthful only when combined with another signal. |
| **StartState** | `RimconemyStartState.IsCompletedFor(map, "event")` | True iff an explicit game-event mark has been written; cheap and audit-friendly but requires a writer. |

Three forms are canonical:

| Form | Beispiel-Slot | Warum |
|---|---|---|
| **single-signal** | `campfire-built`, `smithy-built`, `smelting-research-finished`, `power-grid-online` | One signal is sufficient when that signal alone is truthful (e.g. building exists means it was built; research finished means it was researched). |
| **conjunction(N)** | `first-cooked-meal`, `first-steel-smelted`, `machine-parts-built` | Multiple signals ANDed together; each side rules out a distinct false-positive class. |
| **StartState** | `single-survivor-start`, `outpost-constructed`, `empire-tribute-paid` | Predicate reads an explicit event mark; the writer is upstream (scenario, outpost-builder, tribute-pay). |

A predicate that is just Counter-only or Building-only on a resource the player can stockpile / trade is **forbidden** — it will fire on the first caravan visit. A predicate that is just StartState and lacks an upstream writer is **also forbidden** — it will never fire.

### Acceptance gate

- [x] Mod 02 build: 0 warnings, 0 errors.
- [x] Mod 01 build: 0 warnings, 0 errors.
- [x] Bash `-n` on runtime_test.sh, dev_quick_test.sh.
- [x] XML well-formedness on all changed language files.
- [x] PhaseProgressResolverTests summary line emitted during runtime-test (`PhaseProgress regression tests: N passed, M failed`).
- [x] `required_summaries` gate in `runtime_test.sh` includes the new summary line.
- [x] Predicate truthfulness hardened (2026-08-05): `first-cooked-meal` campfires, `first-steel-smelted` triple conjunction, `machine-parts-built` building-gate.
- [ ] **OPEN — Live-evidence only:** RimWorld boot screenshot showing the new "📈 Phase Progress" hub-tab and the resolver output for a saved game. Closable by `scripts/runtime_test.sh` post-deploy.


---

## §11 Patch Stufen-Pattern Tightening (Stufe 1/Stufe 2/Stufe 3)

> **SSOT:** Patch-Stufen werden ab jetzt verbindlich in dieser Sektion dokumentiert. Jeder Patch-Tag wird auf der "vergessene Standort" -Hitlist geprüft und kann von jedem Contributor genau hier wiedergefunden werden.

### Stufen-Übersicht

| Stufe | Idiom | Erlaubt? | Beispiel-Pfad |
|---|---|---|---|
| 0 | keine Modifikation an BuildableDef-Listen | ✓ | (z. B. das Trial selbst) |
| 1 | XML-DOM-add via `<li>`/`XML-DOM-add via xpath`-Additions ohne Listen-Replace | ✓ | `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/WallDoorBarricade_Bauschutt_Patches.xml` Part 3 |
| 2 | `<PatchOperationRemove>` mit Node-targeted Xpath; auto-idempotent | ✓ | `WallDoorBarricade_Bauschutt_Patches.xml` Part 2 (`<li[text()="Woody"]</li>` direkt entfernen) |
| 2+ | `<PatchOperationTest>` gefolgt von `<PatchOperationAdd>` als native idempotente Sequenz | ✓ | `WallDoorBarricade_Bauschutt_Patches.xml` Part 3 (Test-Xpath prüft "Stony noch nicht da" → Add-Stony oder Add-empty-List) |
| 3 | `<PatchOperationReplace>` auf costList/stuffCategories/compProperties/graphicData | **✗ VERBOTEN** | keine |
| 4 | Scene-state-abhängige Modifikation zur Load-Time | **✗ VERBOTEN** | keine |

### KonstruktionsDebris als Steel-Alternative (Phase-Contract Gate)

| Aspekt | Wert |
|---|---|
| Def | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/ConstructionDebris.xml` |
| StuffCategory der Debris | **Stony** (Canonical-Vanilla) |
| Idiom | `<li>Stony</li>` als StuffCategory in Wall/Door/Barricade/Sandbag addieren |
| Warum nicht `<li>ConstructionDebris</li>` | Vanilla 1.6 hat keine `ConstructionDebris` StuffCategory; Stony ist die kanonische StuffCategory und akzeptiert unsere Debris automatisch via `<stuffProps.categories/Stony>` |
| Activation-Gate | `<Operation ... MayRequire="rimconemy.scavengerinfrastructure,rimconemy.survivalprogression">` (Package-Präsenz) + `<Operation ... PatchOperationTest xpath=…>` (Def-Status) |
| DefModExt-Gate | `Rimconemy.SurvivalProgression.Phase.PhaseContractGate` (addiert durch Mod-02 Patches/PhaseContractGate_Additions.xml) |
| Revocation-Pfad | Entfernt man die PhaseContractGate DefModExt aus Wall/Door/Barricade/Sandbag, schlagen alle Folge-Patches (Stufe 2+) automatisch fehl — kein Stufe-3 Replace nötig, kein Multi-Merge-Konflikt mit Wall/Door/Barricade-Stuff_category-Reihenfolge |

### Idempotenz-Erklärung

- `<PatchOperationRemove>` mit Node-targeted Xpath (`<li[text()="Woody"]`) feuert niemals zweimal auf demselben Knoten; Engine tut nichts wenn Xpath leer ist.
- `<PatchOperationTest>` ist explizit eine Test-Operation, ändert den Def nicht; die nächste `<PatchOperationAdd>`-Operation kann in derselben Patch-Datei folgen und beide sind atomar pro Def-Load.
- `<Operation ... MayRequire="rimconemy.survivalprogression">` ist ein Package-Availability-Gate; falls Survival-Progression nicht aktiv ist, schlägt die ganze Operation fehl und keine Doppel-Add-Schritte entstehen.

### Was NICHT erlaubt ist (Phase-Contract-Stufe-3-Falle)

| Aktion | Warum verboten | Ausweich-Pfad |
|---|---|---|
| `<PatchOperationReplace xpath="…/costList">` mit komplett neuer Liste | löscht den ursprünglichen costList-Tag, kollidiert mit anderen Mods die Material-Kosten ergänzen | `<PatchOperationAdd>` mit `<li>`-Element additiv |
| `<PatchOperationFindMod modId="…">` + Mehrfach-Patch (Layer) | Stufe 4 — Patch-Load-Order chaos | DefModExt-Gate stattdessen |
| C#-Resolver-Callback aus XML-Patch | zur Load-Time nicht verfügbar | DefModExt-Wert als Marker |

### Acceptance Gate (Stufe 11)

- [x] Mod 02 build: 0 warnings, 0 errors.
- [x] Mod 03 build: 0 warnings, 0 errors. XML well-formedness geprüft.
- [x] Bauschutt_Remap_Patches.xml umbenannt in `WallDoorBarricade_Bauschutt_Patches.xml` mit `PatchOperationTest` + `PatchOperationAdd`.
- [x] Barricade / Sandbag nun erfasst (vorher Vanilla-for-MVP-Skip laut docs/falsification).
- [x] PhaseContractGate DefModExt installiert (Mod 02 / Source / Phase / PhaseContractGate.cs).
- [ ] **OPEN — Live-Evidence:** RimWorld-Boot mit ersten Walls auf Bauschutt (ConstructionDebris-Material) muss visuell den Hügel-Charakter eines Stein-Buildings zeigen. Closable by `scripts/runtime_test.sh` post-deploy.

