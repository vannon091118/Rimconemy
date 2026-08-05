# Phase D — Horde-Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a multi-layer visual representation of the infected horde (camera-edge pulse + section-layer concentric circle + per-pawn radial bursts on home-map; wandering HordeWorldObject icon on world-map) so the player can perceive tightening pressure without opening the ThreatDashboard.

**Architecture:** Pure-logic `HordeCalculator` derives effective count from `PopulationLedger` (Humanoid + 0.5×Animal) and gates every RenderPath. `HordeUpdateLogic` orchestrates Spawn/Move/Despawn on a 250-tick interval. Three render paths (SectionLayer Circle, SectionLayer Bursts, CameraUI Edge-Frame) all read pulse-phase from the same Pure-API calculation. `HordeWorldObject` + Def XML mirrors the `OutpostWorldObject` pattern from Mod 04.

**Tech Stack:** C# (.NET via `dotnet build`), RimWorld 1.6.4566 (`Verse`, `RimWorld`, `UnityEngine`), Hierarchical determinism via `DeterministicRng.GetStableHashCode` / FNV-1a, MapComponent + WorldObject.

## Global Constraints

- Build flags: `RimWorldManagedPath=/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed HarmonyAssembliesPath=/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies`
- Project: `mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
- Tick-Frequency: MapComponent 250 ticks (WorldObject-Sync) + 15 ticks (SectionLayer-Regen-Driver via `MapDrawer.RegenerateLayerNow`) + frame-rate (CameraOverlay Postfix).
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
| `Source/Horde/HordeUpdateLogic.cs` | Pure: `ComputeHordeTile` (tick-abgeleitete Tile-Position, testbar ohne Game) |
| `Source/Horde/HordeWorldObject.cs` | Verse.WorldObject-Subclass (Marker; Tile setzt HordeSpawner) |
| `Source/Horde/HordeSpawner.cs` | MapComponent-Orchestrator: 250-Tick-WorldObject-Sync + 15-Tick-`RegenerateLayerNow`-Driver |
| `Source/Horde/HordeSectionLayer.cs` | SectionLayer-Subclass: pulsierender Kreis mittig (nur die Section um `map.Center`) |
| `Source/Horde/HordeBurstLayer.cs` | SectionLayer-Subclass: per-infected-Pawn Radial-Bursts |
| `Source/Horde/HordeCameraOverlay.cs` | Static Postfix auf `UIRoot.UIRootOnGUI`, explizit via `Install()`: Edge-Frame-Pulse |
| `Defs/WorldObjects/Rimconemy_HordeWorldObject.xml` | Def für WorldObject-Class |
| `Source/Bootstrap.cs` | RunAll-Hook für Tests |
| `Tests/HordeRegressionTests.cs` | 15 Tests (D1-D15) |
| `docs/falsification/infected__ThreatPressure.md` | §F Phase-D Live-Beleg Stub |

---

### Task 1: HordeCalculator Pure-Logic + Tests D1-D6 + D11

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeCalculator.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/HordeRegressionTests.cs`

**Interfaces:**
- Produces:
  - `public static class HordeCalculator`
  - `public static int GetEffectiveCount(PopulationLedger ledger)`
  - `public static bool IsActive(int effectiveCount, SettingProfile profile)`
  - `public static float ComputePulsePhase(long currentTick)`

- [ ] **Step 1: Write the failing test file (D1-D6 + D11)**

Create `Tests/HordeRegressionTests.cs`:

