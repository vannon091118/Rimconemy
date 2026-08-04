using System;

namespace Rimconemy.Foundation.Save
{
    /// <summary>
    /// Owner: Foundation.
    /// One migration step. The <see cref="Apply"/> delegate captures the
    /// owning migrator instance via closure, so direct field access is
    /// possible without downcasting.
    ///
    /// Per-instance allocation cost is one closure object per step, paid
    /// once per game-load when the owning migrator first builds its
    /// <c>Steps</c> list. The cost is negligible compared to Scribe IO.
    /// </summary>
    public sealed class SchemaStep
    {
        public int FromVersion { get; }
        public int ToVersion { get; }
        public string Description { get; }
        public Action Apply { get; }

        public SchemaStep(int fromVersion, int toVersion, string description, Action apply)
        {
            if (toVersion <= fromVersion)
                throw new ArgumentException(
                    $"SchemaStep requires ToVersion ({toVersion}) > FromVersion ({fromVersion}).");
            if (apply == null)
                throw new ArgumentNullException(nameof(apply));

            FromVersion = fromVersion;
            ToVersion = toVersion;
            Description = description ?? "";
            Apply = apply;
        }
    }
}
