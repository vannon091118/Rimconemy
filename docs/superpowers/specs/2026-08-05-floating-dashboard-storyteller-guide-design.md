# Spec: Floating Dashboard (RimPad) + Storyteller-Guide Tutorial

**Datum:** 2026-08-05  
**Status:** Draft → Review → Implementation  
**Projekt:** Rimconemy (5-Paket RimWorld 1.6 Mod-Suite)  
**Ziel:** Spieler-orientierte Haupt-UI (RimPad) + Storyteller-Guide als Tutorial-System

---

## 1. Ziel & Scope

**Was wir bauen:**
- **RimPad** – verschiebbares, Tablet/Pip-Boy-stylisches Floating-Dashboard als **Haupt-Spieler-UI** (nicht Debug). Vereint alle 5 Pakete in Sektionen.
- **Storyteller-Guide** – simulierter Guide (kein eigener StorytellerDef), der via Letter-Pipeline Tutorial-Schritte als Popup-Dialoge mit Portrait anzeigt, Funktionen kontextbezogen freischaltet und erklärt.
- **Intro-Sequenz** – Black Screen → Flow-Text → Kamera-Cuts → 3-Sek-Zombie-Horde-Flash → RimPad-Notification startet Guide.
- **Start-Szenario** – Endzeit-Story (ISS-Rückkehr, Zombie-Horde-Flash), Gewitter-Kulisse (Optik only), Infizierte Garantie ≥1.

**Was NICHT im Scope:**
- Neuer StorytellerDef / StorytellerComp (Vanilla bleibt autoritativ, DECISIONS §34).
- Debug-Tabs ersetzen (bleiben für Entwicklung erhalten).
- Komplexe Quest-/Story-Engine – nur Tutorial-Schritte (Guide).

---

## 2. Benutzer-Sicht (User Journey)

1. **Neues Spiel starten** → „Rimconemy – The Last Survivor“ wählen.
2. **Intro-Sequenz:**
   - Black Screen + Flow-Text (ISS-Rückkehr nach 5 Jahren, Freude auf Familie).
   - Kamera-Cuts über generierte Karte (Landezone, Ruinen, Kartenrand).
   - **Zombie-Horde-Flash:** 3 Sek. Spawn am Kartenrand → Kamera rahmt → Despawn.
3. **Gameplay-Start:** RimPad-Notification-Button pulsiert → Klick → **Guide-Popup #1** („Tag 0 – Die Ankunft“).
4. **Guide führt durch erste Schritte** (Popup mit Portrait + Text + Dismiss-Button):
   - Step 1: „Du bist wach“ (Story-Einstieg).
   - Step 2: „Schutz suchen“ → Trigger: erste Wand/Barrikade gebaut.
   - Step 3: „Erstes Feuer“ → Trigger: Campfire entzündet.
   - Step 4: „Sie kommen“ → Trigger: erster Infizierten-Kontakt (Sicht).
   - Step 5: „Vorräte“ → Trigger: erste Ressource gesammelt.
5. **Bei jedem Step:** Popup erklärt kontextbezogen, was freigeschaltet wurde (Architect-Menü, Rezept, UI-Element).
6. **RimPad** ist ab jetzt zentrale Anlaufstelle: Sektionen **Überleben / Infrastruktur / Wirtschaft / Bedrohung / Diagnose**, verschiebbar, Position persistiert, Hotkey `Strg+T` + Toolbar-Button.
6. **Guide abschaltbar** in Mod-Einstellungen.

---

## 3. Technische Architektur (RimWorld 1.6 + DLCs)

### 3.1 Paket-Zuordnung