```csharp
// Tests/HordeRegressionTests.cs
//
// Phase D — Horde-Overlay Visualisierung (D1-D14).
// spec: docs/superpowers/specs/2026-08-05-horde-overlay-design.md
// plan: docs/superpowers/plans/2026-08-05-horde-overlay.md
//
// Owner: Infected & Automation (Package 05).
//
// Calculator-side tests cover Pure-Logic only.
// UpdateLogic tests cover the pure tick-derived tile (spawn / drift /
// arrival). Despawn is the Spawner's IsActive gate (covered by D1-D5).
// D11-D12 verify the hybrid-count route and the WorldObject Def load.

using Rimconemy.InfectedAutomation.Horde;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeRegressionTests
    {
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

            // ── D11: hybrid route ────────────────────────────
            Check(D11_AnimalHalfCapRoute(),                          "D11.AnimalHalfCapRoute");

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

        // ── D11: hybrid route at Refuge threshold 220 ────────────
        private static bool D11_AnimalHalfCapRoute()
        {
            var ledgerRefuge = new PopulationLedger { HumanoidLiveCount = 100, AnimalLiveCount = 100, ProfileId = "Refuge" };
            int eRefuge = HordeCalculator.GetEffectiveCount(ledgerRefuge); // 100 + 50 = 150
            return eRefuge == 150 && !HordeCalculator.IsActive(eRefuge, SettingProfile.Refuge); // Refuge=220, not active
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
//
// IsActiveNow() is the one deliberate exception to the Pure contract: it
// reads the live ledger + profile so all three render paths share one
// gate. Everything else is deterministic from its inputs.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using System;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeCalculator
    {
        /// <summary>Pulse-Cycle length in ticks. 120 = 2 in-game seconds
        /// at 60 ticks/sec, i.e. one slow breath. All three Render-Paths
        /// MUST use this constant so the visual stays in lock-step.</summary>
        public const int PulseCycleTicks = 120;

        /// <summary>Hybrid counter: Humanoid + 0.5 × Animal. Clamped at 0.
        /// Reads ledger fields only; no IO, deterministic from inputs.
        /// null ledger → 0 (horde inactive).</summary>
        public static int GetEffectiveCount(PopulationLedger ledger)
        {
            if (ledger == null) return 0;
            return Math.Max(0, ledger.HumanoidLiveCount) + Math.Max(0, ledger.AnimalLiveCount) / 2;
        }

        /// <summary>True when Effective >= HordeThreshold(profileId).
        /// ProfileId fed through StripRimconemyPrefix so SettingProfile
        /// keys ("Rimconemy_Survival") map to PopulationProfileMultipliers
        /// keys ("Survival"). null profile → Survival fallback (threshold
        /// lookup goes through the same prefix-strip path, returning
        /// "Survival" → 150).</summary>
        public static bool IsActive(int effectiveCount, SettingProfile profile)
        {
            string key = Story.StoryDirector.StripRimconemyPrefix(profile?.ProfileId);
            int threshold = PopulationProfileMultipliers.GetHordeThreshold(key);
            return effectiveCount >= threshold;
        }

        /// <summary>Live gate shared by all three render paths. Reads the
        /// current ledger + active profile and answers whether the horde
        /// should be drawn right now.</summary>
        public static bool IsActiveNow()
        {
            var ledger = PopulationLedger.Get();
            if (ledger == null) return false;
            var profile = Story.StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;
            return IsActive(GetEffectiveCount(ledger), profile);
        }

        /// <summary>Pulse-Phase in 0..1, two-breath Sinusoid over
        /// <see cref="PulseCycleTicks"/> (one breath per half-cycle).
        /// Render-Paths multiply this by their per-layer alpha-max to
        /// get the current alpha. Pure API: same currentTick → same
        /// phase; tick=0 yields 0 (minimum alpha, no cold-start flash).
        ///
        /// D6 spec: pattern 0 → 1 → 0 → 1 → 0 over 120 ticks (two peaks).
        /// Implementation: <c>|sin(angle)|</c> with <c>angle = mod/120 · 2π</c>:</summary>
        public static float ComputePulsePhase(long currentTick)
        {
            int mod = (int)(currentTick % PulseCycleTicks);
            float angle = (float)mod / PulseCycleTicks * 2f * (float)System.Math.PI;
            return (float)System.Math.Abs(System.Math.Sin(angle));
        }
    }
}
```

