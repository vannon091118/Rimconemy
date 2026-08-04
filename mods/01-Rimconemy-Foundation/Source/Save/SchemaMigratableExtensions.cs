using Verse;

namespace Rimconemy.Foundation.Save
{
    /// <summary>
    /// Owner: Foundation.
    /// Static helper that centralises the standard
    /// <see cref="ISchemaMigratable.MigrateIfNeeded"/> wrapper:
    /// (1) self-register with the central registry, (2) delegate the walk to
    /// <see cref="MigrationStepWalker"/>, (3) record the bump for the unified
    /// cross-package migration report. Migrators that need Foundation-specific
    /// side-effects (e.g. <c>FoundationSaveData</c> setting
    /// <c>WasMigrated</c> + <c>MigrationDetail</c>) keep their entry point
    /// custom and call this helper internally for the canonical work.
    /// </summary>
    public static class SchemaMigratableExtensions
    {
        /// <summary>
        /// Canonical migration walk for non-Foundation migrators. This is a
        /// pure orchestration helper: it does not own any migrator-side
        /// state. Foundation-specific diagnostics stay in
        /// <c>FoundationSaveData.MigrateIfNeeded</c>.
        /// </summary>
        public static void RunMigration(this ISchemaMigratable self)
        {
            if (self == null) return;

            MigrationRegistry.Register(self);
            int oldV = self.SchemaVersion;
            int newV = MigrationStepWalker.Migrate(self);

            // Record only if a real schema bump occurred (the walker already
            // emits a Log.Message; this appends to the unified registry log).
            if (newV > oldV)
                MigrationRegistry.RecordMigration(self.ClassId, oldV, newV);
        }
    }
}