| Komponente | Paket | Begründung |
|---|---|---|
| `RimPadWindow`, `RimPadTheme`, `RimPadTabDrawer` | **01 Foundation** | Zentrale UI-Basis, wird von allen genutzt |
| `RimPadButtonDef` (MainButtonDef) | **01 Foundation** | Toolbar-Button für RimPad |
| `IntroFlowWindow`, `ScenPart_IntroSequence` | **05 Infected** | Start-Szenario gehört zu Infected (Story/Intro) |
| `TutorialDirector` (GameComponent), `TutorialStepDef`, `TutorialState` | **05 Infected** | Nutzt StoryDirector-Pipeline (Letter/Idempotenz) |
| `TutorialStepDefs` (Steps 1–5) | **05 Infected** | Definiert Steps, Trigger, Unlocks, Portrait |
| `TutorialTriggerBridge` (Callbacks) | **05 Infected** | Registriert Paket-02/03/04 Trigger via Foundation |
| Paket-02 Trigger (`OnCampfireBuilt`, `OnFirstInfectedContact`, `OnResourceCollected`, `OnWallBuilt`) | **02 Survival** | Registriert sich bei Foundation Bridge |
| Paket-03 Trigger (`OnGeneratorBuilt`, `OnTurretBuilt`) | **03 Scavenger** | Registriert sich bei Foundation Bridge |
| Paket-04 Trigger (`OnOutpostFounded`, `OnTradeDone`) | **04 Economy** | Registriert sich bei Foundation Bridge |
| `RimPadWindow`, `RimPadTabDrawer`, `RimPadTheme` | **01 Foundation** | Zentrale UI, von allen Paketen befüllt |
| `RimPadButtonDef` + Hotkey `Strg+T` | **01 Foundation** | Ein-/Ausblenden |

**Compile-Isolation:** Keine direkten DLL-Refs zwischen Paketen. Kommunikation **nur** über Foundation `CapabilityAudit` / `EventBus` / `Bridge`-Callbacks.

---

## 4. Detaillierte Komponenten

### 4.1 Intro-Sequenz (`IntroFlowWindow` + `ScenPart_IntroSequence`)

**Dateien:**
- `mods/05-Rimconemy-Infected-Automation/Source/UI/IntroFlowWindow.cs`
- `mods/05-Rimconemy-Infected-Automation/Source/Scenarios/ScenPart_IntroSequence.cs`
- `mods/05-Rimconemy-Infected-Automation/Defs/Scenarios/IntroSequencePart.xml`

**Ablauf (in `PostMapGenerate`):**
1. `Find.WindowStack.Add(new IntroFlowWindow())` → `ForcePause = true`, `PreventCameraMotion = true`.
2. **Flow-Text:** Array von Text-Blöcken, Timer (Ticks) → nächster Block.
3. **Kamera-Cuts:** Alle 200–300 Ticks `Find.CameraDriver.JumpToCurrentMapLoc(predefinedCells)` → Landezone, Ruinen, Kartenrand.
3. **Zombie-Horde-Flash (3 Sek.):**
   - `PawnGenerator.GeneratePawn(InfectedRavager, HiddenInfectedFaction)` × 4 an `CellFinder.RandomEdgeCell`.
   - `Find.CameraDriver.JumpToCurrentMapLoc(firstPawn.Position)`.
   - `Hediff` „VisualOnly“ (kein AI, keine Jobs) oder `mindState.duty = null`.
   - Nach 180 Ticks: `pawn.Destroy(DestroyMode.Vanish)` für alle.
4. **Ende:** `Find.WindowStack.TryRemove(IntroFlowWindow)` → `TutorialDirector.NotifyIntroCompleted()`.

**API-Verifikation (Spike):**
- `CameraDriver.JumpToCurrentMapLoc(IntVec3)` ✅
- `CameraDriver.SetRootPosAndSize(Vector3, float)` ✅
- `PawnGenerator.GeneratePawn(PawnKindDef, Faction)` ✅
- `pawn.Destroy(DestroyMode.Vanish)` ✅
- `Window` mit `ForcePause`, `PreventCameraMotion` ✅

### 4.2 Zombie-Horde-Flash (Detail)

```csharp
// In IntroFlowWindow nach Flow-Text + Kamera-Cuts
var faction = InfectedFactionUtility.EnsureHiddenInfectedFaction();
var kind = DefDatabase<PawnKindDef>.GetNamed("Rimconemy_InfectedRavager");
var pawns = new List<Pawn>();
for (int i = 0; i < 4; i++) {
    var cell = CellFinder.RandomEdgeCell(map);
    var pawn = PawnGenerator.GeneratePawn(kind, faction);
    // No AI: remove duty
    pawn.mindState.duty = null;
    GenSpawn.Spawn(pawn, cell, map);
    pawns.Add(pawn);
}
Find.CameraDriver.JumpToCurrentMapLoc(pawns[0].Position);
// Schedule despawn after 180 ticks
LongEventHandler.ExecuteWhenFinished(() => {
    // using a simple tick callback via GameComponent or lambda with tick check
});
```

