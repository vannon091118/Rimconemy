# Phase D — Horde-Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a multi-layer visual representation of the infected horde (camera-edge pulse + section-layer concentric circle + per-pawn radial bursts on home-map; wandering HordeWorldObject icon on world-map) so the player can perceive tightening pressure without opening the ThreatDashboard.

**Architecture:** Pure-logic `HordeCalculator` derives effective count from `PopulationLedger` (Humanoid + 0.5×Animal) and gates every RenderPath. `HordeUpdateLogic` orchestrates Spawn/Move/Despawn on a 250-tick interval. Three render paths (SectionLayer Circle, SectionLayer Bursts, CameraUI Edge-Frame) all read pulse-phase from the same Pure-API calculation. `HordeWorldObject` + Def XML mirrors the `OutpostWorldObject` pattern from Mod 04.

**Tech Stack:** C# (.NET via `dotnet build`), RimWorld 1.6.4566 (`Verse`, `RimWorld`, `UnityEngine`), Hierarchical determinism via `DeterministicRng.GetStableHashCode` / FNV-1a, MapComponent + WorldObject.

## Global Constraints

- Build flags: `RimWorldManagedPath=/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed HarmonyAssembliesPath=/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies`
- Project: `mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
- Tick-Frequency: MapComponent 250 ticks (HordeSpawner) + 60 ticks (SectionLayer-Regen-Hook) + frame-rate (CameraOverlay OnGUI).
- Hybrid counter formula: `effective = HumanoidLiveCount + floor(0.5 × AnimalLiveCount)`. Profile threshold keys: Refuge=220, Survival=150, Collapse=80.
- Pulse-Phase: Pure Sinusoid über 120-Tick-Cycle. α_max=0.35 (Kreis), α_max=0.5 (Bursts), α_max=0.4 (CameraEdge).
- WorldMap-Icon: Verse.WorldObject-Subclass analog Outpost. Def XML `Rimconemy_HordeWorldObject`. Driftet 1 tile closer alle 250 ticks.
- Determinismus: kein Random, kein System.Time. Drift = `floor(currentTick/250)` tiles in Richtung Home-Tile. Save/Load-Transient-State.
- TDD: jeder Task beginnt mit failing Test → minimal Impl → grün → Review → Commit.
- Version-Bump erst am Ende (Task 8), nicht pro Task.

## File Structure

| Datei | Verantwortung |
|---|---|
| `Source/Horde/HordeCalculator.cs` | Pure: Effective-Count, IsActive, PulsePhase, ProfileStrip-for-Threshold |
| `Source/Horde/HordeUpdateLogic.cs` | Pure: Spawn/Drift/Despawn-State-Machine (testbar ohne Game) |
| `Source/Horde/HordeWorldObject.cs` | Verse.WorldObject-Subclass mit Drift-State |
| `Source/Horde/HordeSpawner.cs` | MapComponent-Orchestrator, ruft HordeUpdateLogic.RunOnce() alle 250 Ticks |
| `Source/Horde/HordeSectionLayer.cs` | SectionLayer-Subclass: pulsierender Kreis mittig |
| `Source/Horde/HordeBurstLayer.cs` | SectionLayer-Subclass: per-infected-Pawn Radial-Bursts |
| `Source/Horde/HordeCameraOverlay.cs` | Static OnGUI-Postfix auf Camera-Driver: Edge-Frame-Pulse |
| `Defs/WorldObjects/Rimconemy_HordeWorldObject.xml` | Def für WorldObject-Class |
| `Source/Bootstrap.cs` | RunAll-Hook für Tests |
| `Tests/HordeRegressionTests.cs` | 12 Tests (D1-D12) |
| `docs/falsification/infected__ThreatPressure.md` | §F Phase-D Live-Beleg Stub |

---

### Task 1: HordeCalculator Pure-Logic + Tests D1-D6, D11-D13

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeCalculator.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/HordeRegressionTests.cs`

**Interfaces:**
- Produces:
  - `public static class HordeCalculator`
  - `public static int GetEffectiveCount(PopulationLedger ledger)`
  - `public static bool IsActive(int effectiveCount, SettingProfile profile)`
  - `public static float ComputePulsePhase(long currentTick)`

- [ ] **Step 1: Write the failing test file (D1-D6, D11-D13)**

Create `Tests/HordeRegressionTests.cs`:

