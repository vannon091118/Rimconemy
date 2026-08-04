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
