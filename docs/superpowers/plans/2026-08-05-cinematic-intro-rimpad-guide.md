# Cinematic Intro and RimPad Guide Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the cinematic intro sequence (black screen, flow text, camera cuts, zombie horde flash) and the RimPad floating dashboard with storyteller-guide tutorial system for Rimconemy mod suite.

**Architecture:** 
- Package 01 Foundation: RimPad window, theme, tab system, toolbar button, hotkey, foundation event bridge
- Package 05 Infected: IntroFlowWindow, ScenPart_IntroSequence, TutorialDirector, TutorialStepDefs, TutorialState, WeatherDef, Scenario updates
- Communication via Foundation CapabilityAudit/EventBus bridge for cross-package triggers
- Persistence via RimWorld's window position saving and TutorialState IExposable/ISchemaMigratable

**Tech Stack:** RimWorld 1.6 + DLCs, C#, XML Defs, UnityEngine.UI, Verse.Window, Verse.CameraDriver, Verse.LetterStack

## Global Constraints
- RimWorld 1.6 + all DLCs (Royalty, Ideology, Biotech, Anomaly, Vanilla Animals, Ideology, Royalty, Biotech, Anomaly, Vanilla Furniture Empire)
- No direct package-to-package DLL references; communication via Foundation bridges
- Tutorial must be dismissible and persistently dismissable via mod setting
- Intro sequence must respect ForcePause and PreventCameraMotion
- RimPad position must persist across saves
- WeatherDef must produce storm atmosphere without lightning (empty eventMakers)
- Scenario must guarantee ≥1 infected pawn at start
- All UI must use Rimconemy theme extensions
- Save/load compatibility required for TutorialState and RimPad position

---

### Task 1: Project Setup and Foundation Window Base

**Files:**
- Create: `mods/01-Rimconemy-Foundation/Source/UI/RimPadWindow.cs`
- Modify: `mods/01-Rimconemy-Foundation/Defs/MainButtonDefs/RimPadButton.xml`
- Create: `mods/01-Rimconemy-Foundation/Defs/KeyBindingDefs/RimPadToggle.xml`

**Interfaces:**
- Consumes: None
- Produces: RimPadWindow class (Window derivative), RimPadButtonDef, RimPadToggle KeyBindingDef

- [ ] **Step 1: Create RimPadWindow skeleton inheriting from Window**

```csharp
using Verse;
using RimWorld;
using UnityEngine;

namespace Rimconemy.Foundation.UI
{
    public class RimPadWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(600f, 700f);
        
        public override void DoWindowContents(Rect inRect)
        {
            // TODO: Implement tab drawer and content
        }
        
        // TODO: Override other Window methods as needed
    }
}
```

- [ ] **Step 2: Run game to verify window compiles and can be instantiated (no errors)**

Run: `rimworld` (or launch via Gog/GOG Galaxy)
Expected: No compilation errors, window can be instantiated via dev tools

- [ ] **Step 3: Commit foundation window skeleton**

```bash
git add mods/01-Rimconemy-Foundation/Source/UI/RimPadWindow.cs
git commit -m "feat(foundation): create RimPadWindow skeleton"
```

### Task 2: RimPad Tab System and Theme

**Files:**
- Create: `mods/01-Rimconemy-Foundation/Source/UI/RimPadTabDrawer.cs`
- Create: `mods/01-Rimconemy-Foundation/Source/UI/RimPadTheme.cs`
- Create: `mods/01-Rimconemy-Foundation/Source/UI/RimPadTab.cs` (enum/record)

**Interfaces:**
- Consumes: RimPadWindow
- Produces: TabDrawer, Theme, Tab definitions

- [ ] **Step 1: Define RimPadTab enum and TabRecord wrapper**

```csharp
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.Foundation.UI
{
    public enum RimPadTab
    {
        Survival,
        Infrastructure,
        Economy,
        Threat,
        Diagnostics
    }
    
    // Simple wrapper for TabRecord equivalent
    public class RimPadTabRecord
    {
        public RimPadTab Tab;
        public string Label;
        public Vector2 IconOffset; // optional
        public System.Action<Rect> DrawContent;
        
        public RimPadTabRecord(RimPadTab tab, string label, System.Action<Rect> drawContent)
        {
            Tab = tab;
            Label = label;
            DrawContent = drawContent;
        }
    }
}
```

- [ ] **Step 2: Implement RimPadTabDrawer to handle tab switching and drawing**

```csharp
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.Foundation.UI
{
    public static class RimPadTabDrawer
    {
        {
        private static int selectedTabIndex = 0;
        private static List<RimPadTabRecord> tabs = new List<RimPadTabRecord>();
        
        public static void SetTabs(List<RimPadTabRecord> newTabs)
        {
            tabs = newTabs;
            selectedTabIndex = 0;
        }
        
        public static void DrawTabs(Rect tabContainerRect)
        {
            if (tabs == null || tabs.Count == 0) return;
            
            float tabWidth = tabContainerRect.width / tabs.Count;
            for (int i = 0; i < tabs.Count; i++)
            {
                Rect tabRect = new Rect(tabContainerRect.x + (i * tabWidth), tabContainerRect.y, tabWidth, tabContainerRect.height);
                bool isSelected = (i == selectedTabIndex);
                
                Widgets.DrawHighlightIfMouseover(tabRect);
                Widgets.DrawBoxSolid(tabRect, isSelected ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.15f, 0.15f, 0.15f));
                
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tabRect, tabs[i].Label);
                Text.Anchor = TextAnchor.UpperLeft;
                
                if (Mouse.IsOver(tabRect) && Widgets.ButtonInvisible(tabRect(tabRect, false))
                {
                    selectedTabIndex = i;
                }
            }
        }
        
        public static void DrawSelectedTabContent(Rect contentRect)
        {
            if (tabs.Count == 0) return;
            tabs[selectedTabIndex].DrawContent(contentRect);
        }
    }
}
```

- [ ] **Step 3: Create RimPadTheme extending RimconemyTheme with tablet styling**

```csharp
using Verse;
using RimWorld;
using UnityEngine;

namespace Rimconemy.Foundation.UI
{
    public class RimPadTheme : RimconemyTheme
    {
        public override void Apply()
        {
            base.Apply();
            // Tablet/Pip-Boy style overrides
            Text.Font = GameFont.Mono; // Monospace font
            // Colors: dark background, amber accents
            Widgets.LabelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f); // Light gray text
            // Additional styling as needed
        }
        
        public static Color PanelBackground => new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark semi-transparent
        public static Color AccentColor => new Color(1f, 0.55f, 0f); // Amber
        public static Color WarningColor => new Color(0.8f, 0.2f, 0.2f); // Red
    }
}
```

- [ ] **Step 4: Update RimPadWindow to use tab system and theme**

