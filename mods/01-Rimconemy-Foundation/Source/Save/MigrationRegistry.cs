using System.Collections.Generic;
using System.Text;
using Verse;

namespace Rimconemy.Foundation.Save
{
    /// <summary>
    /// Owner: Foundation.
    /// Central registry of <see cref="ISchemaMigratable"/> instances.
    ///
    /// Migrators self-register on first <c>MigrateIfNeeded()</c> call;
    /// <see cref="MigrateAll"/> then walks the entire registry. The
    /// registry also owns the unified, cross-package migration report
    /// (<see cref="GetMigrationLog"/>) so a single pass through the Save/
    /// Load pipeline produces a single readable summary instead of four
    /// scattered log lines.
    ///
    /// Idempotent registrations are deliberately tolerated: another call
    /// with the same <see cref="ISchemaMigratable.ClassId"/> replaces the
    /// previous entry — typically because a new game instance overwrote
    /// the old reference. A warning is logged when the replacement is a
    /// different object so a bug (e.g. duplicated component registration)
    /// stays visible.
    /// </summary>
    public static class MigrationRegistry
    {
        private static readonly Dictionary<string, ISchemaMigratable> _byClassId
            = new Dictionary<string, ISchemaMigratable>();

        private static readonly List<string> _migrationLog = new List<string>();

        /// <summary>Read-only view of the registry. Snapshot — do not mutate.</summary>
        public static IReadOnlyDictionary<string, ISchemaMigratable> ByClassId => _byClassId;

        /// <summary>
        /// Self-registration by migrators. Idempotent: re-registering the
        /// same instance is silent; replacing with a different instance
        /// logs a warning so an architectural mistake surfaces.
        /// </summary>
        public static void Register(ISchemaMigratable migratable)
        {
            if (migratable == null) return;

            string classId = migratable.ClassId;
            if (string.IsNullOrEmpty(classId))
            {
                Log.Warning($"{MigrationStepWalker.LogPrefix} MigrationRegistry.Register called with empty ClassId; ignoring.");
                return;
            }

            if (_byClassId.TryGetValue(classId, out var existing)
                && !ReferenceEquals(existing, migratable))
            {
                Log.Warning(
                    $"{MigrationStepWalker.LogPrefix} MigrationRegistry classId " +
                    $"'{classId}' re-registered with a different instance; replacing.");
            }
            _byClassId[classId] = migratable;
        }

        /// <summary>Removes an entry from the registry.</summary>
        public static void Unregister(string classId)
        {
            if (string.IsNullOrEmpty(classId)) return;
            _byClassId.Remove(classId);
        }

        /// <summary>
        /// Wipes the registry and the migration log. Intended for hot-reload
        /// between tests and for the rare case of a clean Save re-import.
        /// </summary>
        public static void Clear()
        {
            _byClassId.Clear();
            _migrationLog.Clear();
        }

        /// <summary>
        /// Drives <c>MigrateIfNeeded()</c> on every registered migrator.
        /// Order is determined by <see cref="ISchemaMigratable.ClassId"/>
        /// for determinism — useful for predictable log ordering.
        /// </summary>
        public static void MigrateAll()
        {
            if (_byClassId.Count == 0) return;

            // Copy keys to avoid InvalidOperationException if a migrator
            // re-registers itself during its own walk.
            string[] keys = new string[_byClassId.Count];
            int idx = 0;
            foreach (var k in _byClassId.Keys) keys[idx++] = k;
            System.Array.Sort(keys, System.StringComparer.Ordinal);

            for (int i = 0; i < keys.Length; i++)
            {
                if (_byClassId.TryGetValue(keys[i], out var m) && m != null)
                    m.MigrateIfNeeded();
            }
        }

        /// <summary>
        /// Records a successful bump so the unified migration report can
        /// report later what happened during the last Save/Load.
        /// </summary>
        public static void RecordMigration(string classId, int oldVersion, int newVersion)
        {
            if (string.IsNullOrEmpty(classId)) return;
            _migrationLog.Add($"{classId}: v{oldVersion} -> v{newVersion}");
        }

        /// <summary>
        /// Returns a unified, human-readable summary of every migration
        /// applied during the current save/load cycle.
        /// </summary>
        public static string GetMigrationLog()
        {
            if (_migrationLog.Count == 0)
                return "No Rimconemy schema migrations recorded for this save/load cycle.";

            var sb = new StringBuilder();
            sb.Append("Rimconemy schema migrations recorded: ");
            for (int i = 0; i < _migrationLog.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append(_migrationLog[i]);
            }
            return sb.ToString();
        }
    }
}
