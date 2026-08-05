using System;
using Rimconemy.Foundation.Catalog;
using Rimconemy.Foundation.DLC;
using Rimconemy.Foundation.Events;
using Rimconemy.Foundation.Profile;
using Rimconemy.Foundation.Registry;
using Verse;

namespace Rimconemy.Foundation
{
    /// <summary>
    /// Owner: Foundation
    /// Static bootstrap that initializes the Foundation system at game startup.
    ///
    /// Order: PackageRegistry (self-registers Foundation) -> ProfileDetector (reads registry) -> EventLog.
    /// FoundationSaveData (GameComponent) is auto-discovered by RimWorld's reflection
    /// and attached to every Game instance automatically.
    ///
    /// ServiceBus removal note (2026-08-04 audit): FoundationServiceBus was
    /// deleted because no package subscribed or published through it, leaving
    /// the [ThreadStatic]/Monitor.Wait/WouldCreateWaitCycle machinery
    /// unused. Cross-package hooks resume through late-bound
    /// PackageRegistry.IsRegistered / Capability checking (see
    /// INTERFACE_CONTRACT.md). When a typed subscriber emerges, the bus
    /// will be reintroduced in a lean, main-thread-only form.
    ///
    /// Hook reason: StaticConstructorOnStartup guarantees this runs after
    /// all mods have loaded their assemblies but before the game world initializes.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            Log.Message("[Rimconemy.Foundation] ========================================");
            Log.Message("[Rimconemy.Foundation] Foundation bootstrap starting...");

            // Static constructors handle class-level init.
            // Explicit access ensures they run in the right order.
            // PackageRegistry runs first (self-registration), then ProfileDetector reads it.

            Log.Message($"[Rimconemy.Foundation] Registry: {PackageRegistry.RegisteredCount} package(s) registered");
            // Defensive trigger: ensure the canonical "Profile detected" summary
            // line has been emitted at least once after Foundation's cctor chain
            // settles. The dedup gate in TryEmitDetection collapses this call
            // against the static-cctor's earlier emission when state is unchanged,
            // so this is a no-op in the common path. If a feature package arrived
            // between the cctor and this point, TryEmitDetection will emit the
            // state-transition line here.
            if (ProfileDetector.TryEmitDetection(out string bootstrapSummary))
                Log.Message(bootstrapSummary);
            Log.Message($"[Rimconemy.Foundation] Profile: {ProfileDetector.CurrentProfile}");
            Log.Message("[Rimconemy.Foundation] UI-Toolkit loaded: RimconemyTheme + RimconemyUi + 3 base classes (Phase 0-A)");

            // Phase-5 EventLog guard (2026-08-04, audit follow-up to runtime log
            // analysis): during the static-constructor phase RimWorld is not yet
            // fully initialised. Current.Game is typically null until later in
            // the load. EventLog.Record with the current code is defensive
            // against Find.TickManager?.TicksGame being null, but historically
            // the deployed DLL had a non-defensive version that threw NRE here
            // ("EventLog.Record failed during bootstrap"). We DEFER the Record
            // call to a post-Bootstrap phase where Current.Game and TickManager
            // are guaranteed non-null, and skip cleanly if the guard fails.
            // This silences the warning for OLD deployed DLLs and is a no-op
            // for the current code (which already handles null safely).
            if (Current.Game != null && Find.TickManager != null)
            {
                try
                {
                    EventLog.Record("Package", "Initialized", "rimconemy.foundation",
                        "Foundation bootstrap complete.",
                        $"Registry={PackageRegistry.RegisteredCount} Profile={ProfileDetector.CurrentProfile}");
                }
                catch (Exception ex)
                {
                    // Phase-5 follow-up (2026-08-04): include the exception type
                    // so the runtime log gives the operator the diagnosis rather
                    // than just "Object reference not set to an instance" without
                    // any indicator where it came from.
                    Log.Warning($"[Rimconemy.Foundation] EventLog.Record failed during bootstrap: {ex.GetType().Name}: {ex.Message}");
                }
            }
            else
            {
                Log.Message("[Rimconemy.Foundation] Skipping EventLog.Record (no Current.Game / TickManager at static-ctor time).");
            }

