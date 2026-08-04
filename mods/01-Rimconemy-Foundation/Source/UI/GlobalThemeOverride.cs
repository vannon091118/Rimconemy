using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase 0-A: Opt-in bridge to the RimThemes mod (aRandomKiwi/RimThemes).
    /// Detects RimThemes at runtime via reflection, applies our preferred
    /// theme folder if available, and otherwise logs a graceful fallback.
    ///
    /// Safety: this class never throws. All reflection failures are caught
    /// and logged so a missing or version-mismatched RimThemes install does
    /// not break Foundation or any other package.
    ///
    /// Activation policy: explicitly opt-in via ThemeSettings. We do NOT
    /// patch Widgets.* globally — content-level theming (Tokens + Toolkit)
    /// is the default. This class is the *global-escape-hatch* requested
    /// by the spec when the user wants their RimWorld UI to look
    /// conspicuously different.
    /// </summary>
    public static class GlobalThemeOverride
    {
        // Cached across calls: RimThemes re-discovery every game start is fine;
        // re-discovery on every tick would be wasteful. Static cache survives
        // until the AppDomain unloads.
        private static bool _probeComplete;
        private static Type _rimThemesApiType;
        private static MethodInfo _setThemeMethod;
        private static string _detectedPackageId;

        /// <summary>
        /// Called from Bootstrap after FoundationSaveData is alive and the
        /// game is past ModsConfig-ready (post-static-init). No-op when
        /// user has not opted in.
        /// </summary>
        public static void ApplyIfRequested()
        {
            try
            {
                if (!ThemeSettings.IsOverrideEnabled)
                    return;

                if (!ProbeRimThemes())
                {
                    Log.Message("[Rimconemy.Foundation] Theme-override requested but RimThemes not active — content-level theming active.");
                    return;
                }

                // Apply a sensible default theme (Rimconemy ships none;
                // user is responsible to pick a theme directory in their
                // RimThemes setup). We pass an empty/default name and let
                // RimThemes decide. If you want a specific theme, set it
                // via a separate Foundation mod setting in a later phase.
                InvokeSetTheme(null);
            }
            catch (Exception ex)
            {
                // Hard guarantee: never break Foundation because of a theme probe.
                Log.Warning($"[Rimconemy.Foundation] GlobalThemeOverride failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Reflective probe for a RimThemes-API type. Looks for any type
        /// whose namespace or name mentions "RimThemes" and that exposes
        /// at least one point we can poke (a static method taking a string,
        /// or a static writable property).
        /// </summary>
        private static bool ProbeRimThemes()
        {
            if (_probeComplete) return _rimThemesApiType != null;

            _probeComplete = true; // mark before doing work — so re-entry short-circuits

            try
            {
                // Preferred fast-path: load by known package id list. RimThemes
                // historically shipped under package "RimThemes" — but this can
                // vary by fork. Fall back to type-name search if absent.
                var candidates = new[] { "RimThemes", "aRandomKiwi.RimThemes", "RimThemesMod" };
                foreach (var pkg in candidates)
                {
                    if (ModLister.GetActiveModWithIdentifier(pkg) != null)
                    {
                        _detectedPackageId = pkg;
                        break;
                    }
                }

                if (_detectedPackageId == null)
                {
                    // Rimworld keeps an "ActiveModsInLoadOrder" list — scan as fallback.
                    // Note: ActiveModsInLoadOrder enumerates ModMetaData (Defs), not ModContentPack.
                    // ModMetaData.PackageId is the canonical id property (capital P).
                    foreach (var m in ModsConfig.ActiveModsInLoadOrder ?? Array.Empty<ModMetaData>())
                    {
                        if (m == null) continue;
                        var name = (m.PackageId ?? "").ToLowerInvariant();
                        if (name.Contains("rimthemes"))
                        {
                            _detectedPackageId = m.PackageId;
                            break;
                        }
                    }
                }

                if (_detectedPackageId == null)
                    return false;

                // Now scan all loaded assemblies for a type that looks like the API.
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type found = null;
                    try
                    {
                        found = asm.GetTypes()
                            .FirstOrDefault(t => (t.Namespace ?? "").Contains("RimThemes")
                                              || (t.Name ?? "").StartsWith("RimThemes"));
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Partial assembly load — collect what we got.
                        found = (ex.Types ?? Array.Empty<Type>())
                            .FirstOrDefault(t => t != null &&
                                ((t.Namespace ?? "").Contains("RimThemes") ||
                                 (t.Name ?? "").StartsWith("RimThemes")));
                    }

                    if (found == null) continue;

                    _rimThemesApiType = found;

                    // Look for a static method with one string parameter —
                    // typical signature: SetActiveTheme(string folderName) /
                    // SwitchTheme(string url) / etc.
                    _setThemeMethod = found.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .FirstOrDefault(m =>
                        {
                            if (m.GetParameters().Length != 1) return false;
                            var p = m.GetParameters()[0].ParameterType;
                            return p == typeof(string) || p == typeof(Uri);
                        });

                    if (_setThemeMethod != null) break;
                }

                if (_rimThemesApiType == null || _setThemeMethod == null)
                {
                    Log.Message($"[Rimconemy.Foundation] RimThemes package detected ({_detectedPackageId}) but no usable API surface found via reflection. Falling back.");
                    _rimThemesApiType = null;
                    _setThemeMethod = null;
                    return false;
                }

                Log.Message($"[Rimconemy.Foundation] RimThemes detected: package={_detectedPackageId}, apiType={_rimThemesApiType.FullName}, method={_setThemeMethod.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rimconemy.Foundation] RimThemes probe failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static void InvokeSetTheme(string themeName)
        {
            try
            {
                if (_setThemeMethod == null) return;
                var arg = themeName == null ? null : (object)themeName;
                _setThemeMethod.Invoke(null, new[] { arg });
            }
            catch (TargetInvocationException tie)
            {
                Log.Warning($"[Rimconemy.Foundation] RimThemes.SetTheme threw: {tie.InnerException?.GetType().Name}: {tie.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rimconemy.Foundation] RimThemes.SetTheme invocation error: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
