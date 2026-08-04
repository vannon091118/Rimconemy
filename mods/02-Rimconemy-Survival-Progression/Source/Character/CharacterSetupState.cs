using System.Collections.Generic;
using RimWorld;
using Rimconemy.SurvivalProgression.Character;
using Verse;

namespace Rimconemy.SurvivalProgression.Character
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02).
    ///
    /// Persists the per-pawn character setup scorecard (skills + traits + agefix)
    /// so Save/Load deterministically reproduces the BioRemap + SkillBudget
    /// pipeline. Phase-4.2 of the Character Setup work (ROADMAP §8.4).
    ///
    /// The GameComponent owner-site is `ProgressionGameComponent`.
    /// This class is a *passive* carrier - it does NOT run game logic.
    /// Logic is delegated to <see cref="CharacterSetup"/> at
    /// `Page_ConfigureStartingPawns.PreOpen` (visual fix) and on
    /// `ProgressionGameComponent.FinalizeInit` (save-load replay).
    ///
    /// Stored values:
    ///   - schema version (must match or we trigger migration)
    ///   - was-applied flag (idempotent re-entry)
    ///   - per pawn: thingIDNumber, ageFix-corrected ticks, skillTotals,
    ///     chosenTraitDefNames, neutral-band classification
    ///
    /// Specification: docs/H5-character-setup-formula.md + H6 spike notes.
    /// </summary>
    public sealed class CharacterSetupState : GameComponent, IExposable
    {
        public const int CurrentSchemaVersion = 1;

        // Schema version for migration. Bump ONLY when fields change shape.
        public int SchemaVersion = 1;

        // True once the BioRemap+SkillBudget pipeline has been applied at
        // least once for the current save. Saved across Save/Load so we
        // don't re-apply on every load.
        public bool Applied;

        // Per-pawn scorecard. Key is pawn.thingIDNumber (stable across Save/Load).
        public Dictionary<int, PawnSetupRecord> Records = new Dictionary<int, PawnSetupRecord>();

        public CharacterSetupState(Game game) { }

        /// <summary>Adds / overwrites a pawn record.</summary>
        public void Upsert(Pawn pawn, PawnSetupRecord record)
        {
            if (pawn == null || pawn.thingIDNumber == 0 || record == null) return;
            if (Records == null) Records = new Dictionary<int, PawnSetupRecord>();
            Records[pawn.thingIDNumber] = record;
        }

        /// <summary>
        /// Records the completed setup for each supplied pawn. Re-recording a
        /// pawn replaces its scorecard instead of creating duplicate entries.
        /// The Applied flag is only raised when the supplied records are valid
        /// and, when requested, complete; empty or partial input cannot claim
        /// completion. Each attempt replaces the active scorecard atomically,
        /// so failed retries cannot leave stale pawn records behind.
        /// </summary>
        public int RecordAppliedPawns(IEnumerable<Pawn> pawns)
        {
            return RecordAppliedPawns(pawns, -1);
        }

        /// <summary>
        /// Records a setup only when the optional expected count matches the
        /// distinct, valid Pawn records. A non-negative expected count is a
        /// fail-closed completion gate: partial initialization never becomes
        /// a falsely persistent setup.
        /// </summary>
        public int RecordAppliedPawns(IEnumerable<Pawn> pawns, int expectedCount)
        {
            // This method describes the current attempt, not historical
            // success. A failed retry must never leave stale records in the
            // active scorecard or leave the UI claiming that the new
            // selection was persisted.
            Applied = false;
            Records = new Dictionary<int, PawnSetupRecord>();
            if (pawns == null) return 0;
            if (expectedCount < -1 || expectedCount == 0) return 0;

            var pending = new Dictionary<int, PawnSetupRecord>();
            foreach (var pawn in pawns)
            {
                if (pawn == null || pawn.thingIDNumber == 0 || pending.ContainsKey(pawn.thingIDNumber)) continue;
                pending[pawn.thingIDNumber] = new PawnSetupRecord(pawn);
            }

            if (expectedCount >= 0 && pending.Count != expectedCount)
            {
                // The current attempt is authoritative. Invalid/partial
                // input leaves an empty inactive scorecard rather than
                // exposing records from a previous pawn selection.
                return 0;
            }

            foreach (var pair in pending)
                Records[pair.Key] = pair.Value;

            if (pending.Count > 0 && (expectedCount < 0 || pending.Count == expectedCount))
                Applied = true;
            return pending.Count;
        }

        /// <summary>Reads a pawn record. Returns null if absent.</summary>
        public PawnSetupRecord GetFor(int thingIdNumber)
        {
            if (thingIdNumber == 0 || Records == null) return null;
            return Records.TryGetValue(thingIdNumber, out var r) ? r : null;
        }

        /// <summary>Public access for tests/UI.</summary>
        public static CharacterSetupState Get()
        {
            if (Current.Game == null) return null;
            return Current.Game.GetComponent<CharacterSetupState>();
        }

        public override void GameComponentTick() { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref SchemaVersion, "charSetupSchema", 1);
            Scribe_Values.Look(ref Applied, "charSetupApplied", false);
            Scribe_Collections.Look(ref Records, "charSetupRecords", LookMode.Deep);

            // Schema-migration scaffold: if the saved SchemaVersion is
            // older, run a no-op migration path so the field is consistent
            // with current SchemaVersion after Save/Load.
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Records == null) Records = new Dictionary<int, PawnSetupRecord>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && SchemaVersion < CurrentSchemaVersion)
            {
                Log.Message(
                    "[Rimconemy.SurvivalProgression] CharacterSetupState migration: " +
                    $"v{SchemaVersion} -> v{CurrentSchemaVersion} (no-op upgrade)");
                SchemaVersion = CurrentSchemaVersion;
            }
        }
    }

    /// <summary>Per-pawn scorecard that is persisted across Save/Load.</summary>
    public sealed class PawnSetupRecord : IExposable
    {
        public int AppliedTick;
        public int AgeBiologicalYears;
        public int AgeChronologicalYears;

        // Skill budget application: parallel lists so RimWorld's Scribe does
        // not have to reflect over our tuple-typed item type. Indices must
        // stay aligned across SkillDefNames/Levels.
        public List<string> SkillDefNames = new List<string>();
        public List<int> SkillLevels = new List<int>();

        public List<string> TraitDefNames = new List<string>();

        // Classification band from SkillBudgetCalculator.Classify(balance).
        // Stored as int for forward-compat; matches CharacterSetup.Neutral / PositiveLight
        // / PositiveStrong / NegativeLight / NegativeStrong.
        public int NeutralBand;

        public PawnSetupRecord() { }

        public PawnSetupRecord(Pawn pawn)
        {
            if (pawn?.ageTracker != null)
            {
                AppliedTick = Current.Game != null ? Find.TickManager.TicksGame : 0;
                long ticksBio = pawn.ageTracker.AgeBiologicalTicks;
                AgeBiologicalYears = (int)(ticksBio / (long)Rimconemy.Foundation.TimeConstants.TicksPerDay / 60);
                long ticksChr = pawn.ageTracker.AgeChronologicalTicks;
                AgeChronologicalYears = (int)(ticksChr / (long)Rimconemy.Foundation.TimeConstants.TicksPerDay / 60);
            }

            if (pawn?.skills?.skills != null)
            {
                int spentPoints = 0;
                foreach (var s in pawn.skills.skills)
                {
                    // The scorecard mirrors the applied H5 budget, not every
                    // mod-added/disabled skill that may exist on the pawn.
                    if (s?.def == null || s.TotallyDisabled
                        || !CharacterSetup.EligibleSkills.Contains(s.def)) continue;
                    SkillDefNames.Add(s.def.defName);
                    SkillLevels.Add(s.Level);
                    spentPoints += SkillBudgetCalculator.CostForLevel(s.Level);
                }
                NeutralBand = (int)SkillBudgetCalculator.Classify(
                    spentPoints - SkillBudgetCalculator.NeutralCenter);
            }

            if (pawn?.story?.traits?.allTraits != null)
            {
                foreach (var tr in pawn.story.traits.allTraits)
                {
                    if (tr?.def == null) continue;
                    TraitDefNames.Add(tr.def.defName);
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref AppliedTick, "appliedTick", 0);
            Scribe_Values.Look(ref AgeBiologicalYears, "ageBioYears", 0);
            Scribe_Values.Look(ref AgeChronologicalYears, "ageChrYears", 0);
            Scribe_Values.Look(ref NeutralBand, "neutralBand", 0);
            Scribe_Collections.Look(ref SkillDefNames, "skillDefNames", LookMode.Value);
            Scribe_Collections.Look(ref SkillLevels, "skillLevels", LookMode.Value);
            Scribe_Collections.Look(ref TraitDefNames, "traitDefNames", LookMode.Value);

            // After-scribe migration guards: missing fields fall to empty
            // list so downstream consumers do not see null entries.
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (SkillDefNames == null) SkillDefNames = new List<string>();
                if (SkillLevels == null) SkillLevels = new List<int>();
                if (TraitDefNames == null) TraitDefNames = new List<string>();
            }
        }
    }
}