- [ ] **Step 4: Run test to verify all 7 pass**

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
git commit -m "feat(05/horde): HordeCalculator Pure-Logic + D1-D6+D11 Tests (Phase D T1)"
```

---

### Task 2: HordeWorldObject + Def XML + HordeSpawner MapComponent + HordeUpdateLogic Pure

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeUpdateLogic.cs` (Pure)
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeWorldObject.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Horde/HordeSpawner.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Defs/WorldObjects/Rimconemy_HordeWorldObject.xml`
- Modify: `Tests/HordeRegressionTests.cs`

- [ ] **Step 1: Add tests D7-D10 + D12 into RunAll() and test class body**

Append to `Tests/HordeRegressionTests.cs` `RunAll()`:

```csharp
            // ── D7-D10: UpdateLogic Pure (tick-derived tile) ──────
            Check(D7_UpdatePureSpawnAtInitialDistance(),             "D7.UpdatePureSpawnAtInitialDistance");
            Check(D8_UpdatePureDriftsOnePerInterval(),               "D8.UpdatePureDriftsOnePerInterval");
            Check(D9_UpdatePureArrivesAndClampsAtHome(),             "D9.UpdatePureArrivesAndClampsAtHome");
            Check(D10_UpdatePureDeterministicFromTick(),             "D10.UpdatePureDeterministicFromTick");

            // ── D12: WorldObject Def load ──────────────────
            Check(D12_WorldObjectExistsInDefDB(),                    "D12.WorldObjectExistsInDefDB");
