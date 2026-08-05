# Phase E — Animal-Infection via Random Encounter (Design)
**Datum:** 2026-08-05
**Spec-Author:** Phase-E Brainstorming-Welle
**Status:** Brainstorming approved, pending writing-plans

## 1. Zweck

Die bestehende Inoculation-Pipeline (`RandomInoculationService`) konvertiert heute wild lebende Tiere in infizierte Wildtiere — aber **einmalig** (Caps via `PopulationProfileMultipliers.MaxAnimalInoculationsPerDay` werden **nie** mit Live-Trigger-Daten verbunden). Der Spieler soll jetzt laufend erleben, dass aus heimischen Wildtieren (Wolf, Bär, Mufflon etc.) aggressiv-getriebene Infizierte werden. Die Häufigkeit skaliert mit dem bestehenden Bedrohungsniveau (Horde-Cap-getrieben: je voller die Bedrohungslage, desto öfter Tiere umschlagen).

## 2. Scope (in)

- **Recurring Driver**: `AnimalInfectionDriver` als Day-Tick-Gate in `StoryDirector.GameComponentTick`.
- **Trigger-Logik**: Horden-Cap-getriebene Frequenz pro `SettingProfile` (Survival / Collapse / Refuge).
- **Caps**: Verwendet `PopulationProfileMultipliers.MaxAnimalInoculationsPerDay` als hartes Tageslimit; `MinIntervalTicks` als Mindestabstand zwischen zwei Auslösungen.
- **AI-Override**: konvertierte Tiere werden aggressiv (greifen Colonists + Tamed Animals an, Bewegungsgeschwindigkeit +50 %).
- **Visual-Marker**: rotes Warnsymbol über infizierten Tieren auf der Map-Sicht (Map-Component OnGUI-Postfix).
- **Determinismus**: Seeded aus `(currentTick mod 60000, ProfileId, HordeCount)` via FNV-1a; Save/Load-safe via `PopulationLedger.LastAnimalInfectionTick`.

## 3. Scope (out)

- **Tierart-spezifisches Verhalten** (Wolf anders als Mufflon) — alle infizierten Tiere verhalten sich gleich-aggressiv.
- **Revert-Pfad**: keine Rück-Konvertierung, wenn die Horden-Bedrohung wieder sinkt. Infektion bleibt persistent für die Save-Lifetime.
- **Ideology-Patch**: kein eigener Thoughts-Patch — der bestehende `ThoughtWorker_Transparency` reicht.
- **Mechadroid-Gegenseite**: keine direkten Mechadroid-Reaktionen auf infizierte Tiere.

## 4. Architektur

### 4.1 Komponenten-Übersicht

| Datei | Verantwortlichkeit |
|---|---|
| **Source/Inoculation/AnimalInfectionDriver.cs** (NEU) | Map-Component, prüft jede Tick ob `currentTick % TickInterval == 0` und ruft dann den Check. Stellt sicher dass `RandomInoculationService` aufgerufen wird. |
| **Source/Inoculation/AnimalInfectionChance.cs** (NEU) | Pure-Logic: `ComputeInfectionChance(long tick, HordeSnapshot snap, SettingProfile profile) -> (bool shouldFire, int count)` |
| **Source/Inoculation/AnimalInfectionAiOverlay.cs** (NEU) | MapComponent-OnGUI Postfix-Hook: malt rotes „!" Symbol über jeden infizierten Tier-Pawn auf sichtbarer Position. |
| **Source/HarmonyPatches/JobDriver_InfectedAnimalAggressive.cs** (NEU) | Harmony-Postfix auf `Pawn_JobTracker.StartJob` (oder `_WorkTick`-Variante in 1.6) der für pawn.RaceProps.Animal + faction=HiddenInfected den Movement-Stat + Speed-TempModifier auf +50% setzt und Job zu Melee-Attack zwingt. |
| **Source/Story/StoryDirector.cs** (MOD) | Wire `AnimalInfectionDriver` in `GameComponentTick` nach `ResetDailyCounters` und vor `RevengeQuota` neu. |
| **Source/Inoculation/RandomInoculationService.cs** (MOD) | Bereits enthält die Konvertierungs-Pipeline. unverändert. |
| **Source/Population/PopulationLedger.cs** (MOD) | Neue Felder: `LastAnimalInfectionTick`, `AnimalInfectionCountToday`. Beide scribe-bar. |
| **Source/Population/PopulationProfileMultipliers.cs** (MOD) | Neue Felder: `AnimalInfectionBaseChance` (PerProfile), `AnimalInfectionHordeScalingFactor` (PerProfile). |
| **DefXml/InfectedWildlife: CombatPower** (MOD) | `Rimconemy_InfectedWildlife.xml` CombatPower von 30 → 50 (wegen aggressiver AI), kein Race-Swap. |