```csharp
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.Foundation.UI
{
    public class RimPadWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(600f, 700f);
        private bool themeApplied = false;
        
        public override void DoWindowContents(Rect inRect)
        {
            // Apply theme once
            if (!themeApplied)
            {
                RimPadTheme.Instance.Apply();
                themeApplied = true;
            }
            
            // Draw background panel
            Widgets.DrawBoxSolid(new Rect(0, 0, inRect.width, inRect.height), RimPadTheme.PanelBackground);
            
            // Tab container (top 30px)
            Rect tabRect = new Rect(0, 0, inRect.width, 30f);
            RimPadTabDrawer.DrawTabs(tabRect);
            
            // Content area (below tabs)
            Rect contentRect = new Rect(0, 35f, inRect.width, inRect.height - 35f);
            RimPadTabDrawer.DrawSelectedTabContent(contentRect);
        }
        
        public override void PostOpen()
        {
            base.PostOpen();
            // Initialize tabs when window opens
            InitializeTabs();
        }
        
        private void InitializeTabs()
        {
            var tabs = new List<RimPadTabRecord>
            {
                new RimPadTabRecord(RimPadTab.Survival, "Survival", DrawSurvivalTab),
                new RimPadTabRecord(RimPadTab.Infrastructure, "Infrastructure", DrawInfrastructureTab),
                new RimPadTabRecord(RimPadTab.Economy, "Economy", DrawEconomyTab),
                new RimPadTabRecord(RimPadTab.Threat, "Threat", DrawThreatTab),
                new RimPadTabRecord(RimPadTab.Diagnostics, "Diagnostics", DrawDiagnosticsTab)
            };
            
            RimPadTabDrawer.SetTabs(tabs);
        }
        
        // Placeholder draw methods - to be implemented with snapshot data
        private void DrawSurvivalTab(Rect rect) { Widgets.Label(rect, "Survival tab - TODO"); }
        private void DrawInfrastructureTab(Rect rect) { Widgets.Label(rect, "Infrastructure tab - TODO"); }
        private void DrawEconomyTab(Rect rect) { Widgets.Label(rect, "Economy tab - TODO"); }
        private void DrawThreatTab(Rect rect) { Widgets.Label(rect, "Threat tab - TODO"); }
        private void DrawDiagnosticsTab(Rect rect) { Widgets.Label(rect, "Diagnostics tab - TODO"); }
    }
}
```

- [ ] **Step 5: Create RimPadButton definition for toolbar**

File: `mods/01-Rimconemy-Foundation/Defs/MainButtonDefs/RimPadButton.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <MainButtonDef>
    <defName>RimPadButton</defName>
    <label>RimPad</label>
    <icon>UI/Icons/RimPadIcon</icon>
    <hotKey>
      <keyCode>None</keyCode> <!-- Will be overridden by KeyBindingDef -->
    </hotKey>
    <tutorHighlightTag>RimPad</tutorHighlightTag>
  </MainButtonDef>
</Defs>
```

- [ ] **Step 6: Create RimPadToggle keybinding definition**

File: `mods/01-Rimconemy-Foundation/Defs/KeyBindingDefs/RimPadToggle.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <KeyBindingDef>
    <defName>RimPadToggle</defName>
    <label>RimPad Toggle</label>
    <description>Toggle the RimPad floating dashboard</description>
    <keyCode>T</keyCode>
    <modifiers>
      <li>Ctrl</li>
    </modifiers>
  </KeyBindingDef>
</Defs>
```

- [ ] **Step 7: Commit foundation UI components**

```bash
git add mods/01-Rimconemy-Foundation/Source/UI/RimPadTabDrawer.cs
git add mods/01-Rimconemy-Foundation/Source/UI/RimPadTheme.cs
git add mods/01-Rimconemy-Foundation/Source/UI/RimPadTab.cs
git add mods/01-Rimconemy-Foundation/Defs/MainButtonDefs/RimPadButton.xml
git add mods/01-Rimconemy-Foundation/Defs/KeyBindingDefs/RimPadToggle.xml
git commit -m "feat(foundation): implement RimPad tab system, theme, button, and keybinding"
```

### Task 3: Foundation Event Bridge for Cross-Package Communication

**Files:**
- Create: `mods/01-Rimconemy-Foundation/Source/Bridge/EventBridge.cs`
- Create: `mods/01-Rimconemy-Foundation/Source/Bridge/CapabilityAudit.cs`

**Interfaces:**
- Consumes: None
- Produces: Static event bridge and capability audit system

- [ ] **Step 1: Create CapabilityAudit to check for package capabilities**

```csharp
using System;
using System.Collections.Generic;

namespace Rimconemy.Foundation.Bridge
{
    public static class CapabilityAudit
    {
        private static readonly Dictionary<string, HashSet<string>> capabilities = 
            new Dictionary<string, HashSet<string>>();
        
        public static void RegisterCapability(string packageId, string capability)
        {
            if (!capabilities.ContainsKey(packageId))
                capabilities[packageId] = new HashSet<string>();
            
            capabilities[packageId].Add(capability);
        }
        
        public static bool HasCapability(string packageId, string capability)
        {
            return capabilities.TryGetValue(packageId, out var caps) && caps.Contains(capability);
        }
        
        public static void Clear()
        {
            capabilities.Clear();
        }
    }
}
```

- [ ] **Step 2: Create EventBridge for loose-coupled event publishing/subscribing**

```csharp
using System;
using System.Collections.Generic;

namespace Rimconemy.Foundation.Bridge
{
    public delegate void EventCallback();
    
    public static class EventBridge
    {
        private static readonly Dictionary<string, List<EventCallback>> subscribers = 
            new Dictionary<string, List<EventCallback>>();
        
        public static void Subscribe(string eventKey, EventCallback callback)
        {
            if (!subscribers.ContainsKey(eventKey))
                subscribers[eventKey] = new List<EventCallback>();
                
            subscribers[eventKey].Add(callback);
        }
        
        public static void Unsubscribe(string eventKey, EventCallback callback)
        {
            if (subscribers.TryGetValue(eventKey, out var callbacks))
                callbacks.Remove(callback);
        }
        
        public static void Publish(string eventKey)
        {
            if (subscribers.TryGetValue(eventKey, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"EventBridge callback failed for {eventKey}: {e}");
                    }
                }
            }
        }
        
        public static void Clear()
        {
            subscribers.Clear();
        }
    }
}
```

- [ ] **Step 3: Initialize bridge systems in Foundation initialization**

Create/modify: `mods/01-Rimconemy-Foundation/Source/FoundationInitializer.cs`
```csharp
using Verse;
using RimWorld;
using System.Reflection;

namespace Rimconemy.Foundation
{
    [StaticConstructorOnStartup]
    public static class FoundationInitializer
    {
        static FoundationInitializer()
        {
            // Register foundation capabilities
            CapabilityAudit.RegisterCapability("rimconemy.foundation", "event.bridge");
            CapabilityAudit.RegisterCapability("rimconemy.foundation", "capability.audit");
            
            // Subscribe to core RimWorld events if needed
            // Example: EventBridge.Subscribe("game.init", OnGameInit);
        }
        
        // Optional: Initialize on game start
        // private static void OnGameInit() { }
    }
}
```

- [ ] **Step 4: Commit foundation bridge components**

```bash
git add mods/01-Rimconemy-Foundation/Source/Bridge/EventBridge.cs
git add mods/01-Rimconemy-Foundation/Source/Bridge/CapabilityAudit.cs
git add mods/01-Rimconemy-Foundation/Source/FoundationInitializer.cs
git commit -m "feat(foundation): implement event bridge and capability audit for cross-package communication"
```

