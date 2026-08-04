# Track A — Character Setup & Bio-Remap — Design Spec

> **Datum:** 2026-08-04
> **Owner:** Survival & Progression (Package 02)
> **Status:** Spec-Draft → wartet auf User-Review
> **Bezug:** [`docs/H5-character-setup-formula.md`](../../H5-character-setup-formula.md),
> [`ROADMAP.md §8.4`](../../../ROADMAP.md#84-phase-34--storage-only--character-setup-offen), [`ROADMAP.md §3 Phase 4`](../../../ROADMAP.md#phase-4--character-setup-und-bio-remap)

## 1. Zweck

Track A schließt die Lücke zwischen H5-Spec (PLANNED, reine Zahlen) und dem
laufenden Spiel. Der Spieler soll beim Szenario-Start einen fertigen
Charakter mit folgenden Eigenschaften erhalten:

- **Startalter 18/18** (deterministisch, kein Vanilla-Zufallsbereich).
- **12 Skills (inkl. Shooting + Melee)** im Skill-Budget verteilbar.
- **30 Punkte** als Gesamtbudget mit **linearem** Kostenmodell
  (Stufe = Budget-Punkte).
- **Pufferzone** ±5…+3 um den NeutralCenter 25: kein positiver/negativer
  Trait.
- **Außerhalb Pufferzone**: 1 positiver oder 1–2 negative Traits je nach
  Balance-Vorzeichen.
- **Bio wird komplett überschrieben** über PawnGenerationRequest mit
  `FixedBiologicalAge=18`. Vanilla-Backstory-Generierung entfällt.
- **Traits als Hybrid-Skin** (`Rimconemy_Trait_*` XMLs, die Vanilla-RimWorld
  `TraitDef`-Stat-Offsets als `<parent>` referenzieren).

## 2. Nicht-Ziele (Track A)

Keine Änderung an:

- Mod 03 (Scavenger) — Storage-Adapter bleibt bestehen.
- Mod 04 (Economy) — Wallet/Outpost-Stubs nicht berührt.
- Mod 05 (Infected) — StoryDirector/H2-Spec unangetastet.
- Ideology-Trägern (H3) — kommt in Track B.
- Story-Event-Catalog-Erweiterung (Track C).
- Save-Migration zwischen Rimconemy-Versionen über die 0.1.x → 0.2.0-Linie
  hinaus: keine alten Saves, die SkillBudgetWindow-Schemaversion 0
  enthalten, werden rückwärtskompatibel deserialisiert. Warn-Log + Defaults.

## 3. Designentscheidungen

### D1 — Combat-Skills eingeschlossen (verbindlich)

`Shooting` und `Melee` werden aus `EligibleSkills` nicht mehr ausgefiltert
(`CharacterSetup.cs:34`, `SkillBudgetWindow.GetEligibleSkills():213`).
Effekt: Budget verteilt sich über alle 12 Vanilla-Skills.

### D2 — Skill-Kosten linear (default; Phase-2-progressiv)

`SkillBudgetCalculator.CostForLevel(int level) → level` für 0 ≤ level ≤ 10.
Phase 2 kann auf progressiv-Stufen umstellen (H5 §2), aber aktuell ist
linear = robust + entspricht dem User-Statement *„feste Skill-Punkten die
man zu Beginn verteilen kann"* ohne Ballast.

### D3 — Bio-Komplett-Override (verbindlich)

`SingleSurvivorScenario.PostWorldGenerate`-Hook baut den Survivor via
`PawnGenerator.GeneratePawn(PawnGenerationRequest)` mit:

```csharp
request.FixedBiologicalAge = 18;
request.FixedChronologicalAge = 18;
request.ForceGenerateNewPawn = true;
request.KindDef = PawnKindDefOf.Colonist;
// BackstoryGeneration: nur AdventureChildhood/Adulthood-Slots bleiben für Flavor.
```

**Vanilla-Backstory-Texte werden mit Bio-Flavor-Lite überschrieben**
(kein eigener Bio-Text-Pool in Phase 1 — wir setzen einen Platzhalter,
der die Survivor-Herkunft + Anpassungswille benennt, ohne Spielernarrativ).
Phase 2: Bio-Pool mit 4–6 Varianten.

### D4 — Trait-Pool als Hybrid-Skin (verbindlich)

`Rimconemy_Trait_*.xml`-Defs tragen `<parent>`-Referenzen auf
Vanilla-RimWorld-TraitDefs (z.B. `FastLearner`,
`Industrious`, `IronWilled`). TraitAssigner vergibt die Rimconemy-Skin
über `DefDatabase<TraitDef>.GetNamedSilentFail("Rimconemy_Trait_Hardy")`,
intern wirkt Vanilla-Stat-Offsets.

Vorteile:

- Eindeutige Rimconemy-Sichtbarkeit (Spieler sieht UI-Suffix `rimconemy`).
- Vanilla-Backend-Stat-Sync kostenlos.
- H5-Ausschlussregeln (z.B. `FastLearner` ⇎ `SlowLearner`) werden
  RimWorld-nativ über `TraitDef.exclusionTags` realisiert.

### D5 — Buffer-Schwellen aus H5 übernehmen (verbindlich)

| Balance (spent − 25) | Trait-Zone | # Traits | Pool-Reihenfolge |
|---|---|---|---|
| `> +5` | positiv | 1 (stark) | Rimconemy_Trait_QuickLearner / Unbreakable |
| `+3 … +5` | positiv (light) | 1 (light) | Rimconemy_Trait_Hardy / Attentive |
| `-5 … +3` | **Puffer** | 0 | — |
| `-10 … -6` | negativ (light) | 1 (light) | Rimconemy_Trait_Unfocused / Hesitant |
| `< -10` | negativ (stark) | 2 (heavy) | Exhausted + Frail (oder Paranoid + Frail, etc.) |

Spezialfall: `SpecializationThreshold=7` (≥ 7 Punkte in einem Skill)
→ zusätzlicher Passion-Marker (kein Trait).

## 4. Datenmodelle

### 4.1 SkillBudgetCalculator (rein, ohne Abhängigkeit)

```csharp
namespace Rimconemy.SurvivalProgression.Character
{
    public static class SkillBudgetCalculator
    {
        public const int TotalBudget = 30;
        public const int NeutralCenter = 25;
        public const int NeutralThresholdLow = -5;   // spent < 20 → negativ
        public const int NeutralThresholdHigh = +3;  // spent > 28 → positiv
        public const int MaxPerSkill = 10;
        public const int MinPerSkill = 0;
        public const int SpecializationThreshold = 7;

        // Linear: Stufe == Budget-Punkte (1:1) bis 10, dann Decke.
        public static int CostForLevel(int level)
        {
            if (level <= 0) return 0;
            return level;
        }

        public static int TotalSpent(Dictionary<SkillDef, int> alloc)
        {
            int s = 0;
            foreach (var kvp in alloc) s += CostForLevel(kvp.Value);
            return s;
        }

        public static int Balance(int spent) => spent - NeutralCenter;

        public enum TraitZone { Buffer, PositiveLight, PositiveStrong,
                                NegativeLight, NegativeStrong }

        public static TraitZone Classify(int spent)
        {
            int balance = Balance(spent);
            if (balance > 5) return TraitZone.PositiveStrong;
            if (balance > NeutralThresholdHigh) return TraitZone.PositiveLight;
            if (balance >= NeutralThresholdLow) return TraitZone.Buffer;
            if (balance >= -10) return TraitZone.NegativeLight;
            return TraitZone.NegativeStrong;
        }
    }
}
```

### 4.2 CharacterSetupState (Scribe-Schema)

```csharp
public sealed class CharacterSetupState : IExposable
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion = CurrentSchemaVersion;
    public int Seed;
    public int SpentPoints;
    public List<string> AllocatedSkillsXml;  // defnames + level
    public List<string> TraitDefNames;       // defnames nach Assign
    public int BufferZoneHits;              // # Spawns in Buffer
    public bool MigratedFromV0;

    public void ExposeData()
    {
        Scribe_Values.Look(ref SchemaVersion, "schemaVersion", 1);
        Scribe_Values.Look(ref Seed, "seed");
        Scribe_Values.Look(ref SpentPoints, "spentPoints");
        Scribe_Collections.Look(ref AllocatedSkillsXml, "allocatedSkills", LookMode.Value);
        Scribe_Collections.Look(ref TraitDefNames, "traits", LookMode.Value);
        Scribe_Values.Look(ref BufferZoneHits, "bufferHits", 0);
        Scribe_Values.Look(ref MigratedFromV0, "migratedFromV0", false);
    }
}
```

Pro Spieler-Pawn **ein** CharacterSetupState (statt nur globale Map). Key ist
`pawn.ThingID`. Persistiert in neuem GameComponent `CharacterSetupGameComponent`
(Subklasse von `GameComponent`).

### 4.3 Trait-Pool v2 (Defs/Traits/*.xml)

| defName | parent | Wirkung | Stat-Quellen |
|---|---|---|---|
| `Rimconemy_Trait_Hardy` | Vanilla `Hard_Vitality` (1.6) | Pain +15 % | Vanilla `PainShockThreshold` |
| `Rimconemy_Trait_QuickLearner` | `NaturalMood`/Learn-Stat | XP-Rate +20 % | Vanilla-Learnrate-Offset |
| `Rimconemy_Trait_Unbreakable` | Vanilla `Nerves` | Mental-Break-Schwelle -0.10 | Vanilla |
| `Rimconemy_Trait_Attentive` | Vanilla `Drug Free` o. ä. | Konzentrationsmarker | Vanilla-Offset |
| `Rimconemy_Trait_Unfocused` | Vanilla `Neurotic` | Workspeed -10 % | Vanilla |
| `Rimconemy_Trait_Hesitant` | Vanilla `Volatile` | Combat-Stat -5 % | Vanilla |
| `Rimconemy_Trait_Exhausted` | Vanilla `Depressive` | Move -20 % + Workspeed | Vanilla |
| `Rimconemy_Trait_Paranoid` | Vanilla `Paranoia_Common` | Social -15 % | Vanilla |
| `Rimconemy_Trait_Frail` | Vanilla `FragileHealth` | Health-Vuln +20 % | Vanilla |

Neue 4 Einträge (Track A Phase 3):

| defName | parent | Wirkung |
|---|---|---|
| `Rimconemy_Trait_Tough` | Vanilla `Tough` | Recoil weniger, Schmerz-Dampf |
| `Rimconemy_Trait_Jogger` | Vanilla `Jogger` | +0.4 Move |
| `Rimconemy_Trait_GreatMemory` | Vanilla `GreatMemory` | Skill-Decay +50 % |
| `Rimconemy_Trait_Nimble` | Vanilla `Nimble` | Nahkampf-Dodge +15 % |

**Ausschluss-Tags**: `<exclusionTags><li>Rimconemy_Trait_Polarity_Learn</li></exclusionTags>` für alle
positiven/nagativen Learn-Trait-Pairs.

## 5. Spawn-Hook-Kette

```
ScenPart_SingleSurvivorRimconemy (in Defs/Scenarios/SingleSurvivor.xml)
  ↓ PostWorldGenerate
CharacterSetupGameComponent.OnWorldInitialize()
  ↓
  1. BioOverride.GeneratePawn(age: 18, kind: Colonist, bioFlavor: 1-of-4) → pawn
  2. CharacterSetup.FixAge(pawn)        ← redundant aber Safety
  3. SkillBudgetWindow.Open(pawn)       ← player verteilt manuell
  4. CharacterSetup.ApplyBudget(pawn, alloc)
  5. SpecializationBonus.Check(pawn)
  6. TraitAssigner.AssignForBalance(pawn, spentPoints)
  7. CharacterSetupState.Persist(pawn.ThingID)
```

Hook-Anker:
`Current.Game.GetComponent<CharacterSetupGameComponent>()` nach
`PostWorldGenerate` (frühestens da RimWorld Karten/Save-State hat).

Fallback wenn Player `SkillBudgetWindow` schließt ohne Verteilung:
**Default-Equal** (3 pro Skill × ~10 = 30, exakt analog zu
`SkillBudgetWindow.GetEligibleSkills().Count = 12` → 2 pro Skill + 6×0 + …)
konkret: 12 Skills × 2 = 24 + Verteilung des Rests (6) auf die ersten 6
Skills alphabetisch.

## 6. UI-Anpassungen

`SkillBudgetWindow`:

- Größe auf 600×640 (12 Skills + Slider).
- Header zeigt „**Verfügbar: (30 − spent) Punkte**".
- Live-Badge Zone-Indikator: Buffer (grün) / Positive (blau) / Negative (rot).
- Beim Apply: Warn-Dialog wenn Zone ≠ Buffer.

`SurvivalProgressionDashboard` (existiert):
- Tab „Charakter-Setup" zeigt für jeden aktiven Survivor:
  `SpentPoints`, `Zone`, `TraitDefNames[]`.

## 7. Save / Load

Beim Laden (alte Saves ohne `CharacterSetupState`):
- Scribe gibt leere Listen zurück.
- `BufferZoneHits = 0` Default.
- `MigratedFromV0 = false` (kein leerer Apply, kein Trait-Re-Roll).

Solange `CharacterSetupGameComponent.MigratedFromV0 == false`, wird beim
nächsten Spawn eines neuen Survivors `BioOverride.GeneratePawn(...)`
ausgeführt. Bereits existierende Pawns in alten Saves bleiben
unangetastet (kein Re-Override auf existierende Pawns).

## 8. Tests (Gate G5)

`Source/Character/Tests/CharacterSetupTests.cs`:

| Test | Erwartung |
|---|---|
| `TestSkillBudget_EqualDistribution_StaysInBuffer` | 12 × 2 + 6 alpha-first → spent=30 → Zone= Positive (balance=+5) |
| `TestSkillBudget_HeavyFrontload_LeavesBuffer` | 1 Skill auf 5, Rest auf 0 → spent=5 → Zone= NegativeStrong (balance=-20) |
| `TestTraitZone_Boundaries` | spent=19 → NegativeLight, 20 → Buffer, 28 → PositiveLight, 29 → PositiveStrong |
| `TestBioOverride_AgeEquals18` | GeneratePawn → pawn.ageTracker.BiologicalYears == 18 |
| `TestBudgetRoundtrip_Load_GivesSameSpent` | Save → Load → spent == pre-save spent |
| `TestTraitExclusion_NumericFastAndSlow` | `Rimconemy_Trait_QuickLearner` + `Rimconemy_Trait_SlowLearner` (nicht existent aber exempl.) → `Trait.GetTraitConflictsWithNewTrait` blockiert |

Tests werden über `Bootstrap.cs` via `StaticConstructorOnStartup.RunAll()`
analog `StorySelectorTests` angefacht.

## 9. Dateien (Δ)

### Phase A-1 — Härtung

**Neu:**
- `Source/Character/SkillBudgetCalculator.cs`

**Modifiziert:**
- `Source/Character/CharacterSetup.cs` (Combat-Skills einschließen, Cost-aware Verteilung)
- `Source/Character/SkillBudgetWindow.cs` (Combat-Skills, Zone-Badge, 600×640)
- `Source/Character/TraitAssigner.cs` (H5-Schwellen, 2-Negativ-Pfad, Specialization-Bonus-Passion-Marker)

### Phase A-2 — Bio Override + Save

**Neu:**
- `Source/Character/BioOverride.cs` (PawnGenerationRequest + Flavor-Slots)
- `Source/Character/BiographyFlavor.cs` (4 Platzhalter-Texte; Phase 2: echter Pool)
- `Source/Character/CharacterSetupState.cs` (IExposable)
- `Source/Character/CharacterSetupGameComponent.cs` (GameComponent mit OnWorldInitialize-Hook + Spawn-Chain)
- `Source/Scenarios/SingleSurvivorScenarioHooks.cs` (ScenPart-PostWorldGenerate Bridge)

**Modifiziert:**
- `Defs/Scenarios/SingleSurvivor.xml` (Custom ScenPart-Klasse referenzieren — neue `<parts>`-Section)

### Phase A-3 — Trait-Pool + Tests

**Neu:**
- `Defs/Traits/Rimconemy_Trait_Tough.xml`
- `Defs/Traits/Rimconemy_Trait_Jogger.xml`
- `Defs/Traits/Rimconemy_Trait_GreatMemory.xml`
- `Defs/Traits/Rimconemy_Trait_Nimble.xml`
- `Source/Character/Tests/CharacterSetupTests.cs` (6 Tests aus §8)

**Modifiziert:**
- `Source/Character/TraitAssigner.cs` (Pool-Verweise um 4 neue Traits erweitern)
- `Source/Bootstrap.cs` (CharacterSetupTests aufrufen)

## 10. Risiken & Migrationspfade

**Risiko R1 — PawnGenerationRequest 1.6-Signatur unbekannt:**
`FixedBiologicalAge` ist in H1 als COMPILES markiert. Wir prüfen zur
Laufzeit + Compile-Zeit; wenn ein Build-Fehler auftritt (z. B. weil der
Property-Typ `int?` und nicht `int` ist), fällt `BioOverride` zurück auf
`ageTracker.BirthAbsTicks += ageAdjustment` (Workaround, identisch zur
heutigen `FixAge`-Logik). Logging-Trace `BioOverride.AGE_FALLBACK_USED`.

**Risiko R2 — Vanilla-TraitDef-`<parent>`-Verkettung ist 1.6-spezifisch:**

Falls `<parent>` mit `TraitDef.ParentName` nicht durchkompiliert, Alternative:
Hybrid-XMLs duplizieren die Stat-Offsets direkt (kein Parent-Verweis).
Verhalten identisch; nur mehr Def-Verwaltungsaufwand.

**Risiko R3 — `SkillBudgetWindow.PostClose` findet keinen offenen PostApply-Pfad:**

Aktueller Code ruft `CharacterSetup.ApplyStoredBudget()` aus `PostClose`,
aber `CharacterSetup` ist statisch. Bei Nicht-Existenz des Spielers (z. B.
Zombie-Pawn) könnte `pawn.story` null sein. Schutz: `pawn.story != null`
schon vorhanden. **OK**.

**Risiko R4 — Audit-Korrektur-Konflikt**:

Wir haben kürzlich die `StoryDirector.QueueSelectedIncident`-Methode
eingeführt. Diese nutzt `Find.Storyteller.TryFire(firingIncident, queued: true)`.
Die hier entstehende `CharacterSetupGameComponent` läuft **vor** dem
StoryDirector-Eval (T=0 Phase). Keine Race.

**Risiko R5 — Alte Saves (`SchemaVersion=0`)**:

Wir persistieren `SchemaVersion = 0` als „alt", migrieren nicht rückwärts
in Track A. Erst Phase 3 mit echtem Migrationspfad (H2 §5).

## 11. Akzeptanzkriterien (Gate G5)

1. Frischer Save mit Szenario `Rimconemy_SingleSurvivor`:
   - Survivor hat BiologicalAge == 18 + ChronologicalAge == 18.
   - Survivor hat 12 Skill-Einträge (kein Skillwert > 10).
   - Summe der Skill-Level (alias budget spent) zwischen 0 und 30.
   - Wenn spent ∈ [20, 28]: keine Traits aus Rimconemy-Pool.
   - Wenn spent < 20: 1 oder 2 Negative-Traits aus Rimconemy_Pool.
   - Wenn spent > 28: 1 Positive-Trait.

2. Manuelle Spawn-Kette durchläuft:
   `ScenPart_PostWorldGenerate → BioOverride → SkillBudgetWindow → Apply → TraitAssign → CharacterSetupState.Persist`, ohne `Log.Error`.

3. 6 Tests aus §8 laufen grün via `Bootstrap.RunAll()`.

4. Build aller 5 Pakete: 0W/0E. Versions-Bump +0.0.1 auf Paket 02.

## 12. Folge-Sprints (nicht in Track A)

- Track B — Ideology-Träger (ResourceFairness/CollectiveDefense/Transparency) als PreceptDef + RoleDef + ThoughtDef.
- Track C — Story-Event-Catalog auf 12→24 Events erweitern + Familien-Mapping über `EventFamilyMap`.
- Track D — Resource-Tracking: `StorageQuery` als einzige Survivor-Quelle, `SurvivalNeedCategory` refactored.
- Phase 2-Härtung: progressive Skill-Kosten 11+.

---

> **Spec-Self-Review** durchgeführt:
> 1. Placeholder-Scan: keine TODO/TBD im Text.
> 2. Internal-Konsistenz: `CharacterSetupState` (§4.2) ↔ Persistenz (§7) konsistent.
> 3. Scope: Track A bleibt in Mod 02 — Phase-2-frei. OK für ein Plan.
> 4. Ambiguity: `Balance(spent)` und `TraitZone` sind eindeutig definiert (§4.1).
>    `Parent`-TraitDef-Verkettung (R2) als Alternative dokumentiert.