### 4.2 Determinismus

```
seedKey   = FNV1a("{TickDayBucket}|{ProfileId}|{HordeCount|FloorDiv 10}")
roll      = seedKey.To01()                            // [0,1)
result    = roll < ComputeInfectionChance(...)
resultCount = ComputeInfectionCount(...)
```

- `TickDayBucket := currentTick / 60000` — ein Bucket pro In-Game-Tag, damit Refresh nicht ständig neu würfelt.
- `HordeCount|FloorDiv 10` — Horden-Buckets in 10er-Schritten, damit kleine Schwankungen nicht den Würfel-Wert ändern.
- `LastAnimalInfectionTick` persistiert in `PopulationLedger` (Scribe-safe).

### 4.3 Save/Load

- `_lastAnimalInfectionTick` und `_animalInfectionCountToday` werden in `PopulationLedger` gescrieben — class ist bereits `ISchemaMigratable`, Schema bleibt `1`.
- `FinalizeInit` setzt `_lastAnimalInfectionTick = -1L` falls nicht im Scribe-Stream — verhindert NaN/Null-Probleme.
- Day-Bucket-Reset läuft analog zu `ResetDailyCounters` (Tick-Bucket-Vergleich).

### 4.4 Datenfluss

```
GameComponentTick (StoryDirector)
  ↓ Day-Tick-Gate
AnimalInfectionDriver.TryFireOnce(currentTick, ledger, profile)
  ↓ ComputeInfectionChance Pure
  ↓ Decision: fire + count
RandomInoculationService.TryInfectWildAnimals(count)
  ↓ (bereits implementiert)
  ├─ walk map for wild animals → filter non-infected
  ├─ pick first N candidates
  └─ for each: kindDef swap + faction rebrand → ledger.NoteInoculation
        ↓
        Postfix (JobDriver_Patch): aggressive AI unlocked
        Postfix (AiOverlay): visual "!" über pawn rendert
```

### 4.5 API-Signaturen

```csharp
// AnimalInfectionChance.cs (Pure)
public static class AnimalInfectionChance
{
    public static double ComputeChancePerDay(
        long tickDayBucket, int hordeCount, SettingProfile profile);

    public static int ComputeInfectionCount(
        long tickDayBucket, int hordeCount, SettingProfile profile);

    // Determines if today should fire based on: profile allows +
    // horde threshold met + daily-cap not exceeded
    public static bool ShouldFireToday(
        long currentTick, int todayCount, int hordeCount, SettingProfile profile);
}

// AnimalInfectionDriver.cs (MapComponent + Pure seam)
public sealed class AnimalInfectionDriver : MapComponent
{
    private long _lastTickProcessed = -1L;

    public static int StubTodayCount;  // test seam

    public override void MapComponentTick()
    {
        long now = Find.TickManager.TicksGame;
        // Re-Audit-gate: skip if not Day-Tick multiple
        if (now - _lastTickProcessed < TickInterval) return;
        _lastTickProcessed = now;

        var ledger = PopulationLedger.Get();
        var profile = Story.StoryDirector.Get()?.ActiveProfile;
        if (ledger == null || profile == null) return;

        int hordeCount = ledger.HumanoidLiveCount + ledger.AnimalLiveCount / 2;

        if (!AnimalInfectionChance.ShouldFireToday(
                now, ledger.AnimalInfectionCountToday, hordeCount, profile))
            return;

        int count = AnimalInfectionChance.ComputeInfectionCount(now, hordeCount, profile);
        if (count <= 0) return;

        int actually = RandomInoculationService.TryInfectWildAnimals(count);
        if (actually > 0)
        {
            ledger.LastAnimalInfectionTick = now;
            ledger.AnimalInfectionCountToday += actually;
        }
    }
}
```