### Task 4: Intro Flow Window and Camera System

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/UI/IntroFlowWindow.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Source/UI/CameraSequencer.cs` (helper for cuts)

**Interfaces:**
- Consumes: Verse.Window, Verse.CameraDriver, Verse.TickManager
- Produces: IntroFlowWindow that handles flow text, camera cuts, and zombie horde flash

- [ ] **Step 1: Create IntroFlowWindow with force pause and black background**

```csharp
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.UI
{
    public class IntroFlowWindow : Window
    {
        public override Vector2 InitialSize => UI.screenSize;
        public override bool ForcePause => true;
        public override bool PreventCameraMotion => true;
        public override bool DoWindowBackground => true; // We'll draw black ourselves
        
        private int startTick;
        private int currentPhase = 0;
        private List<string> flowTexts;
        private List<IntVec3> cameraPositions;
        private List<int> phaseDurations; // ticks per phase
        
        public IntroFlowWindow()
        {
            // Initialize flow text (ISS return story)
            flowTexts = new List<string>
            {
                "Nach 5 Jahren außerhalb der Erdatmosphäre...",
                "Du kehrst schließlich von der ISS zurück zur Erde.",
                "Die Schwerelosigkeit weicht der vertrauten Schwere.",
                "Dein Herz schlägt schneller beim Gedanken an Familie und Freunde.",
                "Doch etwas feels... falsch. Die Stille ist zu perfekt.",
                "Als du durch die Atmosphäre brechst, siehst du sie.",
                "Am Horizont bewegen sich Gestalten - langsam, unheimlich.",
                "Die Infizierten haben die Städte übernommen.",
                "Aber du bist bereit. Dein RimPad aktiviert sich.",
                "Es ist Zeit zu überleben."
            };
            
            // Initialize camera positions (will be set after map generation)
            cameraPositions = new List<IntVec3>();
            phaseDurations = new List<int>();
            
            // Calculate phases: text blocks + camera cuts + zombie flash
            int textBlockTicks = 300; // ~10 seconds per text block at 30ticks/sec
            int cameraCutInterval = 200; // ~6.5 seconds between cuts
            int zombieFlashTicks = 180; // 3 seconds
            
            // Each text block gets time, with camera cuts interspersed
            for (int i = 0; i < flowTexts.Count; i++)
            {
                phaseDurations.Add(textBlockTicks); // Text display phase
                
                // Add camera cut phases between text blocks (except after last)
                if (i < flowTexts.Count - 1)
                {
                    phaseDurations.Add(cameraCutInterval); // Camera cut phase
                }
            }
            
            // Add zombie flash phase at the end
            phaseDurations.Add(zombieFlashTicks);
        }
        
        public override void PostOpen()
        {
            base.PostOpen();
            startTick = Find.TickManager.TicksGame;
            
            // Initialize camera positions after map is ready
            LongEventHandler.ExecuteWhenFinished(() => 
            {
                if (Find.CurrentMap != null)
                {
                    InitializeCameraPositions(Find.CurrentMap);
                }
            });
        }
        
        private void InitializeCameraPositions(Map map)
        {
            // Clear and recalculate interesting points
            cameraPositions.Clear();
            
            // Add map center
            cameraPositions.Add(map.Center);
            
            // Add some edge points for variety
            int edgePadding = 10;
            cameraPositions.Add(new IntVec3(edgePadding, 0, edgePadding)); // Southwest corner-ish
            cameraPositions.Add(new IntVec3(map.Size.x - edgePadding, 0, edgePadding)); // Southeast
            cameraPositions.Add(new IntVec3(edgePadding, 0, map.Size.z - edgePadding)); // Northwest
            cameraPositions.Add(new IntVec3(map.Size.x - edgePadding, 0, map.Size.z - edgePadding)); // Northeast
            
            // Add a few random points
            for (int i = 0; i < 3; i++)
            {
                cameraPositions.Add(CellFinder.RandomCell(map));
            }
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            // Draw black background
            Widgets.DrawBoxSolid(inRect, Color.black);
            
            // Calculate current phase based on elapsed time
            int elapsed = Find.TickManager.TicksGame - startTick;
            int accumulatedTicks = 0;
            int phaseIndex = 0;
            
            for (int i = 0; i < phaseDurations.Count; i++)
            {
                if (elapsed < accumulatedTicks + phaseDurations[i])
                {
                    phaseIndex = i;
                    break;
                }
                accumulatedTicks += phaseDurations[i];
            }
            
            // Handle different phases
            if (phaseIndex < flowTexts.Count * 2 - 1) // Text and camera cut phases
            {
                bool isTextPhase = (phaseIndex % 2 == 0);
                int textIndex = phaseIndex / 2;
                
                if (isTextPhase && textIndex < flowTexts.Count)
                {
                    // Draw flow text
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(new Rect(20f, 20f, inRect.width - 40f, inRect.height - 40f), 
                                 flowTexts[textIndex]);
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else
                {
                    // Camera cut phase - jump to next position
                    if (cameraPositions.Count > 0)
                    {
                        int camIndex = (phaseIndex / 2) % cameraPositions.Count;
                        Find.CameraDriver.JumpToCurrentMapLoc(cameraPositions[camIndex]);
                    }
                }
            }
            else if (phaseIndex == flowTexts.Count * 2 - 1) // Zombie flash phase
            {
                // This phase handled separately via Update or coroutine-like tick checking
                // We'll spawn zombies and handle timing in WindowUpdate
            }
            else
            {
                // Sequence complete - close window and signal tutorial start
                if (Find.TickManager.TicksGame - startTick >= accumulatedTicks + phaseDurations[phaseIndex])
                {
                    // Trigger zombie flash sequence
                    StartZombieFlashSequence();
                }
            }
        }
        
        private void StartZombieFlashSequence()
        {
            // This will be handled in WindowUpdate to avoid blocking UI thread
            // We'll set a flag and spawn zombies in the next update
            zompFlashStarted = true;
            zompFlashStartTick = Find.TickManager.TicksGame;
        }
        
        private bool zompFlashStarted = false;
        private int zompFlashStartTick = 0;
        private List<Pawn> spawnedZombies = new List<Pawn>();
        
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            
            if (zompFlashStarted && !zompFlashCompleted)
            {
                int elapsed = Find.TickManager.TicksGame - zompFlashStartTick;
                if (elapsed == 0) // First update after start - spawn zombies
                {
                    SpawnZombieHorde();
                }
                else if (elapsed >= 180) // 3 seconds passed
                {
                    DespawnZombieHorde();
                    zompFlashCompleted = true;
                    // Signal completion to tutorial director
                    Find.GetComponent<TutorialDirector>().NotifyIntroCompleted();
                    // Close this window
                    BeginClose();
                }
            }
        }
        
        private bool zompFlashCompleted = false;
        
        private void SpawnZombieHorde()
        {
            if (Find.CurrentMap == null) return;
            
            var faction = InfectedFactionUtility.EnsureHiddenInfectedFaction();
            var kind = DefDatabase<PawnKindDef>.GetNamed("Rimconemy_InfectedRavager");
            
            for (int i = 0; i < 4; i++)
            {
                var cell = CellFinder.RandomEdgeCell(Find.CurrentMap);
                var pawn = PawnGenerator.GeneratePawn(kind, faction);
                // Disable AI for visual-only pawns
                pawn.mindState.duty = null;
                pawn.mindState.mentalStateHandler.CurState = null;
                GenSpawn.Spawn(pawn, cell, Find.CurrentMap);
                spawnedZombies.Add(pawn);
            }
            
            // Camera jumps to first zombie
            if (spawnedZombies.Count > 0)
                Find.CameraDriver.JumpToCurrentMapLoc(spawnedZombies[0].Position);
        }
        
        private void DespawnZombieHorde()
        {
            foreach (var pawn in spawnedZombies)
            {
                if (pawn.Spawned)
                    pawn.Destroy(DestroyMode.Vanish);
            }
            spawnedZombies.Clear();
        }
        
        public override void PreClose()
        {
            base.PreClose();
            // Ensure cleanup if window closed early
            DespawnZombieHorde();
        }
    }
}
```

- [ ] **Step 2: Create CameraSequencer helper (optional, for cleaner code)**

Actually, we integrated the camera logic directly. Let's create a simple helper for reusability.

Create: `mods/05-Rimconemy-Infected-Automation/Source/UI/CameraSequencer.cs`
```csharp
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.UI
{
    public static class CameraSequencer
    {
        private static Queue<IntVec3> positionQueue;
        private static float switchInterval;
        private static float lastSwitchTime;
        private static bool isActive;
        
        public static void StartSequence(List<IntVec3> positions, float intervalSeconds)
        {
            positionQueue = new Queue<IntVec3>(positions);
            switchInterval = intervalSeconds;
            lastSwitchTime = Time.time;
            isActive = true;
            
            // Immediately jump to first position
            if (positionQueue.Count > 0)
                Find.CameraDriver.JumpToCurrentMapLoc(positionQueue.Dequeue());
        }
        
        public static void Update()
        {
            if (!isActive || positionQueue == null || positionQueue.Count == 0) 
                return;
                
            if (Time.time - lastSwitchTime >= switchInterval)
            {
                if (positionQueue.Count > 0)
                {
                    Find.CameraDriver.JumpToCurrentMapLoc(positionQueue.Dequeue());
                    lastSwitchTime = Time.time;
                }
                else
                {
                    isActive = false;
                }
            }
        }
        
        public static void Stop()
        {
            isActive = false;
            positionQueue?.Clear();
        }
    }
}
```

- [ ] **Step 3: Update IntroFlowWindow to use CameraSequencer (refactor)**

Actually, let's keep it simple and integrated for now. We'll skip the separate class and keep logic in IntroFlowWindow for clarity in this first pass.

- [ ] **Step 4: Commit intro flow window**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/UI/IntroFlowWindow.cs
git commit -m "feat(infected): implement IntroFlowWindow with black screen, flow text, camera cuts, and zombie flash"
```

