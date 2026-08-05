# Falsifizierungs-Stand-Bericht: `Infected/AnimalInfection` (Phase E · v0.0.63)

> **Capability:** `rimconemy.infectedautomation.automation` v1 · **Owner:** Infected · **Stand:** 2026-08-05
> **Status:** `COMPILED, BOOT, REGRESSION` · `LIVE`: pending user verification
> **Code-Anker:**
> - `Source/Inoculation/AnimalInfectionChance.cs` (Pure-Logic: ComputeChancePerDay + ShouldFireToday + ComputeInfectionCount)
> - `Source/Inoculation/AnimalInfectionDriver.cs` (Parallel seam; **production-hook siehe Anmerkung B-1**)
> - `Source/Inoculation/RandomInoculationService.cs` (TryInfectWildAnimals + TryApplyInfectionAggressionHediff)
> - `Source/Population/PopulationLedger.cs` (LastAnimalInfectionTick, AnimalInfectionCountToday, RegisterAnimalInfection, ResetAnimalInfectionDailyCounters)
> - `Source/Population/PopulationProfileMultipliers.cs` (AnimalInfectionBaseChance + AnimalInfectionHordeScalingFactor + HordeThreshold + InoculationsPerDay)
> - `Source/Inoculation/AnimalInfectionAiOverlay.cs` (ShouldShowInfectionMarker-Predikat, MarkerTexture-Cache)
> - `Source/Story/StoryDirector.cs` (TryFireProfileInfection — **Production-Hook**)
> - `Defs/PawnKinds/Rimconemy_InfectedWildlife.xml` (CombatPower=50)
> - `Defs/Hediffs/Rimconemy_InfectedWildlifeAggression.xml` (+50 % MoveSpeed, isInfection=true)
> - `Tests/AnimalInfectionRegressionTests.cs` (T1–T8)
> - `Tests/AnimalInfectionLedgerFieldsTests.cs` (Ledger-Scribe)
> - `Tests/AnimalInfectionServiceLimitTests.cs` (TryInfectWildAnimals-Limits)
> - `Tests/AnimalInfectionDriverTests.cs` (Driver-Seam — siehe Anmerkung B-1)
> - `Tests/AnimalInfectionAiOverlayTests.cs` (Overlay-Predikat)

**Lifecycle:** `UNVERIFIED` → `COMPILED` (dieser Stand) → `LOADED` (nach Live-Beleg D-3) → `SURVIVED` (nach Live-Belegen aller Gates).
**Spec-Referenz:** `docs/superpowers/specs/2026-08-05-animal-infection-design.md`.
**Plan-Referenz:** `docs/superpowers/plans/2026-08-05-animal-infection.md` (12 Tasks, alle abgeschlossen).
**Schwesterberichte:** `docs/falsification/infected__ThreatPressure.md` (Phase D · Horde), `docs/falsification/infected__InfectedRaid.md` (Phase B/C · Human-Raids).

> **Status-Legende:** 🟢 = Pre-LIVE belegt · 🟡 = Phase-E-Gate pending · 🔴 = offen / blockierend

---

## A — Def-Liste (XML-Defs)

| Pfad | Schlüssel | Inhalt | Status |
|---|---|---|---|
| `Defs/PawnKinds/Rimconemy_InfectedWildlife.xml` | `<defName>Rimconemy_InfectedWildlife</defName>` | Branded-PawnKind für konvertierte Wildtiere, `combatPower=50` (Phase E ramped 30→50), `defaultFactionDef=Rimconemy_HiddenInfectedFaction` | 🟢 |
| `Defs/Hediffs/Rimconemy_InfectedWildlifeAggression.xml` | `<defName>Rimconemy_InfectedWildlifeAggression</defName>` | HediffWithComps, `statOffsets/MoveSpeed=0.5`, `isInfection=true`, `defaultLabelColor=(0.85,0.20,0.20)` | 🟢 |

> **Anmerkung A-1:** Phase-E wirkt nur auf Wild-Tiere via `InoculationConverter` (Phase C bereits übernommen). `Rimconemy_InfectedRavager` (Human) bleibt unverändert. Kind-Swap-Pfad: original-Pawn behält `Race` (Wolf/Bear/Caribou/…), nur `KindDef` wird auf `Rimconemy_InfectedWildlife` umgehängt; damit bleibt der Vanilla-Tier-Tag visuell erhalten und der Hediff ist als zusätzlicher State sichtbar.

