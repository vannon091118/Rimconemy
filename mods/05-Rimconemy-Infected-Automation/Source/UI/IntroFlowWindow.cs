using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Scenarios;

namespace Rimconemy.InfectedAutomation.UI
{
    /// <summary>
    /// IntroFlowWindow - Cinematic intro sequence with black screen, flow text, camera cuts, and zombie horde flash.
    /// Force pauses the game and prevents camera motion during the intro.
    /// </summary>
    public class IntroFlowWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(1920f, 1080f);
        
        private int startTick;
        private List<string> flowTexts;
        private List<IntVec3> cameraPositions;
        private List<int> phaseDurations; // ticks per phase
        
        private bool zombieFlashStarted = false;
        private int zombieFlashStartTick = 0;
        private bool zombieFlashCompleted = false;
        private List<Pawn> spawnedZombies = new List<Pawn>();
        
        public IntroFlowWindow()
        {
            // Set base Window fields for cinematic intro
            forcePause = true;
            doCloseButton = false;
            doCloseX = false;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
            draggable = false;
            
            // Initialize flow text (ISS return story)
            flowTexts = new List<string>
            {
                "Nach 5 Jahren außerhalb der Erdatmosphäre...",
                "Du kehrst schließlich von der ISS zurück zur Erde.",
                "Die Schwerelosigkeit weicht der vertrauten Schwere.",
                "Dein Herz schlägt schneller beim Gedanken an Familie und Freunde.",
                "Doch etwas fühlt sich... falsch an. Die Stille ist zu perfekt.",
                "Als du durch die Atmosphäre brichst, siehst du sie.",
                "Am Horizont bewegen sich Gestalten - langsam, unheimlich.",
                "Die Infizierten haben die Städte übernommen.",
                "Aber du bist bereit. Dein RimPad aktiviert sich.",
                "Es ist Zeit zu überleben."
            };
            
            // Initialize camera positions (will be set after map generation)
            cameraPositions = new List<IntVec3>();
            phaseDurations = new List<int>();
            
            // Calculate phases: text blocks + camera cuts + zombie flash
            int textBlockTicks = 300; // ~10 seconds per text block at 30 ticks/sec
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
            cameraPositions.Add(new IntVec3(edgePadding, 0, edgePadding)); // Southwest
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
            int totalTextAndCameraPhases = flowTexts.Count * 2 - 1; // Text phases + camera cut phases
            
            if (phaseIndex < totalTextAndCameraPhases) // Text and camera cut phases
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
            else if (phaseIndex == totalTextAndCameraPhases) // Zombie flash phase
            {
                // Draw hint text
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(new Rect(20f, 20f, inRect.width - 40f, inRect.height - 40f), 
                             "Die Infizierten zeigen sich...");
                Text.Anchor = TextAnchor.UpperLeft;
                
                // Trigger zombie flash on first frame of this phase
                if (!zombieFlashStarted && elapsed >= accumulatedTicks)
                {
                    StartZombieFlashSequence();
                }
            }
            else
            {
                // Sequence complete - close window and signal tutorial start
                if (Find.TickManager.TicksGame - startTick >= accumulatedTicks + phaseDurations[phaseIndex])
                {
                    // Trigger zombie flash sequence if not already done
                    if (!zombieFlashCompleted)
                    {
                        StartZombieFlashSequence();
                    }
                }
            }
        }
        
        private void StartZombieFlashSequence()
        {
            if (zombieFlashStarted) return;
            
            zombieFlashStarted = true;
            zombieFlashStartTick = Find.TickManager.TicksGame;
        }
        
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            
            if (zombieFlashStarted && !zombieFlashCompleted)
            {
                int elapsed = Find.TickManager.TicksGame - zombieFlashStartTick;
                if (elapsed == 0) // First update after start - spawn zombies
                {
                    SpawnZombieHorde();
                }
                else if (elapsed >= 180) // 3 seconds passed (60 ticks/sec * 3 = 180)
                {
                    DespawnZombieHorde();
                    zombieFlashCompleted = true;
                    // Signal completion to tutorial director (will be implemented in Task 6)
                    // var tutorialDirector = Current.Game.GetComponent<Rimconemy.InfectedAutomation.Tutorial.TutorialDirector>();
                    // if (tutorialDirector != null)
                    // {
                    //     tutorialDirector.NotifyIntroCompleted();
                    // }
                    // Close this window
                    Close();
                }
            }
        }
        
        private void SpawnZombieHorde()
        {
            if (Find.CurrentMap == null) return;
            
            var faction = InfectedFactionUtility.EnsureHiddenInfectedFaction();
            var kind = DefDatabase<PawnKindDef>.GetNamed("Rimconemy_InfectedRavager", true);
            
            for (int i = 0; i < 4; i++)
            {
                var cell = CellFinder.RandomEdgeCell(Find.CurrentMap);
                var pawn = PawnGenerator.GeneratePawn(kind, faction);
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