using System;
using System.Linq;
using System.Reflection;
using Rimconemy.Foundation.Registry;
using Verse;

namespace Rimconemy.Foundation.CrossPackage
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase B / F-V2: late-bound reflection bridge for cross-package READS.
    ///
    /// Mod 02 (ProgressionGameComponent) needs to consume state written by
    /// Mod 05 (StoryDirector). A direct DLL reference from Mod 02 → Mod 05
    /// would create a binary cycle (cross-package) which INTERFACE_CONTRACT §0
    /// forbids. We solve this by introducing a reflection lookup against the
    /// StoryDirector type at runtime — Mod 02 has a DLL ref to Foundation only,
    /// so the cycle is broken at compile-time and detected one-way at runtime.
    ///
    /// Trade-offs:
    ///  + No binary cycle. Mod 02 still ships as a fully standalone build.
    ///  + Capability-gated reads via CapabilityAudit (no silent-NRE).
    ///  − Reflection has minor runtime cost; only called once per Mod 02 tick
    ///    (250 ticks ≈ 4.2 seconds in-game), well below the P1 perf budget.
    ///
    /// Coverage today:
    ///  - TryReadStoryGameOverPending(out string) → consumes pending from Mod 05.
    ///
    /// Future hooks (Phase B-Subsequent):
    ///  - TryReadStoryThreatPressure(out float)
    ///  - TryReadStoryIdeoPresetId(out string)
    /// </summary>
    public static class CrossPackageState
    {
        private const string Mod05AssemblyName = "Rimconemy.InfectedAutomation";
        private const string Mod05StoryDirectorTypeName = "Rimconemy.InfectedAutomation.Story.StoryDirector";
        private const string Mod05InstancePropertyName = "Instance";
        // CRITICAL FIX (Post-Review 2026-08-04): StoryDirector declares
        // `public StoryState State;` as a FIELD, not a property. Fields are
        // not visible to GetProperty(), so reflection was returning null even
        // when Mod 05 was loaded and the wipe was signaled. The Sole-Owner
        // Game-Over still prevented duplicate CheckOrUpdateGameOver calls,
        // but the reason text silently fell back to local "ReasonOutOfColonists".
        // We now probe GetProperty first, then fall back to GetField; both
        // supported on the same name. Newer 0.1.11+ implementations that move
        // to a property are still picked up by the first probe.
        private const string Mod05StateMemberName = "State";
        private const string Mod05ConsumeGoPendingMethodName = "ConsumeGameOverPending";

        /// <summary>
        /// Returns true if Mod 02 should pull a game-over reason from Mod 05.
        /// The capability gate fires before any reflection takes place.
        /// </summary>
        public static bool TryReadStoryGameOverPending(out string reason)
        {
            reason = null;

            if (!CapabilityAudit.HasCapabilityOrWarn(
                    packageId: "rimconemy.infectedautomation",
                    capabilityId: "rimconemy.infectedautomation.automation",
                    minVersion: 1,
                    readerContext: "GameOver-Read"))
            {
                return false;
            }

            try
            {
                // Resolve the type without taking a hard DLL ref.
                var directorType = ResolveType(Mod05AssemblyName, Mod05StoryDirectorTypeName);
                if (directorType == null) return false;

                // StoryDirector.Get() is the canonical accessor. Static method
                // returning the GameComponent instance. No need for Instance
                // property hack if this exists. (Looking at StoryDirector.cs:
                // Get() is the documented accessor.)
                var getStatic = directorType.GetMethod("Get",
                    BindingFlags.Public | BindingFlags.Static);
                object director = null;
                if (getStatic != null)
                {
                    director = getStatic.Invoke(null, null);
                }
                else
                {
                    // Fallback: instance property "Instance" — defensive only.
                    var instanceProp = directorType.GetProperty(Mod05InstancePropertyName,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                    if (instanceProp != null)
                        director = instanceProp.GetValue(null);
                }
                if (director == null)
                {
                    // StoryDirector not loaded yet (main menu) — log nothing.
                    // This is a normal state; we already passed the capability
                    // gate so we know Mod 05 is registered.
                    return false;
                }

                // Resolve State: try property first (newer code), then field
                // (current 0.1.11+ code uses a field). CRITICAL FIX
                // 2026-08-04 (Q2 from code-reviewer).
                object state = null;
                var stateProp = directorType.GetProperty(Mod05StateMemberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateProp != null)
                {
                    state = stateProp.GetValue(director);
                }
                else
                {
                    var stateField = directorType.GetField(Mod05StateMemberName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (stateField != null)
                        state = stateField.GetValue(director);
                }
                if (state == null) return false;
                return InvokeConsumeGameOverPending(state, out reason);
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation.CrossPackageState] TryReadStoryGameOverPending reflection failed: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static Type ResolveType(string assemblyName, string typeFullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                    string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal))
                ?.GetType(typeFullName, throwOnError: false);
        }

        private static bool InvokeConsumeGameOverPending(object storyState, out string reason)
        {
            reason = null;
            var type = storyState.GetType();
            var method = type.GetMethod(Mod05ConsumeGoPendingMethodName,
                BindingFlags.Public | BindingFlags.Instance);
            if (method == null) return false;

            var args = new object[] { null };
            object result;
            try
            {
                result = method.Invoke(storyState, args);
            }
            catch (TargetInvocationException tie)
            {
                Log.Warning("[Rimconemy.Foundation.CrossPackageState] ConsumeGameOverPending threw: " + tie.InnerException?.GetType().Name + ": " + tie.InnerException?.Message);
                return false;
            }

            reason = args[0] as string;
            return (bool)result;
        }
    }
}