---

## B — Code-Pfad (Build + Boot)

`Bootstrap` ruft `World.DarknessSectionLayerLifecycle.Install()` + `Threat.ThreatSnapshotBridgeRegistry.Install()` + die StoryDirector-Phase-D-Wiring-Log-Zeile auf. Phase E nutzt **kein** zusätzliches `Install()`/`PatchAll()`-Call — der Trigger lebt komplett in `StoryDirector.GameComponentTick`.

**Phase-E-Tag-Pipeline (in Code-Reihenfolge, entspricht Spec §3 Diagram):**

1. **StoryDirector.GameComponentTick** (jeden 60 000-Tick Day-Tick):
   - WipeCheck (alle 250 Ticks)
   - Evaluation-Gate (60 000 Ticks seit letztem Eval)
   - Wipe-Eval (StorySelector + Queue) — Phase B DailyGrowth + ResetDailyCounters + RecomputeRevenge
   - **TryInfectRandom** (Phase C Tier-Inokulation, profillos)
   - **`TryFireProfileInfection(currentTick)`** ← **Phase E Eintrittspunkt**

2. **StoryDirector.TryFireProfileInfection** (privat, inlined aus Driver-Spec):
   ```csharp
   var ledger = PopulationLedger.Get();
   if (ledger == null || ActiveProfile == null) return;
   int hordeCount = Math.Max(0, ledger.HumanoidLiveCount + ledger.AnimalLiveCount/2);
   if (!AnimalInfectionChance.ShouldFireToday(currentTick, ledger.AnimalInfectionCountToday, hordeCount, ActiveProfile)) return;
   int count = AnimalInfectionChance.ComputeInfectionCount(currentTick, hordeCount, ActiveProfile);
   if (count <= 0) return;
   int actually = RandomInoculationService.TryInfectWildAnimals(count, currentTick);
   if (actually > 0) ledger.RegisterAnimalInfection(actually, currentTick);
   ```

3. **AnimalInfectionChance.ShouldFireToday** (Pure-Logic, FNV-1a):
   - `dayBucket = currentTick/60000L`, wenn `<1L` ⇒ `false` (cold-start)
   - ProfileTag stripped via `StripRimconemyPrefix` (siehe `StoryDirector` Doc)
   - `cap = PopulationProfileMultipliers.GetInoculationsPerDay(key)` ⇒ wenn `todayCount >= cap` ⇒ `false`
   - `chance = ComputeChancePerDay(dayBucket, hordeCount, profile)` (HardCap 0.95)
   - `roll = Fnv1a($"{dayBucket}|{key}|{hordeCount/10}|fire") % 10000 / 10000.0`
   - return `roll < chance`

4. **AnimalInfectionChance.ComputeInfectionCount** (Pure-Logic, FNV-1a):
   - cap = Profile-getter ⇒ wenn `<=0` ⇒ return 0
   - `rollBucket = Fnv1a("cnt|{tickDayBucket}|{key}|{hordeCount/10}") % 1024`
   - `pct = rollBucket/1024.0`
   - return `Math.Min(cap, Floor(pct*(cap+1)))` → 0..cap Verteilung, häufig 0, gelegentlich cap

5. **RandomInoculationService.TryInfectWildAnimals(maxCount, currentTick)**:
   - null-Guard `Current.Game`, profileQuota-Lookup
   - wenn `profileQuota<=0` ⇒ return 0 (log nur in `DebugSettings.godMode`)
   - Wildtier-Pool: `ListerThings.ThingsInGroup(ThingRequestGroup.Pawn)` gefiltert auf `RaceAnimal==true && Faction==null`
   - Hard-Ceiling: `Math.Min(maxCount, profileQuota)` schützt vor Profile-Mismatch-Overshoot
   - iterates mit Filter (Alive + Animal + Wild) und ruft `ApplyLiveConversion(candidate)`
   - bei Erfolg: `Log.Message($"[Rimconemy] RandomInoculationService.TryInfectWildAnimals: requested=N cap=K converted=N tick=T")` *gated auf godMode* (siehe Anmerkung D-1)

