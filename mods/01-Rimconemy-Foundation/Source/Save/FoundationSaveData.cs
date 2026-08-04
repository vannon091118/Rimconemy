using System;
using System.Collections.Generic;
using System.Globalization;
using Rimconemy.Foundation.Events;
using Rimconemy.Foundation.Models;
using Rimconemy.Foundation.Profile;
using Rimconemy.Foundation.Registry;
using Verse;

namespace Rimconemy.Foundation.Save
{
    /// <summary>
    /// Owner: Foundation
    /// GameComponent that persists Foundation state: profile status,
    /// package registrations, event log state, and save schema version.
    ///
    /// Handles migration: old saves without Foundation data are detected
    /// and migrated with a logged warning.
    ///
    /// Hook reason: GameComponent is the canonical RimWorld mechanism for
    /// per-save persistent data. ExposeData handles save/load.
    /// </summary>
    public class FoundationSaveData : GameComponent
    {
        public const int CurrentSchemaVersion = 1;

        /// <summary>Schema version found in the save; 0 means new game or pre-Foundation data.</summary>
        public int LoadedSchemaVersion { get; private set; }

        /// <summary>Whether a migration was applied during the last load.</summary>
        public bool WasMigrated { get; private set; }

        /// <summary>Migration detail message for UI display.</summary>
        public string MigrationDetail { get; private set; }

        /// <summary>Profile status persisted across save/load.</summary>
        public ProfileStatus SavedProfileStatus { get; private set; }

        /// <summary>Missing package IDs persisted across save/load.</summary>
        public List<string> SavedMissingPackageIds { get; private set; }

        /// <summary>Registered package IDs and versions persisted across save/load.</summary>
        public List<string> SavedPackageVersions { get; private set; }

        /// <summary>Persisted event log entries for survival across save/load (last 100).</summary>
        public List<EventRecord> SavedEvents { get; private set; }

        /// <summary>
        /// Phase 0-A: User-set preference for global theme override (opt-in,
        /// requires RimThemes to be active). Persisted across save/load.
        /// Public setter is intentional: ThemeSettings must toggle this from
        /// the UI; FoundationSaveData remains the canonical owner.
        /// </summary>
        public bool EnableGlobalThemeOverride { get; set; }

        /// <summary>
        /// Track 2-C / F-T2: Sandbox mode flag. When true, ProgressionGameComponent
        /// does NOT trigger GameOver on colony-wipe; Storyteller continues running.
        /// Setter is public because ScenPart_StartInSandbox (Mod 02) toggles this
        /// on scenario-start. FoundationSaveData remains canonical owner.
        /// </summary>
        public bool IsSandboxMode { get; set; }

        private bool _isLoadingSave;

        public FoundationSaveData(Game game)
        {
            LoadedSchemaVersion = 0;
            WasMigrated = false;
            MigrationDetail = "";
            SavedProfileStatus = ProfileStatus.Standalone;
            SavedMissingPackageIds = new List<string>();
            SavedPackageVersions = new List<string>();
            SavedEvents = new List<EventRecord>();
            EnableGlobalThemeOverride = false;
            IsSandboxMode = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            _isLoadingSave = Scribe.mode == LoadSaveMode.LoadingVars;

            int schemaVersion = LoadedSchemaVersion;
            Scribe_Values.Look(ref schemaVersion, "foundationSchemaVersion", 0);
            LoadedSchemaVersion = schemaVersion;

            // Refresh owned read-only status before saving; feature data is never written here.
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                SavedProfileStatus = ProfileDetector.CurrentProfile;
                SavedMissingPackageIds = new List<string>(ProfileDetector.MissingPackageIds);
                SavedPackageVersions = new List<string>(PackageRegistry.GetRegisteredPackageVersions());
            }

            // Persist profile status
            var profileStr = SavedProfileStatus.ToString();
            Scribe_Values.Look(ref profileStr, "foundationProfileStatus", "Standalone");
            if (Scribe.mode == LoadSaveMode.LoadingVars
                && System.Enum.TryParse<ProfileStatus>(profileStr, out var parsed))
                SavedProfileStatus = parsed;