**Kein AI:** `mindState.duty = null` reicht; Pawns stehen nur rum, keine Pfadfindung.

### 4.3 RimPad – Floating Dashboard

**Dateien (Paket 01 Foundation):**
- `Source/UI/RimPadWindow.cs` – erbt `RimconemyWindow` (bzw. `Window`).
- `Source/UI/RimPadTabDrawer.cs` – `TabRecord` Liste, Zeichnen.
- `Source/UI/RimPadTheme.cs` – erweitert `RimconemyTheme` (Tablet/Pip-Boy Style).
- `Source/UI/RimPadTab.cs` – Enum/Record für Sektionen.
- `Defs/MainButtonDefs/RimPadButton.xml` – Toolbar-Button.
- `Defs/KeyBindingDefs/RimPadToggle.xml` – Hotkey `Strg+T`.

**Struktur (Tabs/Sektionen):**
| Tab | Key | Inhalt (Beispiele) |
|---|---|---|
| **Überleben** | `Survival` | Bedürfnisse (Nahrung/Sicherheit/Sozial), XP/Progression, Bedrohung, GameOver-Status |
| **Infrastruktur** | `Infrastructure` | Lagerbestand (StorageSnapshot), Power (PowerChainSnapshot), Gebäude, Rezepte |
| **Wirtschaft** | `Economy` | Credits-Wallet, Markt-Preise, Outposts, Proxy-Status |
| **Bedrohung** | `Threat` | ThreatPressure, Infizierten-Raids, Mechadroids, Horde-Status |
| **Diagnose** | `Diagnostics` | EventLog, Save-Status, Paket-Versionen, Performance |

**Technik:**
- Erbt `RimconemyWindow` (bzw. `Window`) → `windowRect` wird von RimWorld pro Save persistiert.
- `TabDrawer` + `List<TabRecord>` → Standard RimWorld Tab-Umsetzung.
- `RimPadTheme` erweitert `RimconemyTheme`: dunkles Panel, CRT-Shader (falls 1.6 `Shader` unterstützt), monospace Font, abgerundete Ecken.
- **Ein-/Ausblenden:** `Find.WindowStack.Add/Remove` via Toolbar-Button (`MainButtonDef`) + Hotkey `Strg+T` (`KeyBindingDef`).

**API-Verifikation:**
- `Window` mit `windowRect` Auto-Persistenz ✅
- `TabDrawer` + `TabRecord` Standard ✅
- `MainButtonDef` (nicht `MainTabDef`!) ✅
- `KeyBindingDef` ✅

### 4.4 Storyteller-Guide / TutorialDirector

**Dateien (Paket 05 Infected):**
- `Source/Tutorial/TutorialDirector.cs` – `GameComponent`.
- `Source/Tutorial/TutorialStepDef.cs` – `Def`.
- `Source/Tutorial/TutorialState.cs` – `IExposable`, `ISchemaMigratable`.
- `Source/Tutorial/TutorialStep.cs` – Runtime-Record.
- `Source/Tutorial/TutorialTriggerBridge.cs` – Registrierung Paket-Trigger.
- `Defs/TutorialSteps/` – XML-Defs für Steps 1–5.

