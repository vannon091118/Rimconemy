using System.Collections.Generic;
using Verse;

namespace Rimconemy.Foundation.Save
{
    /// <summary>
    /// Owner: Foundation.
    /// Static walker that applies <see cref="SchemaStep"/> objects to a
    /// migrator. Garbage-value normalisation lives here — once per walk —
    /// so individual migrators never repeat <c>if (SchemaVersion &lt; 0)
    /// SchemaVersion = 0;</c>.
    ///
    /// Walker emits the canonical, one-line migration log. Migrators must
    /// drop their own <c>Log.Message</c> calls so the log line count stays
    /// at most one per migrator per save.
    ///
    /// Failure policy: a step exception propagates. We do NOT catch and
    /// we do NOT mark the step as applied — that would lie about success.
    /// RimWorld's Scribe keeps PostLoadInit on the main thread; the next
    /// boot will retry from the current state.
    /// </summary>
    public static class MigrationStepWalker
    {
        public const string LogPrefix = "[Rimconemy.Foundation.Save]";

        /// <summary>
        /// Walks <see cref="ISchemaMigratable.Steps"/> for the given
        /// migrator. Returns the new <see cref="ISchemaMigratable.SchemaVersion"/>.
        /// Idempotent: re-running on a state already at
        /// <see cref="ISchemaMigratable.CurrentSchemaVersion"/> is a no-op
        /// and emits no log line.
        /// </summary>
        public static int Migrate(ISchemaMigratable migratable)
        {
            if (migratable == null) return 0;

            // Garbage normalisation: contracts don't expect negative schema
            // values. Clamp once per walk.
            if (migratable.SchemaVersion < 0)
                migratable.SchemaVersion = 0;

            if (migratable.SchemaVersion == migratable.CurrentSchemaVersion)
                return migratable.SchemaVersion;

            int old = migratable.SchemaVersion;
            int applied = 0;

            IList<SchemaStep> steps = migratable.Steps;
            if (steps != null)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    if (step == null || step.Apply == null) continue;

                    // Step is applicable if SchemaVersion sits in
                    // [FromVersion, ToVersion). A step we already walked past
                    // is also skipped (target version not yet reached means
                    // we are below the gate).
                    if (migratable.SchemaVersion >= step.ToVersion) continue;
                    if (migratable.SchemaVersion < step.FromVersion) continue;

                    // Apply without try/catch: an exception means the step
                    // failed and the walk must visibly stall. RimWorld's
                    // PostLoadInit logs the stack trace, and the next boot
                    // retries from the current SchemaVersion.
                    step.Apply();
                    migratable.SchemaVersion = step.ToVersion;
                    applied++;
                    Log.Message(
                        $"{LogPrefix} {migratable.ClassId} step " +
                        $"v{step.FromVersion}->v{step.ToVersion}: {step.Description}.");
                }
            }

            // Safety net: no remaining step brought us forward. Force to
            // current so the next save-roundtrip starts clean. This is
            // strictly less safe than walking a real step, so it logs a
            // warning whenever it kicks in.
            if (migratable.SchemaVersion < migratable.CurrentSchemaVersion)
            {
                Log.Warning(
                    $"{LogPrefix} {migratable.ClassId} migration chain " +
                    $"stuck at v{migratable.SchemaVersion}, expected " +
                    $"v{migratable.CurrentSchemaVersion}. Forcing forward " +
                    $"(no covering step in registered Steps list).");
                migratable.SchemaVersion = migratable.CurrentSchemaVersion;
            }

            // Canonical one-line summary, emitted exactly once per migration
            // walk (not per step). Migrators must not log themselves; the
            // walker is the single source of truth.
            if (applied > 0 || old != migratable.SchemaVersion)
            {
                Log.Message(
                    $"{LogPrefix} {migratable.ClassId} MigrateIfNeeded: " +
                    $"v{old} -> v{migratable.SchemaVersion} ({applied} step(s)).");
            }

            return migratable.SchemaVersion;
        }
    }
}
