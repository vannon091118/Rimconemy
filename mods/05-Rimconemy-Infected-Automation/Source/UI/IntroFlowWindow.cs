using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Scenarios;

namespace Rimconemy.InfectedAutomation.UI
{
    /// <summary>
    /// Cinematic intro sequence with flow text, deterministic camera cuts, and a
    /// short visual-only infected flash. Real time is used because the window
    /// pauses the game and game ticks may therefore stop advancing.
    /// </summary>
    public class IntroFlowWindow : Window
    {
        private const float TextPhaseSeconds = 10f;
        private const float CameraPhaseSeconds = 6.666f;
        private const float ZombieFlashSeconds = 3f;

        public override Vector2 InitialSize => new Vector2(1920f, 1080f);

        private readonly List<string> flowTexts;
        private readonly List<float> phaseDurations;
        private readonly List<IntVec3> cameraPositions = new List<IntVec3>();
        private readonly List<Pawn> spawnedZombies = new List<Pawn>();

        private float startTime;
        private float zombieFlashStartTime;
        private int lastPhaseIndex = -1;
        private bool zombieFlashStarted;
        private bool zombieFlashCompleted;

        public IntroFlowWindow()
        {
            forcePause = true;
            doCloseButton = false;
            doCloseX = false;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
            draggable = false;

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

            phaseDurations = new List<float>();
            for (int i = 0; i < flowTexts.Count; i++)
            {
                phaseDurations.Add(TextPhaseSeconds);
                if (i < flowTexts.Count - 1)
                    phaseDurations.Add(CameraPhaseSeconds);
            }
            phaseDurations.Add(ZombieFlashSeconds);
        }

        public override void PostOpen()
        {
            base.PostOpen();
            startTime = Time.realtimeSinceStartup;

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (Find.CurrentMap != null)
                    InitializeCameraPositions(Find.CurrentMap);
            });
        }

        private void InitializeCameraPositions(Map map)
        {
            cameraPositions.Clear();
            cameraPositions.Add(map.Center);

            int padding = 10;
            int maxX = Mathf.Max(0, map.Size.x - padding - 1);
            int maxZ = Mathf.Max(0, map.Size.z - padding - 1);
            int minX = Mathf.Min(padding, maxX);
            int minZ = Mathf.Min(padding, maxZ);

            cameraPositions.Add(new IntVec3(minX, 0, minZ));
            cameraPositions.Add(new IntVec3(maxX, 0, minZ));
            cameraPositions.Add(new IntVec3(minX, 0, maxZ));
            cameraPositions.Add(new IntVec3(maxX, 0, maxZ));
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, Color.black);

            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - startTime);
            int phaseIndex = GetPhaseIndex(elapsed);
            EnterPhaseIfNeeded(phaseIndex);

            int textPhaseCount = flowTexts.Count * 2 - 1;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            if (phaseIndex < textPhaseCount && phaseIndex % 2 == 0)
            {
                int textIndex = phaseIndex / 2;
                Widgets.Label(
                    new Rect(20f, 20f, inRect.width - 40f, inRect.height - 40f),
                    flowTexts[textIndex]);
            }
            else if (phaseIndex == phaseDurations.Count - 1)
            {
                Widgets.Label(
                    new Rect(20f, 20f, inRect.width - 40f, inRect.height - 40f),
                    "Die Infizierten zeigen sich...");
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private int GetPhaseIndex(float elapsed)
        {
            float accumulated = 0f;
            for (int i = 0; i < phaseDurations.Count; i++)
            {
                accumulated += phaseDurations[i];
                if (elapsed < accumulated)
                    return i;
            }

            return phaseDurations.Count - 1;
        }

        private void EnterPhaseIfNeeded(int phaseIndex)
        {
            if (phaseIndex == lastPhaseIndex)
                return;

            lastPhaseIndex = phaseIndex;
            int textPhaseCount = flowTexts.Count * 2 - 1;
            if (phaseIndex < textPhaseCount && phaseIndex % 2 == 1 && cameraPositions.Count > 0)
            {
                int cameraIndex = (phaseIndex / 2) % cameraPositions.Count;
                Find.CameraDriver.JumpToCurrentMapLoc(cameraPositions[cameraIndex]);
            }

            if (phaseIndex == phaseDurations.Count - 1)
                StartZombieFlashSequence();
        }

        private void StartZombieFlashSequence()
        {
            if (zombieFlashStarted)
                return;

            zombieFlashStarted = true;
            zombieFlashStartTime = Time.realtimeSinceStartup;
            SpawnZombieHorde();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();

            if (!zombieFlashStarted || zombieFlashCompleted)
                return;

            if (Time.realtimeSinceStartup - zombieFlashStartTime < ZombieFlashSeconds)
                return;

            DespawnZombieHorde();
            zombieFlashCompleted = true;
            Close();
        }

        private void SpawnZombieHorde()
        {
            Map map = Find.CurrentMap;
            if (map == null)
                return;

            Faction faction = InfectedFactionUtility.EnsureHiddenInfectedFaction();
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Rimconemy_InfectedRavager");
            if (faction == null || kind == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] IntroFlowWindow: infected flash skipped because faction or pawn kind is missing.");
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                IntVec3 cell = CellFinder.RandomEdgeCell(map);
                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                pawn.mindState.duty = null;
                GenSpawn.Spawn(pawn, cell, map);
                spawnedZombies.Add(pawn);
            }

            if (spawnedZombies.Count > 0)
                Find.CameraDriver.JumpToCurrentMapLoc(spawnedZombies[0].Position);
        }

        private void DespawnZombieHorde()
        {
            foreach (Pawn pawn in spawnedZombies)
            {
                if (pawn != null && pawn.Spawned)
                    pawn.Destroy(DestroyMode.Vanish);
            }

            spawnedZombies.Clear();
        }

        public override void PreClose()
        {
            DespawnZombieHorde();
            base.PreClose();
        }
    }
}
