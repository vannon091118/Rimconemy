using System.Collections.Generic;
using Rimconemy.Foundation.Save;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Tutorial State — persists completed steps, current step, dismissed flag.
    /// Owner: Infected & Automation (Paket 05).
    /// Implements ISchemaMigratable for save/load migration.
    /// </summary>
    public class TutorialState : IExposable, ISchemaMigratable
    {
        public HashSet<string> CompletedSteps = new();
        public int CurrentStepIndex = -1;
        public bool Dismissed = false;

        // ── ISchemaMigratable contract ────────────────────────

        public const int CurrentSchemaVersion = 1;

        public string ClassId => "rimconemy.infectedautomation.tutorial";

        int ISchemaMigratable.SchemaVersion
        {
            get => SchemaVersion;
            set => SchemaVersion = value;
        }

        public int SchemaVersion = CurrentSchemaVersion;

        int ISchemaMigratable.CurrentSchemaVersion => CurrentSchemaVersion;

        private List<SchemaStep> _cachedSteps;
        public IList<SchemaStep> Steps
        {
            get
            {
                if (_cachedSteps != null) return _cachedSteps;
                _cachedSteps = new List<SchemaStep>
                {
                    // v0 → v1: ensure CompletedSteps is non-null.
                    new SchemaStep(0, 1,
                        "Initialize CompletedSteps HashSet if missing.",
                        () => { if (CompletedSteps == null) CompletedSteps = new HashSet<string>(); }),
                };
                return _cachedSteps;
            }
        }

        public void MigrateIfNeeded()
        {
            this.RunMigration();
        }

        // ── IExposable ────────────────────────────────────────

        public void ExposeData()
        {
            Scribe_Collections.Look(ref CompletedSteps, "completedSteps", LookMode.Value);
            Scribe_Values.Look(ref CurrentStepIndex, "currentStepIndex");
            Scribe_Values.Look(ref Dismissed, "dismissed");
            Scribe_Values.Look(ref SchemaVersion, "schemaVersion", 1);

            // PostLoadInit: migration trigger
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (CompletedSteps == null) CompletedSteps = new HashSet<string>();
                MigrateIfNeeded();
            }
        }
    }
}