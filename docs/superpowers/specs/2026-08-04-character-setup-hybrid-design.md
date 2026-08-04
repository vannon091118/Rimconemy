# Character Setup Hybrid Design

> **Stand:** 2026-08-04  
> **Status:** Vom Nutzer freigegebenes Design zur Review vor der Implementierung  
> **Scope:** Roadmap Phase 4 / H5 Character Setup — Pure-Logik und Determinismus; direkter Pawn-Generator-Hook bleibt API-Gate

## Ziel

Der Character-Setup-Kern soll Skillbudget, Neutralzone und Trait-Ausgleich reproduzierbar berechnen. Gleiche Eingaben und derselbe Seed müssen dasselbe Ergebnis liefern; die Auswahl darf nicht von globalem `Rand`-Zustand oder der Reihenfolge anderer Spielaktionen abhängen.

## Nicht-Ziel

In diesem Schnitt wird kein unbestätigter Harmony-Patch auf `PawnGenerator.GeneratePawn` oder eine vermeintliche `PawnGenerationRequest`-Signatur eingeführt. `FixedBiologicalAge` und `FixedChronologicalAge` bleiben als explizite 18/18-Vertragswerte und der bestehende, dokumentierte `FixAge`-Fallback erhalten, bis der lokale API-Spike `API-START-01` eine konkrete Signatur und einen Runtime-Beleg liefert.

## Architektur

### 1. Pure Budgetlogik

`SkillBudgetCalculator` bleibt die einzige Quelle für:

- kumulative Skillkosten;
- Budgetvalidierung;
- `spent - NeutralCenter`-Bilanz;
- Trait-Zone und Trait-Anzahl;
- Level-Clamping.

UI, CharacterSetup und Tests verwenden dieselben Funktionen. Die H5-Werte bleiben unverändert: Gesamtbudget 30, NeutralCenter 25, Neutralzone `[-5, +3]`, maximal ein positiver und maximal zwei negative Traits.

### 2. Deterministische Trait-Auswahl

`TraitAssigner` erhält einen expliziten Seed-Eingang. Die Trait-Auswahl wird aus einer stabilen, lokalen Seed-Funktion und dem Trait-Pool-Index berechnet. Sie liest keinen globalen `Verse.Rand`-Zustand mehr für die H5-Auswahl.

Der Seed-Vertrag lautet:

```text
traitSeed = explicitSeed XOR stableHash(pawnIdentityOrFallback)
traitIndex = deterministicIndex(traitSeed, poolLength)
```

Der Fallback muss auch ohne Pawn-Objekt reproduzierbar sein; die Pure-Logik testet direkt mit Seed und Pool. Für den Runtime-Wrapper wird ein stabiler Pawn-Identifier verwendet, wenn vorhanden, sonst der übergebene Seed unverändert.

Die öffentliche API wird ergänzt um:

```csharp
public static TraitSelectionResult SelectTraitsForBudget(
    int spentPoints,
    int seed,
    IReadOnlyList<string> availablePositiveTraits,
    IReadOnlyList<string> availableNegativeTraits);
```

`TraitSelectionResult` enthält mindestens Zone, Balance, positive Trait-IDs und negative Trait-IDs. Das Ergebnis ist frei von `Pawn`, `DefDatabase`, `Rand` und `Log` und somit direkt testbar. Der bestehende Pawn-Wrapper löst anschließend IDs gegen `TraitDef` auf, prüft Def-Verfügbarkeit und wendet die bereits ausgewählten Traits idempotent an.

### 3. Runtime-Kompatibilitätsgrenze

`CharacterSetup.FixAge(Pawn)` bleibt der aktuelle Fallback für den späteren Post-Generation-Pfad. Ein direkter Generator-Hook ist in dieser Arbeit nicht enthalten, weil H1 `FixedBiologicalAge`, `FixedChronologicalAge` und `GenerateTraits` noch nicht als lokal kompilierte/verhaltensverifizierte API freigibt.

## Datenfluss

```text
Skill input + explicit seed
        ↓
SkillBudgetCalculator.ValidateBudget
        ↓
spent / balance / TraitZone
        ↓
TraitAssigner.SelectTraitsForBudget (pure, deterministic)
        ↓
Pawn wrapper: Def lookup + idempotent application
        ↓
CharacterSetup / SkillBudgetWindow
```

## Fehler- und Randfallregeln

- `null`- oder leere Pools erzeugen keine Trait-ID und keinen Fehler.
- Die Neutralzone erzeugt keine positiven oder negativen Trait-IDs.
- Ein überzogenes Budget bleibt ungültig; die Pure-Auswahl darf daraus keine zusätzliche Belohnung ableiten.
- Trait-Pools werden vor Auswahl stabil sortiert oder in der dokumentierten Reihenfolge unverändert verwendet; Tests legen diese Entscheidung fest.
- Doppelte IDs im Pool werden nicht doppelt angewendet.
- Ein fehlender `TraitDef` erzeugt eine Warnung und wird übersprungen; der gesamte Pawn-Start darf dadurch nicht abbrechen.
- Wiederholte Anwendung auf denselben Pawn darf keine bereits vorhandene Trait-ID erneut hinzufügen.

## Tests und Belege

Die paketinterne Test-Suite erhält Pure-Tests für:

1. H5-Kosten-Tabelle und Budgetgrenze;
2. Neutralgrenzen `-5` und `+3`;
3. Trait-Anzahlen für Buffer, leichte/starke Negativzone und Positivzone;
4. gleicher Seed + gleiche Eingabe = byte-/wertgleiches Ergebnis;
5. anderer Seed kann eine andere Auswahl aus einem Pool erzeugen;
6. leere Pools und Neutralzone bleiben fehlerfrei und traitfrei;
7. Trait-Auswahl ist unabhängig von globalem `Verse.Rand`-Zustand;
8. bestehende 18/18- und Pre-Game-Bio-Remap-Regressionen bleiben erhalten.

Der Build-Beleg bleibt `dotnet build` für Paket 02 mit den lokalen RimWorld-/Harmony-Referenzen. Ein Spielstart-, Def-Load- und Save-/Load-Beleg für den Charakterpfad bleibt als separates Runtime-Gate offen.

## Abgrenzung zu Folgearbeit

Nach Abschluss dieses Schnitts:

- `API-START-01` konkret mit lokaler Assembly/Decompilation prüfen;
- erst danach entscheiden, ob ein Generator-Harmony-Patch sicher implementiert werden kann;
- Save-Schema `CharacterSetupState` und Bio-Remap-Fenster als separaten A-2-Schnitt planen;
- Live-Test mit identischem Bio-/Skill-Input und Seeds 42/99 durchführen.