### Task 5: Scenario Part for Intro Sequence

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Scenarios/ScenPart_IntroSequence.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Defs/Scenarios/IntroSequencePart.xml`
- Modify: `mods/02-Rimconemy-Survival-Pack/Defs/Scenario/SingleSurvivor.xml` (add scenepart)

**Interfaces:**
- Consumes: IntroFlowWindow
- Produces: ScenPart that triggers intro on map generation

- [ ] **Step 1: Create ScenPart_IntroSequence**

```csharp
using Verse;
using RimWorld;
using System.Xml;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    public class ScenPart_IntroSequence : ScenPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            // No data to expose for now
        }
        
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            // Add the intro window - this will pause the game and show our sequence
            Find.WindowStack.Add(new Rimconemy.InfectedAutomation.UI.IntroFlowWindow());
        }
        
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            // No editables needed
            base.DoEditInterface(listing);
        }
        
        public override string GetSummaryLabel(List<ScenPart> allParts)
        {
            return "Rimconemy Intro Sequence";
        }
    }
}
```

- [ ] **Step 2: Create IntroSequencePart XML definition**

File: `mods/05-Rimconemy-Infected-Automation/Defs/Scenarios/IntroSequencePart.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Scenop>
    <ClassName>Rimconemy.InfectedAutomation.Scenarios.ScenPart_IntroSequence</ClassName>
  </Scenop>
</Defs>
```

- [ ] **Step 3: Update SingleSurvivor scenario to include intro part**

File: `mods/02-Rimconemy-Survival-Pack/Defs/Scenario/SingleSurvivor.xml`
```xml
<!-- Add this inside the <sceneDef> tag, after other parts -->
<li Class="Rimconemy.InfectedAutomation.Scenarios.ScenPart_IntroSequence" />
```

- [ ] **Step 4: Commit scenario components**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Scenarios/ScenPart_IntroSequence.cs
git add mods/05-Rimconemy-Infected-Automation/Defs/Scenarios/IntroSequencePart.xml
git add mods/02-Rimconemy-Survival-Pack/Defs/Scenario/SingleSurvivor.xml
git commit -m "feat(infected/survival): add intro sequence scenepart to single survivor scenario"
```