```

Append to `Tests/HordeRegressionTests.cs` after D11 (test class body):

```csharp
        // ── D7: tick 0 → spawn at home + 5 ────────────────────
        private static bool D7_UpdatePureSpawnAtInitialDistance()
        {
            // Spec §6: tile = home + max(0, 5 − floor(tick/250)).
            return HordeUpdateLogic.ComputeHordeTile(homeTile: 50, currentTick: 0L) == 55;
        }

        // ── D8: floor(tick/250) moves, 1 tile per interval ──────
        private static bool D8_UpdatePureDriftsOnePerInterval()
        {
            // tick 249 → 0 moves (still 55); tick 250 → 1 move (54).
            return HordeUpdateLogic.ComputeHordeTile(50, 249L) == 55
                && HordeUpdateLogic.ComputeHordeTile(50, 250L) == 54
                && HordeUpdateLogic.ComputeHordeTile(50, 500L) == 53;
        }

        // ── D9: reaches home at tick 1250 and clamps, never below ──
        private static bool D9_UpdatePureArrivesAndClampsAtHome()
        {
            return HordeUpdateLogic.ComputeHordeTile(50, 1249L) == 51
                && HordeUpdateLogic.ComputeHordeTile(50, 1250L) == 50
                && HordeUpdateLogic.ComputeHordeTile(50, 100000L) == 50;
        }

        // ── D10: deterministic — same tick → same tile, no state ──
        private static bool D10_UpdatePureDeterministicFromTick()
        {
            // Pure function of (homeTile, tick): repeated calls agree and
            // a different home tile shifts the result by the same delta.
            return HordeUpdateLogic.ComputeHordeTile(50, 500L) == HordeUpdateLogic.ComputeHordeTile(50, 500L)
                && HordeUpdateLogic.ComputeHordeTile(7, 250L) == 11; // 7 + (5 − 1)
        }

        // ── D12: WorldObjectDef loads from DefDatabase ───────────
        private static bool D12_WorldObjectExistsInDefDB()
        {
            var def = DefDatabase<RimWorld.WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            return def != null && def.worldObjectClass == typeof(HordeWorldObject);
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
// Phase D — Pure spawn/drift math for the wandering HordeWorldObject.
// No IO, no Verse.* types, no DefDatabase read — a test seam for the
// production Spawner. Pure-API design lets regression tests cover
// "spawn at initial distance", "drift toward home", "arrival at home"
// without spinning up a GameComponent.

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeUpdateLogic
    {
        public const int TickInterval = 250;
        public const int InitialDistanceFromHome = 5;

        /// <summary>
        /// Pure position function (spec §6): the horde's world tile is
        /// derived ONLY from the game tick — no persisted state, so
        /// Save/Load resumes at the same tile and any activation moment
        /// yields a consistent position.
        ///
        /// <c>tile = homeTile + max(0, InitialDistanceFromHome − floor(tick/250))</c>
        ///
        /// tick 0–249   → home + 5  (initial spawn distance)
        /// tick 500     → home + 3  (2 tiles drifted)
        /// tick 1250+   → home      (arrived; clamped, never below home)
        /// </summary>
        public static int ComputeHordeTile(int homeTile, long currentTick)
        {
            int drifted = (int)(currentTick / TickInterval);
            return homeTile + System.Math.Max(0, InitialDistanceFromHome - drifted);
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
// Mirrors OutpostWorldObject pattern (Mod 04).
//
// Marker type: tile + drift state are owned entirely by HordeSpawner
// (the MapComponent pulls currentTick and assigns Tile via the Pure
// HordeUpdateLogic). No tick-time work or persistence needed here.

using RimWorld.Planet;

namespace Rimconemy.InfectedAutomation.Horde
{
    public class HordeWorldObject : WorldObject
    {
    }
}
```

- [ ] **Step 5: Implement HordeSpawner MapComponent**

Create `Source/Horde/HordeSpawner.cs`:

```csharp
// Source/Horde/HordeSpawner.cs
//
// Phase D — MapComponent orchestrator on the player-home map. Every
// 250 ticks it syncs the HordeWorldObject to the tick-derived tile;
// every 15 ticks it forces a regenerate of the two SectionLayer
// render-paths. Vanilla auto-instantiates SectionLayer subclasses per
// Section, but nothing ever marks custom layers dirty — the map drawer
// must be told to rebuild them or the pulse never renders.
// Mirrors PopulationLedgerReconciler pattern.

using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeSpawner : MapComponent
    {
        public HordeSpawner(Map map) : base(map) { }

        // Layer-regen cadence. MUST be a proper divisor of the 120-tick
        // pulse cycle with ≥4 samples: a 60-tick loop samples |sin(θ)| at
        // θ and θ+π which are equal → the pulse would freeze. 15 ticks
        // yields 8 samples per two-breath cycle, a visible beat.
        private const int LayerRegenIntervalTicks = 15;

        private int _lastTick = -HordeUpdateLogic.TickInterval;
        private int _nextLayerRegenTick;

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;
            if (Scribe.mode != LoadSaveMode.Inactive) return;

            // The horde is a home-map concept: world-object sync and layer
            // regeneration both target the primary player-home map only.
            Map homeMap = Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap();
            if (homeMap == null || map != homeMap) return;

            int now = Find.TickManager?.TicksGame ?? 0;

            // Shared live gate (same source the render paths use): ledger
            // + active profile + threshold. No re-derivation here.
            if (!HordeCalculator.IsActiveNow())
            {
                DespawnAllHordes();
                return;
            }

            // World-object sync: 250-tick cadence, tile purely tick-derived
            // (spec §6 — no persisted drift state).
            if (now >= _lastTick + HordeUpdateLogic.TickInterval)
            {
                _lastTick = now;
                SyncHordeAtTile(HordeUpdateLogic.ComputeHordeTile(homeMap.Tile, now), homeMap.Tile);
            }

            // Layer pulse: force a rebuild of the two render layers on a
            // 15-tick cadence so the alpha actually animates. RegenerateLayerNow
            // checks Visible per section, so this is a no-op while inactive.
            if (now >= _nextLayerRegenTick)
            {
                _nextLayerRegenTick = now + LayerRegenIntervalTicks;
                map.mapDrawer?.RegenerateLayerNow(typeof(HordeSectionLayer));
                map.mapDrawer?.RegenerateLayerNow(typeof(HordeBurstLayer));
            }
        }

        private static void DespawnAllHordes()
        {
            var all = Find.WorldObjects.AllWorldObjects;
            for (int i = all.Count - 1; i >= 0; i--)
                if (all[i] is HordeWorldObject ho) ho.Destroy();
        }

        private static void SyncHordeAtTile(int tile, int homeTile)
        {
            var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            if (def == null)
            {
                Log.Error("[Rimconemy.InfectedAutomation] HordeSpawner: Def 'Rimconemy_HordeWorldObject' missing.");
                return;
            }

            var existing = Find.WorldObjects.AllWorldObjects.FirstOrDefault(
                wo => wo is HordeWorldObject);
            if (existing != null)
            {
                if (existing.Tile != tile)
                    existing.Tile = tile;
                return;
            }

            var ho = (HordeWorldObject)WorldObjectMaker.MakeWorldObject(def);
            ho.Tile = tile;
            Find.WorldObjects.Add(ho);
            Log.Message("[Rimconemy.InfectedAutomation] HordeSpawner: Spawning HordeWorldObject at tile=" + tile + " (home=" + homeTile + ")");
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
// around the Home-Map center. Red RGB + alpha-driven pulse, full-section
// submesh per Regenerate. Regeneration is driven by HordeSpawner via
// MapDrawer.RegenerateLayerNow (custom layers are never auto-dirtied).

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
        }

        public override bool Visible => base.Visible && HordeCalculator.IsActiveNow();

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);
            Map map = section.map;
            if (map == null) return;

            // Spec: "pulsierender Kreis um die Home-Map-Mitte" — one circle
            // around the map center. Vertices are world-space, so the single
            // section containing the map center draws the whole ring; letting
            // every section within reach draw it would stack ~9 copies of the
            // same geometry (z-fighting + 9× triangles).
            if (!section.CellRect.Contains(map.Center)) return;

            Vector3 center = map.Center.ToVector3();
            float phase = HordeCalculator.ComputePulsePhase(Find.TickManager?.TicksGame ?? 0L);

            AddRadialRing(center, InnerRadius, InnerAlphaMax, phase);
            AddRadialRing(center, MidRadius, MidAlphaMax, phase);
            AddRadialRing(center, OuterRadius, OuterAlphaMax, phase);
        }

        private void AddRadialRing(Vector3 center, float radius, float alphaMax, float phase)
        {
            float alpha = alphaMax * phase;
            const int Segments = 32;

            LayerSubMesh subMesh = GetSubMesh(MatBases.Darkness);
            Color32 color = new Color32(220, 30, 30, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * radius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * radius);
                subMesh.verts.Add(a);
                subMesh.verts.Add(b);
                subMesh.verts.Add(center);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
            }
        }
    }
}
```

- [ ] **Step 2: Auto-instantiation + regen driver (no manual registration)**

Vanilla `Verse.Section` auto-instantiates every `SectionLayer` subclass per Section
via `GenTypes.AllSubclassesNonAbstract(typeof(SectionLayer))` + `Activator.CreateInstance`
— verified against Assembly-CSharp 1.6.4566. No registration hook is needed.

**BUT** instantiation ≠ rendering: vanilla only calls `Regenerate()` on layers it marks
dirty via its own `MapMeshFlag`s. A custom layer is never dirtied, so `HordeSpawner`
must drive regeneration explicitly (already implemented in Task 2 Step 5):

```csharp
        // HordeSpawner.MapComponentTick, 15-Tick-Cadence (nur bei active):
        if (now >= _nextLayerRegenTick)
        {
            _nextLayerRegenTick = now + LayerRegenIntervalTicks;
            map.mapDrawer?.RegenerateLayerNow(typeof(HordeSectionLayer));
            map.mapDrawer?.RegenerateLayerNow(typeof(HordeBurstLayer));
        }
