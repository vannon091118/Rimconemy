using RimWorld;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05)
    /// H8: Ideology Assigner - maps profile to Rimconemy IdeoPreset.
    ///
    /// Two-track console for the Survival setting:
    ///   1. <see cref="AssignForProfile"/> - logs the recommended IdeoPreset
    ///      for the active profile. Always safe to call.
    ///   2. <see cref="TryAutoAssignToPlayerFaction"/> - defensively tries
    ///      to attach the matched IdeoPreset to the player's faction via
    ///      <c>Faction.OfPlayer.ideos</c>. Wrapped in try/catch because
    ///      IdeoManager API stability is NOT verified (Spike API-IDEOLOGY-01).
    ///
    /// Specification: Sprint-Plan H8.
    /// Runtime auto-assignment was deferred to a SPIKE because programmatic
    /// Ideo creation can conflict with biotech/ideology DLC loaders. We
    /// honor that contract: never crash, log diagnostics when API is
    /// unavailable.
    /// </summary>
    public static class IdeologyAssigner
    {
        /// <summary>Maps profile id to matching Rimconemy IdeoPreset defName.</summary>
        public static string GetPresetDefNameForProfile(SettingProfile profile)
        {
            if (profile == null) return null;
            return profile.ProfileId switch
            {
                "Rimconemy_Refuge" => "Rimconemy_Ideo_Refuge",
                "Rimconemy_Survival" => "Rimconemy_Ideo_Survival",
                "Rimconemy_Collapse" => "Rimconemy_Ideo_Collapse",
                _ => null,
            };
        }

        /// <summary>
        /// Resolves the IdeoPreset for the given profile.
        /// Returns null if Ideology DLC is inactive, profile is unknown,
        /// or the def is not in the database.
        /// </summary>
        public static IdeoPresetDef GetIdeoPresetForProfile(SettingProfile profile)
        {
            if (profile == null) return null;
            if (!ModsConfig.IdeologyActive)
            {
                Log.Message(
                    "[Rimconemy.InfectedAutomation] Assignment skipped: Ideology DLC inactive. " +
                    $"Recommended for profile {profile.ProfileId} but no preset resolved.");
                return null;
            }

            var presetDefName = GetPresetDefNameForProfile(profile);
            if (presetDefName == null)
            {
                Log.Warning(
                    $"[Rimconemy.InfectedAutomation] Unknown profile '{profile.ProfileId}'; " +
                    "cannot recommend an IdeoPreset.");
                return null;
            }

            var preset = DefDatabase<IdeoPresetDef>.GetNamedSilentFail(presetDefName);
            if (preset == null)
            {
                Log.Warning(
                    $"[Rimconemy.InfectedAutomation] IdeoPreset '{presetDefName}' not found in database. " +
                    "Did the Ideology defs ship with the package?");
                return null;
            }

            return preset;
        }

        /// <summary>
        /// Logs the recommended IdeoPreset for a profile. Always safe to call.
        /// Called from <c>StoryDirector.FinalizeInit</c> after profile resolution.
        /// </summary>
        public static void AssignForProfile(SettingProfile profile)
        {
            var preset = GetIdeoPresetForProfile(profile);
            if (preset == null) return;
            Log.Message(
                $"[Rimconemy.InfectedAutomation] Recommended ideology: {preset.label} " +
                $"({preset.defName}) for profile {profile.ProfileId}");
        }

        /// <summary>
        /// Defensive attempt to attach the IdeoPreset for a profile to the
        /// player's faction. Returns true on success, false on any failure.
        /// NEVER throws - all IDEOLOGY code paths are wrapped.
        /// </summary>
        /// <remarks>
        /// We use the FactionIdeosTracker implicit API path:
        ///   Faction.OfPlayer.ideos.PrimaryIdeo
        /// If anything fails, we log a warning and return false. The
        /// Spike API-IDEOLOGY-01 is still open, so manual verification
        /// in RimWorld 1.6 is required before flipping this from
        /// "diagnostic-only" to "real auto-assignment".
        /// </remarks>
        public static bool TryAutoAssignToPlayerFaction(SettingProfile profile)
        {
            var preset = GetIdeoPresetForProfile(profile);
            if (preset == null) return false;

            if (Current.Game == null || Faction.OfPlayer == null)
            {
                Log.Message(
                    "[Rimconemy.InfectedAutomation] Runtime auto-assignment deferred: no active Game/Faction. " +
                    "This is normal during Main Menu.");
                return false;
            }

            // Wrap in try/catch so we never break StoryDirector's FinalizeInit
            // when IdeoManager's programmatic creation API is missing on a
            // given patch level.
            try
            {
                var ideos = Faction.OfPlayer.ideos;
                if (ideos == null)
                {
                    Log.Warning(
                        "[Rimconemy.InfectedAutomation] Faction.OfPlayer.ideos is null. " +
                        "Ideology DLC may not be fully loaded; skipping auto-assignment.");
                    return false;
                }

                if (ideos.PrimaryIdeo != null)
                {
                    // Player already has a primary ideology - we don't override
                    // a player choice. Log so the operator sees the decision.
                    Log.Message(
                        $"[Rimconemy.InfectedAutomation] Player faction already has primary ideology: " +
                        $"{ideos.PrimaryIdeo.name}. Recommended preset '{preset.defName}' NOT applied " +
                        "(auto-assign respects existing ideology).");
                    return false;
                }

                // No ideology set yet. Try a soft attach via the IdeoManager.
                // The exact create method depends on the Ideology DLC version;
                // we log-only here so we never trigger an API crash.
                Log.Message(
                    $"[Rimconemy.InfectedAutomation] Runtime auto-assignment available for preset " +
                    $"'{preset.defName}' but Spike API-IDEOLOGY-01 is OPEN; not applying. " +
                    "Player should choose ideology in Ideo Setup screen to enable matching preset.");
                return false;
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    $"[Rimconemy.InfectedAutomation] TryAutoAssignToPlayerFaction guarded exception: " +
                    $"{ex.GetType().Name}: {ex.Message}. Spike API-IDEOLOGY-01 path will document this.");
                return false;
            }
        }

        /// <summary>
        /// Diagnostic diagnostic: count IdeoPresetDef entries that ship with
        /// this package. Counts via DefDatabase. Used by Codex diagnostics to
        /// ensure Ideology defs are loaded.
        /// </summary>
        public static int CountShippedIdeoPresets()
        {
            int count = 0;
            foreach (var def in DefDatabase<IdeoPresetDef>.AllDefs)
            {
                if (def != null && def.defName != null && def.defName.StartsWith("Rimconemy_Ideo_"))
                    count++;
            }
            return count;
        }
    }
}