### Task 6: Tutorial Director and State System

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialDirector.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialState.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialStepDef.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialStep.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialTriggerBridge.cs`

**Interfaces:**
- Consumes: Verse.GameComponent, Verse.LetterStack, Verse.ContentFinder, Foundation bridges
- Produces: Tutorial system that shows letter-based popups with portraits

- [ ] **Step 1: Create TutorialState for save/load persistence**

```csharp
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    public class TutorialState : IExposable, ISchemaMigratable
    {
        public HashSet<string> CompletedSteps = new HashSet<string>();
        public HashSet<string> DismissedSteps = new HashSet<string>();
        public bool DismissedAll = false;
        public bool Completed = false;
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref CompletedSteps, "completedSteps", LookValue.Mode.Value);
            Scribe_Collections.Look(ref DismissedSteps, "dismissedSteps", LookValue.Mode.Value);
            Scribe_Values.Look(ref DismissedAll, "dismissedAll", false);
            Scribe_Values.Look(ref Completed, "completed", false);
        }
        
        // ISchemaMigratable implementation - for versioning
        public int Version => 1;
        
        public void MigrateFrom(int version)
        {
            // No migrations needed for v1
        }
        
        public void MarkStepShown(string stepDefName)
        {
            CompletedSteps.Add(stepDefName);
        }
        
        public bool IsStepShown(string stepDefName)
        {
            return CompletedSteps.Contains(stepDefName);
        }
        
        public void DismissStep(string stepDefName)
        {
            DismissedSteps.Add(stepDefName);
        }
        
        public bool IsStepDismissed(string stepDefName)
        {
            return DismissedSteps.Contains(stepDefName);
        }
    }
}
```

- [ ] **Step 2: Create TutorialStepDef definition class**

```csharp
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    public class TutorialStepDef : Def
    {
        public string label;
        public string text;
        public int order = 0;
        public TriggerType triggerType = TriggerType.OnIntroCompleted;
        public string letterDefName = ""; // Reference to defined LetterDef with portrait icon
        public LookTargets lookTargets;
        public List<string> unlockDefs = new List<string>(); // Def names to unlock
        
        public enum TriggerType
        {
            OnIntroCompleted,
            OnCampfireBuilt,
            OnFirstInfectedContact,
            OnResourceCollected,
            OnWallBuilt,
            OnGeneratorBuilt,
            OnTurretBuilt,
            OnOutpostFounded,
            OnTradeDone
        }
    }
}
```

- [ ] **Step 3: Create TutorialStep runtime wrapper**

```csharp
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    public class TutorialStep
    {
        public TutorialStepDef Def;
        private bool triggered = false;
        
        public TutorialStep(TutorialStepDef def)
        {
            Def = def;
        }
        
        public bool CheckTrigger()
        {
            if (triggered) return false;
            
            switch (Def.triggerType)
            {
                case TriggerType.OnIntroCompleted:
                    triggered = TutorialDirector.IsIntroCompleted;
                    break;
                case TriggerType.OnCampfireBuilt:
                    triggered = TutorialTriggerBridge.CampfireBuilt;
                    break;
                case TriggerType.OnFirstInfectedContact:
                    triggered = TutorialTriggerBridge.FirstInfectedContact;
                    break;
                case TriggerType.OnResourceCollected:
                    triggered = TutorialTriggerBridge.ResourceCollected;
                    break;
                case TriggerType.OnWallBuilt:
                    triggered = TutorialTriggerBridge.WallBuilt;
                    break;
                case TriggerType.OnGeneratorBuilt:
                    triggered = TutorialTriggerBridge.GeneratorBuilt;
                    break;
                case TriggerType.OnTurretBuilt:
                    triggered = TutorialTriggerBridge.TurretBuilt;
                    break;
                case TriggerType.OnOutpostFounded:
                    triggered = TutorialTriggerBridge.OutpostFounded;
                    break;
                case TriggerType.OnTradeDone:
                    triggered = TutorialTriggerBridge.TradeDone;
                    break;
            }
            
            return triggered;
        }
        
        public void ShowStep()
        {
            // Use the predefined LetterDef that has the portrait icon set via XML
            var letterDef = DefDatabase<LetterDef>.GetNamed(Def.letterDefName, false);
            if (letterDef == null)
            {
                Log.Error($"Could not find LetterDef '{Def.letterDefName}' for tutorial step {Def.defName}");
                letterDef = LetterDefOf.PositiveEvent; // fallback
            }
            
            Find.LetterStack.ReceiveLetter(Def.label, Def.text, letterDef, Def.lookTargets, 
                                          null, null, null, null, 0, true);
        }
    }
}
```

- [ ] **Step 4: Create TutorialDirector GameComponent**

```csharp
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    public class TutorialDirector : GameComponent
    {
        public TutorialState State;
        private List<TutorialStep> steps = new List<TutorialStep>();
        private int currentStepIndex = 0;
        private bool introCompleted = false;
        
        public TutorialDirector(Game game) : base(game)
        {
            State = new TutorialState();
            InitializeSteps();
        }
        
        public static bool IsIntroCompleted => 
            Find.GetComponent<TutorialDirector>()?.introCompleted ?? false;
        
        public void NotifyIntroCompleted()
        {
            introCompleted = true;
            // Reset to first step if not already started
            if (currentStepIndex == 0 && !State.DismissedAll)
            {
                // Start from beginning
            }
        }
        
        private void InitializeSteps()
        {
            // Load all tutorial step defs and order them
            var allDefs = DefDatabase<TutorialStepDef>.AllDefsListForReading;
            steps = allDefs.OrderBy(d => d.order).ToList();
            
            // Convert to runtime steps
            steps = steps.Select(def => new TutorialStep(def)).ToStep(def)).ToList();
        }
        
        public override void GameComponentTick()
        {
            base.GameComponentTick();
        }
        
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            
            // Skip if tutorial is disabled or completed
            if (ModLister.GetActiveModWithIdentifier("rimconemy.core")?.IsActive == false) return;
            // TODO: Add actual mod setting check for tutorial enabled
            
            if (State.Completed || State.DismissedAll) return;
            if (!introCompleted) return; // Wait for intro to finish
            
            // Check if we've shown all steps
            if (currentStepIndex >= steps.Count)
            {
                State.Completed = true;
                return;
            }
            
            var currentStep = steps[currentStepIndex];
            if (currentStep.CheckTrigger())
            {
                currentStep.ShowStep();
                currentStepIndex++;
                // Mark step as shown in state
                State.MarkStepShown(currentStep.Def.defName);
            }
        }
        
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Subscribe to reset signals if needed
        }
    }
}
```

- [ ] **Step 5: Create TutorialTriggerBridge for cross-package communication**

```csharp
using Verse;
using Rimconemy.Foundation.Bridge;

namespace Rimconemy.InfectedAutomation.Tutorial
{
    public static class TutorialTriggerBridge
    {
        // These will be set by other packages via the foundation bridge
        public static bool CampfireBuilt { get; private set; }
        public static bool FirstInfectedContact { get; private set; }
        public static bool ResourceCollected { get; private set; }
        public static bool WallBuilt { get; private set; }
        public static bool GeneratorBuilt { get; private set; }
        public static bool TurretBuilt { get; private set; }
        public static bool OutpostFounded { get; private set; }
        public static bool TradeDone { get; private set; }
        
        public static void Reset()
        {
            CampfireBuilt = false;
            FirstInfectedContact = false;
            ResourceCollected = false;
            WallBuilt = false;
            GeneratorBuilt = false;
            TurretBuilt = false;
            OutpostFounded = false;
            TradeDone = false;
        }
        
        // Methods to be called by other packages
        public static void OnCampfireBuilt()
        {
            CampfireBuilt = true;
            // Publish via foundation event bridge if needed
            EventBridge.Publish("tutorial.trigger.campfire_built");
        }
        
        public static void OnFirstInfectedContact()
        {
            FirstInfectedContact = true;
            EventBridge.Publish("tutorial.trigger.first_infected_contact");
        }
        
        public static void OnResourceCollected()
        {
            ResourceCollected = true;
            EventBridge.Publish("tutorial.trigger.resource_collected");
        }
        
        public static void OnWallBuilt()
        {
            WallBuilt = true;
            EventBridge.Publish("tutorial.trigger.wall_built");
        }
        
        public static void OnGeneratorBuilt()
        {
            GeneratorBuilt = true;
            EventBridge.Publish("tutorial.trigger.generator_built");
        }
        
        public static void OnTurretBuilt()
        {
            TurretBuilt = true;
            EventBridge.Publish("tutorial.trigger.turret_built");
        }
        
        public static void OnOutpostFounded()
        {
            OutpostFounded = true;
            EventBridge.Publish("tutorial.trigger.outpost_founded");
        }
        
        public static void OnTradeDone()
        {
            TradeDone = true;
            EventBridge.Publish("tutorial.trigger.trade_done");
        }
    }
}
```

- [ ] **Step 6: Create TutorialLetterDefs XML files for each step**

First, let's create the directory and then the files.

Create: `mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/`
Then create the XML files.

File: `mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/TutorialLetterDefs.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- Step 1: Wake Up -->
  <LetterDef>
    <defName>Rimconemy_Tutorial_WakeUp</defName>
    <label>Tag 0 – Die Ankunft</label>
    <icon>UI/HeroArt/Storytellers/RimconemyLarge</icon>
  </LetterDef>
  
  <!-- Step 2: Seek Shelter -->
  <LetterDef>
    <defName>Rimconemy_Tutorial_Shelter</defName>
    <label>Schutz suchen</label>
    <icon>UI/HeroArt/Storytellers/RimconemyLarge</icon>
  </LetterDef>
  
  <!-- Step 3: First Fire -->
  <LetterDef>
    <defName>Rimconemy_Tutorial_Fire</defName>
    <label>Erstes Feuer</label>
    <icon>UI/HeroArt/Storytellers/RimconemyLarge</icon>
  </LetterDef>
  
  <!-- Step 4: They Come -->
  <LetterDef>
    <defName>Rimconemy_Tutorial_Threat</defName>
    <label>Sie kommen</label>
    <icon>UI/HeroArt/Storytellers/RimconemyLarge</icon>
  </LetterDef>
  
  <!-- Step 5: Supplies -->
  <LetterDef>
    <defName>Rimconemy_Tutorial_Supplies</defName>
    <label>Vorräte</label>
    <icon>UI/HeroArt/Storytellers/RimconemyLarge</icon>
  </LetterDef>
