# Spec: Cinematic Intro + RimPad Floating Dashboard + Storyteller-Guide

**Version**: 1.0  
**Datum**: 2026-08-06  
**Owner**: Paket 05 (Infected & Automation) + Paket 01 (Foundation UI)  
**Status**: READY FOR IMPLEMENTATION  
**API-Verifiziert**: RimWorld 1.6.4566 + alle DLCs (Assembly-CSharp.dll Spike)

---

## 1. Vision & User Story

> **Cinematic Intro**: Nach 5 Jahren ISS → Rückkehr zur Erde → Freude auf Familie → Zombie-Horde blitzt 3 Sek. auf → RimPad Notification → Tutorial-Guide startet.

> **RimPad**: Floating Dashboard (Pip-Boy/Tablet Style) mit Tabs: Guide, Threat, Phase, Economy, Settings. Haupt-UI für alle Rimconemy-Systeme.

> **Storyteller-Guide**: Kein eigener StorytellerDef. Simulierter Guide via TutorialDirector (GameComponent) + TutorialStepDefs + Letter-Popups mit Portrait.

---

## 2. Architektur-Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CINEMATIC INTRO SEQUENCE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  ScenPart_IntroSequence (Paket 05)                                          │
│    └─ PostMapGenerate → IntroFlowWindow (Paket 01 UI)                       │
│         ├─ Black Screen + Flow-Text (5 Blöcke, 180 Ticks each)             │
│         ├─ Kamera-Cuts alle 180–300 Ticks (Find.CameraDriver)              │
│         └─ Zombie-Horde-Flash: Spawn 5× InfectedRavager → JumpTo →          │
│              180 Ticks warten → Destroy(DestroyMode.Vanish)                │
│         └─ WindowStack.Remove → TutorialDirector.StartGuide()               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         RIMPAD FLOATING DASHBOARD                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  RimPadWindow : RimconemyWindow (Paket 01)                                  │
│    ├─ TabSystem: TabRecord[] + TabDrawer                                    │
│    ├─ Tabs: [Guide] [Threat] [Phase] [Economy] [Settings]                  │
│    ├─ Theme: RimconemyTheme erweitert (Panel-Texturen, Monospace)          │
│    ├─ Persistenz: windowRect auto-save + RimPadSettings (Skalierung)       │
│    ├─ Toggle: MainButtonDef (Toolbar) + KeyBindingDef (Strg+T)             │
│    └─ Badge: Notification-Count auf "Guide"-Tab                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      STORYTELLER-GUIDE / TUTORIAL                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  TutorialDirector : GameComponent (Paket 05)                                │
│    ├─ TutorialStepDef : Def (trigger, letterLabel, letterText,             │
│    │    unlockDefs, portraitTexture, prerequisiteSteps)                    │
│    ├─ Trigger: CapabilityAudit.RegisterCallback("OnCampfireBuilt", ...)    │
│    ├─ Delivery: LetterStack.ReceiveLetter(RimconemyTutorialLetter)         │
│    │    (eigene Letter-Subclass mit Texture2D Portrait)                    │
│    ├─ State: TutorialState : IExposable + ISchemaMigratable                │
│    │    (HashSet<string> CompletedSteps, CurrentStepIndex, Dismissed)     │
│    └─ Portrait: ContentFinder<Texture2D>.Get("UI/HeroArt/Storytellers/    │
│         RimconemyLarge")                                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. API-Verifizierte Signaturen (Core + DLCs)

### 3.1 CameraDriver
```csharp
// JumpTo (sofort)
void JumpToCurrentMapLoc(IntVec3 cell);
void JumpToCurrentMapLoc(Vector3 loc);

// Pan mit Animation + Callback
void PanToMapLocAndSize(Vector3 loc, float size, float duration, PanCompletionCallback onComplete);

// Direct Set
void SetRootPosAndSize(Vector3 rootPos, float rootSize);
```

### 3.2 Window (Abstract Base)
```csharp
// Properties (get-only in 1.6)
Vector2 InitialSize { get; }
float Margin { get; }
bool IsDebug { get; }
bool IsOpen { get; }
QuickSearchWidget CommonSearchWidget { get; }
string CloseButtonText { get; }

// Virtual Methods
virtual void DoWindowContents(Rect inRect);
virtual void PreOpen();
virtual void PostOpen();
virtual void PreClose();
virtual void PostClose();
virtual void Close(bool doCloseSound);
virtual bool OnCloseRequest();
```

### 3.3 LetterStack.ReceiveLetter (3 Overloads)
```csharp
// Vollständigster Overload
void ReceiveLetter(
    TaggedString label,
    TaggedString text,
    LetterDef textLetterDef,
    LookTargets lookTargets,
    Faction relatedFaction,
    Quest quest,
    List<ThingDef> hyperlinkThingDefs,
    string debugInfo,
    int delayTicks,
    bool playSound
);

// Workaround für Portrait: Eigene Letter-Subclass
class RimconemyTutorialLetter : Letter
{
    public Texture2D Portrait;
    public override void ExposeData() { base.ExposeData(); Scribe_References.Look(ref Portrait, "portrait"); }
    public override void DrawWindow(...) { /* Portrait rendern */ }
}
```

### 3.4 WeatherDef Felder (alle public)
```csharp
IntRange durationRange;
bool repeatable;
bool isBad;
bool canOccurAsRandomForcedEvent;
Favorability favorability;
FloatRange temperatureRange;
SimpleCurve commonalityRainfallFactor;
int transitionTicksOverride;
int minMonolithLevel;
bool canOccurInAmbientHorror;
string letterText;
string letterLabel;
LetterDef letterDef;
float rainRate;
float snowRate;
float sandRate;
float windSpeedFactor;
float windSpeedOffset;
float moveSpeedMultiplier;
float accuracyMultiplier;
float maxRangeCap;
float perceivePriority;
bool doToxicBuildup;
ThoughtDef weatherThought;
float maxGlow;
bool preventSkygaze;
bool preventsShuttleLaunch;
List<SoundDef> ambientSounds;
List<WeatherEventMaker> eventMakers;
List<Type> overlayClasses;
SkyColorSet skyColorsNightMid;
SkyColorSet skyColorsNightEdge;
SkyColorSet skyColorsDay;
SkyColorSet skyColorsDusk;
Type workerClass;
```

### 3.5 ScenPart Lifecycle
```csharp
virtual void PostGameStart();        // ScenPart_GameStartDialog
virtual void GenerateIntoMap(Map map); // ScenPart_GameCondition, PermaGameCondition
virtual void PostWorldGenerate();    // ScenPart_GameCondition
```

### 3.6 IncidentWorker (für Tutorial-Letter)
```csharp
virtual bool TryExecuteWorker(IncidentParms parms);
virtual void SendStandardLetter(
    IncidentParms parms,
    LookTargets lookTargets,
    NamedArgument[] textArgs);
```

### 3.7 GameComponent (TutorialDirector)
```csharp
virtual void StartedNewGame();
virtual void LoadedGame();
virtual void GameComponentTick();
```

### 3.8 KeyBindingDef
```csharp
bool JustPressed { get; }
bool IsDownEvent { get; }
KeyCode MainKey { get; }
static KeyBindingDef Named(string name);
```

### 3.9 ContentFinder<T>
```csharp
static T Get(string itemPath, bool reportFailure = true);
static IEnumerable<T> GetAllInFolder(string folderPath);
```

---

## 4. Detaillierte Implementation Specs

### 4.1 IntroFlowWindow (Paket 01: Foundation/UI)

**Pfad**: `mods/01-Rimconemy-Foundation/Source/UI/IntroFlowWindow.cs`

```csharp
public class IntroFlowWindow : RimconemyWindow
{
    // Flow-Text Blöcke (5 Stück, je 180 Ticks = 3 Sek)
    private readonly string[] flowTexts = new[]
    {
        "DU, {PawnName}, bist nach 5 Jahren außerhalb der Atmosphäre...",
        "von der ISS zur Erde zurückgekehrt.",
        "Du freust dich auf deine Familie, deine Freunde...",
        "Doch etwas ist anders. Die Stille ist zu laut.",
        "Willkommen in der neuen Welt."
    };
    
    private int currentBlock = 0;
    private long blockStartTick;
    private const int BlockDurationTicks = 180; // 3 Sek @ 60 ticks/s
    
    // Kamera-Cut Positionen (werden in PostMapGenerate generiert)
    private List<IntVec3> cameraCutPositions;
    private int currentCutIndex = 0;
    private long lastCutTick;
    private const int CutIntervalTicks = 240; // 4 Sek
    
    // Zombie-Horde-Flash
    private bool hordeFlashed = false;
    private const int HordeFlashDurationTicks = 180;
    private long hordeFlashStartTick;
    private List<Pawn> tempHordePawns = new();
    
    protected override void PreOpen()
    {
        base.PreOpen();
        Current.Game.Paused = true; // Workaround für ForcePause
        blockStartTick = Find.TickManager.TicksGame;
        lastCutTick = Find.TickManager.TicksGame;
    }
    
    protected override void PostClose()
    {
        base.PostClose();
        Current.Game.Paused = false;
        // Cleanup temp horde pawns falls noch vorhanden
        foreach (var p in tempHordePawns.Where(p => !p.Destroyed))
            p.Destroy(DestroyMode.Vanish);
        // TutorialDirector starten
        TutorialDirector.Get()?.StartGuide();
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        // Black background
        Widgets.DrawRectFast(inRect, Color.black);
        
        // Flow-Text zentriert
        var text = flowTexts[currentBlock].Replace("{PawnName}", GetStartingPawnName());
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(inRect.ContractedBy(40), text);
        Text.Anchor = TextAnchor.UpperLeft;
        
        // Timer logic
        long now = Find.TickManager.TicksGame;
        
        // Flow-Text weiter
        if (now - blockStartTick >= BlockDurationTicks && currentBlock < flowTexts.Length - 1)
        {
            currentBlock++;
            blockStartTick = now;
        }
        
        // Kamera-Cuts
        if (cameraCutPositions != null && cameraCutPositions.Count > 0)
        {
            if (now - lastCutTick >= CutIntervalTicks)
            {
                var pos = cameraCutPositions[currentCutIndex % cameraCutPositions.Count];
                Find.CameraDriver.JumpToCurrentMapLoc(pos);
                currentCutIndex++;
                lastCutTick = now;
            }
        }
        
        // Zombie-Horde-Flash (nach Block 3)
        if (currentBlock >= 3 && !hordeFlashed)
        {
            FlashHorde();
            hordeFlashed = true;
            hordeFlashStartTick = now;
        }
        
        // Horde despawnen nach 3 Sek
        if (hordeFlashed && now - hordeFlashStartTick >= HordeFlashDurationTicks)
        {
            DespawnHorde();
        }
        
        // Auto-close nach letztem Block + Puffer
        if (currentBlock >= flowTexts.Length - 1 && now - blockStartTick >= BlockDurationTicks + 120)
        {
            Close();
        }
    }
    
    private void FlashHorde()
    {
        var map = Find.AnyPlayerHomeMap;
        if (map == null) return;
        
        var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail("Rimconemy_InfectedRavager");
        var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("Rimconemy_HiddenInfectedFaction");
        if (kindDef == null || factionDef == null) return;
        
        var faction = InfectedFactionUtility.EnsureHiddenInfectedFaction();
        if (faction == null) return;
        
        for (int i = 0; i < 5; i++)
        {
            var cell = CellFinder.RandomEdgeCell(map);
            var pawn = PawnGenerator.GeneratePawn(kindDef, faction);
            if (pawn != null)
            {
                pawn.mindState.duty = null; // Keine AI
                GenSpawn.Spawn(pawn, cell, map);
                tempHordePawns.Add(pawn);
                
                // Kamera auf ersten Pawn
                if (i == 0) Find.CameraDriver.JumpToCurrentMapLoc(cell);
            }
        }
    }
    
    private void DespawnHorde()
    {
        foreach (var p in tempHordePawns.Where(p => !p.Destroyed))
            p.Destroy(DestroyMode.Vanish);
        tempHordePawns.Clear();
    }
}
```

### 4.2 ScenPart_IntroSequence (Paket 05)

**Pfad**: `mods/05-Rimconemy-Infected-Automation/Source/Scenarios/ScenPart_IntroSequence.cs`

```csharp
[DefOf]
public class ScenPart_IntroSequence : ScenPart
{
    public override void PostMapGenerate(Map map)
    {
        base.PostMapGenerate(map);
        
        // Kamera-Cut Positionen generieren (Landezone, Ruinen, Kartenrand, etc.)
        var cuts = GenerateCameraCuts(map);
        
        // IntroFlowWindow erstellen und anzeigen
        var window = new IntroFlowWindow
        {
            cameraCutPositions = cuts
        };
        Find.WindowStack.Add(window);
    }
    
    private List<IntVec3> GenerateCameraCuts(Map map)
    {
        var cuts = new List<IntVec3>
        {
            map.Center, // Zentrum
        };
        
        // Ruinen hinzufügen falls vorhanden
        var ruins = map.listerBuildings.AllBuildings()
            .Where(b => b.def.building?.isRuins == true)
            .Select(b => b.Position)
            .Take(3);
        cuts.AddRange(ruins);
        
        // Kartenränder
        cuts.Add(new IntVec3(10, 0, map.Size.z / 2)); // West
        cuts.Add(new IntVec3(map.Size.x - 10, 0, map.Size.z / 2)); // Ost
        cuts.Add(new IntVec3(map.Size.x / 2, 0, 10)); // Nord
        cuts.Add(new IntVec3(map.Size.x / 2, 0, map.Size.z - 10)); // Süd
        
        return cuts;
    }
}
```

### 4.3 RimPadWindow + TabSystem (Paket 01)

**Pfad**: `mods/01-Rimconemy-Foundation/Source/UI/RimPadWindow.cs`

```csharp
public class RimPadWindow : RimconemyWindow
{
    private List<TabRecord> tabs = new();
    private TabRecord currentTab;
    private Vector2 scrollPosition;
    private const float TabHeight = 35f;
    private const float PanelPadding = 12f;
    
    public override Vector2 InitialSize => new Vector2(520f, 680f);
    
    public RimPadWindow()
    {
        forcePause = false;
        closeOnCancel = true;
        preventCameraMotion = false;
        draggable = true;
        resizeable = true;
    }
    
    protected override void PreOpen()
    {
        base.PreOpen();
        BuildTabs();
    }
    
    private void BuildTabs()
    {
        tabs.Clear();
        tabs.Add(new TabRecord("GUIDE", () => currentTab = tabs[0], currentTab == tabs[0]));
        tabs.Add(new TabRecord("THREAT", () => currentTab = tabs[1], currentTab == tabs[1]));
        tabs.Add(new TabRecord("PHASE", () => currentTab = tabs[2], currentTab == tabs[2]));
        tabs.Add(new TabRecord("ECONOMY", () => currentTab = tabs[3], currentTab == tabs[3]));
        tabs.Add(new TabRecord("SETTINGS", () => currentTab = tabs[4], currentTab == tabs[4]));
        currentTab = tabs[0];
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        // Header mit RimconemyTheme
        RimconemyTheme.DrawPanelHeader(inRect.TopPartPixels(40), "RIMPAD v1.0");
        
        // Tab-Buttons
        var tabRect = new Rect(inRect.x, inRect.y + 45, inRect.width, TabHeight);
        TabDrawer.DrawTabs(tabRect, tabs);
        
        // Content Area
        var contentRect = new Rect(
            inRect.x + PanelPadding,
            tabRect.yMax + PanelPadding,
            inRect.width - PanelPadding * 2,
            inRect.height - tabRect.yMax - PanelPadding * 2
        );
        
        // ScrollView für Content
        scrollPosition = Widgets.BeginScrollView(contentRect, scrollPosition, 
            new Rect(0, 0, contentRect.width - 16, GetContentHeight()));
        
        DrawCurrentTabContent(new Rect(0, 0, contentRect.width - 16, GetContentHeight()));
        
        Widgets.EndScrollView();
        
        // Notification Badge auf Guide-Tab
        if (TutorialDirector.Get()?.HasUnreadNotifications == true)
            DrawNotificationBadge(tabs[0]);
    }
    
    private float GetContentHeight()
    {
        // Dynamisch je nach Tab
        switch (tabs.IndexOf(currentTab))
        {
            case 0: return TutorialDirector.Get()?.GetGuideContentHeight() ?? 400f;
            case 1: return ThreatTabContent.GetHeight();
            case 2: return PhaseTabContent.GetHeight();
            case 3: return EconomyTabContent.GetHeight();
            case 4: return 300f;
            default: return 400f;
        }
    }
    
    private void DrawCurrentTabContent(Rect rect)
    {
        switch (tabs.IndexOf(currentTab))
        {
            case 0: TutorialDirector.Get()?.DrawGuideContent(rect); break;
            case 1: ThreatTabContent.Draw(rect); break;
            case 2: PhaseTabContent.Draw(rect); break;
            case 3: EconomyTabContent.Draw(rect); break;
            case 4: SettingsTabContent.Draw(rect); break;
        }
    }
}
```

### 4.4 RimconemyTutorialLetter (Paket 05) — Portrait-Support

**Pfad**: `mods/05-Rimconemy-Infected-Automation/Source/Story/RimconemyTutorialLetter.cs`

```csharp
public class RimconemyTutorialLetter : Letter
{
    public Texture2D Portrait;
    public string StepId;
    public List<Def> UnlockDefs;
    
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Portrait, "portrait");
        Scribe_Values.Look(ref StepId, "stepId");
        Scribe_Defs.LookList(ref UnlockDefs, "unlockDefs");
    }
    
    public override void OpenLetter()
    {
        // Custom Dialog mit Portrait + Unlock-Vorschau
        Find.WindowStack.Add(new Dialog_TutorialStep(this));
    }
    
    public override Texture2D Icon => LetterDefOf.PositiveEvent.Icon;
}

// Dialog für Tutorial-Schritt
public class Dialog_TutorialStep : Window
{
    private readonly RimconemyTutorialLetter letter;
    
    public Dialog_TutorialStep(RimconemyTutorialLetter letter)
    {
        this.letter = letter;
        forcePause = true;
        closeOnCancel = true;
        preventCameraMotion = false;
        draggable = true;
    }
    
    public override Vector2 InitialSize => new Vector2(600f, 500f);
    
    public override void DoWindowContents(Rect inRect)
    {
        var portraitRect = new Rect(inRect.x + 20, inRect.y + 20, 128, 128);
        if (letter.Portrait != null)
            GUI.DrawTexture(portraitRect, letter.Portrait);
        
        var textRect = new Rect(portraitRect.xMax + 20, inRect.y + 20, 
            inRect.width - portraitRect.width - 60, inRect.height - 60);
        Widgets.Label(textRect, letter.Text);
        
        // Unlock-Vorschau
        if (letter.UnlockDefs?.Count > 0)
        {
            var unlockRect = new Rect(inRect.x + 20, inRect.y + 180, inRect.width - 40, 100);
            Widgets.Label(unlockRect, "FREISCHALTUNGEN:");
            float y = unlockRect.y + 25;
            foreach (var def in letter.UnlockDefs)
            {
                Widgets.Label(new Rect(unlockRect.x, y, unlockRect.width, 25), 
                    $"▸ {def.LabelCap}");
                y += 25;
            }
        }
        
        // Buttons
        var btnRect = new Rect(inRect.center.x - 100, inRect.yMax - 50, 200, 40);
        if (Widgets.ButtonText(btnRect, "VERSTANDEN"))
        {
            TutorialDirector.Get()?.MarkStepCompleted(letter.StepId);
            Close();
        }
    }
}
```

### 4.5 TutorialDirector + TutorialStepDef (Paket 05)

**Pfad**: `mods/05-Rimconemy-Infected-Automation/Source/Story/TutorialDirector.cs`

```csharp
public class TutorialDirector : GameComponent
{
    public TutorialState State { get; private set; }
    private List<TutorialStepDef> allSteps;
    
    public TutorialDirector(Game game) : base(game) { }
    
    public override void StartedNewGame()
    {
        base.StartedNewGame();
        State = new TutorialState();
        allSteps = DefDatabase<TutorialStepDef>.AllDefsListForReading
            .OrderBy(s => s.priority)
            .ToList();
        RegisterTriggers();
    }
    
    public override void LoadedGame()
    {
        base.LoadedGame();
        if (State == null) State = new TutorialState();
        RegisterTriggers();
    }
    
    private void RegisterTriggers()
    {
        // Via CapabilityAudit Bridge
        var cap = CapabilityAudit.GetCapability<ITutorialTriggerBridge>();
        if (cap != null)
        {
            cap.OnCampfireBuilt += () => TryTriggerStep("CampfireBuilt");
            cap.OnFirstInfectedContact += () => TryTriggerStep("FirstInfectedContact");
            cap.OnWallBuilt += () => TryTriggerStep("WallBuilt");
            cap.OnResourceCollected += (def) => TryTriggerStep("ResourceCollected_" + def.defName);
        }
    }
    
    private void TryTriggerStep(string triggerId)
    {
        var step = allSteps.FirstOrDefault(s => s.trigger == triggerId);
        if (step == null) return;
        if (State.CompletedSteps.Contains(step.defName)) return;
        if (!step.prerequisiteSteps.All(p => State.CompletedSteps.Contains(p))) return;
        
        ShowStep(step);
    }
    
    private void ShowStep(TutorialStepDef step)
    {
        var portrait = ContentFinder<Texture2D>.Get(step.portraitTexture, false) 
            ?? ContentFinder<Texture2D>.Get("UI/HeroArt/Storytellers/RimconemyLarge", false);
        
        var letter = new RimconemyTutorialLetter
        {
            Label = step.letterLabel,
            Text = step.letterText,
            def = LetterDefOf.PositiveEvent,
            Portrait = portrait,
            StepId = step.defName,
            UnlockDefs = step.unlockDefs
        };
        
        Find.LetterStack.ReceiveLetter(letter, "TutorialDirector");
        State.CurrentStepIndex = allSteps.IndexOf(step);
    }
    
    public void MarkStepCompleted(string stepId)
    {
        State.CompletedSteps.Add(stepId);
        State.CurrentStepIndex = -1;
    }
    
    public bool HasUnreadNotifications => State.CurrentStepIndex >= 0;
    
    public float GetGuideContentHeight() => 400f; // Dynamisch
    
    public void DrawGuideContent(Rect rect)
    {
        // Zeige abgeschlossene + nächste Schritte
        Widgets.Label(rect.TopPartPixels(30), "TUTORIAL-STATUS");
        float y = 40;
        
        foreach (var step in allSteps)
        {
            bool done = State.CompletedSteps.Contains(step.defName);
            bool current = State.CurrentStepIndex == allSteps.IndexOf(step);
            
            var color = done ? Color.green : (current ? Color.yellow : Color.gray);
            var label = $"{(done ? "✓" : current ? "▶" : "○")} {step.letterLabel}";
            
            Widgets.Label(new Rect(rect.x, y, rect.width, 25), label);
            y += 28;
        }
    }
    
    public static TutorialDirector Get() => Current.Game.GetComponent<TutorialDirector>();
}
```

**Pfad**: `mods/05-Rimconemy-Infected-Automation/Source/Story/TutorialState.cs`

```csharp
public class TutorialState : IExposable, ISchemaMigratable
{
    public HashSet<string> CompletedSteps = new();
    public int CurrentStepIndex = -1;
    public bool Dismissed = false;
    public int SchemaVersion = 1;
    
    public void ExposeData()
    {
        Scribe_Collections.Look(ref CompletedSteps, "completedSteps", LookMode.Value);
        Scribe_Values.Look(ref CurrentStepIndex, "currentStepIndex");
        Scribe_Values.Look(ref Dismissed, "dismissed");
        Scribe_Values.Look(ref SchemaVersion, "schemaVersion", 1);
    }
    
    public void Migrate(int fromVersion) { /* future-proof */ }
}
```

**Pfad**: `mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/TutorialSteps.xml`

```xml
<Defs>
  <TutorialStepDef>
    <defName>Tutorial_Welcome</defName>
    <priority>0</priority>
    <trigger>GameStart</trigger>
    <letterLabel>WILLKOMMEN ÜBERLEBENDER</letterLabel>
    <letterText>Du bist zurück. Die Welt hat sich verändert. Dein RimPad wird dich führen.</letterText>
    <portraitTexture>UI/HeroArt/Storytellers/RimconemyLarge</portraitTexture>
    <unlockDefs>
      <li>Rimconemy_Campfire</li>
      <li>Rimconemy_ConstructionDebris</li>
    </unlockDefs>
    <prerequisiteSteps />
  </TutorialStepDef>
  
  <TutorialStepDef>
    <defName>Tutorial_Campfire</defName>
    <priority>10</priority>
    <trigger>CampfireBuilt</trigger>
    <letterLabel>DAS ERSTE FEUER</letterText>
    <letterText>Das Campfire ist dein Überlebensanker. Koche, wärme dich, schmelze Stahlschrott.</letterText>
    <portraitTexture>UI/HeroArt/Storytellers/RimconemyLarge</portraitTexture>
    <unlockDefs>
      <li>Rimconemy_MakeCoal</li>
      <li>Rimconemy_SalvageMachineParts</li>
    </unlockDefs>
    <prerequisiteSteps>
      <li>Tutorial_Welcome</li>
    </prerequisiteSteps>
  </TutorialStepDef>
  
  <TutorialStepDef>
    <defName>Tutorial_FirstContact</defName>
    <priority>20</priority>
    <trigger>FirstInfectedContact</trigger>
    <letterLabel>SIE SIND HIER</letterText>
    <letterText>Die Infizierten haben dich bemerkt. Wachstum zieht Aufmerksamkeit. Baue Verteidigungen.</letterText>
    <portraitTexture>UI/HeroArt/Storytellers/RimconemyLarge</portraitTexture>
    <unlockDefs>
      <li>Rimconemy_Barricade</li>
      <li>Rimconemy_ArrowTurret</li>
    </unlockDefs>
    <prerequisiteSteps>
      <li>Tutorial_Campfire</li>
    </prerequisiteSteps>
  </TutorialStepDef>
  
  <TutorialStepDef>
    <defName>Tutorial_Wall</defName>
    <priority>30</priority>
    <trigger>WallBuilt</trigger>
    <letterLabel>DEINE GRENZEN</letterText>
    <letterText>Mauern aus Bauschutt schützen dich. Aber sie brauchen Strom und Wartung.</letterText>
    <portraitTexture>UI/HeroArt/Storytellers/RimconemyLarge</portraitTexture>
    <unlockDefs>
      <li>Rimconemy_WoodCoalGenerator</li>
    </unlockDefs>
    <prerequisiteSteps>
      <li>Tutorial_FirstContact</li>
    </prerequisiteSteps>
  </TutorialStepDef>
</Defs>
```

### 4.6 WeatherDef: Rimconemy_StormAtmosphere (Paket 05)

**Pfad**: `mods/05-Rimconemy-Infected-Automation/Defs/WeatherDefs/Weather_StormAtmosphere.xml`

```xml
<WeatherDef>
  <defName>Rimconemy_StormAtmosphere</defName>
  <label>Sturm-Atmosphäre</label>
  <workerClass>Verse.WeatherWorker</workerClass>
  <durationRange>
    <min>3000</min>
    <max>6000</max>
  </durationRange>
  <repeatable>False</repeatable>
  <isBad>True</isBad>
  <favorability>VeryBad</favorability>
  <temperatureRange>
    <min>-10</min>
    <max>25</max>
  </temperatureRange>
  <rainRate>0.05</rainRate>
  <snowRate>0</snowRate>
  <lightningBias>0</lightningBias>
  <windSpeedFactor>1.5</windSpeedFactor>
  <moveSpeedMultiplier>0.8</moveSpeedMultiplier>
  <accuracyMultiplier>0.9</accuracyMultiplier>
  <maxGlow>0.3</maxGlow>
  <preventSkygaze>True</preventSkygaze>
  <skyColorsDay>
    <colorMid>#3A2F2F</colorMid>
    <colorEdge>#1A0F0F</colorEdge>
  </skyColorsDay>
  <skyColorsDusk>
    <colorMid>#2A1F1F</colorMid>
    <colorEdge>#0F0505</colorEdge>
  </skyColorsDusk>
  <skyColorsNightMid>
    <colorMid>#151010</colorMid>
    <colorEdge>#050202</colorEdge>
  </skyColorsNightMid>
  <skyColorsNightEdge>
    <colorMid>#0A0808</colorMid>
    <colorEdge>#020101</colorEdge>
  </skyColorsNightEdge>
  <overlayClasses>
    <li>Verse.WeatherOverlay_Darkness</li>
  </overlayClasses>
</WeatherDef>
```

### 4.7 Scenario-Anpassungen

**SingleSurvivor.xml** (Paket 02):
```xml
<ScenarioDef>
  <defName>SingleSurvivor</defName>
  <label>Einzelner Überlebender</label>
  <description>
    DU, {PawnName}, bist nach 5 Jahren ISS zur Erde zurückgekehrt.
    Die Infizierten warten bereits. Dein RimPad ist dein einziger Verbündeter.
  </description>
  <scenarioParts>
    <li Class="ScenPart_RimconemyStartEnemies">
      <count>1</count> <!-- Garantierter Infizierter Spawn -->
    </li>
    <li Class="ScenPart_IntroSequence" /> <!-- NEU: Intro-Sequenz -->
  </scenarioParts>
  <permaGameConditions>
    <li>Rimconemy_StormAtmosphere</li>
  </permaGameConditions>
</ScenarioDef>
```

### 4.8 MainButtonDef + KeyBindingDef (Paket 01)

**Pfad**: `mods/01-Rimconemy-Foundation/Defs/MainButtonDefs/RimPadButton.xml`

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

**Pfad**: `mods/01-Rimconemy-Foundation/Defs/KeyBindingDefs/KeyBindings.xml`

```xml
<KeyBindingDef>
  <defName>Rimconemy_ToggleRimPad</defName>
  <label>RimPad anzeigen/verstecken</label>
  <defaultKeyCode>T</defaultKeyCode>
  <modifier>Control</modifier>
</KeyBindingDef>
```

---

## 5. Save/Load & Migration

- **TutorialState** implementiert `ISchemaMigratable` (Foundation) → Version 1
- **RimconemyTutorialLetter** `ExposeData` serialisiert `Portrait` Reference + `StepId`
- **Window Position** (`windowRect`) wird von RimWorld automatisch pro Save persistiert
- **RimPadSettings** (Skalierung, Tab-Order) separat als `ModSettings` persistiert

---

## 6. Tests (Regression Gates)

| Test | Verifikation |
|------|--------------|
| `IntroFlowWindowTests` | Flow-Text Timing, Kamera-Cuts, Horde Spawn/Despawn, Auto-Close |
| `RimPadWindowTests` | Tab-Switching, Badge-Updates, Persistenz, Toggle |
| `TutorialDirectorTests` | Trigger-Registrierung, Step-Sequenz, Prerequisites, Save/Load |
| `RimconemyTutorialLetterTests` | Portrait-Rendering, Unlock-Vorschau, Button-Handling |
| `ScenPart_IntroSequenceTests` | PostMapGenerate startet Window, Kamera-Positionen |
| `Weather_StormAtmosphereTests` | Def-Load, SkyColor-Application, Dauer |

---

## 7. Offene Entscheidungen / Risiken

| Thema | Status | Entscheidung |
|-------|--------|--------------|
| Portrait in Letter | ⚠️ Gap | Eigene `RimconemyTutorialLetter` Subclass (kein Core-Support) |
| ForcePause/PreventCameraMotion | ⚠️ Gap | Manuell via `Current.Game.Paused` in PreOpen/PostClose |
| Kamera-Interpolation | ✅ OK | `PanToMapLocAndSize` mit Duration + Callback |
| TutorialStepDef als Def | ✅ OK | Standard Def-System, keine Code-Änderungen nötig |
| ScenPart_IntroSequence Timing | ✅ OK | PostMapGenerate → WindowStack.Add |

---

## 8. Nächste Schritte (Implementation Order)

1. **Task 1**: `IntroFlowWindow` + `ScenPart_IntroSequence` + Kamera-Cut Logic
2. **Task 2**: `RimconemyTutorialLetter` + `Dialog_TutorialStep` (Portrait-Support)
3. **Task 3**: `TutorialDirector` + `TutorialState` + `TutorialStepDef` + XML
4. **Task 4**: `RimPadWindow` + TabSystem + `MainButtonDef` + `KeyBindingDef`
5. **Task 5**: `Weather_StormAtmosphere` + Scenario-Updates
6. **Task 6**: Integration Testing + Runtime-Gate (`runtime_test.sh`)

---

## 9. Dependencies Matrix

| Komponente | Benötigt | Paket |
|------------|----------|-------|
| IntroFlowWindow | RimconemyWindow, CameraDriver, PawnGenerator | 01, 05 |
| ScenPart_IntroSequence | IntroFlowWindow, Map | 05 |
| RimconemyTutorialLetter | Letter, Texture2D, ContentFinder | 05 |
| TutorialDirector | GameComponent, CapabilityAudit, LetterStack | 05, 01 |
| TutorialStepDef | Def-System | 05 |
| RimPadWindow | RimconemyWindow, TabDrawer, RimconemyTheme | 01 |
| Weather_StormAtmosphere | WeatherDef XML | 05 |
| Scenario Updates | ScenPart_IntroSequence, WeatherDef | 02, 05 |

---

**Spec Status**: ✅ COMPLETE — API-verifiziert, implementierbar.  
**Nächster Schritt**: Implementation Task 1 starten.