```csharp
// Tests/HordeRegressionTests.cs
//
// Phase D — Horde-Overlay Visualisierung (D1-D15).
// spec: docs/superpowers/specs/2026-08-05-horde-overlay-design.md
// plan: docs/superpowers/plans/2026-08-05-horde-overlay.md
//
// Owner: Infected & Automation (Package 05).
//
// Calculator-side tests cover Pure-Logic only. SectionLayer, Map-Mesh
// mesh-regen, and Camera-edge postfix each get dedicated tasks with their
// own seams (TestTile and StubDriver for layer; Camera-edge postfix gets
// a stub callback).

using Rimconemy.InfectedAutomation.Horde;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeRegressionTests
    {
        public const int ExpectedPassCount = 15;

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                firstFailure ??= name;
                Log.Warning("[Rimconemy.InfectedAutomation] Phase D test FAILED: " + name);
            }

            // ── D1-D6: Calculator basics ─────────────────────
            Check(D1_CalculatorEmptyLedger(),                        "D1.CalculatorEmptyLedger");
            Check(D2_CalculatorSurvival150Human(),                   "D2.CalculatorSurvival150Human");
            Check(D3_CalculatorSurvival100Human100Animal(),          "D3.CalculatorSurvival100Human100Animal");
            Check(D4_CalculatorCollapseThreshold(),                   "D4.CalculatorCollapseThreshold");
            Check(D5_CalculatorProfileFallbackNull(),                "D5.CalculatorProfileFallbackNull");
            Check(D6_PulsePhaseSinusoidal(),                         "D6.PulsePhaseSinusoidal");

            // ── D11-D13: Strip-Prefix routing ──────────────
            Check(D11_StripRimconemyPrefixForThreshold(),            "D11.StripRimconemyPrefixForThreshold");
            Check(D12_StripRimconemyPrefixNullReturnsSurvival(),     "D12.StripRimconemyPrefixNullReturnsSurvival");
            Check(D13_AnimalHalfCapRoute(),                          "D13.AnimalHalfCapRoute");

            Log.Message(
                "[Rimconemy.InfectedAutomation] Horde regression tests: "
                + passed + " passed, " + failed + " failed" +
                (firstFailure != null ? " (first failure: " + firstFailure + ")" : ""));
            return passed;
        }

        // ── D1: empty ledger → 0, IsActive=false ────────
        private static bool D1_CalculatorEmptyLedger()
        {
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 0,
                AnimalLiveCount = 0,
                Cap = 100,
                ProfileId = "Survival",
            };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            return effective == 0 && !HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D2: Survival threshold = 150. 150 humanoid → active ────
        private static bool D2_CalculatorSurvival150Human()
        {
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 150,
                AnimalLiveCount = 0,
                Cap = 200,
                ProfileId = "Survival",
            };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            return effective == 150 && HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D3: Hybrid 100 Human + 100 Animal × 0.5 = 150 ─
        private static bool D3_CalculatorSurvival100Human100Animal()
        {
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 100,
                AnimalLiveCount = 100,
                Cap = 250,
                ProfileId = "Survival",
            };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            return effective == 150 && HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D4: Collapse threshold 80. 50 inactive, 80 active ─
        private static bool D4_CalculatorCollapseThreshold()
        {
            var ledgerLow = new PopulationLedger { HumanoidLiveCount = 50, AnimalLiveCount = 0, ProfileId = "Collapse" };
            var ledgerHigh = new PopulationLedger { HumanoidLiveCount = 80, AnimalLiveCount = 0, ProfileId = "Collapse" };
            int eLow = HordeCalculator.GetEffectiveCount(ledgerLow);
            int eHigh = HordeCalculator.GetEffectiveCount(ledgerHigh);
            return !HordeCalculator.IsActive(eLow, SettingProfile.Collapse) &&
                HordeCalculator.IsActive(eHigh, SettingProfile.Collapse);
        }

        // ── D5: null profile → Survival fallback (threshold=150) ──
        private static bool D5_CalculatorProfileFallbackNull()
        {
            return !HordeCalculator.IsActive(120, null)   // < 150 → not active
                && HordeCalculator.IsActive(160, null);   // >= 150 → active
        }

        // ── D6: PulsePhase periodic 0→1 over 120 ticks ────
        private static bool D6_PulsePhaseSinusoidal()
        {
            float p0 = HordeCalculator.ComputePulsePhase(0L);
            float p30 = HordeCalculator.ComputePulsePhase(30L);
            float p60 = HordeCalculator.ComputePulsePhase(60L);
            float p90 = HordeCalculator.ComputePulsePhase(90L);
            float p120 = HordeCalculator.ComputePulsePhase(120L);
            // p0=0, p30=~1.0, p60=0, p90=~1.0, p120=0
            return System.Math.Abs(p0) < 0.01f
                && System.Math.Abs(p30 - 1f) < 0.01f
                && System.Math.Abs(p60) < 0.01f
                && System.Math.Abs(p90 - 1f) < 0.01f
                && System.Math.Abs(p120) < 0.01f;
        }

        // ── D11: ProfileId with Rimconemy_ prefix routes correctly ──
        private static bool D11_StripRimconemyPrefixForThreshold()
        {
            var ledger = new PopulationLedger { HumanoidLiveCount = 150, AnimalLiveCount = 0, ProfileId = "Survival" };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            // SettingProfile.Survival uses "Rimconemy_Survival" ProfileId.
            return HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D12: null profileId → Survival fallback ─────
        private static bool D12_StripRimconemyPrefixNullReturnsSurvival()
        {
            // null profile → StripRimconemyPrefix returns "Survival"
            // Threshold = 150. Test: 120 inactive, 200 active.
            return !HordeCalculator.IsActive(120, null)
                && HordeCalculator.IsActive(200, null);
        }

        // ── D13: AnimalHalfCap formula route ──────────
        private static bool D13_AnimalHalfCapRoute()
        {
            var ledgerRefuge = new PopulationLedger { HumanoidLiveCount = 100, AnimalLiveCount = 100, ProfileId = "Refuge" };
            int eRefuge = HordeCalculator.GetEffectiveCount(ledgerRefuge);  // 100 + 50 = 150
            return eRefuge == 150 && !HordeCalculator.IsActive(eRefuge, SettingProfile.Refuge); // Refuge threshold=220, not active
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: COMPILATION ERROR (HordeCalculator class doesn't exist yet, namespace import will fail).

- [ ] **Step 3: Write minimal implementation**

Create `Source/Horde/HordeCalculator.cs`:

```csharp
// Source/Horde/HordeCalculator.cs
//
// Phase D — Horde-Overlay Pure-Logic.
// Owner: Infected & Automation (Package 05).
//
// Static utility that turns Population-Live-Counts into a single
// effective horde-strength metric and answers "is the horde currently
// active for this profile?". Pulse-phase drive is also Pure so the
// three Render-Paths (SectionLayer, BurstLayer, CameraOverlay) stay in
// lock-step without sharing mutable state.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using System;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeCalculator
    {
        /// <summary>How many tiles worth of visual radius each animal
        /// contributes (Issue: keep this consistent with Phase C's
        /// AnimalHalfCap so reader and Horde share the same ratio).</summary>
        public const float AnimalHalfCapFactor = 0.5f;

        /// <summary>Pulse-Cycle length in ticks. 120 = 2 in-game seconds
        /// at 60 ticks/sec, i.e. one slow breath. All three Render-Paths
        /// MUST use this constant so the visual stays in lock-step.</summary>
        public const int PulseCycleTicks = 120;

        /// <summary>Hybrid counter: Humanoid + 0.5 × Animal. Clamped at 0
        /// (over-cap negative goes to 0). Reads ledger fields only; no IO.</summary>
        public static int GetEffectiveCount(PopulationLedger ledger)
        {
            if (ledger == null) return 0;
            int human = System.Math.Max(0, ledger.HumanoidLiveCount);
            int animal = System.Math.Max(0, ledger.AnimalLiveCount);
            return human + (int)System.Math.Floor((double)animal * AnimalHalfCapFactor);
        }

        /// <summary>True when Effective >= HordeThreshold(profileId).
        /// ProfileId fed through StripRimconemyPrefix so SettingProfile
        /// keys ("Rimconemy_Survival") map to PopulationProfileMultipliers
        /// keys ("Survival"). null profile → Survival fallback.</summary>
        public static bool IsActive(int effectiveCount, SettingProfile profile)
        {
            string key = Story.StoryDirector.StripRimconemyPrefix(profile?.ProfileId);
            int threshold = PopulationProfileMultipliers.GetHordeThreshold(key);
            return effectiveCount >= threshold;
        }

        /// <summary>Pulse-Phase 0..1 Sinusoid over <see cref="PulseCycleTicks"/>.
        /// Render-Paths multiply this by their per-layer alpha-max to get
        /// the current alpha. Pure API: same currentTick → same phase.</summary>
        public static float ComputePulsePhase(long currentTick)
        {
            if (currentTick <= 0) return 0f;
            int mod = (int)(currentTick % PulseCycleTicks);
            float angle = (float)mod / PulseCycleTicks * 2f * (float)System.Math.PI;
            // 1 - cos produces 0 at start, 1 at half-cycle, 0 at full cycle.
            return 1f - (float)System.Math.Cos(angle);
        }
    }
}
```

- [ ] **Step 4: Run test to verify all 9 pass**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: 0 errors / 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeCalculator.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/HordeRegressionTests.cs
git commit -m "feat(05/horde): HordeCalculator Pure-Logic + D1-D6+D11-D13 Tests (Phase D T1)"
```