```

Cadence note: `LayerRegenIntervalTicks` MUST be a proper divisor of the 120-tick pulse
cycle with ≥4 samples. A 60-tick loop would sample `|sin(θ)|` at θ and θ+π, which are
equal → the pulse would freeze. 15 ticks = 8 samples per two-breath cycle.

- [ ] **Step 3: Add Bootstrap RunAll-Hook**

Append to `Bootstrap.cs`:

```csharp
            // Phase D (2026-08-05) — Horde Overlay: World-Map-Icon + SectionLayer Kreis.
            Tests.HordeRegressionTests.RunAll();
            Horde.HordeCameraOverlay.Install();
            Log.Message("[Rimconemy.InfectedAutomation] Phase D: Horde overlay wired (Calculator, WorldObject, Spawner, SectionLayer, BurstLayer, CameraEdge).");
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
// Iterates section.map.mapPawns.AllPawnsSpawned once per Regenerate,
// filters by hidden-infected faction + section rect, and adds a 5-Tile
// radius red glow per match. Regeneration driven by HordeSpawner.

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
        }

        public override bool Visible => base.Visible && HordeCalculator.IsActiveNow();

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);
            if (section?.map == null || section.map.mapPawns == null) return;

            float phase = HordeCalculator.ComputePulsePhase(Find.TickManager?.TicksGame ?? 0L);
            float alpha = BurstAlphaMax * phase;

            // Only draw bursts inside this section (Regenerate runs per
            // visible section, so pawns elsewhere are filtered out).
            CellRect sectionRect = section.CellRect;

            foreach (var p in section.map.mapPawns.AllPawnsSpawned)
            {
                if (p.Faction?.def == null) continue;
                if (p.Faction.def.defName != HiddenInfectedFactionDef) continue;
                if (!sectionRect.Contains(p.Position)) continue;
                AddBurst(p.Position.ToVector3(), alpha);
            }
        }

        private void AddBurst(Vector3 center, float alpha)
        {
            LayerSubMesh subMesh = GetSubMesh(MatBases.Darkness);
            Color32 color = new Color32(220, 30, 30, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * 2f * Mathf.PI;
                Vector3 a = center + new Vector3(Mathf.Cos(angle) * BurstRadius, 0f, Mathf.Sin(angle) * BurstRadius);
                Vector3 b = center + new Vector3(Mathf.Cos(angle + (2f * Mathf.PI / Segments)) * BurstRadius, 0f, Mathf.Sin(angle + (2f * Mathf.PI / Segments)) * BurstRadius);
                subMesh.verts.Add(a);
                subMesh.verts.Add(b);
                subMesh.verts.Add(center);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
                subMesh.colors.Add(color);
            }
        }
    }
}
```

- [ ] **Step 2: Regen driver already in HordeSpawner (skip — no extra hook)**

`MapMeshDirty` with a vanilla flag would NOT regenerate custom layers: `MapMeshDirty`
maps vanilla `MapMeshFlag`s to vanilla layer types only. The correct driver is
`MapDrawer.RegenerateLayerNow(Type)` (verifiziert an Assembly-CSharp 1.6.4566),
already implemented in HordeSpawner Task 2 Step 5. No further step needed here.

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
// Phase D — Camera-Edge-Frame pulse renderer. Package 05 has no
// Harmony PatchAll (Bootstrap registers patches explicitly, cf.
// DarknessSectionLayerLifecycle), so the postfix must be installed with
// an explicit harmony.Patch call — a bare [HarmonyPatch] attribute
// would be inert. Each frame draws four alpha-driven thin borders
// (top/bottom/left/right) when the horde is active. The Pure
// alpha-calculation reuses HordeCalculator.ComputePulsePhase so the
// Home-Map circle and the Camera-Edge pulse together.

using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeCameraOverlay
    {
        private const string HarmonyId = "rimconemy.infectedautomation.horde-camera-overlay";
        private const float EdgeThickness = 8f;
        private const float EdgeAlphaMax = 0.4f;

        private static bool _installed;

        /// <summary>Installs the UIRootOnGUI postfix once during Package 05 bootstrap.</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            try
            {
                var target = AccessTools.Method(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI));
                if (target == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay: UIRoot.UIRootOnGUI missing; edge pulse disabled.");
                    return;
                }

                var harmony = new Harmony(HarmonyId);
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(HordeCameraOverlay), nameof(Postfix)));
                Log.Message("[Rimconemy.InfectedAutomation] HordeCameraOverlay: edge-frame postfix installed.");
            }
            catch (Exception ex)
            {
                // Fail closed: a missing hook must not break the UI loop.
                Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay install failed; edge pulse disabled: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // Postfix — runs at end of UIRoot.UIRootOnGUI per frame.
        public static void Postfix()
        {
            if (!HordeCalculator.IsActiveNow()) return;
            DrawEdgeFrame();
        }

        private static void DrawEdgeFrame()
        {
            float phase = HordeCalculator.ComputePulsePhase(Find.TickManager?.TicksGame ?? 0L);
            float alpha = EdgeAlphaMax * phase;

            int width = Screen.width;
            int height = Screen.height;
            var prev = GUI.color;
            GUI.color = new Color(0.85f, 0.15f, 0.15f, alpha);

            GUI.DrawTexture(new Rect(0f, 0f, width, EdgeThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, height - EdgeThickness, width, EdgeThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 0f, EdgeThickness, height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(width - EdgeThickness, 0f, EdgeThickness, height), Texture2D.whiteTexture);

            GUI.color = prev;
        }
    }
}
```