</Defs>
```

- [ ] **Step 7: Create TutorialStepDefs XML for the 5 steps**

File: `mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/TutorialStepDefs.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- Step 1: Wake Up (triggered after intro) -->
  <TutorialStepDef>
    <defName>Tutorial_Step1_WakeUp</defName>
    <label>Tag 0 – Die Ankunft</label>
    <text>Du bist nach 5 Jahren ISS zurück... Die Welt hat sich verändert. Dein RimPad aktiviert sich.</text>
    <order>1</order>
    <triggerType>OnIntroCompleted</triggerType>
    <letterDefName>Rimconemy_Tutorial_WakeUp</letterDefName>
  </TutorialStepDef>
  
  <!-- Step 2: Seek Shelter -->
  <TutorialStepDef>
    <defName>Tutorial_Step2_Shelter</defName>
    <label>Schutz suchen</label>
    <text>Baue eine einfache Wand oder ein Bett, um dich vor den Elementen und den Infizierten zu schützen.</text>
    <order>2</order>
    <triggerType>OnWallBuilt</triggerType>
    <letterDefName>Rimconemy_Tutorial_Shelter</letterDefName>
  </TutorialStepDef>
  
  <!-- Step 3: First Fire -->
  <TutorialStepDef>
    <defName>Tutorial_Step3_Fire</defName>
    <label>Erstes Feuer</label>
    <text>Entzünde ein Lagerfeuer, um warm zu bleiben und Nahrung zu kochen. Feuer zieht auch Aufmerksamkeit auf sich - benutze es weise.</text>
    <order>3</order>
    <triggerType>OnCampfireBuilt</triggerType>
    <letterDefName>Rimconemy_Tutorial_Fire</letterDefName>
  </TutorialStepDef>
  
  <!-- Step 4: They Come -->
  <TutorialStepDef>
    <defName>Tutorial_Step4_Threat</defName>
    <label>Sie kommen</label>
    <text>Die Infizierten nähern sich. Bereite deine Verteidigung vor und halte Ausschau nach Bewegungen in der Ferne.</text>
    <order>4</order>
    <triggerType>OnFirstInfectedContact</triggerType>
    <letterDefName>Rimconemy_Tutorial_Threat</letterDefName>
  </TutorialStepDef>
  
  <!-- Step 5: Supplies -->
  <TutorialStepDef>
    <defName>Tutorial_Step5_Supplies</defName>
    <label>Vorräte</label>
    <text>Sammle Ressourcen aus der Umgebung. Holz, Stahl und Nahrung sind entscheidend für dein Überleben.</text>
    <order>5</order>
    <triggerType>OnResourceCollected</triggerType>
    <letterDefName>Rimconemy_Tutorial_Supplies</letterDefName>
  </TutorialStepDef>
</Defs>
```

- [ ] **Step 8: Commit tutorial system components**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialDirector.cs
git add mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialState.cs
git add mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialStepDef.cs
git add mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialStep.cs
git add mods/05-Rimconemy-Infected-Automation/Source/Tutorial/TutorialTriggerBridge.cs
git add mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/TutorialLetterDefs.xml
git add mods/05-Rimconemy-Infected-Automation/Defs/TutorialSteps/TutorialStepDefs.xml
git commit -m "feat(infected): implement tutorial director, state, step defs, bridge, and letter defs"
```

### Task 7: Integrate Tutorial Triggers with Survival Package

**Files:**
- Modify: `mods/02-Rimconemy-Survival-Pack/Source/Survival/CampfireManager.cs` (or similar)
- Modify: `mods/02-Rimconemy-Survival-Pack/Source/Survival/WallBuilder.cs` (or similar)
- Modify: `mods/02-Rimconemy-Survival-Pack/Source/Survival/ResourceCollector.cs` (or similar)
- Create: `mods/02-Rimconemy-Survival-Pack/Source/Bridge/SurvivalTutorialBridge.cs`

**Interfaces:**
- Consumes: Survival package systems
- Produces: Calls to TutorialTriggerBridge when relevant events occur

- [ ] **Step 1: Create SurvivalTutorialBridge to register callbacks**

```csharp
using Verse;
using Rimworld;
using Rimconemy.Foundation.Bridge;
using Rimconemy.InfectedAutomation.Tutorial;

namespace Rimconemy.SurvivalPack.Bridge
{
    public static class SurvivalTutorialBridge
    {
        public static void Initialize()
        {
            // Subscribe to survival events and forward to tutorial bridge
            // Example: Campfire built event
            CampfireManager.OnCampfireBuilt += () => TutorialTriggerBridge.OnCampfireBuilt();
            
            // Wall built event
            WallBuilder.OnWallBuilt += () => TutorialTriggerBridge.OnWallBuilt();
            
            // Resource collected
            ResourceCollector.OnResourceCollected += () => TutorialTriggerBridge.OnResourceCollected();
            
            // Register capability
            CapabilityAudit.RegisterCapability("rimconemy.survivalpack", "survival.tutorial.triggers");
        }
    }
}
```

- [ ] **Step 2: Update SurvivorPackage initializer to call the bridge**

Find or create the survivor package initialization class and add:

```csharp
SurvivalTutorialBridge.Initialize();
```

- [ ] **Step 3: Implement actual event triggers in survival systems**

This requires looking at the survival package code to see where to inject the calls.
Since we don't have the exact survival package code, we'll create placeholder implementations
that would need to be adapted to the actual survival package.

For the sake of the plan, we'll assume we can modify:

- CampfireManager: Call `TutorialTriggerBridge.OnCampfireBuilt()` when a campfire is successfully built
- WallBuilder: Call `TutorialTriggerBridge.OnWallBuilt()` when a wall is completed
- ResourceCollector: Call `TutorialTriggerBridge.OnResourceCollected()` when resources are harvested/mined

We'll show the pattern for one:

In `CampfireManager.cs` (hypothetical):
```c
public static class CampfireManager
{
    public static event System.Action OnCampfireBuilt;
    
    public static void TryBuildCampfire(...)
    {
        // ... existing build logic ...
        if (successfullyBuilt)
        {
            OnCampfireBuilt?.Invoke();
        }
    }
}
```

Similar patterns for other systems.

- [ ] **Step 4: Commit survival bridge integration**

```bash
git add mods/02-Rimconemy-Survival-Pack/Source/Bridge/SurvivalTutorialBridge.cs
git add mods/02-Rimconemy-Survival-Pack/Source/Survival/CampfireManager.cs  # modified
git add mods/02-Rimconemy-Survival-Pack/Source/Survival/WallBuilder.cs    # modified
git add mods/02-Rimconemy-Survival-Pack/Source/Survival/ResourceCollector.cs # modified
git commit -m "feat(survival): integrate tutorial triggers for campfire, walls, and resource collection"
```

### Task 8: Implement Infrastructure, Economy, Threat Triggers (Packages 03-04)

**Files:**
- Similar to task 7 but for packages 03 and 04
- Create bridge classes in each package
- Modify relevant systems to trigger events

