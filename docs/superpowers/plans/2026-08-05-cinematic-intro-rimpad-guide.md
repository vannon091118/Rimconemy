# Implementation Plan: Cinematic Intro + RimPad + Storyteller-Guide

**Spec**: `docs/superpowers/specs/2026-08-05-cinematic-intro-rimpad-guide.md`
**Status**: READY FOR IMPLEMENTATION
**API-Verifiziert**: RimWorld 1.6.4566 + alle DLCs

---

## Task Breakdown

| Task | Beschreibung | Files | Dependencies | Est. Zeit |
|------|--------------|-------|--------------|-----------|
| **1** | IntroFlowWindow + ScenPart_IntroSequence + Kamera-Cuts + Horde-Flash | `IntroFlowWindow.cs`, `ScenPart_IntroSequence.cs` | RimconemyWindow, CameraDriver, PawnGenerator | 2-3h |
| **2** | RimconemyTutorialLetter + Dialog_TutorialStep (Portrait-Support) | `RimconemyTutorialLetter.cs`, `Dialog_TutorialStep.cs` | Letter, Texture2D, ContentFinder | 1-2h |
| **3** | TutorialDirector + TutorialState + TutorialStepDef XML | `TutorialDirector.cs`, `TutorialState.cs`, `TutorialSteps.xml` | GameComponent, CapabilityAudit, LetterStack | 2-3h |
| **4** | RimPadWindow + TabSystem + MainButtonDef + KeyBindingDef | `RimPadWindow.cs`, `MainButtonDefs/RimPadButton.xml`, `KeyBindings.xml` | RimconemyWindow, TabDrawer, RimconemyTheme | 2-3h |
| **5** | Weather_StormAtmosphere + Scenario Updates | `Weather_StormAtmosphere.xml`, `SingleSurvivor.xml` | WeatherDef, ScenarioDef | 30min |
| **6** | Integration Testing + Runtime Gate | `runtime_test.sh` run, regression tests | All above | 1h |

---

## Task 1: IntroFlowWindow + ScenPart_IntroSequence (START HERE)

### Files to Create/Modify

```
mods/01-Rimconemy-Foundation/Source/UI/IntroFlowWindow.cs          ← NEW
mods/05-Rimconemy-Infected-Automation/Source/Scenarios/ScenPart_IntroSequence.cs  ← NEW
mods/05-Rimconemy-Infected-Automation/Defs/Scenarios/IntroSequencePart.xml        ← NEW
```

### Step-by-Step

#### 1.1 IntroFlowWindow.cs erstellen
- Erbt von `Rimconemy.Foundation.UI.RimconemyWindow`
- Properties: `cameraCutPositions`, `flowTexts` (5 Blöcke), Timer-Logic
- `PreOpen()`: `Current.Game.Paused = true` (ForcePause Workaround)
- `DoWindowContents()`:
  - Black background + zentrierter Flow-Text
  - Timer: Block-Wechsel alle 180 Ticks
  - Kamera-Cuts: `Find.CameraDriver.JumpToCurrentMapLoc(pos)` alle 240 Ticks
  - Horde-Flash nach Block 3: Spawn 5× InfectedRavager an Kartenrändern
  - 180 Ticks warten → `pawn.Destroy(DestroyMode.Vanish)`
  - Auto-Close nach letztem Block + 120 Ticks Puffer
- `PostClose()`: Cleanup + `TutorialDirector.Get()?.StartGuide()`

#### 1.2 ScenPart_IntroSequence.cs erstellen
- Erbt von `ScenPart`
- `PostMapGenerate(Map map)`:
  - Generiere Kamera-Cut Positionen (Zentrum, Ruinen, Kartenränder)
  - `Find.WindowStack.Add(new IntroFlowWindow { cameraCutPositions = cuts })`

#### 1.3 IntroSequencePart.xml erstellen
```xml
<ScenPartDef>
  <defName>ScenPart_IntroSequence</defName>
  <class>Rimconemy.InfectedAutomation.Scenarios.ScenPart_IntroSequence</class>
</ScenPartDef>
```