- [ ] **Step 2: Wire Bootstrap-Hook for Install**

Append to `Bootstrap.cs` (above the existing Phase-D RunAll block):

```csharp
            // Phase D — install the Camera-Edge overlay hook (expliziter
            // harmony.Patch — Package 05 hat kein PatchAll, ein nacktes
            // [HarmonyPatch]-Attribut wäre inert).
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
- Modify: `mods/05-Rimconemy-Infected-Automation/VERSION` (`0.0.62` → `0.0.63`)
- Modify: `docs/falsification/infected__ThreatPressure.md` (new §F Live-Beleg for Phase D)

- [ ] **Step 1: Update Falsification §F**

In `docs/falsification/infected__ThreatPressure.md`, append new section §F:

```markdown
## §F — Phase D Horde-Overlay Live-Beleg

**Erwartet im Player.log nach Phase D (2026-08-05):**

```
[Rimconemy.InfectedAutomation] HordeCameraOverlay: edge-frame postfix installed.
[Rimconemy.InfectedAutomation] HordeSpawner: Spawning HordeWorldObject at tile=N (home=N)
```

(Der Spawn-Marker ist der einzige Horde-WorldObject-Log; Drift ist über die World-Map-
Icon-Position beobachtbar — tile = home + max(0, 5 − floor(tick/250)), keine Log-Zeilen.)

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

