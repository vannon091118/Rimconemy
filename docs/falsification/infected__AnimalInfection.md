# Falsification §G — Animal-Infection via Random Encounter (Live-Beleg)

**Datum:** 2026-08-05
**Spec:** `docs/superpowers/specs/2026-08-05-animal-infection-design.md`
**Plan:** `docs/superpowers/plans/2026-08-05-animal-infection.md`

## Zweck

Phase E verifiziert dass der AnimalInfection-Driver aus dem StoryDirector
Day-Tick heraus zündet, Wildtiere per Faction-Switch konvertiert, den
Aggression-Hediff appliziert, und sichtbar im Player.log auftaucht.

## Schritt-für-Schritt (User-Pflicht: manuell in RimWorld)

### Schritt 1 — Setup
- Survival-Profil starten, neue Kolonie gründen.
- Dev-Mode an (`!`), Wildnis erkunden bis mind. 5 wild lebende Tiere
  im Radar sichtbar sind (Wolf, Mufflon, Karibu, Bär etc.).
- Aktuelle Kolonie-Größe notieren (`p` → Colony Summary).
- Profile-Anzeige in ThreatDashboard prüfen: 'Survival'.

### Schritt 2 — Trigger-Simulation (manuell)
- `StoryDirector.EvaluateNow(currentTick: Find.TickManager.TicksGame)`
  in Dev-Konsole eingeben. Damit wird der Day-Tick sofort gefeuert.
- Alternativ: 1 In-Game-Tag warten (60 000 Ticks) ohne direkten Trigger.

### Schritt 3 — Log-Beobachtung (A/B-Vergleich)

Erwartung im Player.log (alle 3 Zeilen müssen erscheinen):

```
[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals: requested=N cap=K converted=N tick=T
[Rimconemy.InfectedAutomation] AnimalInfectionDriver: N wild animals infected at tick=T profile=Rimconemy_Survival hordeCount=H
[Rimconemy.InfectedAutomation] Infection Hediff applied: Rimconemy_InfectedWildlifeAnggression to kinddef=Rimconemy_InfectedWildlife
```

Falls Zeile 3 fehlt → Hediff-Apply-Pfad ist broken.
Falls Zeile 2 fehlt → Driver feuerte nicht (gating-Logik fehlerhaft).
Falls Zeile 1 mit `converted=0` → Service fand keine Candidates (Map leer,
Filter rejects). Force-Spawn wolves via DevTools.

### Schritt 4 — In-Game-Visual-Check

- Mindestens 1 Tier zeigt sichtbar einen roten "!" Marker (Phase E T8
  AnimalInfectionAiOverlay).
- Health-Tab des Tiers zeigt Label "infected wildlife aggression" in
  roter Farbe.
- Tier bewegt sich **schneller** als unbefallene Tiere derselben Spezies
  (sichtbar bei Wander-Pfaden; ~50 % MoveSpeed-Boost durch Hediff).

### Schritt 5 — Combat-Log-Verifikation

- gezielt: Angriffe des infizierten Tiers auf einen Colonist oder Tamed
  Animal sollten Combat-Log-Einträge produzieren.
- Erwartung:

```
Colonist X was bitten by Wolf
```

Wenn Aggression-AI keine AttackMelee-Jobs erzwingt: Combat-Log zeigt
eher passive Wander-Movements (nicht das gewünschte Verhalten). Dann
ist der opt-in-Patch aus Phase E T7 nicht aktiv. **Hinweis:** der
aggressive-AI-Override (AttackMelee auf Colonist) wurde in T7 bewusst
auf die Hediff-basierte Speed-Boost gelöst; eine echte
JobDriver-Override-Harmony kann in Phase E+ nachgerüstet werden.

### Schritt 6 — Determinus-Verifikation

- Save → Quit → Reload mit demselben Tag + derselben HordeCap.
- Erwartung: identische Conversion-Anzahl (Luck-T0-Vergleich ergibt
  denselben Wert).

### Schritt 7 — Profile-Switching-Test

- Profile auf Collapse wechseln (Difficulty ändern → re-evaluate).
- Erwartung: höhere Conversion-Rate (BaseChance 0.15 statt 0.05,
  HardCap 0.95).
- Profile auf Refuge wechseln (~0.02 Chance, 0 InoculationsPerDay).
- Erwartung: NOTICE: TryInfectWildAnimals wird durch `profileQuota==0`
  übersprungen — keine neuen Infektionen.

## Acceptance Gates

| Gate | Pass-Criterion |
|---|---|
| EG-1 | `[Rimconemy...] RandomInoculationService.TryInfectWildAnimals: requested=N cap=K converted=N` im Player.log |
| EG-2 | `[Rimconemy...] AnimalInfectionDriver: N wild animals infected at tick=…` |
| EG-3 | Selected Pawn hat HediffSet mit "infected wildlife" Eintrag |
| EG-4 | Combat-Log zeigt Angriffe auf Colonist/Tamed |
| EG-5 | Save → Load → identischer Wert (Determinismus) |
| EG-6 | Profile-Wechsel verändert Rate spürbar |

## Regression-Coverage (Pre-Live-Tests)

- T1-T8 (AnimalInfectionChance): Pure-Chance + Profile-Multipliers
- T9-T13 (AnimalInfectionDriver): Driver-Logik
- T14-T15 (Service Limit): cap & null-guards
- T16-T19 (Overlay): Marker-Predikat

Build: 0 warnings, 0 errors. runtime_test PASS seit Commit
`9d83b87`.

## Known Limitations

- Phase E T7 hat **kein Harmony-Patch** auf `Pawn_JobTracker.StartJob`
  implementiert. Stattdessen wird der Aggression-Hediff mit +50 %
  MoveSpeed appliziert; das macht das Tier spürbar schneller und
  gefährlicher, aber es bleibt auf Vanilla Animal-AI (passives
  Wander) sitzen. Eine echte "aggressive animals attack colonists on
  sight" AI-Override kann als Phase E+ mit eigenem Harmony-Patch
  nachgerüstet werden.
- Die Rote-Marker-Visualisierung wird vom AnimalInfectionAiOverlay
  vorgemerkt, aber noch nicht voll an die UIRoot-OnGUI-Postfix-Pattern
  angeflanscht. Der Predikat (`ShouldShowInfectionMarker`) ist fertig;
  ein zukünftiger RenderHook kann die `MarkerTexture`-Property lesen
  und über `GUI.DrawTexture` zeichnen.

## Siehe auch

- `docs/superpowers/specs/2026-08-05-animal-infection-design.md`
- `docs/superpowers/plans/2026-08-05-animal-infection.md`
- Spec §5 (Tests-Liste T1-T19) und §8 (Falsification)
