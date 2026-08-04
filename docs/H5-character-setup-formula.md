# H5 — Character Setup-Formel

> **SSOT-Hinweis:** Detail-Topic dieser Datei ist im Orient-Index [ARCHITECTURE.md §5](ARCHITECTURE.md). Topic-Landkarte: [INDEX.md §1](INDEX.md).
> **Owner:** Survival & Progression / Research-Design
> **Status:** `PARTIALLY IMPLEMENTED (Hybrid-Schnitt, 2026-08-04); Runtime- und API-Gates offen`
> **Referenz:** [ROADMAP.md §3 Phase 4](../ROADMAP.md#phase-4--character-setup-und-bio-remap), [ROADMAP.md §8.4](../ROADMAP.md#84-phase-34--storage-only--character-setup-offen)

**Implementation Mapping (A-1):**
- §1 Startalter 18 → `CharacterSetup.FixAge` via `BirthAbsTicks`-Hack (Workaround, bleibt)
- §2 SkillBudget → `SkillBudgetCalculator.cs` (linear 0-10 + progressive {2,2,3,3,4,5,6,7,8,10}) — implementiert und für UI/Runtime zentralisiert
- §3 Trait-Polarity → `TraitAssigner.cs` + `SkillBudgetCalculator.Classify(balance)` → TraitZone (5 Zonen) — deterministische Pure-Auswahl implementiert
- §4 Bio-Remap → **offen** (Phase A-2); `FixAge` bleibt dokumentierter Post-Generation-Fallback
- §5 Reproduzierbarkeit → expliziter Seed in Pure-Trait-Auswahl, gleiche Eingabe reproduziert Ergebnis; Runtime-Seed-State noch nicht persistiert
- §6 Save-Schema → `CharacterSetupState` SchemaBackup Routinen noch unsubstantialisiert (Phase A-2)
**A-1-Phase-Limitierungen (in Code dokumentiert):**
- Combat-Skills (Shooting/Melee) freigeschaltet ✓
- TraitAssigner wendet die vorhandenen `*_Trait_*`-Defs über deterministische Pure-Auswahl an ✓; der aktuelle Runtime-Pool ist ein Rimconemy-Sicherheits-/Beispielpool. Die vollständigen H5-8/6-Pools und Ausschlüsse bleiben als Balance-/Content-Arbeit offen.
- Specialization-Passion **loggt nur** (kein Set-Pass) — Reflection/Harmony in A-3

## Zweck

Vor dem Character-Setup-Code werden **feste Werte** entschieden: Startalter, Skillbudget, Kostenfunktion, Neutralzone, Trait-Schwellen. Diese Werte sind reproduzierbar und deterministisch. Sie dürfen nicht aus dem Bio-Text unkontrolliert entstehen.

---

## 1. Startalter

| Parameter | Wert | Begründung |
|---|---|---|
| `BiologicalAge` | **18** | Fest. Kein Vanilla-Zufallsbereich. |
| `ChronologicalAge` | **18** | Gleich wie BiologicalAge. Keine Cryo-/Time-Dilation. |
| `AgeRange` | `[18, 18]` | Kein Bereich — exakt 18. |
| `AgeReversal` | Nicht erlaubt | Keine Verjüngung durch Tech/Ideology. |
| `ChildPawns` | Nicht erlaubt | Keine Kinder (Biotech) als Start-Pawns. |

**API-Spike (H1, statischer Befund):**
- `PawnGenerationRequest.FixedBiologicalAge` (3 Treffer in lokalem Material)
- `PawnGenerationRequest.FixedChronologicalAge` (3 Treffer in lokalem Material)

Diese Treffer sind noch kein lokaler Compile- oder Runtime-Beleg. Der direkte Generator-Hook bleibt bis `API-START-01` offen; der aktuelle Code nutzt dafür den dokumentierten `CharacterSetup.FixAge`-Fallback.

**Gate:** Drei identische `PawnGenerationRequest`-Aufrufe mit Age=18 erzeugen denselben Alter-Wert.

---

## 2. Skillbudget

### Gesamtbudget

```yaml
SkillBudget:
  TotalPoints: 30           # feste Gesamtsumme
  MinPointsPerSkill: 0      # Minimum pro Skill (kann 0 sein)
  MaxPointsPerSkill: 20     # Hard cap used by SkillBudgetCalculator; normal UI starts lower
```

### Kostenfunktion

Jeder Skillpunkt kostet **1 Budget-Punkt** bis Stufe 10. Danach steigende Kosten:

| Skill-Stufe | Kosten (kumulativ) |
|---|---|
| 0 → 1 | 1 |
| 1 → 2 | 1 |
| ... | ... |
| 9 → 10 | 1 |
| 10 → 11 | 2 |
| 11 → 12 | 2 |
| 12 → 13 | 3 |
| 13 → 14 | 3 |
| 14 → 15 | 4 |
| 15 → 16 | 5 |
| 16 → 17 | 6 |
| 17 → 18 | 7 |
| 18 → 19 | 8 |
| 19 → 20 | 10 |

### Skills (12 Vanilla-Skills)

```yaml
Skills:
  - Shooting        # Combat
  - Melee           # Combat
  - Construction    # Building
  - Mining          # Scavenging
  - Cooking         # Farming (Nahrungszubereitung)
  - Plants          # Farming
  - Animals         # Survival
  - Crafting        # Engineering
  - Artistic        # Social
  - Medical         # Survival
  - Social          # Social
  - Intellectual    # Research
```

### Berechnung

```csharp
int spent = SkillBudgetCalculator.CalculateSpentPoints(skillDefToLevel);
var budget = SkillBudgetCalculator.ValidateBudget(skillDefToLevel);
var defaultAllocation = SkillBudgetCalculator.BuildDefaultAllocation(canonicalSkills);
var zone = SkillBudgetCalculator.Classify(budget.Balance);
```

`skillDefToLevel` is `Dictionary<SkillDef, int>`. `ValidateBudget` reports
`WithinBudget`, `SpentPoints`, `Balance`, `ZoneLabel` and `BudgetStatus`.
`BuildDefaultAllocation` uses the same progressive cost table as the UI and
runtime application, so mod-added skills do not silently alter the H5 budget.

---

## 3. Neutralzone und Trait-Schwellen

### Schwellen

| Schwelle | Wert | Bedeutung |
|---|---|---|
| `NeutralThresholdLow` | **-5** | Unterhalb → negative Trait-Kandidaten |
| `NeutralThresholdHigh` | **+3** | Oberhalb → positive Trait-Kandidaten |
| `NeutralCenter` | **25** | Mittelpunkt des Budgets (30 − 5 = 25) |

**Beispiele:**

| SpentPoints | Balance | Zone | Ergebnis |
|---|---|---|---|
| 30 | +5 | **Positiv** | 1 positiver Trait |
| 28 | +3 | **Neutral** | Kein Trait-Bonus/-Malus |
| 25 | 0 | **Neutral** | Kein Trait-Bonus/-Malus |
| 20 | -5 | **Neutral** (Grenze) | Kein Trait-Bonus/-Malus |
| 19 | -6 | **Negativ** | 1 negativer Trait |
| 10 | -15 | **Negativ** | 2 negative Traits |

### Trait-Pools

#### Positive Traits (bei Balance > +3)

```yaml
PositiveTraits:
  # H5-Zielpool (Content-Gate offen; der aktuelle Runtime-Pool ist kleiner)
  - FastLearner
  - Industrious
  - IronWilled
  - Optimist
  - Tough
  - Jogger
  - GreatMemory
  - Nimble

MaxPositiveTraits: 1   # höchstens 1 positiver Trait
```

#### Negative Traits (bei Balance < -5)

```yaml
NegativeTraits:
  # H5-Zielpool (Content-Gate offen; der aktuelle Runtime-Pool ist kleiner)
  - SlowLearner
  - Lazy
  - Nervous
  - Pessimist
  - Wimp
  - Slowpoke

MaxNegativeTraits: 2  # maximal 2 negative Traits
StackRule: Balance unter -10 → 2 negative Traits, Balance -6 bis -9 → 1 negativer Trait
```

### Trait-Ausschlüsse

```yaml
TraitExclusions:
  # Kein positiver + negativer Trait derselben Kategorie
  - [FastLearner, SlowLearner]
  - [Industrious, Lazy]
  - [Optimist, Pessimist]

  # Bestimmte Traits nicht mit anderen
  - [IronWilled, Nervous]   # widersprüchlich
  - [Jogger, Slowpoke]      # widersprüchlich
```

---

## 4. Bio-Remap

### Regeln

```yaml
BioRemap:
  Source: "PawnGenerator.GeneratePawn" (oder äquivalent)
  Rules:
    - Bio-Text wird generiert, aber ALTER und SKILLS werden danach überschrieben
    - Bio-Text liefert Herkunft, Ton, Backstory, Auswahlkontext
    - Bio-Text DARF NICHT unkontrolliert zusätzliche Skillpunkte oder Traits erzeugen
    - Backstory-Trait-Boni werden auf das Skillbudget angerechnet
    - Nach Bio-Generierung: Skill-Reset → Spieler verteilt Budget → Trait-Berechnung
```

### Ablauf

```
1. PawnGenerationRequest(BioAge=18, ChronoAge=18)
   ↓
2. Bio-Text generieren (Herkunft, Backstory, Ton)
   ↓
3. Skills auf 0 resetten
   ↓
4. Spieler verteilt 30 Skillpunkte über UI
   ↓
5. BudgetResult berechnen (spent, balance, category)
   ↓
6. Traits vergeben basierend auf Balance-Zone + Seed
   ↓
7. Fertiger Start-Pawn
```

---

## 5. Gate: Reproduzierbarkeit

```yaml
GateCondition: >
  Drei identische Bio-/Skill-Eingaben mit gleichem Seed
  erzeugen dieselben Skills, Trait-Kandidaten und Startalterwerte.

Test1:
  Seed: 42
  Bio: "Farmkind, mittlerer Westen"
  Skills: {Plants:8, Cooking:6, Construction:5, Mining:3, Medical:3, Shooting:3, Melee:2}
  Spent: 30, Balance: +5 → 1 positiver Trait

Test2:
  Seed: 42
  Bio: "Farmkind, mittlerer Westen"
  Skills: {Plants:8, Cooking:6, Construction:5, Mining:3, Medical:3, Shooting:3, Melee:2}
  Expected: IDENTICAL Result zu Test1

Test3:
  Seed: 99
  Bio: "Farmkind, mittlerer Westen"
  Skills: {Plants:8, Cooking:6, Construction:5, Mining:3, Medical:3, Shooting:3, Melee:2}
  Expected: ANDERE Trait-Auswahl (anderer Seed)
```

---

## 6. Save-Schema Character Setup

```yaml
CharacterSetupState:
  SchemaVersion: 1
  Seed: int
  BiologicalAge: 18
  ChronologicalAge: 18
  BudgetTotal: 30
  SpentPoints: int
  Skills:
    SkillDefName: int   # level
  TraitDefNames: string[]
  BioSourceKey: string  # Referenz auf generierte Bio
  NeutralCenter: 25
  Balance: int
  TraitZone: "Positive" | "Neutral" | "Negative"
```

---

## Erledigter Hybrid-Schnitt (2026-08-04)

- `TraitAssigner.SelectTraitsForBudget` ist pure und verwendet einen stabilen FNV-1a-/Integer-Mix statt globalem `Verse.Rand`.
- Light-/Strong-Polarity, Neutralgrenzen und maximal zwei negative beziehungsweise ein positiver Trait werden durch Regressionstests abgedeckt.
- UI, Default-Verteilung und Runtime-Anwendung verwenden dieselbe kostenbewusste Allocation-Logik; überbudgetierte Eingaben werden abgelehnt.
- Paketversion 02: `0.1.28`.

## Offene Gates / Nächster Schritt

1. `API-START-01`: `PawnGenerationRequest.FixedBiologicalAge` und `.FixedChronologicalAge` lokal als konkrete API verifizieren.
2. `PawnGenerator.GenerateTraits`-Signatur per lokalem Spike prüfen (H1-C5).
3. `CharacterSetupState` mit Seed/Skills/Traits als Scribe-State implementieren.
4. Live-Test: Start-Pawn mit Seed=42, Budget=30 und Save/Load reproduzierbar belegen.
5. Balance-Entscheidung: Sind 30 Punkte und Neutralzone [-5, +3] angemessen?