            // Phase-7 DLC-Architektur (Phase-2, 2026-08-04):
            //
            // Order: ApplyFromLoadedDefs FIRST, EmitBootstrapSummary SECOND.
            // Damit zeigt die Summary-Line die post-Override Werte (ist der
            // häufigere Fall wenn ein ContentPack DLCContentPolicy_*.xml mit
            // Overrides liefert). ohne Def überschreibt nichts und die
            // Summary emittiert die hartkodierten Phase-1-Werte.
            try
            {
                DLCPolicyConfig.ApplyFromLoadedDefs();
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[Rimconemy.Foundation] DLCPolicyConfig.ApplyFromLoadedDefs failed: " +
                    $"{ex.GetType().Name}: {ex.Message}. Phase-1 defaults remain active.");
            }

            // Phase-7 DLC-Architektur (2026-08-04): emittiert einmal pro Session
            // die DLCContentPolicy-Bootstrap-Summary:  aktiv/total Flags je DLC
            // + welche DLC-Pakete registriert sind. Pattern: thin reading der
            // Policy-Tabelle in DLCContentPolicy + DLCFilter-Helfer für die
            // LoadedModManager-Scan-Logik. Operator sieht sofort ob ein erwartetes
            // DLC fehlt, ohne decrypt F3-Warnings manuell zu müssen.
            try
            {
                DLCFilter.EmitBootstrapSummary();
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[Rimconemy.Foundation] DLCFilter.EmitBootstrapSummary failed: " +
                    $"{ex.GetType().Name}: {ex.Message}");
            }

            Tests.FoundationCapabilityGateTests.RunAll();
            Tests.FoundationCrossPackageStateTests.RunAll();
            Tests.FoundationEventLogRegressionTests.RunAll();
            Tests.FoundationColonialReaderTests.RunAll();
            Tests.FoundationProfileRefreshTests.RunAll();
            Tests.ProfileDetectorDedupTests.RunAll();
            Tests.FoundationBuildingCapabilityTests.RunAll();
            Tests.FoundationTimeConstantsRegressionTests.RunAll();
            Tests.FoundationWindowFallbackTests.RunAll();
            Tests.FoundationHonestBannerAudit.RunAll();
            Tests.FoundationCanonicalLayerTests.RunAll();
            // Anti-Slop-Guard: scans all Test files across all packages for
            // anti-patterns (empty catches, skip tricks, tautology returns).
            // 1 skip = warning, 2nd skip = HARD BLOCK demanding ROOTCAUSE FIX.
            Tests.AntiSlopGuardTests.RunAll();
            Log.Message("[Rimconemy.Foundation] Building capability gate declared; live construction remains an interactive A-gate.");
            Log.Message("[Rimconemy.Foundation] Canonical layer active: MaterialIdentity + SettingIdentity + RoomRoleResolver.");

            // Phase-5 Storyteller-Probe (2026-08-05): enumerate DefDatabase<StorytellerDef>
            // once defs are loaded and surface a stable summary line. Late-bind path
            // in FoundationDashboard re-captures lazily if this early call finds an
            // empty database — defensive id+log keeps the bootstrap quiet.
            try
            {
                if (StorytellerInventory.EnsureCaptured())
                {
                    StorytellerInventory.LogBootstrapSummary();
                }
                else
                {
                    Log.Message("[Rimconemy.Foundation] StorytellerInventory capture returned 0 (defs may not be fully loaded yet; lazy re-run on dashboard open).");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rimconemy.Foundation] StorytellerInventory bootstrap probe failed: {ex.GetType().Name}: {ex.Message}");
            }

            Log.Message("[Rimconemy.Foundation] Bootstrap complete.");
            Log.Message("[Rimconemy.Foundation] ========================================");
        }
    }
}