**TutorialDirector (GameComponent):**
```csharp
public class TutorialDirector : GameComponent {
    public TutorialState State;
    private int currentStepIndex = 0;
    private readonly List<TutorialStepDef> steps = DefDatabase<TutorialStepDef>.AllDefsListForReading.OrderBy(d => d.order).ToList();

    public override void GameComponentTick() {
        if (State.Completed || State.Dismissed) return;
        if (currentStepIndex >= steps.Count) { State.Completed = true; return; }

        var step = steps[currentStepIndex];
        if (step.CheckTrigger()) {
            ShowStep(step);
            currentStepIndex++;
        }
    }

    private void ShowStep(TutorialStepDef step) {
        // Portrait texture
        var portrait = ContentFinder<Texture2D>.Get(step.portraitPath, false);
        // LetterDef with Icon (Portrait)
        var letterDef = new LetterDef {
            defName = "Rimconemy_Tutorial_" + step.defName,
            label = step.label,
            text = step.text,
            icon = portrait,
            // baseLetterDef can be LetterDefOf.PositiveEvent
        };
        // Or use custom Letter with portrait field
        Find.LetterStack.ReceiveLetter(step.label, step.text, LetterDefOf.PositiveEvent, step.lookTargets, null, null, null, null, 0, true);
        // Mark step as shown in State
        State.MarkStepShown(step.defName);
    }

    public void NotifyIntroCompleted() { /* reset currentStepIndex = 0 */ }
}
```

**TutorialStepDef (Def):**
```xml
<TutorialStepDef>
  <defName>Tutorial_Step1_WakeUp</defName>
  <label>Tag 0 – Die Ankunft</label>
  <text>Du bist nach 5 Jahren ISS zurück... Die Welt hat sich verändert. Dein RimPad aktiviert sich.</text>
  <order>1</order>
  <triggerType>OnIntroCompleted</triggerType>
  <portraitPath>UI/HeroArt/Storytellers/RimconemyLarge</portraitPath>
  <unlockDefs /> <!-- optional: Architect-Menu-Items, Rezepte -->
</TutorialStepDef>
```