#### 1.4 SingleSurvivor.xml erweitern
- Füge `<li Class="ScenPart_IntroSequence" />` zu scenarioParts hinzu
- Füge `<li>Rimconemy_StormAtmosphere</li>` zu permaGameConditions hinzu

### Tests
- `IntroFlowWindowTests`: Timing, Kamera-Cuts, Horde Spawn/Despawn, Auto-Close
- Manuell: Neues Spiel mit SingleSurvivor Szenario → Intro spielt ab

---

## Task 2: RimconemyTutorialLetter + Dialog_TutorialStep

### Files
```
mods/05-Rimconemy-Infected-Automation/Source/Story/RimconemyTutorialLetter.cs  ← NEW
mods/05-Rimconemy-Infected-Automation/Source/Story/Dialog_TutorialStep.cs      ← NEW
```

### Step-by-Step

#### 2.1 RimconemyTutorialLetter.cs
- Erbt von `Letter`
- Felder: `Texture2D Portrait`, `string StepId`, `List<Def> UnlockDefs`
- `ExposeData()`: Serialisierung aller Felder
- `OpenLetter()`: Öffnet `Dialog_TutorialStep(this)`

#### 2.2 Dialog_TutorialStep.cs
- Erbt von `Window`
- `DoWindowContents()`: Portrait links (128×128), Text rechts, Unlock-Liste, "VERSTANDEN" Button
- Button → `TutorialDirector.Get()?.MarkStepCompleted(letter.StepId); Close()`

### Tests
- Letter mit Portrait wird korrekt gerendert
- Unlock-Vorschau zeigt Defs an
- Button schließt Dialog und markiert Step als done

---

## Task 3: TutorialDirector + TutorialState + TutorialStepDef XML

### Files
```
mods/05-Rimconemy-Infected-Automation/Source/Story/TutorialDirector.cs          ← NEW
mods/05-Rimconemy-Infected-Automation/Source/Story/TutorialState.cs             ← NEW
mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/TutorialSteps.xml      ← NEW
mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/TutorialStepDef.xml    ← NEW (DefOf)
```

### Step-by-Step

#### 3.1 TutorialState.cs
- Implementiert `IExposable` + `ISchemaMigratable`
- Felder: `HashSet<string> CompletedSteps`, `int CurrentStepIndex`, `bool Dismissed`, `int SchemaVersion`

#### 3.2 TutorialStepDef.cs (DefOf)
```csharp
[DefOf]
public class TutorialStepDef : Def
{
    public int priority;
    public string trigger;           // "GameStart", "CampfireBuilt", "FirstInfectedContact", "WallBuilt", "ResourceCollected_ThingDefName"
    public string letterLabel;
    public string letterText;
    public string portraitTexture;   // Pfad für ContentFinder
    public List<Def> unlockDefs;
    public List<string> prerequisiteSteps;
}
```

#### 3.3 TutorialSteps.xml
- 4 Steps: Welcome → Campfire → FirstContact → Wall
- Je: trigger, label, text, portrait, unlockDefs, prerequisites

#### 3.4 TutorialDirector.cs
- Erbt von `GameComponent`
- `StartedNewGame()`: State init, Steps laden, Trigger registrieren
- `RegisterTriggers()`: Via `CapabilityAudit.GetCapability<ITutorialTriggerBridge>()`
- `TryTriggerStep(triggerId)`: Prerequisites prüfen, Step anzeigen
- `ShowStep(step)`: Portrait laden, `RimconemyTutorialLetter` erstellen, `LetterStack.ReceiveLetter`
- `MarkStepCompleted(stepId)`: State aktualisieren
- `DrawGuideContent(rect)`: Für RimPad Tab — zeigt alle Steps mit Status

### Tests
- Trigger feuern korrekt
- Prerequisites blockieren korrekt
- Save/Load persistiert CompletedSteps
- RimPad Tab zeigt korrekten Status

---

## Task 4: RimPadWindow + TabSystem

### Files
```
mods/01-Rimconemy-Foundation/Source/UI/RimPadWindow.cs                    ← NEW
mods/01-Rimconemy-Foundation/Defs/MainButtonDefs/RimPadButton.xml         ← NEW
mods/01-Rimconemy-Foundation/Defs/KeyBindingDefs/KeyBindings.xml          ← MODIFY (add)
mods/01-Rimconemy-Foundation/Source/UI/RimconemyTheme.cs                  ← MODIFY (Tablet style)
```