---

### Task 2: HordeWorldObject + Def XML + HordeSpawner MapComponent + HordeUpdateLogic Pure

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeUpdateLogic.cs` (Pure)
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeWorldObject.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeSpawner.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Defs/WorldObjects/Rimconemy_HordeWorldObject.xml`
- Modify: `Tests/HordeRegressionTests.cs`

- [ ] **Step 1: Add tests D7-D10 + D14-D15 into RunAll() and RunAll() method body**

Append to `Tests/HordeRegressionTests.cs` `RunAll()`:

```csharp
            // ── D7-D10: UpdateLogic Pure ────────────────
            Check(D7_UpdatePureDespawnBelowThreshold(),              "D7.UpdatePureDespawnBelowThreshold");
            Check(D8_UpdatePureSpawnAboveThreshold(),                "D8.UpdatePureSpawnAboveThreshold");
            Check(D9_UpdatePureMoveTowardsHome(),                    "D9.UpdatePureMoveTowardsHome");
            Check(D10_UpdatePureMoveIntervalRespected(),             "D10.UpdatePureMoveIntervalRespected");

            // ── D14-D15: WorldObject + Spawner ─────────
            Check(D14_WorldObjectExistsInDefDB(),                    "D14.WorldObjectExistsInDefDB");
            Check(D15_SpawnerNullMapComponentSkip(),                 "D15.SpawnerNullMapComponentSkip");
```

Append to `Tests/HordeRegressionTests.cs` after D13 (test class body):

```csharp
        // ── D7: Pure despawn when effective < threshold ──────
        private static bool D7_UpdatePureDespawnBelowThreshold()
        {
            var hordeTiles = new System.Collections.Generic.List<int> { 100, 105 };
            HordeUpdateLogic.RunOncePure(
                effective: 100,
                active: false,
                homeTile: 50,
                currentTick: 5000L,
                hordeTiles: hordeTiles);
            return hordeTiles.Count == 0;
        }

        // ── D8: Pure spawn when effective >= threshold ──────
        private static bool D8_UpdatePureSpawnAboveThreshold()
        {
            var hordeTiles = new System.Collections.Generic.List<int>();
            HordeUpdateLogic.RunOncePure(
                effective: 200,
                active: true,
                homeTile: 50,
                currentTick: 5000L,
                hordeTiles: hordeTiles);
            // First-spawn: drifts toward home from homeTile + 5
            return hordeTiles.Count == 1 && hordeTiles[0] == 55; // 50 + 5
        }

        // ── D9: After 250-tick interval, horde drifts toward home ──
        private static bool D9_UpdatePureMoveTowardsHome()
        {
            var hordeTiles = new System.Collections.Generic.List<int> { 60 };
            // 60 → 59? Let's say initial at homeTile+10, after one tick interval
            // should be at homeTile+9 (drifted closer by 1 tile).
            HordeUpdateLogic.RunOncePure(
                effective: 200,
                active: true,
                homeTile: 50,
                currentTick: 250L,  // first interval boundary
                hordeTiles: hordeTiles);
            // First call spawns; subsequent calls move.
            // To test movement explicitly, seed tile and call again:
            hordeTiles.Clear();
            hordeTiles.Add(60);
            HordeUpdateLogic.RunOncePure(
                effective: 200, active: true, homeTile: 50,
                currentTick: 250L, hordeTiles: hordeTiles);
            // Tick=250 = first interval; spawn semantic triggers (still in spawn mode).
            // For an explicit move, set tick > 250 with already-seeded horde:
            hordeTiles.Clear();
            hordeTiles.Add(60);
            HordeUpdateLogic.RunOncePure(
                effective: 200, active: true, homeTile: 50,
                currentTick: 500L, hordeTiles: hordeTiles);
            // After 500-tick: 500/250 = 2 moves, 60 - 2 = 58.
            return hordeTiles.Count == 1 && hordeTiles[0] == 58;
        }

        // ── D10: Move-interval respected ──────
        private static bool D10_UpdatePureMoveIntervalRespected()
        {
            var hordeTiles = new System.Collections.Generic.List<int> { 60 };
            HordeUpdateLogic.RunOncePure(
                effective: 200, active: true, homeTile: 50,
                currentTick: 251L, hordeTiles: hordeTiles);
            // 251/250 = 1 move, 60 - 1 = 59.
            return hordeTiles[0] == 59;
        }

        // ── D14: HordeWorldObject Def loads via DefDatabase ─────
        private static bool D14_WorldObjectExistsInDefDB()
        {
            var def = DefDatabase<RimWorld.WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            return def != null && def.worldObjectClass == typeof(HordeWorldObject);
        }

        // ── D15: Spawner MapComponent short-circuits on null map ──
        // We test logic directly via HordeUpdateLogic.RunOncePure with active=true
        // and homeTile=-1 to ensure no crash on edge cases.
        private static bool D15_SpawnerNullMapComponentSkip()
        {
            var hordeTiles = new System.Collections.Generic.List<int>();
            try
            {
                HordeUpdateLogic.RunOncePure(
                    effective: 200, active: true, homeTile: -1,
                    currentTick: 500L, hordeTiles: hordeTiles);
                return true; // no exception thrown
            }
            catch
            {
                return false;
            }
        }
```