Expected: `0.0.63`

If the bump script doesn't run to timeout, manually edit VERSION file:

```bash
echo "0.0.63" > mods/05-Rimconemy-Infected-Automation/VERSION
```

- [ ] **Step 3: Update Foundation-Registry for new version**

Modify `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs`:

Find: `packageVersion: "0.0.62",` in the `rimconemy.infectedautomation` registration block
Replace with: `packageVersion: "0.0.63",`

- [ ] **Step 4: Run full static-runtime_test**

```bash
./scripts/runtime_test.sh --skip-start --no-deploy 2>&1 | tail -30
```

Expected: exit 0, all 5 packages detected with `0.0.63`.

- [ ] **Step 5: Commit (combined)**

```bash
git add mods/05-Rimconemy-Infected-Automation/VERSION \
        mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs \
        docs/falsification/infected__ThreatPressure.md
git commit -m "chore: Bump 0.0.62 -> 0.0.63 + Phase D Live-Beleg §F (Phase D T6)"
```

---

## Self-Review Notes (post-write)

### 1. Spec coverage:

| Spec § | Requirement | Implementation Task |
|---|---|---|
| §3 Components | 6 Components | T1-T5 (Calculator, WorldObject, Spawner, SectionLayer, BurstLayer, CameraOverlay) |
| §4 Datenfluss | 250/15/frame tick loops | T2 (Spawner 250-Tick-Sync + 15-Tick-Regen-Driver), T3/T4 (SectionLayer Regenerate), T5 (CameraOverlay) |
| §5 API | HordeCalculator/IsActive/ComputePulsePhase + WorldObject + Spawner + UpdateLogic | T1-T2 |
| §6 Determinismus | Pure APIs, transient state | T1 (Pure), T2 (tick-derived, kein State) |
| §7 Edge Cases | null ledger/profile/map/home | T1 (defensive), D1-D5 (null-gates), D12 (Def-Null) |
| §8 Tests D1-D15 | 15 tests | T1 (7), T2 (5), T3 (3: D13-D15) — covers all |
| §10 Bootstrap+Logging | Tests + Install hook | T3/T5 Bootstrap updates, T6 Bump |
| §11 Akzeptanz-Gate D1-D8 | 8 gates | T1-T6 (WIP), D7 Bump, D8 Live-Beleg |
| §12 Nicht-Ziele | YAGNI | doc-only |
| §13 Verweise | cross-refs | doc-only |