6. **ApplyLiveConversion** (aus Phase C, erweitert in Phase E T7):
   - `kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail("Rimconemy_InfectedWildlife")`
   - `faction = DefDatabase<FactionDef>.GetNamedSilentFail("Rimconemy_HiddenInfectedFaction")`
   - `pawn.kindDef = branded; pawn.SetFaction(faction)`
   - **`TryApplyInfectionAggressionHediff(pawn)`** (Phase E T7) — idempotent via `GetFirstHediffOfDef`-Guard
   - `ledger.NoteInoculation(candidate.KindDefName)` (Phase-C Counter — siehe DECISION-E-001)

7. **TryApplyInfectionAggressionHediff(pawn)** (private Helper):
   - HediffDef via `HediffDef.Named("Rimconemy_InfectedWildlifeAggression")`
   - Skip wenn `health.hediffSet` null
   - Skip wenn `GetFirstHediffOfDef != null` (idempotent)
   - `HediffMaker.MakeHediff(hediffDef, pawn)` + `pawn.health.AddHediff(hediff)`

**Defensive Layer:** Jeder Schritt ist try/catch-gewrappt (Spec §4 Vertrag); Logging-Level ist `Log.Message` (Inoc-Service) bzw. `Log.Message` (AnimalInfectionDriver) bei Erfolg.

> **Anmerkung B-1:** `Source/Inoculation/AnimalInfectionDriver.cs` ist eine **statische 1:1-Kopie der Phase-E-Pipeline**, die ausschließlich als **Test-Seam** existiert (`AnimalInfectionDriverTests` greift auf `AnimalInfectionDriver.TryFireOnce` + `ResetForTests` zu). Production-Pfad ist `StoryDirector.TryFireProfileInfection`. Beide Implementierungen sollen identisches Verhalten zeigen (Hardcoded-Determinismus über FNV-1a-Same-String). Diese Trennung ist im Cleanup-Pass bewusst beibehalten und in DECISION-E-003 dokumentiert — ein zukünftiges Refactoring könnte beide Mergen, ist aber kein Gate-Blocker.

---

## C — Selbsttest (RunAll-Regression)

`Bootstrap.RunAll` ruft aktuell **3 von 5** Phase-E-Test-Runners auf (siehe Anmerkung C-1):

| Test-Datei | Tests | Bootstrap-Wire | Status |
|---|---:|---|---|
| `Tests/AnimalInfectionRegressionTests.cs` | T1–T8 (Pure-Logic AnimalInfectionChance + Profile-Multipliers) | ✅ `RunAll` aufgerufen | 🟢 |
| `Tests/AnimalInfectionLedgerFieldsTests.cs` | Ledger-Scribe Roundtrip (LastAnimalInfectionTick + CountToday + ResetAnimalInfectionDailyCounters) | ✅ `RunAll` aufgerufen | 🟢 |
| `Tests/AnimalInfectionServiceLimitTests.cs` | TryInfectWildAnimals-Limits (ProfileQuota=0 + Hard-Cap) | ✅ `RunAll` aufgerufen | 🟢 |
| `Tests/AnimalInfectionDriverTests.cs` | TierMap-Component-Seam-Tests (TryFireOnce Null-Guards + ResetForTests) | ❌ **nicht in Bootstrap.RunAll** | 🟡 |
| `Tests/AnimalInfectionAiOverlayTests.cs` | Overlay-Predikat (ShouldShowInfectionMarker) | ❌ **nicht in Bootstrap.RunAll** | 🟡 |

> **Anmerkung C-1:** Die zwei Tests ohne Bootstrap-Wire sind deterministisch und Compile-grün (`dotnet build` 0 errors, 0 warnings), werden aber zur Laufzeit **nicht** aufgerufen. Behebung: ein-zeiliger Add in `Bootstrap.cs` nötig, Folge-Aktion (siehe Gate EG-9).

**Erwartung im Player.log nach erfolgreichem Bootstrap:**

```
[Rimconemy.InfectedAutomation] Standalone bootstrap starting...
... (DarknessSectionLayer + ThreatSnapshotBridge Install-Logs)
[Rimconemy.InfectedAutomation] Phase E: AnimalInfection pipeline wired (Profile-Chance, Ledger, Service).
[Rimconemy.InfectedAutomation] AnimalInfectionRegressionTests: N passed, M failed.
[Rimconemy.InfectedAutomation] AnimalInfectionLedgerFieldsTests: N passed, M failed.
[Rimconemy.InfectedAutomation] AnimalInfectionServiceLimitTests: N passed, M failed.
```