**Trigger-Typen (C# Enum + Switch in `CheckTrigger`):**
- `OnIntroCompleted` – nach IntroFlowWindow.
- `OnCampfireBuilt` – Paket 02 registriert Callback.
- `OnFirstInfectedContact` – Paket 05 (Sichtkontakt).
- `OnResourceCollected` – Paket 02/03 (StorageSnapshot Änderung).
- `OnWallBuilt` / `OnGeneratorBuilt` / `OnTurretBuilt` / `OnOutpostFounded` / `OnTradeDone` – je Paket.

**Trigger-Registrierung (Foundation Bridge):**
```csharp
// In TutorialTriggerBridge (Paket 05 FinalizeInit)
if (CapabilityAudit.HasCapability("rimconemy.survivalprogression", "trigger.callbacks")) {
    // via Foundation EventBus oder direkter Callback-Registrierung
    SurvivalCallbacks.OnCampfireBuilt += () => tutorialDirector.NotifyTrigger(TriggerType.OnCampfireBuilt);
    // etc.
}
```

**TutorialState (Save/Load):**
```csharp
public class TutorialState : IExposable, ISchemaMigratable {
    public HashSet<string> CompletedSteps = new();
    public HashSet<string> DismissedSteps = new();
    public bool DismissedAll = false;
    public bool Completed = false;

    public void ExposeData() {
        Scribe_Collections.Look(ref CompletedSteps, "completedSteps");
        Scribe_Collections.Look(ref DismissedSteps, "dismissedSteps");
        Scribe_Values.Look(ref DismissedAll, "dismissedAll");
        Scribe_Values.Look(ref Completed, "completed");
    }
    // ISchemaMigratable implementation...
}
```

**Dismiss / Abschaltbar:**
- Jeder Popup: Button „Verstanden“ (schließt Schritt) + „Nicht mehr anzeigen“ (setzt `DismissedAll = true`).
- Mod-Einstellung `Rimconemy_GuideEnabled` (Boolean) → `TutorialDirector` prüft bei Start.

**Portrait im Letter:**
- `LetterDef` mit `Icon` (Texture2D) → `ContentFinder<Texture2D>.Get("UI/HeroArt/Storytellers/RimconemyLarge")` ✅
- `Find.LetterStack.ReceiveLetter(label, text, letterDef, lookTargets, ...)` → zeigt Icon/Portrait.

### 4.5 Intro-Sequenz & Start-Szenario

**ScenarioDef (`SingleSurvivor.xml` Anpassungen):**
- `<label>Rimconemy – The Last Survivor</label>` (ohne `_`).
- `<description>` → Endzeit-Story (ISS, 5 Jahre, Familie, Zombie-Horde).
- Neuer Part: `<li Class="Rimconemy.InfectedAutomation.Scenarios.ScenPart_IntroSequence">`.

**Gewitter-Kulisse (Optik only):**
```xml
<!-- WeatherDef: Rimconemy_StormAtmosphere -->
<WeatherDef>
  <defName>Rimconemy_StormAtmosphere</defName>
  <rainRate>0.8</rainRate>
  <windSpeedFactor>1.5</windSpeedFactor>
  <skyColorsDay>...</skyColorsDay>
  <eventMakers />  <!-- LEER = keine Blitze -->
  <workerClass>Verse.WeatherWorker</workerClass>
</WeatherDef>
```
- **Kein `lightningBias` Feld in 1.6** → `eventMakers` leer lassen = keine `LightningFlash` EventMaker = keine Blitze.
- **ScenPart_GameCondition** im Scenario für Start-Wetter + Dämmerung (falls `GameCondition_ForcedWeather` existiert, sonst nur WeatherDef als Standard).

**Infizierte Garantie ≥1:**
- `ScenPart_RimconemyStartEnemies.CalculateStarterCount` → `Math.Max(1, count)` + Fallback-Spawn am Map-Center (`CellFinder.RandomCell` / `map.Center`) wenn `SpawnStarterInfected` 0 liefert.
- Regressionstest in Paket 05 Tests: `CalculateStarterCount` für alle Difficulty/Map-Size ≥ 1.

### 4.6 RimPad UI – Tablet/Pip-Boy Style

**RimPadTheme (erweitert RimconemyTheme):**
- **Farben:** Dunkles Grau (#1A1A1A), Akzent Orange/Amber (#FF8C00), Text Hellgrau (#E0E0E0), Warnung Rot (#CC3333).
- **Panel:** Abgerundete Ecken (8px), leichter Innen-Schatten, CRT-Scanlines-Overlay (optional, via `GUI.DrawTexture` mit Alpha).
- **Font:** Monospace (z. B. `Consolas`/`Monospace` via `Text.Font = GameFont.Mono`).
- **Tabs:** Oben horizontale Leiste, aktive Tab hervorgehoben (Akzent-Farbe), Icons optional.

**RimPadWindow:**
```csharp
public class RimPadWindow : RimconemyWindow {
    public override Vector2 InitialSize => new Vector2(600f, 700f);
    public override void DoWindowContents(Rect inRect) {
        // TabDrawer oben, Inhalt unten
        var tabRect = new Rect(0, 0, inRect.width, 30f);
        TabDrawer.DrawTabs(tabRect, tabs);
        var contentRect = new Rect(0, 35f, inRect.width, inRect.height - 35f);
        activeTab.DrawContent(contentRect);
    }
}
```

**Sektionen (Content-Drawer) – Data-Binding via Foundation Snapshots:**
- Jede Sektion liest `FoundationSnapshot` (Storage, Power, Threat, Wallet, Progression) → **keine eigene Berechnung**.

---

## 5. Definitionsdateien (XML) – Übersicht

| Datei | Paket | Zweck |
|---|---|---|
| `Defs/Scenarios/IntroSequencePart.xml` | 05 | ScenPart für Intro |
| `Defs/WeatherDefs/Rimconemy_StormAtmosphere.xml` | 05 | Gewitter-Optik |
| `Defs/TutorialSteps/Tutorial_Step1..5.xml` | 05 | Tutorial-Schritte |
| `Defs/MainButtonDefs/RimPadButton.xml` | 01 | Toolbar-Button |
| `Defs/KeyBindingDefs/RimPadToggle.xml` | 01 | Hotkey Strg+T |
| `Defs/LetterDefs/TutorialLetterDefs.xml` | 05 | LetterDefs mit Portrait-Icon |
| `Defs/Scenario/SingleSurvivor.xml` (Update) | 02 | Label/Description/IntroPart |

---

## 6. Save/Load & Migration

- **TutorialState** implementiert `ISchemaMigratable` (Foundation) → Schema v1, Migration via `MigrationStepWalker`.
- **RimPadWindow-Position** (`windowRect`) wird von RimWorld automatisch pro Save persistiert.
- **TutorialState** Schema v1 → Felder: `CompletedSteps`, `DismissedSteps`, `DismissedAll`, `Completed`.
- **Migration:** `MigrationRegistry` (Foundation) registriert `TutorialState` → `RunMigration()` bei Load.

---

## 7. Testing & Gates

| Gate | Test | Erfolgskriterium |
|---|---|---|
| **Unit** | `TutorialDirector` Step-Trigger | Jeder Step feuert exakt 1× bei Trigger |
| **Unit** | `IntroFlowWindow` Flow-Text + Kamera-Cuts | Black Screen → Text → Kamera-Cuts → Zombie-Flash → Ende |
| **Unit** | `WeatherDef` StormAtmosphere | Kein Blitz-EventMaker, nur Regen/Wind |
| **Integration** | `TutorialDirector` + Paket-02 Callback | Campfire gebaut → Step 3 feuert |
| **Integration** | RimPad + Foundation Snapshots | Werte stimmen mit `StorageQuery`/`PowerChain` überein |
| **Save/Load** | TutorialState + RimPad Position | Nach Load: Steps erhalten, RimPad Position wiederhergestellt |
| **Regression** | `ScenPart_RimconemyStartEnemies` | Alle Difficulty/Map-Size → ≥1 Infected |
| **Runtime** | `runtime_test.sh` | PASS (35+ Summaries, 0 Failures) |

---

## 8. Offene Punkte / Risiken

| Risiko | Mitigation |
|---|---|
| **Portrait im Letter** – `ReceiveLetter` hat keinen Portrait-Param. | **Lösung:** `LetterDef.Icon` (Texture2D) setzen → `ReceiveLetter(label, text, letterDef, ...)` zeigt Icon. |
| **Gewitter ohne Blitz** – Kein `lightningBias` Feld. | `eventMakers` leer lassen → keine `LightningFlash` EventMaker. |
| **MainTabDef nicht existent** | `MainButtonDef` nutzen (wie in Paket-Defs schon Standard). |
| **CameraDriver.PanToMapLocAndSize** Dauer/Callback | `PanCompletionCallback` optional; für Intro feste Dauer (duration=60 Ticks) nutzen. |
| **Zombie-Flash Despawn** – `DestroyMode.Vanish` hinterlässt keine Leichen. | ✅ `DestroyMode.Vanish` bestätigt. |

---

## 9. Nächste Schritte (nach Spec-Freigabe)

1. **Implementierungsplan** (`writing-plans` Skill) → konkrete Tasks, Dateien, Reihenfolge.
2. **Code-Implementierung** – Paket 01 (RimPad, Theme, Button), Paket 05 (Intro, TutorialDirector, Steps, WeatherDef, Scenario-Update).
3. **Tests** – Unit + Integration + Regression.
4. **Runtime-Test** (`scripts/runtime_test.sh`) → PASS.
5. **Spec-Review** → Abschluss.

---

## 10. Abnahmekriterien (Definition of Done)

- [ ] **Intro-Sequenz** läuft fehlerfrei (Black Screen → Flow-Text → Kamera-Cuts → Zombie-Flash → RimPad-Notification).
- [ ] **RimPad** öffnet/schließt via Button + Hotkey, Position persistiert, Tabs zeigen korrekte Snapshots.
- [ ] **Guide** feuert 5 Steps in korrekter Reihenfolge, Popups zeigen Portrait + Text, Dismiss funktioniert, abschaltbar.
- [ ] **Start-Szenario** zeigt neuen Namen/Beschreibung, Gewitter-Optik (kein Blitz), Infizierte ≥1.
- [ ] **Save/Load** – TutorialState + RimPad-Position erhalten.
- [ ] **Runtime-Test** PASS (0 Fehler, 35+ Summaries).
- [ ] **Keine Debug-Infos** in Spieler-UI (nur klare, szenische Infos).

---

**Ende Spec.**  
**Bitte Review & Freigabe** → danach `writing-plans` → Implementierung.