            // Persist missing package IDs
            var missingPkgIds = new List<string>(SavedMissingPackageIds);
            Scribe_Collections.Look(ref missingPkgIds, "foundationMissingPackages", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                SavedMissingPackageIds = missingPkgIds ?? new List<string>();

            var packageVersions = new List<string>(SavedPackageVersions);
            Scribe_Collections.Look(ref packageVersions, "foundationPackageVersions", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                SavedPackageVersions = packageVersions ?? new List<string>();

            // Persist event log entries (up to 100 most recent) as a fixed seven-field
            // envelope. User-controlled fields use the escape-aware grammar below
            // so delimiters remain safe without ambiguous chained replacements.
            var eventStrings = new List<string>();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                foreach (var evt in EventLog.RecentEvents)
                {
                    if (eventStrings.Count >= 100) break;
                    eventStrings.Add(SerializeEvent(evt));
                }
            }
            Scribe_Collections.Look(ref eventStrings, "foundationEventLog", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && eventStrings != null)
            {
                SavedEvents.Clear();
                foreach (var entry in eventStrings)
                {
                    var historical = DeserializeEvent(entry);
                    if (historical != null)
                        SavedEvents.Add(historical);
                }
            }

            // Phase 0-A: theme-override preference (opt-in, requires RimThemes-Availability).
            bool themeOverride = EnableGlobalThemeOverride;
            Scribe_Values.Look(ref themeOverride, "foundationEnableGlobalThemeOverride", false);
            EnableGlobalThemeOverride = themeOverride;

            // Track 2-C / F-T2: sandbox-mode flag (default false = Standard policy).
            bool sandboxMode = IsSandboxMode;
            Scribe_Values.Look(ref sandboxMode, "foundationIsSandboxMode", false);
            IsSandboxMode = sandboxMode;

            // Replace the live history before adding load/migration diagnostics.
            if (_isLoadingSave)
                EventLog.ReplaceHistorical(SavedEvents);

            // A LoadingVars pass always represents an existing save. A missing
            // foundationSchemaVersion is therefore the documented v0 migration case;
            // actual new-game initialization happens in FinalizeInit().
            if (Scribe.mode == LoadSaveMode.LoadingVars && LoadedSchemaVersion < CurrentSchemaVersion)
                MigrateFrom(LoadedSchemaVersion);

            // After loading, allow ProfileDetector to re-run if needed
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                ProfileDetector.ResetForReload();
                ProfileDetector.DetectProfile();
                SavedProfileStatus = ProfileDetector.CurrentProfile;
                SavedMissingPackageIds = new List<string>(ProfileDetector.MissingPackageIds);
                SavedPackageVersions = new List<string>(PackageRegistry.GetRegisteredPackageVersions());
            }
        }

        /// <summary>
        /// Migrates save data from an older schema version.
        /// </summary>
        private void MigrateFrom(int oldVersion)
        {
            WasMigrated = true;
            MigrationDetail = $"Migrated from schema v{oldVersion} to v{CurrentSchemaVersion}. ";

            if (oldVersion < 1)
            {
                // Future: migrate from v0 (no Foundation data) to v1
                MigrationDetail += "Initial Foundation schema applied. No data loss.";
            }

            LoadedSchemaVersion = CurrentSchemaVersion;

            EventLog.Record("Save", "Migration", "rimconemy.foundation",
                $"Foundation schema migrated from v{oldVersion} to v{CurrentSchemaVersion}.",
                MigrationDetail);

            Log.Message($"[Rimconemy.Foundation] {MigrationDetail}");
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            // Profile detection runs once at startup via static constructors.
            // Re-detection after save/load is handled by ExposeData which
            // calls ProfileDetector.ResetForReload() + DetectProfile().
        }

        /// <summary>
        /// Called after all game components are loaded. For new games (no prior save),
        /// initializes schema version to current; loaded history was restored in ExposeData().
        /// </summary>
        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // StaticConstructorOnStartup can run before every optional mod
            // assembly has entered AppDomain.CurrentDomain. Refresh once at
            // the game-component boundary so ProfileDetector does not freeze
            // a partial package set from the early bootstrap.
            PackageRegistry.RefreshLoadedFeaturePackages();
            ProfileDetector.ResetForReload();
            ProfileDetector.DetectProfile();
            Tests.FoundationProfileRefreshTests.RunAll();
            Tests.FoundationBuildingCapabilityTests.RunAll();