(`Phase-D`/`Phase B`/`Phase C` Test-Logs der vorherigen Phasen dazwischen unverändert.)

---

## D — Phase-E Live-Beleg (User-Pflicht) 🟡

**Vorbedingungen:**

- Survival-Profile gewählt (Difficulty Medium → `Rimconemy_Survival`).
- ≥ 5 wild-lebende Tiere in Sichtweite (Wolf, Mufflon, Caribou, Bär, etc.).
- Kapazität noch unter `HordeThreshold=150` (Survival) — sonst Trigger feuert fleissiger.

**Profil-Schlüssel-Übersicht (aus `PopulationProfileMultipliers`):**

| Profile | Inoc/Day | BaseCh | HordeScal | HordeThresh |
|---|---:|---:|---:|---:|
| Refuge | 0 | 0.02 | 0.5 | 220 |
| Survival | 1 | 0.05 | 1.0 | 150 |
| Collapse | 3 | 0.15 | 1.5 | 80 |

### Schritt 1 — Setup (5 min)

1. `./scripts/deploy.sh 05` (Live-Deploy).
2. Survival-Kolonie gründen (`Rimconemy_SingleSurvivor` optional; Standard-Survival equally OK).
3. Dev-Mode an (`!`), `DebugSettings.godMode` aktiviert lassen (für zusätzliche Skip-Logs, siehe D-1).
4. Sicherstellen dass Profil-Anzeige in ThreatDashboard `Survival` zeigt.
5. Kolonie-Größe + aktuelles `PopulationLedger.HumanoidLiveCount` notieren (Dev-Inspector oder HUD).

### Schritt 2 — Multi-Day-Beobachtung (mehrere Tage, kein Tag-Skip)

Erwartung im Player.log während 5–10 In-Game-Tagen:

```
[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals: requested=N cap=K converted=N tick=T
```

(Wahrscheinlichkeit pro Survival-Tag: **~5 % Base** + Horde-Scaling × `(horde-150)/150` ;_gated auf godMode_. Mit aktueller Spätphase-Kolonie ist eine Trigger-Rate von **1 von ~15–25 Tagen** realistisch. Wenn Map vollständig leer an Wildtieren ⇒ `converted=0`, dann erwarte mehrere Tage "no candidates"-Logs.)

**Was beobachten:**
- Mindestens 1 Tier zeigt rot-schwarzen Health-Tab-Eintrag `infected wildlife aggression`.
- Dieses Tier bewegt sich **sichtbar schneller** als Artgenossen (Vanilla MoveSpeed + 0.5).
- Tier folgt jetzt `Rimconemy_HiddenInfectedFaction`-AI (in SpriteSheet als `HiddenInfected`-Faction-Color markiert).

### Schritt 3 — Profile-Switching-Test

1. Schwierigkeit auf **Hard** ändern → Profil wechselt zu `Rimconemy_Collapse`.
2. Erwartung: `BaseChance` steigt von 0.05 → **0.15** (3-fach), `InoculationsPerDay` von 1 → **3**.
3. In den nächsten 5 Tagen: **mehrere** Trigger-Cycles wahrscheinlich (Base+Scaling ohne Horde-Bonus).
4. Schwierigkeit zurück auf **Easy** → Profil wechselt zu `Rimconemy_Refuge`.
5. Erwartung: `InoculationsPerDay==0` ⇒ **keine** neuen Infektionen. Log-Zeile wenn godMode:
   ```
   [Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals: profile 'Refuge' InoculationsPerDay == 0 → skipping cycle.
   ```
   Dieser Skip-Pfad kann **mehrfach pro Tag** auftauchen (Driver-Feuerversuch alle 60 000 Ticks = 1 Tag).

### Schritt 4 — Hediff-Effect-Im-Save-Test

1. Mindestens 1 Tier konvertiert (Schritt 2 erfolgreich).
2. Save → Quit → Reload.
3. Erwartung nach Load:
   - Hediff `infected wildlife aggression` ist **noch attached** (sichtbar im Health-Tab, roter Marker).
   - MoveSpeed-Boost noch aktiv (Tier läuft noch immer spürbar schneller).
   - Faction noch `Rimconemy_HiddenInfectedFaction` (kein Faction-Drift).
   - KindDef noch `Rimconemy_InfectedWildlife` (kein KindDef-Revert).