### 4.6 Profile-Multipliers (neu)

| Profile | BaseChance | HordeScalingFactor | MaxAnimalInoculationsPerDay | MinIntervalTicks |
|---|---:|---:|---:|---:|
| Survival | 0.05 (5%) | 1.0 | 2 | 6,000 (1h game-time) |
| Collapse | 0.15 (15%) | 1.5 | 4 | 3,000 (30 min) |
| Refuge | 0.02 (2%) | 0.5 | 1 | 12,000 (2h game-time) |

`ChancePerDay = BaseChance × (1 + HordeScalingFactor × max(0, hordeCount − HordeThreshold) / HordeThreshold)`

Cap: Niemals > 0.95 (sicherheits-Hardcoded, damit nicht 100% Brand täglich).

## 5. Tests

### AnimalInfectionChance-Tests (T1-T8)
- T1: Survival mit 0 Horde → Chance = `AnimalInfectionBaseChance.Survival = 0.05`.
- T2: Survival mit 100 Horde → Chance = 0.05 × (1 + 1×100/150) ≈ 0.0833.
- T3: Survival mit 200 Horde → Chance steigt weiter ≈ 0.117.
- T4: Collapse mit 50 Horde → 0.15 × (1 + 1.5×50/80) ≈ 0.291.
- T5: Refuge mit 0 Horde → 0.02 (Minimum).
- T6: Horde unter Threshold → Chance bleibt auf BaseChance (kein Decay, nur Steigerung).
- T7: Count-Per-Day hard-cap bei 95% Überschreitung: clamp auf 0.95.
- T8: Count-Calc: bei 4 Pawns-per-day-cap (Collapse), Tages-Anzahl n mag <= 4 sein.

### AnimalInfectionDriver-Tests (T9-T15)
- T9: Day-Tick-Gate: nur alle `TickInterval`-Ticks aufgerufen.
- T10: Tier-Settings (StubTodayCount=3) → returns false für TodayShouldFire.
- T11: Initial-State: kein letzter Tick → fires sobald Count-Horizon erreicht.
- T12: Save/Load-Recovery: `LastAnimalInfectionTick=0L` ⇒ Driver resettet.
- T13: `AnimalInfectionCountToday` wird tatsächlich um `actually` inkrementiert.
- T14: Profile-Null bzw. Ledger-Null → Driver-LogWarning + no-op.
- T15: Re-Run auf gleichem Tick ist idempotent (kein Doppelspawn).

### AnimalInfectionAiOverlay-Tests (T16-T19) — Light-Coverage
- T16: Pawn mit `kindDef.defName == "Rimconemy_InfectedWildlife"` → OverlayTarget-Liste enthält ihn.
- T17: Normaler Wildwolf ohne Conversion → nicht in Liste.
- T18: Pawn in Other-Map (off-screen) → nicht gezeichnet.
- T19: null Map / null PawnRender → early-return.

### Harmony-AI-Patch-Tests (T20-T22) — Recommand gegen Mock, stattdessen Regression-Check
- T20: Patch ist registered (`PatchAll` läuft ohne Exception).
- T21: Pawn-in-MapComponent.OnGUI rendert Marker für infizierte Tiere (Build-Check).

Hinweis: AI-Verhalten kann nur in-game verifiziert werden — Falsification deckt das ab.

## 6. Determinismus-Punkte

- **Tages-Bucket-Computation** nutzt `currentTick / 60000` (In-Game-Day) — gleicher Tag = gleicher Wert.
- **Horden-Count** wird mit `FloorDiv 10` quantisiert, sodass Horden-Schwankungen ≤9 keine Driver-Reaktion auslösen.
- **Roll-Outcome** ist deterministisch: gleicher Tick + Count ⇒ gleicher Roll.
- **`Actually`-Counter** wird im Ledger gespeichert, sodass Re-Runs (z. B. nach Save+Load) nicht doppelt zählen.

## 7. Edge-Cases