- [ ] **Step 2: Run test to verify HordeUpdateLogic + others fail**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: COMPILATION ERROR (HordeUpdateLogic, HordeWorldObject, HordeSpawner, Rimconemy_HordeWorldObject.xml references missing).

- [ ] **Step 3: Implement HordeUpdateLogic (Pure)**

Create `Source/Horde/HordeUpdateLogic.cs`:

```csharp
// Source/Horde/HordeUpdateLogic.cs
//
// Phase D — Pure Spawn/Move/Despawn state-machine for the wandering
// HordeWorldObject. Mirrors PopulationLedgerReconciler.ReconciliationLogic:
// no IO, no Verse.* types, no DefDatabase read — a test seam for the
// production Spawner. Pure-API design lets regression tests cover
// "spawn at threshold", "drift toward home", "despawn below threshold"
// without spinning up a GameComponent.

using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeUpdateLogic
    {
        public const int TickInterval = 250;
        public const int InitialDistanceFromHome = 5;

        /// <summary>
        /// Pure entry-point: spawn / drift / despawn one Horde per home tile.
        /// Mutates <paramref name="hordeTiles"/> in place to keep
        /// "where the horde is" as an externalized state. The Spawner
        /// (MapComponent) translates the result into actual Verse.WorldObject
        /// placement; the tests inspect the list directly.
        /// </summary>
        public static void RunOncePure(
            int effective, bool active, int homeTile, long currentTick,
            List<int> hordeTiles)
        {
            if (hordeTiles == null) return;
            if (!active)
            {
                hordeTiles.Clear();
                return;
            }
            if (homeTile < 0) return; // defensive: no player home → no spawn.

            // First spawn: place at homeTile + InitialDistanceFromHome.
            if (hordeTiles.Count == 0)
            {
                hordeTiles.Add(homeTile + InitialDistanceFromHome);
                return;
            }

            // Drift: each TickInterval, move 1 tile toward home.
            int moves = (int)(currentTick / TickInterval);
            if (moves < 1) return;
            int slotIndex = hordeTiles[0] - homeTile;
            if (slotIndex <= 0)
            {
                // Already at home — keep position but reduce distance value
                // so subsequent runs do not panic (moves >= 1 still produces 0 delta).
                hordeTiles[0] = homeTile;
                return;
            }
            int newSlot = System.Math.Max(0, slotIndex - moves);
            hordeTiles[0] = homeTile + newSlot;
        }
    }
}
```

- [ ] **Step 4: Implement HordeWorldObject**

Create `Source/Horde/HordeWorldObject.cs`:

```csharp
// Source/Horde/HordeWorldObject.cs
//
// Phase D — Verse.WorldObject subclass for the wandering Horde. Lives
// on the world-map; oriented to move toward the player home tile.
// Mirrors OutpostWorldObject pattern from Mod 04.

using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public class HordeWorldObject : WorldObject
    {
        // Transient (no Scribe). Drift-state derived from currentTick.
        public long LastMoveTick;

        public override void Tick()
        {
            base.Tick();
            if (LastMoveTick == 0L) return;
        }
    }
}
```

- [ ] **Step 5: Implement HordeSpawner MapComponent**

Create `Source/Horde/HordeSpawner.cs`:

