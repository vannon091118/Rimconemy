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

        // F-01 (2026-08-04 Audit-Bündel B): Late-bound Wallet balance read.
        // Mod 05 (InfectedAutomation) used to compile-reference Mod 04
        // (EconomyTerritory) for WalletService.GetOrCreateLedger().Balance.
        // INTERFACE_CONTRACT §9.1 forbids non-adjacent compile refs; Mod 05
        // should consume wallet state via the same late-bound reflection
        // pattern that Mod 02 uses for Mod 05. The reflection retrieves
        // `Rimconemy.EconomyTerritory.Wallet.WalletService.GetOrCreateLedger()
        //   .Balance` from the loaded Mod 04 assembly.
        private const string Mod04AssemblyName = "Rimconemy.EconomyTerritory";
        private const string Mod04WalletServiceTypeName = "Rimconemy.EconomyTerritory.Wallet.WalletService";
        private const string Mod04GetOrCreateLedgerMethodName = "GetOrCreateLedger";
        private const string Mod04BalanceMemberName = "Balance";

        /// <summary>
        /// Late-bound read of the player's wallet balance from Mod 04.
        /// Returns <c>true</c> when Mod 04 is loaded, capability
        /// <c>rimconemy.economyterritory.wallet</c> is registered, the
        /// static <c>GetOrCreateLedger</c> call succeeds and the ledger
        /// exposes a <c>Balance</c> member. Returns <c>false</c> on every
        /// miss; in that case the caller treats the balance as 0.
        ///
        /// Defensive shape:
        ///  - The capability gate fires first (no reflection on cold start).
        ///  - All reflection exceptions are caught narrowly (ReflectionTypeLoadException,
        ///    TargetInvocationException, generic Exception as last resort).
        ///  - When the static <c>GetOrCreateLedger</c> returns a non-null ledger
        ///    without a numeric <c>Balance</c>, we log a warning and return 0
        ///    rather than throw — the caller renders "0" in placeholders, which
        ///    is the correct fallback.
        /// </summary>
        public static bool TryReadWalletBalance(out long balance)
        {
            balance = 0L;

            if (!CapabilityAudit.HasCapabilityOrWarn(
                    packageId: "rimconemy.economyterritory",
                    capabilityId: "rimconemy.economyterritory.wallet",
                    minVersion: 1,
                    readerContext: "WalletBalance-Read"))
            {
                return false;
            }

            try
            {
                var walletServiceType = ResolveType(Mod04AssemblyName, Mod04WalletServiceTypeName);
                if (walletServiceType == null) return false;

                var getLedger = walletServiceType.GetMethod(
                    Mod04GetOrCreateLedgerMethodName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (getLedger == null)
                {
                    Log.Warning("[Rimconemy.Foundation.CrossPackageState] TryReadWalletBalance: " +
                        "WalletService.GetOrCreateLedger not found on " + walletServiceType.FullName);
                    return false;
                }

                object ledger;
                try
                {
                    ledger = getLedger.Invoke(null, null);
                }
                catch (TargetInvocationException tie)
                {
                    Log.Warning("[Rimconemy.Foundation.CrossPackageState] TryReadWalletBalance " +
                        "GetOrCreateLedger threw: " +
                        (tie.InnerException?.GetType().Name ?? "?") + ": " +
                        (tie.InnerException?.Message ?? tie.Message));
                    return false;
                }
                if (ledger == null) return false;

                // Ledger.Balance — try property first, then field (matches the
                // CRITICAL FIX pattern from TryReadStoryGameOverPending).
                object balanceValue = null;
                var balanceProp = ledger.GetType().GetProperty(
                    Mod04BalanceMemberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (balanceProp != null)
                {
                    balanceValue = balanceProp.GetValue(ledger);
                }
                else
                {
                    var balanceField = ledger.GetType().GetField(
                        Mod04BalanceMemberName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (balanceField != null)
                        balanceValue = balanceField.GetValue(ledger);
                }
                if (balanceValue == null) return false;

                try
                {
                    balance = System.Convert.ToInt64(balanceValue);
                    return true;
                }
                catch (FormatException) { return false; }
                catch (InvalidCastException) { return false; }
                catch (OverflowException) { return false; }
            }
            catch (ReflectionTypeLoadException rtle)
            {
                Log.Warning("[Rimconemy.Foundation.CrossPackageState] TryReadWalletBalance type load failed: " + rtle.Message);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation.CrossPackageState] TryReadWalletBalance reflection failed: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