**Interfaces:**
- Consumes: Package 03 (Scavenger) and 04 (Economy) systems
- Produces: Calls to TutorialTriggerBridge for generator, turret, outpost, trade events

- [ ] **Step 1: Create ScavengerTutorialBridge (Package 03)**

```csharp
using Verse;
using Rimworld;
using Rimconemy.Foundation.Bridge;
using Rimconemy.InfectedAutomation.Tutorial;

namespace Rimconemy.ScavengerPack.Bridge
{
    public static class ScavengerTutorialBridge
    {
        public static void Initialize()
        {
            // Generator built
            PowerGrid.OnGeneratorBuilt += () => TutorialTriggerBridge.OnGeneratorBuilt();
            
            // Turret built
            TurretBuilder.OnTurretBuilt += () => TutorialTriggerBridge.OnTurretBuilt();
            
            // Register capability
            CapabilityAudit.RegisterCapability("rimconemy.scavengerpack", "scavenger.tutorial.triggers");
        }
    }
}
```

- [ ] **Step 2: Create EconomyTutorialBridge (Package 04)**

```csharp
using Verse;
using Rimworld;
using Rimconemy.Foundation.Bridge;
using Rimconemy.InfectedAutomation.Tutorial;

namespace Rimconemy.EconomyPack.Bridge
{
    public static class EconomyTutorialBridge
    {
        public static void Initialize()
        {
            // Outpost founded
            OutpostManager.OnOutpostFounded += () => TutorialTriggerBridge.OnOutpostFounded();
            
            // Trade completed
            TradeManager.OnTradeCompleted += () => TutorialTriggerBridge.OnTradeDone();
            
            // Register capability
            CapabilityAudit.RegisterCapability("rimconemy.economypack", "economy.tutorial.triggers");
        }
    }
}
```

- [ ] **Step 3: Initialize bridges in package initializers**

Add calls to `ScavengerTutorialBridge.Initialize()` and `EconomyTutorialBridge.Initialize()` in their respective package initializers.

- [ ] **Step 4: Implement triggers in scavenge/economy systems** (similar to survival task)

- [ ] **Step 5: Commit scavenge and economy bridge integrations**

```bash
git add mods/03-Rimconemy-Scavenger-Pack/Source/Bridge/ScavengerTutorialBridge.cs
git add mods/03-Rimconemy-Scavenger-Pack/Source/PowerGrid.cs  # modified example
git add mods/03-Rimconemy-Scavenger-Pack/Source/TurretBuilder.cs  # modified example
git add mods/04-Rimconemy-Economy-Pack/Source/Bridge/EconomyTutorialBridge.cs
git add mods/04-Rimconemy-Economy-Pack/Source/OutpostManager.cs  # modified
git add mods/04-Rimconemy-Economy-Pack/Source/TradeManager.cs   # modified
git commit -m "feat(scavenger/economy): integrate tutorial triggers for generators, turrets, outposts, and trade"
```

### Task 9: RimPad Content Implementation (Snapshot Binding)

**Files:**
- Modify: `mods/01-Rimconemy-Foundation/Source/UI/RimPadWindow.cs` (add real content)
- Create: `mods/01-Rimconemy-Foundation/Source/Snapshot/SurvivalSnapshot.cs`
- Create: `mods/01-Rimconemy-Foundation/Source/Snapshot/InfrastructureSnapshot.cs`
- etc. for each tab
- Modify survival/infrastructure/etc. systems to update snapshots

**Interfaces:**
- Consumes: Foundation snapshots, package systems
- Produces: Real-time data display in RimPad tabs

- [ ] **Step 1: Create snapshot interfaces and base class**

```csharp
namespace Rimconemy.Foundation.Snapshot
{
    public abstract class ISnapshot
    {
        public abstract void Update();
        public abstract void DrawContents(Rect rect);
    }
}
```

- [ ] **Step 2: Create SurvivalSnapshot**

```csharp
using Verse;
using RimWorld;
using Rimconemy.Foundation.Bridge;

namespace Rimconemy.Foundation.Snapshot
{
    public class SurvivalSnapshot : ISnapshot
    {
        private float hungerLevel;
        private float restLevel;
        private float joyLevel;
        private int threatLevel;
        
        public override void Update()
        {
            // Get data from survival package via foundation bridge or direct calls
            // Example:
            // hungerLevel = SurvivalStats.GetAverageHunger();
            // This would need to be implemented based on actual survival package
            
            // Placeholder implementation
            hungerLevel = 0.5f;
            restLevel = 0.6f;
            joyLevel = 0.3f;
            threatLevel = Find.TickManager.TicksGame % 100; // fake
        }
        
        public override void DrawContents(Rect rect)
        {
            float lineHeight = 24f;
            float y = 0f;
            
            Widgets.Label(new Rect(0, y, rect.width, lineHeight), 
                         $"Hunger: {hungerLevel:P0}");
            y += lineHeight;
            Widgets.Label(new Rect(0, y, rect.width, lineHeight), 
                         $"Rest: {restLevel:P0}");
            y += lineHeight;
            Widgets.Label(new Rect(0, y, rect.width, lineHeight), 
                         $"Joy: {joyLevel:P0}");
            y += lineHeight;
            Widgets.Label(new Rect(0, y, rect.width, lineHeight), 
                         f"Threat Level: {threatLevel}");
        }
    }
}
```

- [ ] **Step 3: Create InfrastructureSnapshot, EconomySnapshot, etc.** (similar pattern)

- [ ] **Step 4: Update RimPadWindow to use snapshots**

```csharp
using Rimconemy.Foundation.Snapshot;
// ... other using

namespace Rimconemy.Foundation.UI
{
    public class RimPadWindow : Window
    {
        // ... existing code
        
        private ISnapshot survivalSnapshot;
        private ISnapshot infrastructureSnapshot;
        // ... others
        
        public override void PostOpen()
        {
            base.PostOpen();
            InitializeTabs();
            InitializeSnapshots();
        }
        
        private void InitializeSnapshots()
        {
            survivalSnapshot = new SurvivalSnapshot();
            infrastructureSnapshot = new InfrastructureSnapshot();
            // ... initialize others
        }
        
        private void InitializeTabs()
        {
            var tabs = new List<RimPadTabRecord>
            {
                new RimPadTabRecord(RimPadTab.Survival, 
                                   "Survival", 
                                   rect => survivalSnapshot.DrawContent(rect)),
                new RimPadTabRecord(RimPadTab.Infrastructure, 
                                   "Infrastructure", 
                                   rect => infrastructureSnapshot.DrawContent(rect)),
                // ... others
            };
            
            RimPadTabDrawer.SetTabs(tabs);
        }
        
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            
            // Update snapshots periodically
            if (Find.TickManager.TicksGame % 60 == 0) // Update every second
            {
                survivalSnapshot.Update();
                infrastructureSnapshot.Update();
                // ... update others
            }
        }
        
        // ... rest of class
    }
}
```

- [ ] **Step 5: Implement snapshot updates in respective packages** (similar to tutorial triggers)

- [ ] **Step 6: Commit snapshots and RimPad content**