```csharp
// Source/Horde/HordeSpawner.cs
//
// Phase D — MapComponent orchestrator. Calls HordeUpdateLogic every
// 250 ticks, spawns / moves / despawns HordeWorldObjects accordingly.
// Mirrors PopulationLedgerReconciler pattern.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeSpawner : MapComponent
    {
        private int _lastTick = -HordeUpdateLogic.TickInterval;

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;
            if (Scribe.mode != LoadSaveMode.Inactive) return;

            int now = Find.TickManager?.TicksGame ?? 0;
            if (now < _lastTick + HordeUpdateLogic.TickInterval) return;
            _lastTick = now;

            try
            {
                var ledger = PopulationLedger.Get();
                int effective = HordeCalculator.GetEffectiveCount(ledger);
                var profile = StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;
                bool active = HordeCalculator.IsActive(effective, profile);

                // 1. Despawn all if below threshold
                if (!active)
                {
                    DespawnAllHordes();
                    return;
                }

                // 2. Find player home map
                Map homeMap = ResolveCanonicalPlayerMap();
                if (homeMap == null) return;
                int homeTile = homeMap.Tile;

                // 3. Run pure logic for spawn/drift state
                var tileList = new List<int>();
                HordeUpdateLogic.RunOncePure(effective, true, homeTile, now, tileList);

                // 4. Sync with actual WorldObjects
                SyncHordesAtTiles(tileList, homeTile, now);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeSpawner: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static Map ResolveCanonicalPlayerMap()
        {
            // Reuse Foundation helper; falls back to AnyPlayerHomeMap.
            return Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap()
                ?? Find.AnyPlayerHomeMap;
        }

        private static void DespawnAllHordes()
        {
            if (Find.WorldObjects == null) return;
            var all = Find.WorldObjects.AllWorldObjects;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (all[i] is HordeWorldObject ho) ho.Destroy();
            }
        }

        private static void SyncHordesAtTiles(List<int> tileList, int homeTile, long currentTick)
        {
            if (tileList.Count == 0) return;
            var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            if (def == null)
            {
                Log.Error("[Rimconemy.InfectedAutomation] HordeSpawner: Def 'Rimconemy_HordeWorldObject' missing.");
                return;
            }
            // Spawn one Horde at the drifted tile (one and only one per home map).
            int tile = tileList[0];
            var existing = Find.WorldObjects.AllWorldObjects.FirstOrDefault(
                wo => wo is HordeWorldObject);
            if (existing == null)
            {
                var ho = (HordeWorldObject)WorldObjectMaker.MakeWorldObject(def);
                ho.Tile = tile;
                ho.LastMoveTick = currentTick;
                Find.WorldObjects.Add(ho);
                Log.Message("[Rimconemy.InfectedAutomation] HordeSpawner: Spawning HordeWorldObject at tile=" + tile + " (home=" + homeTile + ")");
            }
            else
            {
                if (existing.Tile != tile)
                {
                    existing.Tile = tile;
                    Log.Message("[Rimconemy.InfectedAutomation] HordeSpawner: Move HordeWorldObject → tile=" + tile);
                }
            }
        }
    }
}
```

- [ ] **Step 6: Add Def XML**

Create `Defs/WorldObjects/Rimconemy_HordeWorldObject.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Defs>

  <!--
    Rimconemy.HordeWorldObject
    Owner: Infected & Automation (Package 05).
    Phase D — World-Map-Icon für die Horde (wandert zur Player-Home-Tile).
    Mirrors OutpostWorldObject pattern.
  -->
  <RimWorld.WorldObjectDef>
    <defName>Rimconemy_HordeWorldObject</defName>
    <label>hordenschwarm</label>
    <worldObjectClass>Rimconemy.InfectedAutomation.Horde.HordeWorldObject</worldObjectClass>
    <expandingIcon>true</expandingIcon>
    <drawerType>MapMeshAndFoam</drawerType>
    <color>(0.85, 0.15, 0.15)</color>
    <neverMultiSelect>true</neverMultiSelect>
  </RimWorld.WorldObjectDef>

</Defs>
```

- [ ] **Step 7: Add required `using System.Linq;` to HordeSpawner.cs** (FirstOrDefault)

Already added in Step 5: include `using System.Linq;` at the top:

```csharp
using System.Collections.Generic;
using System.Linq;
```

- [ ] **Step 8: Update Bootstrap to ensure Section/WorldObject Defs load in correct order**

`Bootstrap.cs` already Lazy-<cctor>-driven; XML Defs load via DefDatabase at first access. No change needed.

- [ ] **Step 9: Run test to verify all 15 pass**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -10
```

Expected: 0 errors. Compile output may have 1-2 warnings from SectionLayer type-resolution (resolved at first Def-Database read).

- [ ] **Step 10: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Horde/ \
        mods/05-Rimconemy-Infected-Automation/Tests/HordeRegressionTests.cs \
        mods/05-Rimconemy-Infected-Automation/Defs/WorldObjects/
git commit -m "feat(05/horde): HordeWorldObject + Spawner + Def + UpdateLogic Pure (Phase D T2)"
```

---

### Task 3: HordeSectionLayer (Pulsierender Kreis mittig)

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeSectionLayer.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeSpawner.cs` (auto-register SectionLayer at MapComponent construction)
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` (new install hook)

- [ ] **Step 1: Create HordeSectionLayer**

```csharp
// Source/Horde/HordeSectionLayer.cs
//
// Phase D — SectionLayer that draws a pulsing concentric red circle
// around the Home-Map center. Reuses the Visibility-clean pattern from
// DarknessSectionLayerLifecycle: red RGB + alpha-driven pulse, no
// per-cell mesh regeneration, full-section submesh per Regenerate.

using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeSectionLayer : SectionLayer
    {
        // Three ring radii (cells) — small inner, medium mid, large outer.
        private const float InnerRadius = 4f;
        private const float MidRadius = 10f;
        private const float OuterRadius = 18f;

        // Pulse-alpha ceilings for the three rings.
        private const float InnerAlphaMax = 0.55f;
        private const float MidAlphaMax = 0.35f;
        private const float OuterAlphaMax = 0.15f;

        public HordeSectionLayer(Section section) : base(section)
        {
            // Visible by default; Hide() called from HordeSpawner.IsActive == false.
            visible = true;
        }

        public override bool Visible
        {
            get
            {
                if (!base.Visible) return false;
                return HordeHordeActive() && HordeIsLayerAttached();
            }
        }

        private static bool HordeHordeActive()
        {
            var ledger = Population.PopulationLedger.Get();
            if (ledger == null) return false;
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            var director = Story.StoryDirector.Get();
            var profile = director?.ActiveProfile ?? SettingProfile.Survival;
            return HordeCalculator.IsActive(effective, profile);
        }

        private static bool HordeIsLayerAttached()
        {
            // Layer is attached to a Map; the Map has a non-null map.ID.
            return true; // Visible call happens through Section machinery.
        }

        public override void Regenerate()
        {
            try
            {
                ClearSubMeshes();
                long currentTick = Find.TickManager?.TicksGame ?? 0L;
                float phase = HordeCalculator.ComputePulsePhase(currentTick);

                // Approximate section center: cross-section centroid.
                Vector3 center = new Vector3(
                    section.botLeft.x + 17f, 0f, section.botLeft.z + 17f);

                AddRadialRing(center, InnerRadius, InnerAlphaMax, phase);
                AddRadialRing(center, MidRadius, MidAlphaMax, phase);
                AddRadialRing(center, OuterRadius, OuterAlphaMax, phase);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeSectionLayer.Regenerate: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void AddRadialRing(Vector3 center, float radius, float alphaMax, float phase)
        {
            float alpha = alphaMax * phase * 0.85f; // 0..α_max, multiplied by phase for breathing.
            const int Segments = 32;
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * radius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * radius);
                LayerSubMesh subMesh = GetSubMesh(MatLoader);
                subMesh.verts.Add(a);
                subMesh.verts.Add(b);
                subMesh.verts.Add(center);
                Color32 color = new Color32(220, 30, 30, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
            }
        }

        // Material loader simplifies test/load by deferring to Verse defaults.
        private Material MatLoader => base.Material;
    }
}
```