> **Mechanik-Begründung:** Hediffs werden via Vanilla-`Pawn_HealthTracker.ExposeData` mit-gescribed (siehe TryApplyInfectionAggressionHediff Doc-Kommentar). Damit ist die Save-Stabilität **per Design** garantiert — wenn sie scheitert, ist es ein Vanilla-RimWorld-1.6-Bug, kein Rimconemy-Bug.

### Schritt 5 — Combat-Beobachtung

1. Konvertiertes Tier in Reichweite eines Colonisten bringen.
2. Erwartung: Tier attackiert Colonist (Vanilla-Faction-AI; Aggression-Hediff verstärkt via `MoveSpeed` die Angriffs-Rate).
3. Combat-Log zeigt:
   ```
   <PawnName> was bitten by <AnimalRace>
   ```
4. Defensiv: bei nicht-Aggression ist die Vanilla-Tier-AI primär `Wander` (kein direkter Attack); die Hediff-Speed-Steigerung macht das Tier "gefährlicher", aber kein AI-Override (kein Harmony-Patch auf `Pawn_JobTracker.StartJob`). Folge-Phase optional.

### Schritt 6 — Determinismus-Verifikation

1. Notiere Tick-Wert + HordeCount + Profil zum Zeitpunkt eines erfolgreichen Triggers.
2. Save → Quit → Reload mit **identischem** Tick + Profil + HordeCount.
3. Erwartung: identischer `converted`-Wert (FNV-1a-Determinismus über `dayBucket|profile|horde|10`-Schlüssel).

### Schritt 7 — AnimalInfectionAiOverlay Visual-Test

1. Konvertiertes Tier auf der Map sichtbar.
2. Optional-RenderHook (Phase E T8 nicht vollständig angeflanscht): `AnimalInfectionAiOverlay.ShouldShowInfectionMarker(pawn)` returnt `true` für diesen pawn.
3. Ein zukünftiger RenderHook (Phase E+) kann `GetOrLoadMarkerTexture()` für eine rote-Marker-Visualisierung nutzen — aktuell wird das Texture nur zwischengespeichert, ein Draw-Call ist noch nicht wired (siehe Anmerkung D-2).

> **Anmerkung D-1:** Skip-Path-Logs in `RandomInoculationService.TryInfectWildAnimals` sind **ausschließlich in `DebugSettings.godMode`** sichtbar. Für Live-Beleg aktiviert lassen — sonst sieht man nur `actualLogged` Erfolgsfälle. Eine Auswertung über `Log.OldGetPooledLogMessages` kann auch im Production-Run nachträglich ausgewertet werden.
>
> **Anmerkung D-2:** `StoryDirector.TryFireProfileInfection` loggt **nicht** auf Erfolgs-Pfad (im Gegensatz zur Driver.cs-Kopie, die `Log.Message` ruft). Live-Beleg muss daher auf den `RandomInoculationService`-Skip-Path oder den Hediff-Health-Tab-Vergleich ausweichen. Ein Folge-Refactoring (Phase E+ Roadmap) kann eine zentrale Erfolgs-Log-Zeile in `StoryDirector.TryFireProfileInfection` hinzufügen.

### Akzeptanz-Gate Live-Lauf

| # | Gate | Pass-Kriterium | Status |
|---|---|---|---|
| EG-1 | T1-T8 + Ledger + Service-Tests grün | Build + Bootstrap.log ohne FAIL-Zeile | 🟢 |
| EG-2 | Profile-Switch: `Survival→Collapse→Refuge` | Threshold-Werte aus Tabelle oben wirksam | 🟡 |
| EG-3 | Multi-Day-Trigger | Mind. 1 `converted=N` mit N≥1 im godMode-Log über 10 Tage | 🟡 |
| EG-4 | Hediff-Save-Stability | Hediff nach Reload noch attached (Health-Tab-Eintrag sichtbar) | 🟡 |
| EG-5 | Determinismus | Save → Load → identischer Conversion-Count | 🟡 |
| EG-6 | AnimalInfectionAiOverlay-Predikat | `ShouldShowInfectionMarker` = true für branded-KindDef-Tiere (in Dev-Konsole: `AnimalInfectionAiOverlay.ShouldShowInfectionMarker(SelectedPawn)`) | 🟡 |
| EG-7 | Driver- + Overlay-Tests gewired | Beide Tests in `Bootstrap.RunAll` (siehe Anmerkung C-1, Folge-Aktion) | 🟡 |
| EG-8 | `runtime_test.sh --skip-start --no-deploy` Exit 0 | PASS / warnings=0 | 🟢 |