```bash
git add mods/01-Rimconemy-Foundation/Source/Snapshot/SurvivalSnapshot.cs
git add mods/01-Rimconemy-Foundation/Source/Snapshot/InfrastructureSnapshot.cs
git add mods/01-Rimconemy-Foundation/Source/Snapshot/EconomySnapshot.cs
git add mods/01-Rimconemy-Foundation/Source/Snapshot/ThreatSnapshot.cs
git add mods/01-Rimconemy-Foundation/Source/Snapshot/DiagnosticsSnapshot.cs
git add mods/01-Rimconemy-Foundation/Source/UI/RimPadWindow.cs  # modified
git commit -m "feat(foundation): implement snapshot system and bind to RimPad tabs"
```

### Task 10: WeatherDef and Scenario Updates

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Defs/WeatherDefs/Rimconemy_StormAtmosphere.xml`
- Modify: `mods/02-Rimconemy-Survival-Pack/Defs/Scenario/SingleSurvivor.xml` (add weather part)
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Scenarios/ScenPart_RimconemyStartEnemies.cs`
- Modify: `mods/02-Rimconemy-Survival-Pack/Defs/Scenario/SingleSurvivor.xml` (add enemy scenepart)

**Interfaces:**
- Consumes: None
- Produces: Storm weather definition, guaranteed infected enemies, scenario parts

- [ ] **Step 1: Create StormAtmosphere WeatherDef**

File: `mods/05-Rimconemy-Infected-Automation/Defs/WeatherDefs/Rimconemy_StormAtmosphere.xml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <WeatherDef>
    <defName>Rimconemy_StormAtmosphere</defName>
    <label>Rimconemy-Sturm</label>
    <!-- Storm atmosphere: rain and wind but no lightning -->
    <rainRate>0.8</rainRate>
    <windSpeedFactor>1.5</windSpeedFactor>
    <!-- No lightning - leave eventMakers empty or omit -->
    <eventMakers />
    <!-- Use default weather worker -->
    <workerClass>Verse.WeatherWorker</workerClass>
  </WeatherDef>
</Defs>
```

- [ ] **Step 2: Create ScenPart_RimconemyStartEnemies for guaranteed infected**

```csharp
using Verse;
using RimWorld;
using Rimworld.Planet;
using System;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    public class ScenPart_RimconemyStartEnemies : ScenPart
    {
        // Configuration
        public if needed
        public IntRange enemyCountRange = new IntRange(1, 3);
        public PawnKindDef enemyKindDef;
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enemyCountRange, "enemyCountRange");
            Scribe_Defs.Look(ref enemyKindDef, "enemyKindDef");
        }
        
        public override void GenerateIntoMap(Map map)
        {
            base.GenerateIntoMap(map);
            
            // Ensure enemyKindDef is set
            if (enemyKindDef == null)
            {
                enemyKindDef = DefDatabase<PawnKindDef>.GetNamed("Rimconemy_InfectedRavager", true);
            }
            
            // Calculate how many enemies to spawn
            int count = enemyCountRange.RandomInRange;
            
            // Ensure at least 1
            count = Math.Max(1, count);
            
            var faction = InfectedFactionUtility.EnsureHiddenInfectedFaction();
            
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                // Try to find a good spot, fallback to random
                if (!CellFinder.TryFindRandomEdgeCellWith((int c) => 
                    !GetCenterCell(map).AdjacentTo8Way(c) && 
                    !c.Fogged(map) && 
                    c.Standable(map), 
                    map, out cell))
                {
                    cell = CellFinder.RandomCell(map);
                }
                
                var pawn = PawnGenerator.GeneratePawn(enemyKindDef, faction);
                GenSpawn.Spawn(pawn, cell, map);
            }
        }
        
        private IntVec3 GetCenterCell(Map map)
        {
            return new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
        }
        
        public override string GetSummaryLabel(List<ScenPart> allParts)
        {
            return "Rimconemy Garantierte Infizierte";
        }
    }
}
```

- [ ] **Step 3: Update SingleSurvivor scenario to include weather and enemy parts**

File: `mods/02-Rimconemy-Survival-Pack/Defs/Scenario/SingleSurvivor.xml`
```xml
<!-- Add inside <sceneDef> -->
<!-- Weather part for storm atmosphere -->
<li Class="ScenPart_GameCondition">
  <gameCondition>Rimconemy_StormAtmosphere</gameCondition>
</li>

<!-- Guaranteed infected enemies -->
<li Class="Rimconemy.InfectedAutomation.Scenarios.ScenPart_RimconemyStartEnemies" />
```

- [ ] **Step 4: Commit weather and scenario components**

```bash
git add mods/05-Rimconemy-Infected-Automation/Defs/WeatherDefs/Rimconemy_StormAtmosphere.xml
git add mods/05-Rimconemy-Infected-Automation/Source/Scenarios/ScenPart_RimconemyStartEnemies.cs
git add mods/02-Rimconemy-Survival-Pack/Defs/Scenario/SingleSurvivor.xml
git commit -m "feat(infected/survival): add storm atmosphere weatherdef and guaranteed infected enemies scenepart"
```

### Task 11: Save/Load and Migration Support

**Files:**
- Ensure TutorialState implements ISchemaMigratable (already done)
- Ensure any new Defs have proper defNames and versions
- Test save/load scenarios

**Interfaces:**
- Consumes: RimWorld save/load system
- Produces: Persistent tutorial state and RimPad position

- [ ] **Step 1: Verify TutorialState migration implementation**

Already implemented in Task 6, Step 1. We have:
```csharp
public int Version => 1;
public void MigrateFrom(int version) { /* no-op for v1 */ }
```

- [ ] **Step 2: Verify RimPad window position persistence**

RimPadWindow inherits from Window, which automatically saves/restores its windowRect via RimWorld's window system. No additional code needed.

- [ ] **Step 3: Test save/load with tutorial progress**

Create a test scenario:
1. Start new game with Rimconemy scenario
2. Go through intro
3. Complete a tutorial step (e.g., build a campfire)
4. Save game
5. Load game
6. Verify tutorial state is preserved and next step is correct
7. Verify RimPad position is restored

- [ ] **Step 4: Commit any necessary adjustments**

```bash
git commit -m "test: verify save/load functionality for tutorial state and rimpad position"
```

### Task 12: Integration Testing and Polishing

**Files:**
- Create test scripts if needed
- Adjust timing, balances, visual polish

**Interfaces:**
- Consumes: All implemented systems
- Produces: Polished, working feature

- [ ] **Step 1: Test intro sequence timing and flow**

- [ ] **Step 2: Test zombie horde spawn/despawn**

- [ ] **Step 3: Test tutorial triggers fire correctly**

- [ ] **Step 4: Test RimPad functionality and snapshot updates**

- [ ] **Step 5: Test weather effects (no lightning)**

- [ ] **Step 6: Test guaranteed infected spawn**

- [ ] **Step 7: Test save/load persistence**

- [ ] **Step 8: Polish visuals (RimPad theme, text formatting, etc.)**

- [ ] **Step 9: Commit final adjustments**

```bash
git commit -m "test: integrate and polish all cinematic intro and rimpad guide features"
```

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-08-05-cinematic-intro-rimpad-guide.md`. Two execution options:**

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**

**If Subagent-Driven chosen:**
- **REQUIRED SUB-SKILL:** Use superpowers:subagent-driven-development
- Fresh subagent per task + two-stage review

**If Inline Execution chosen:**
- **REQUIRED SUB-SKILL:** Use superpowers:executing-plans
- Batch execution with checkpoints for review

**Recommendation:** Subagent-driven development for better isolation and faster iteration on individual components.