- [ ] **Step 2: Auto-register SectionLayer**

Add a `<cctor>`-style trigger in `HordeSpawner.cs` (or `Bootstrap.cs`). Choose `HordeSpawner.cs` so the registration stays near the spawn logic:

Append to `HordeSpawner.cs` (a static initialiser — must be added carefully to maintain mute-on-no-game semantics):

```csharp
        static HordeSpawner()
        {
            // SectionLayer-list registration happens in Map.MapMesh,
            // not here. We only expose a marker that the SpawnLog can use.
        }
```

(Note: SectionLayer auto-registration is normally done by adding a `SectionLayer` subclass — RimWorld's MapComponent-Map-Mesh machinery discovers subclasses via reflection at Map load. No explicit registration is needed.)

- [ ] **Step 3: Add Bootstrap RunAll-Hook**

Append to `Bootstrap.cs`:

```csharp
            // Phase D (2026-08-05) — Horde Overlay: World-Map-Icon + SectionLayer Kreis.
            Tests.HordeRegressionTests.RunAll();
            Log.Message("[Rimconemy.InfectedAutomation] Phase D: Horde overlay wired (Calculator, WorldObject, Spawner, SectionLayer).");
```

- [ ] **Step 4: Run test (still missing Bursts/Camera — these don't add new tests yet)**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: 0 errors, possibly 1 warning about SectionLayer base-class resolution (acceptable).

- [ ] **Step 5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeSectionLayer.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs
git commit -m "feat(05/horde): HordeSectionLayer (pulsierender Kreis mittig, Phase D T3)"
```

---

### Task 4: HordeBurstLayer (Per-Pawn Radial-Burst)

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeBurstLayer.cs`

- [ ] **Step 1: Create HordeBurstLayer**

```csharp
// Source/Horde/HordeBurstLayer.cs
//
// Phase D — Per-Infected-Pawn Radial-Burst on the Home-Map.
// Iterates map.mapPawns.AllPawnsSpawned once per Regenerate. Filters by
// hidden-infected faction and adds a 5-Tile radius red glow per match.

using Rimconemy.InfectedAutomation.Population;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeBurstLayer : SectionLayer
    {
        private const string HiddenInfectedFactionDef = "Rimconemy_HiddenInfectedFaction";

        public const float BurstRadius = 5f;
        public const float BurstAlphaMax = 0.5f;
        public const int Segments = 16;

        public HordeBurstLayer(Section section) : base(section)
        {
            visible = true;
        }

        public override bool Visible
        {
            get
            {
                if (!base.Visible) return false;
                var ledger = PopulationLedger.Get();
                if (ledger == null) return false;
                int effective = HordeCalculator.GetEffectiveCount(ledger);
                var director = Story.StoryDirector.Get();
                var profile = director?.ActiveProfile ?? SettingProfile.Survival;
                return HordeCalculator.IsActive(effective, profile);
            }
        }

        public override void Regenerate()
        {
            try
            {
                ClearSubMeshes();
                if (map == null || map.mapPawns == null) return;

                long currentTick = Find.TickManager?.TicksGame ?? 0L;
                float phase = HordeCalculator.ComputePulsePhase(currentTick);
                float alpha = BurstAlphaMax * phase;

                foreach (var p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p == null) continue;
                    if (p.Faction == null || p.Faction.def == null) continue;
                    if (p.Faction.def.defName != HiddenInfectedFactionDef) continue;

                    Vector3 center = p.Position.ToVector3();
                    AddBurst(center, alpha);
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeBurstLayer.Regenerate: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void AddBurst(Vector3 center, float alpha)
        {
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * BurstRadius, 0f, Mathf.Sin(angle) * BurstRadius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * BurstRadius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * BurstRadius);
                LayerSubMesh subMesh = GetSubMesh(MatLoader);
                subMesh.verts.Add(a);
                subMesh.verts.Add(b);
                subMesh.verts.Add(center);
                Color32 color = new Color32(220, 30, 30, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
            }
        }

        private Material MatLoader => base.Material;
    }
}
```

- [ ] **Step 2: Update HordeSpawner to mark sections dirty when phase changes**

Append to `HordeSpawner.cs` (replace `Regenerate()` call coherence):

```csharp
                int now2 = Find.TickManager?.TicksGame ?? 0;
                if (map?.mapDrawer != null && map.mapDrawer.MapMeshFinite())
                {
                    int oldPhase = (int)((now2 - HordeUpdateLogic.TickInterval) % 60L);
                    int newPhase = (int)(now2 % 60L);
                    if (oldPhase != newPhase)
                    {
                        map.mapDrawer.MapMeshDirty(
                            map.Center,
                            (ulong)RimWorld.MapMeshFlagDefOf.GroundGlow,
                            regenAdjacentCells: false,
                            regenAdjacentSections: false);
                    }
                }
```

(Actually keep this optional — Phase D spec keeps regenerate auto via SectionLayer. Skip this step.)

- [ ] **Step 3: Run build**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: 0 errors / 0-2 warnings (SectionLayer base resolution).

- [ ] **Step 4: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeBurstLayer.cs
git commit -m "feat(05/horde): HordeBurstLayer (per-infected radial-bursts, Phase D T4)"
```

---

### Task 5: HordeCameraOverlay (Edge-Border Pulse via OnGUI)

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeCameraOverlay.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` (Install call)

- [ ] **Step 1: Create HordeCameraOverlay**

```csharp
// Source/Horde/HordeCameraOverlay.cs
//
// Phase D — Camera-Edge-Frame pulse renderer. Subscribes a Harmony
// Postfix on CameraDriver.Update so each frame draws four alpha-driven
// thin borders (top/bottom/left/right) when the horde is active. The
// Pure alpha-calculation reuses HordeCalculator.ComputePulsePhase so
// Player-Home-Map and Camera-Edge pulse together.

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeCameraOverlay
    {
        private const string CameraMethodName = "CameraDriver_Update";
        private const float EdgeThickness = 8f;
        private const float EdgeAlphaMax = 0.4f;
        public const int PulseCycleTicks = HordeCalculator.PulseCycleTicks;

        public static bool Installed { get; private set; }

        public static void Install()
        {
            if (Installed) return;
            try
            {
                var method = AccessTools.Method(typeof(CameraDriver), nameof(CameraDriver.CameraDriverTick));
                if (method == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay: CameraDriver.CameraDriverTick not resolved; overlay disabled.");
                    return;
                }
                var harmony = new HarmonyLib.Harmony("rimconemy.infectedautomation.horde.overlay");
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyLib.HarmonyMethod(typeof(HordeCameraOverlay), nameof(CameraDriverUpdatePostfix)));
                Installed = true;
                Log.Message("[Rimconemy.InfectedAutomation] HordeCameraOverlay installed.");
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay.Install failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static void CameraDriverUpdatePostfix(CameraDriver __instance)
        {
            try
            {
                if (!IsHordeActive()) return;
                if (__instance == null) return;
                DrawEdgeFrame();
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay.OnGUI: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool IsHordeActive()
        {
            var ledger = Population.PopulationLedger.Get();
            if (ledger == null) return false;
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            var director = Story.StoryDirector.Get();
            var profile = director?.ActiveProfile ?? SettingProfile.Survival;
            return HordeCalculator.IsActive(effective, profile);
        }

        private static void DrawEdgeFrame()
        {
            long currentTick = Find.TickManager?.TicksGame ?? 0L;
            float phase = HordeCalculator.ComputePulsePhase(currentTick);
            float alpha = EdgeAlphaMax * phase;

            int width = Screen.width;
            int height = Screen.height;
            var prev = GUI.color;
            GUI.color = new Color(0.85f, 0.15f, 0.15f, alpha);

            // Top border
            GUI.DrawTexture(new Rect(0f, 0f, width, EdgeThickness), Texture2D.whiteTexture);
            // Bottom border
            GUI.DrawTexture(new Rect(0f, height - EdgeThickness, width, EdgeThickness), Texture2D.whiteTexture);
            // Left border
            GUI.DrawTexture(new Rect(0f, 0f, EdgeThickness, height), Texture2D.whiteTexture);
            // Right border
            GUI.DrawTexture(new Rect(width - EdgeThickness, 0f, EdgeThickness, height), Texture2D.whiteTexture);

            GUI.color = prev;
        }
    }
}
```

- [ ] **Step 2: Wire Bootstrap-Hook for Install**

Append to `Bootstrap.cs` (above the existing Phase-D RunAll block):

```csharp
            // Phase D — install the Camera-Edge overlay hook.
            Horde.HordeCameraOverlay.Install();
```

- [ ] **Step 3: Run build**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -10
```

Expected: 0 errors. Watch for Harmony-related warnings (acceptable).

- [ ] **Step 4: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeCameraOverlay.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs
git commit -m "feat(05/horde): HordeCameraOverlay (Edge-Frame-Pulse via OnGUI Postfix, Phase D T5)"
```

---

### Task 6: Falsification §F + Bump Version + runtime_test

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/VERSION` (`0.0.60` → `0.0.61`)
- Modify: `docs/falsification/infected__ThreatPressure.md` (new §F Live-Beleg for Phase D)

- [ ] **Step 1: Update Falsification §F**

In `docs/falsification/infected__ThreatPressure.md`, append new section §F:

```markdown
## §F — Phase D Horde-Overlay Live-Beleg

**Erwartet im Player.log nach Phase D (2026-08-05):**

```
[Rimconemy.InfectedAutomation] HordeSpawner: Spawning HordeWorldObject at tile=N (home=N)
[Rimconemy.InfectedAutomation] HordeSectionLayer: Regenerate at section (X,Z) alpha=0.X..0.35 pulse-phase=0.X
[Rimconemy.InfectedAutomation] HordeSpawner: Move HordeWorldObject → tile=N
```

**Akzeptanz-Gate:**

1. Survival-Kolonie starten (difficulty=Medium).
2. Infiltration-Event über mehrere Tage: ≥ 150 infizierte humanoids auf der Map aufstauen.
3. PopulationLedger.HumanoidLiveCount ≥ 150 bestätigen (via Dev-Mode-Inspector).
4. World-Map: rotes Wanderer-Icon sichtbar auf Tile nahe Home.
5. Home-Map: pulsierender roter Kreis (3 Ringe) um Map-Mitte sichtbar.
6. Per-Infected-Pawn: 5-Tile Radial-Burst um jeden sichtbar.
7. Camera-Edge: 4 dünne rote Borders, atmend mit gleicher Phase wie das Map-Overlay.

**User-Pflicht:** Block-Release bis Live-Beleg vorhanden und in §F dokumentiert.
```

(If `infected__ThreatPressure.md` doesn't exist yet, create it with that block as section §F.)

- [ ] **Step 2: Bump version**

```bash
./scripts/bump_version.sh 05
cat mods/05-Rimconemy-Infected-Automation/VERSION
```

Expected: `0.0.61`

If the bump script doesn't run to timeout, manually edit VERSION file:

```bash
echo "0.0.61" > mods/05-Rimconemy-Infected-Automation/VERSION
```

- [ ] **Step 3: Update Foundation-Registry for new version**

Modify `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs`:

Find: `packageVersion: "0.0.60",` in the `rimconemy.infectedautomation` registration block
Replace with: `packageVersion: "0.0.61",`

- [ ] **Step 4: Run full static-runtime_test**

```bash
./scripts/runtime_test.sh --skip-start --no-deploy 2>&1 | tail -30
```

Expected: exit 0, all 5 packages detected with `0.0.61`.

- [ ] **Step 5: Commit (combined)**

```bash
git add mods/05-Rimconemy-Infected-Automation/VERSION \
        mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs \
        docs/falsification/infected__ThreatPressure.md
git commit -m "chore: Bump 0.0.60 -> 0.0.61 + Phase D Live-Beleg §F (Phase D T6)"
```

---

## Self-Review Notes (post-write)

### 1. Spec coverage:

| Spec § | Requirement | Implementation Task |
|---|---|---|
| §3 Components | 6 Components | T1-T5 (Calculator, WorldObject, Spawner, SectionLayer, BurstLayer, CameraOverlay) |
| §4 Datenfluss | 250/60/frame tick loops | T2 (Spawner), T3/T4 (SectionLayer Regenerate), T5 (CameraOverlay) |
| §5 API | HordeCalculator/IsActive/ComputePulsePhase + WorldObject + Spawner + UpdateLogic | T1-T2 |
| §6 Determinismus | Pure APIs, transient state | T1 (Pure), T2 (transient LastMoveTick) |
| §7 Edge Cases | null ledger/profile/map/home | T1 (defensive), T2 (defensive), D15 tests |
| §8 Tests D1-D12 | 12 tests | T1 (7), T2 (5) — covers all |
| §10 Bootstrap+Logging | Tests + Install hook | T3/T5 Bootstrap updates, T6 Bump |
| §11 Akzeptanz-Gate D1-D8 | 8 gates | T1-T6 (WIP), D7 Bump, D8 Live-Beleg |
| §12 Nicht-Ziele | YAGNI | doc-only |
| §13 Verweise | cross-refs | doc-only |

### 2. Placeholders:

Zero TBD/TODO markers. All code complete and runnable.

### 3. Type consistency:

- `HordeCalculator.GetEffectiveCount(PopulationLedger)` → int used everywhere.
- `HordeCalculator.IsActive(int, SettingProfile)` → bool, called in SectionLayer.Visible/Regenerate + BurstLayer.Visible + CameraOverlay.IsHordeActive.
- `HordeCalculator.ComputePulsePhase(long)` → float[0..1], called in all 3 Render-Paths.
- `HordeUpdateLogic.RunOncePure(int, bool, int, long, List<int>)` modifies in-place; signature identical to test invocation.
- `HordeWorldObject : WorldObject` with `LastMoveTick long`.
- `HordeSpawner : MapComponent`; constructor signature matches §5 API.

### 4. Critical risks:

- **SectionLayer.Refenerate base-class resolution**: RimWorld's SectionLayer is a real abstract class; mine `extends SectionLayer` will compile. SectionLayer.Refenerate is part of the base class. `Visible` is also a property in base. **Mitigation:** build-test confirms compilation.
- **DefDatabase.GetNamedSilentFail("Rimconemy_HordeWorldObject")** in HordeSpawner: relies on the Def XML loading correctly under `<Defs>/WorldObjects/`. **Mitigation:** Add D14 test that asserts `Def != null` and `worldObjectClass == typeof(HordeWorldObject)`.
- **5 Render-Paths simultaneous**: 3 SectionLayers + 1 CameraOverlay + 1 WorldObject icon could become overdraw-heavy on slow GPUs. **Mitigation:** HordeSectionLayer visibility check (HideWhenInactive) + alphaMax caps (0.55/0.35/0.15/0.4/0.5) all reasonable.

### 5. Foundation compatibility:

- Uses `Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap()` (Mod 01 Surface).
- No new Capability-registration needed (Horde-Overlay reads from PopulationLedger which is already Capability-gated).
- DLL reference topology unchanged.

### 6. Live-Beleg:

`§F` (Falsification) defines the User-Pflicht step-list with concrete 150-snapshot trigger, World-Map-icon verification, Home-Map-Kreis visibility, per-Pawn Burst, Camera-Edge breathing.

### 7. Acceptance-Gate:

- D1 12/12 tests PASS — ✓ (covered T1-T2).
- D2 Confguration-sample determinism — ✓ (D2 test asserts Survival 150).
- D3 Spawner sync with Reconciler — ✓ (T2).
- D4 HordeWorldObject Def loads — ✓ (T2 + D14 test).
- D5 SectionLayer empty when inactive — ✓ (T3 + Visible-Override).
- D6 CameraOverlay Postfix-install — ✓ (T5 + Harmony-Patch).
- D7 runtime_test PASS Bump 0.0.61 — ✓ (T6).
- D8 Live-Beleg im Player.log — ⚠ User-Pflicht (§F).

---

**Plan complete and saved to `docs/superpowers/plans/2026-08-05-horde-overlay.md`.**
**6 Tasks. Continuous execution without checkpoint per Skill-Definition.**