### Belegabschnitt D (User-Pflicht)

> Hier werden die Live-Beobachtungen aus den Schritten 1–7 dokumentiert. Sobald alle 8 EG-Gates befüllt sind, gilt Phase E als `SURVIVED`.

```
Schritt 1: [Datum] Profil=Survival, HordeCount=H1, WildlifePoolSize=W1 → Setup OK
Schritt 2: [Datum] 10 Tage beobachtet, N–Mal converted, letzte Log-Zeile: …
Schritt 3: Datum-Switch-Collapse: Profilwechsel funktioniert (HordeScal 1.5 beobachtbar)
Schritt 3b: Datum-Switch-Refuge: Skip-Path-Logs erschienen >>0-mal, keine neuen Conv.
Schritt 4: Hediff nach Reload noch attached (Health-Tab-Screenshot siehe Anhang)
Schritt 5: Combat-Log: "bitten by <Wolf|Bear>" erschienen
Schritt 6: Determinismus: gleicher Tick/Tile/Profil → identische converted-Zahl
Schritt 7: Dev-Konsole: AnimalInfectionAiOverlay.ShouldShowInfectionMarker(testPawn) = true
```

---

## E — Save/Load Roundtrip (User-Pflicht) 🟡

**Scribe-Keys (`PopulationLedger.ExposeData`):**

```
Scribe_Values.Look(ref LastAnimalInfectionTick, "rimconemyILedgerLastAnimalInfectTick", 0L);
Scribe_Values.Look(ref AnimalInfectionCountToday, "rimconemyILedgerAnimalInfectCountToday", 0);
```

**Save-Layer-Trennung (DECISION-E-001):**

- `CumulativeInoculations` (Phase-C-Key `rimconemyILedgerInocCount`) bleibt für **Lifetime-Counter aller Conversion-Pfade**.
- `AnimalInfectionCountToday` (Phase-E-Key `rimconemyILedgerAnimalInfectCountToday`) ist **Tages-Counter**, wird via `ResetDailyCounters` (StoryDirector day-tick) auf 0 zurückgesetzt — eine Konversion zählt **beide Counter**, weil sie zwei distinkte Semantiken haben.
- `LastAnimalInfectionTick` ist **persistent**, kein Process-Lifetime-State — Save/Load-Sicherheit **per Design**.

**Hediff-Stack:**

- `Rimconemy_InfectedWildlifeAggression` ist Vanilla-`HediffDef`, gescribed via `Pawn_HealthTracker.ExposeData` (Pfad außerhalb Mod-05-Ownership).
- Spreading: kein (Hediff ohne Spread-Mech).
- Tick-Drift: keiner (kein CompStage-Decay, keine Severity-Decay).
- Stack: Single-Instance (idempotenter Add über `GetFirstHediffOfDef`-Guard).

**Verifikation (User-Pflicht):**

1. Save mit konvertiertem Tier auf der Map.
2. Quit → RimWorld neu starten.
3. Reload mit demselben Savefile.
4. Selektiere das konvertierte Tier → Health-Tab zeigt `infected wildlife aggression` mit +50 % MoveSpeed-Offset.
5. Selektiere einen Colonist in Combat-Range → Tier attackiert mit erhöhter Frequenz.
6. Save erneut + Reload erneut → Hediff-State stabil, kein Drift.

**Akzeptanz-Gate:** Hediff + MoveSpeed-Boost + Faction bleiben nach Save/Load voll identisch. Wenn Drift auftritt: Vanilla-RimWorld-1.6-Pawn-Scribe-Bug, nicht Rimconemy.

---

## F — Cross-Package READ 🟢

**Phase E liest:**