### Step-by-Step

#### 4.1 RimPadWindow.cs
- Erbt von `RimconemyWindow`
- `tabs`: List<TabRecord> für [Guide, Threat, Phase, Economy, Settings]
- `DoWindowContents()`:
  - Header: `RimconemyTheme.DrawPanelHeader`
  - TabButtons: `TabDrawer.DrawTabs`
  - Content: ScrollView + `DrawCurrentTabContent`
  - Notification Badge auf Guide-Tab wenn `TutorialDirector.HasUnreadNotifications`
- `InitialSize`: 520×680

#### 4.2 RimPadButton.xml
```xml
<MainButtonDef>
  <defName>Rimconemy_RimPad</defName>
  <label>RimPad</label>
  <icon>UI/Icons/RimPad</icon>
  <tabWindowClass>Rimconemy.Foundation.UI.RimPadWindow</tabWindowClass>
  <groupKey>Rimconemy</groupKey>
  <order>100</order>
</MainButtonDef>
```

#### 4.3 KeyBindings.xml (hinzufügen)
```xml
<KeyBindingDef>
  <defName>Rimconemy_ToggleRimPad</defName>
  <label>RimPad anzeigen/verstecken</label>
  <defaultKeyCode>T</defaultKeyCode>
  <modifier>Control</modifier>
</KeyBindingDef>
```

#### 4.4 RimconemyTheme.cs erweitern
- `DrawPanelHeader(Rect, string)` für Tablet-Look
- Monospace Font für "Terminal" Feel
- Panel-Texturen (optional)

### Tests
- Toolbar-Button öffnet RimPad
- Strg+T toggelt RimPad
- Tabs switchen korrekt
- Badge erscheint bei unread Tutorial
- Position persistiert über Save/Load

---

## Task 5: Weather + Scenario

### Files
```
mods/05-Rimconemy-Infected-Automation/Defs/WeatherDefs/Weather_StormAtmosphere.xml  ← NEW
mods/02-Rimconemy-Survival-Progression/Defs/Scenarios/SingleSurvivor.xml             ← MODIFY
```

### Step-by-Step
- Weather_StormAtmosphere.xml per Spec (§4.6)
- SingleSurvivor.xml: IntroSequence ScenPart + StormAtmosphere Condition hinzufügen

---

## Task 6: Integration Testing

### Checklist
- [ ] `./scripts/deploy.sh` — Build + Deploy aller 5 Pakete
- [ ] `./scripts/runtime_test.sh` — Runtime Gate PASS
- [ ] Neues Spiel: SingleSurvivor → Intro spielt ab → RimPad Notification → Guide startet
- [ ] RimPad öffnet via Toolbar + Strg+T
- [ ] Tutorial-Steps feuern bei Campfire/Wall/Infected-Contact
- [ ] Save/Load: Tutorial-Status persistiert
- [ ] Regression-Summaries in Player.log: 35+ PASS

---

## Current Blocker Status (aus Player.log)

| Blocker | Status | Fix |
|---------|--------|-----|
| `Harmony PatchAll failed: BioRemap skipped` | OPEN | `Page_ConfigureStartingPawnsBioPatch.cs` prüfen |
| `ScenPart_RimconemyStartEnemies.PostMapGenerate: NullReferenceException` | OPEN | `CalculateStarterCount` null-check |

**Diese müssen VOR Task 1 gefixt werden**, sonst crasht das Spiel bei Intro-Start.

---

## Start Order

1. **FIX CRASHES** (BioRemap Harmony + ScenPart NRE)
2. **Task 1** → IntroFlowWindow + ScenPart_IntroSequence
3. **Task 2** → TutorialLetter + Dialog
4. **Task 3** → TutorialDirector + Steps
5. **Task 4** → RimPadWindow
6. **Task 5** → Weather + Scenario
7. **Task 6** → Full Integration Test

---

**Ready to start with Crash Fixes + Task 1?**