            // For new games, ExposeData hasn't run yet — set schema to current
            if (LoadedSchemaVersion == 0)
            {
                LoadedSchemaVersion = CurrentSchemaVersion;
                MigrationDetail = "New save; no migration needed.";
                EventLog.Record("Save", "NewGame", "rimconemy.foundation",
                    "Foundation save data initialized.",
                    $"Schema v{CurrentSchemaVersion}");
            }

            // Phase 0-A: Apply opt-in RimThemes bridge now that Current.Game / ModsConfig
            // are stable. Best-effort, never throws — see GlobalThemeOverride for details.
            try
            {
                Rimconemy.Foundation.UI.GlobalThemeOverride.ApplyIfRequested();
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rimconemy.Foundation] GlobalThemeOverride bootstrap skipped: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Event fields use an escaped pipe-delimited envelope. The escape
        // grammar is deliberately tiny and processed one character at a time:
        //   \\\\ = literal backslash, \\p = literal pipe.
        // A scanner is required; chained Replace() calls are ambiguous for
        // sequences such as a Windows path ending in \\p.

        private static string PipeEscape(string value)
        {
            if (value == null) return "";
            var escaped = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\\')
                    escaped.Append("\\\\");
                else if (c == '|')
                    escaped.Append("\\p");
                else
                    escaped.Append(c);
            }
            return escaped.ToString();
        }

        private static string PipeUnescape(string value)
        {
            if (value == null) return "";
            var unescaped = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i + 1 >= value.Length)
                {
                    unescaped.Append(c);
                    continue;
                }

                char escaped = value[++i];
                if (escaped == '\\')
                    unescaped.Append('\\');
                else if (escaped == 'p')
                    unescaped.Append('|');
                else
                {
                    // Preserve unknown escape sequences instead of silently
                    // changing user text from a future/legacy format.
                    unescaped.Append('\\');
                    unescaped.Append(escaped);
                }
            }
            return unescaped.ToString();
        }

        private static List<string> SplitEscapedFields(string serialized)
        {
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < serialized.Length; i++)
            {
                char c = serialized[i];
                if (c == '\\' && i + 1 < serialized.Length)
                {
                    // Keep the escape pair intact for PipeUnescape, while
                    // ensuring an escaped pipe is not treated as a delimiter.
                    current.Append(c);
                    current.Append(serialized[++i]);
                }
                else if (c == '|')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString());
            return fields;
        }

        private static string SerializeEvent(EventRecord evt)
        {
            return string.Join("|",
                evt.SequenceId.ToString(CultureInfo.InvariantCulture),
                evt.Tick.ToString(CultureInfo.InvariantCulture),
                PipeEscape(evt.Category ?? ""),
                PipeEscape(evt.EventType ?? ""),
                PipeEscape(evt.SourcePackageId ?? ""),
                PipeEscape(evt.Message ?? ""),
                PipeEscape(evt.Detail ?? ""));
        }

        private static EventRecord DeserializeEvent(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
                return null;

            var parts = SplitEscapedFields(serialized);
            if (parts.Count != 7
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sequenceId)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tick))
            {
                Log.Warning($"[Rimconemy.Foundation] EventLog: skipping unparseable entry (length={parts.Count}, seq={parts[0]}).");
                return null;
            }

            return new EventRecord(
                sequenceId,
                tick,
                PipeUnescape(parts[2]),
                PipeUnescape(parts[3]),
                PipeUnescape(parts[4]),
                PipeUnescape(parts[5]),
                PipeUnescape(parts[6]));
        }

        /// <summary>
        /// Returns a human-readable save diagnosis string for the UI.
        /// </summary>
        public string GetDiagnosis()
        {
            if (LoadedSchemaVersion == CurrentSchemaVersion && !WasMigrated)
                return "Save schema is current. No issues detected.";

            if (WasMigrated)
                return MigrationDetail;

            return $"Save schema v{LoadedSchemaVersion} (current: v{CurrentSchemaVersion}). Migration may be needed.";
        }
    }
}