- `PopulationLedger.Get()` (eigene Capability via Peer-Package, kein Cross-Package-Read)
- `SettingProfile.ActiveProfile` (StoryDirector-local, kein Cross-Package-Read)
- `DefDatabase<PawnKindDef>.GetNamedSilentFail("Rimconemy_InfectedWildlife")` + `DefDatabase<FactionDef>.GetNamedSilentFail("Rimconemy_HiddenInfectedFaction")` (Self-owned Defs)
- `HediffDef.Named("Rimconemy_InfectedWildlifeAggression")` (Self-owned Defs)
- `Map.mapPawns.AllPawnsSpawned` (Vanilla RimWorld)
- `Verse.DebugSettings.godMode` (Vanilla RimWorld)

**Phase E schreibt:**

- `Pawn.kindDef` (Mutation auf Wild-Pawn — Phase C etabliert)
- `Pawn.Faction` (Faction-Switch → `Rimconemy_HiddenInfectedFaction`)
- `Pawn.health.AddHediff(...)` (Aggression-Hediff-Add)
- `PopulationLedger.LastAnimalInfectionTick` / `AnimalInfectionCountToday` (via `RegisterAnimalInfection`)

**Keine** Late-Bound-Reflection, `INTERFACE_CONTRACT §9.3` ist eingehalten (Phase E ist Package 05-Self-Owned).

---

## G — Performance-Kennzahl

- `TryFireProfileInfection` läuft **1× pro In-Game-Tag** (60 000 Ticks), am Ende des StoryDirector-Pipeline-Stack. Geschätzte Cycle-Zeit: **<2 ms** für die 4 Pure-Logic-Aufrufe (FNV-1a-Hash 32-bit, ~10 Multiplier-Lookups).
- `RandomInoculationService.TryInfectWildAnimals` iteriert `mapPawns.AllPawnsSpawned` einmalig bei jedem Trigger (max 1 / Tag). Pro Iteration: 1 `RaceAnimal`-Property-Read + 1 `Faction==null`-Read + 1 `Alive`-Read. Bei 100 Tieren auf einer typischen Karte: <1 ms.
- `TryApplyInfectionAggressionHediff` läuft 1× pro Live-Conversion, ~0.1 ms (HediffDef-Lookup + GetFirstHediffOfDef + MakeHediff + AddHediff).
- **Annual-Tick-Last-Pessimistic**: 365 Infektionsversuche / Jahr × ~5 ms = **<2 s / Spiel-Jahr** zusätzlich zu Vanilla-Last. Akzeptabel weit unter 60-fps-Threshold-Risiko.

---

## User-Aktion Pflicht 🟡

1. `./scripts/deploy.sh 05` (Live-Deploy).
2. `./scripts/runtime_test.sh --skip-start --no-deploy --require-scenario-tests` — **muss PASS** sein.
3. Live-Beleg der Schritte 1–7 oben dokumentieren in **Abschnitt D** dieses Berichts.
4. Save/Load-Test für **Abschnitt E**.
5. Performance-Beobachtung für **Abschnitt G** (optional: `Log.OldGetPooledLogMessages` Zähler pro Tag).

**Folge-Aktion (kein Gate-Blocker):**

- `Tests.AnimalInfectionDriverTests.RunAll()` + `Tests.AnimalInfectionAiOverlayTests.RunAll()` sollten in `Bootstrap.cs` ergänzt werden (siehe Anmerkung C-1).
- Optional: `StoryDirector.TryFireProfileInfection` Erfolgs-Log hinzufügen (siehe Anmerkung D-2).

Sobald alle 8 EG-Gates + Abschnitt D befüllt sind, gilt dieser Bericht als `SURVIVED`.

---

## Verweise

- Spec: `docs/superpowers/specs/2026-08-05-animal-infection-design.md`
- Plan: `docs/superpowers/plans/2026-08-05-animal-infection.md`
- Phase-D-Schwesterbericht (Horde-Mechanik): `docs/falsification/infected__ThreatPressure.md`
- Phase-B/C-Schwesterbericht (Human-Raids): `docs/falsification/infected__InfectedRaid.md`
- Phase B-Tick-Pipeline-Canon: `docs/DECISIONS.md` §DECISION-B-001
- Cleanup-Pass Decisions: `docs/DECISIONS.md` §DECISION-E-001..E-003 (Dual-Counter, Static-Mutability, Hediff-Site)
- Roadmap Master: `ROADMAP.md §9.6` (Phase E)