| Edge | Treatment |
|---|---|
| Map hat 0 Wildtiere | Driver feuert nichts — Log.Message nur im Debug-Build |
| Profile ist ungültig | `LogWarning` + Default Survival-Multiplikatoren |
| RandomInoculationService.Exception | Catch + LogWarning + nächster Tick retryt |
| Save-Quench in der Mitte des Tages | Buckets werden auf Tagesanfang resettet |
| Race-Swap unmöglich (kein kindDef-Eintrag) | Setup-LogWarning + Skip-Infection für diesen Pawn |
| Already-Infected-Tier (Re-Inoculation) | `RandomInoculationService` filtert bereits `Rimconemy_InfectedWildlife`-Pawns raus |

## 8. Falsification

### §G: Animal-Infection Live-Beleg

Schritte für User-In-Game-Test:

1. **Setup**: Survival-Profile starten, Tiere in Wildnis spawnen (z. B. via Dev-Mode oder Zufall).
2. **Trigger simulieren**: Force-spawn Horden-Cap via StoryDirector + Hover-Position beobachten.
3. **Beobachten**: Map-Layer auf Rottöne checken — nach kurzer Zeit erscheint rotes „!" über mindestens einem Tier (Wolf, Bär, Mufflon).
4. **Tier-Verifikation**: Tier greift jetzt aktiv einen Colonist oder Tamed Animal an (Combat-Log-Eintrag).
5. **Speed-Boost-verification**: Pawn-Bewegung merklich schneller als unbefallene Tiere.
6. **Determinismus-Check**: Save → Reload, gleicher Tag, gleicher Horden-Cap → exakt gleiche Conversion-Liste.
7. **Profile-Wechsel-Test**: Survival → Collapse → Refuge und zurück, Multiplier müssen live umschalten.

### §G-Writeup-Format

Siehe bestehender Falsification-§E-§F-Style, mit Schritt-Bullet, Erwartete-Beobachtung, Player.log-Snippet-Placeholder.

## 9. Plan-Outline (writing-plans-Input)

- **T1-T3**: `AnimalInfectionChance.cs` (Pure + Test-T1-T8).
- **T4**: `PopulationProfileMultipliers.cs` (neue Felder + Tests).
- **T5**: `PopulationLedger.cs` (LastAnimalInfectionTick + CountToday + Scribe).
- **T6**: `RandomInoculationService.cs`-Erweiterung um TryInfectWildAnimals(int max) (Test-T15 idempotenz).
- **T7**: `AnimalInfectionDriver.cs` (MapComponent + StoryDirector-Wiring + Tests T9-T14).
- **T8**: `Rimconemy_InfectedWildlife.xml` CombatPower-Erhöhung (Def-Update).
- **T9**: `JobDriver_InfectedAnimalAggressive.cs` Harmony-Patch + Speed-Modifier (manuell in-game verifiziert).
- **T10**: `AnimalInfectionAiOverlay.cs` MapComponent-OnGUI-Postfix + Tests T16-T19.
- **T11**: Falsification §G + VERSION-Bump 0.0.62 → 0.0.63 + Foundation-Registry-Sync.
- **T12**: runtime_test + Code-Review.

## 10. Risiken

- **Harmony-Patch-Konflikt** mit anderen Animal-AI-Mods möglich. Mitigation: Postfix-only, kein Prefix. Falls Konflikt, LogWarning + Default-Verhalten (kein Patch-Anwendung, keine Verhaltensänderung).
- **Horden-Erkennung** ist abhängig von `PopulationLedger.HumanoidLiveCount+AnimalLiveCount/2` (Horde-Cap-Threshold). Falls Ledger falsche Werte liefert, feuert Driver zu früh. Mitigation: Hardcoded Lower-Bound (mind. 1 Horde).
- **Symbol-Render-Overhead**: bei vielen infizierten Tieren auf einer Map. Mitigation: nur sichtbare Cells rendern; alle Tiere in OnGUI via foreach (pawn verbindlich nur einmal pro Frame gezeichnet).

## 11. Reviewer-Punkte (Final Whole-Branch)

- BD1-T1..T8 (Pure-Logic Outcome) — pure, deterministic.
- BD1-Driver Mausklick-Verify — User-Pflicht.
- BD1-Harmony-AI-Patch — Build-Clean ohne andere Mods (verifiziert in runtime_test).
- BD1-VIS-Verify — User-Pflicht in-game.

---

**Spec-Final:** Geht an writing-plans-Skill für Implementation-Plan.