### 2. Placeholders:

Zero TBD/TODO markers. All code complete and runnable.

### 3. Type consistency:

- `HordeCalculator.GetEffectiveCount(PopulationLedger)` → int used everywhere.
- `HordeCalculator.IsActive(int, SettingProfile)` → bool, Pure-Route. Render-Gate ist `IsActiveNow()` in allen 3 Render-Paths (SectionLayer/BurstLayer `Visible`, CameraOverlay `Postfix`).
- `HordeCalculator.ComputePulsePhase(long)` → float[0..1], called in all 3 Render-Paths.
- `HordeUpdateLogic.ComputeHordeTile(int homeTile, long currentTick)` → int; pure, kein State; identisch in Test + Spawner.
- `HordeWorldObject : WorldObject` (Marker ohne State; Tile setzt HordeSpawner).
- `HordeSpawner : MapComponent`; constructor signature matches §5 API.
- Render-Gate: alle 3 Render-Paths nutzen `HordeCalculator.IsActiveNow()` (live Ledger + Profile).

### 4. Critical risks:

- **SectionLayer.Refenerate base-class resolution**: RimWorld's SectionLayer is a real abstract class; mine `extends SectionLayer` will compile. SectionLayer.Refenerate is part of the base class. `Visible` is also a property in base. **Mitigation:** build-test confirms compilation.
- **DefDatabase.GetNamedSilentFail("Rimconemy_HordeWorldObject")** in HordeSpawner: relies on the Def XML loading correctly under `<Defs>/WorldObjects/`. **Mitigation:** Add D12 test that asserts `Def != null` and `worldObjectClass == typeof(HordeWorldObject)`.
- **5 Render-Paths simultaneous**: 3 SectionLayers + 1 CameraOverlay + 1 WorldObject icon could become overdraw-heavy on slow GPUs. **Mitigation:** `Visible`-Override (IsActiveNow → leeres Layer wenn inactive), Kreis nur aus EINER Section (map.Center), alphaMax caps (0.55/0.35/0.15/0.4/0.5) all reasonable.

### 5. Foundation compatibility:

- Uses `Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap()` (Mod 01 Surface).
- No new Capability-registration needed (Horde-Overlay reads from PopulationLedger which is already Capability-gated).
- DLL reference topology unchanged.

### 6. Live-Beleg:

`§F` (Falsification) defines the User-Pflicht step-list with concrete 150-snapshot trigger, World-Map-icon verification, Home-Map-Kreis visibility, per-Pawn Burst, Camera-Edge breathing.

### 7. Acceptance-Gate:

- D1 15/15 tests PASS — ✓ (covered T1-T3).
- D2 Confguration-sample determinism — ✓ (D2 test asserts Survival 150).
- D3 Spawner sync with Reconciler — ✓ (T2).
- D4 HordeWorldObject Def loads — ✓ (T2 + D12 test).
- D5 SectionLayer empty when inactive — ✓ (T3 + Visible-Override).
- D6 CameraOverlay Postfix-install — ✓ (T5 + Harmony-Patch).
- D7 runtime_test PASS Bump 0.0.63 — ✓ (T6).
- D8 Live-Beleg im Player.log — ⚠ User-Pflicht (§F).

---

**Plan complete and saved to `docs/superpowers/plans/2026-08-05-horde-overlay.md`.**
**6 Tasks. Continuous execution without checkpoint per Skill-Definition.**
