using System.Collections.Generic;

namespace Rimconemy.Foundation.Save
{
    /// <summary>
    /// Owner: Foundation.
    /// First-class schema-migration domain.
    ///
    /// Open-coded if/else cascades for "if (SchemaVersion &lt; 1) doX; if
    /// (SchemaVersion &lt; 2) doY;" are replaced with a structured list of
    /// <see cref="SchemaStep"/> objects whose <see cref="SchemaStep.Apply"/>
    /// delegates carry the migration code. The walker
    /// (<see cref="MigrationStepWalker"/>) and registry
    /// (<see cref="MigrationRegistry"/>) together own the lifecycle: per-step
    /// log emission, garbage-value normalisation, idempotence, and the
    /// unified save/load migration report.
    ///
    /// Owner-Constraint: a class implementing this interface is the SOLE
    /// owner of its schema migration code. Cross-package callers must only
    /// read <see cref="CurrentSchemaVersion"/>; they may not invoke
    /// <see cref="MigrateIfNeeded"/>.
    ///
    /// Reference: docs/H4-storage-query-contract.md, docs/INTERFACE_CONTRACT.md.
    /// </summary>
    public interface ISchemaMigratable
    {
        /// <summary>
        /// Stable owner-declared identifier used as the registry key. Must
        /// be non-null and non-empty. Convention: lowercase, package-prefixed,
        /// e.g. <c>"rimconemy.foundation.savedata"</c>.
        /// </summary>
        string ClassId { get; }

        /// <summary>
        /// Current schema version of the instance. Migrators MAY set this
        /// from Scribe data; the walker also writes to it. Negative values
        /// are normalised to 0 by <see cref="MigrationStepWalker.Migrate"/>.
        /// </summary>
        int SchemaVersion { get; set; }

        /// <summary>
        /// Schema version the running code knows. Migrators expose this
        /// implicitly via <c>int ISchemaMigratable.CurrentSchemaVersion
        /// =&gt; ClassName.CurrentSchemaVersion</c> to keep the existing
        /// type-level const usable from tests.
        /// </summary>
        int CurrentSchemaVersion { get; }

        /// <summary>
        /// Ordered migration steps. The walker iterates this list once and
        /// applies any step whose From/To range covers the current
        /// <see cref="SchemaVersion"/>. Migrators should build this list
        /// lazily and cache it for the lifetime of the instance.
        /// </summary>
        IList<SchemaStep> Steps { get; }

        /// <summary>
        /// Entry point invoked by <see cref="ExposeData"/> after Scribe has
        /// finished loading. Implementation should:
        /// 1. call <see cref="MigrationRegistry.Register"/> with this instance,
        /// 2. call <see cref="MigrationStepWalker.Migrate"/> with this instance,
        /// 3. record the result via <see cref="MigrationRegistry.RecordMigration"/>
        ///    if a real schema bump occurred.
        /// </summary>
        void MigrateIfNeeded();
    }
}
