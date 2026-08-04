using System.Collections.Generic;
using Rimconemy.SurvivalProgression.Character;
using RimWorld;
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
            if (pawn == null || record == null) return;
            Records[pawn.thingIDNumber] = record;
        }

        /// <summary>Reads a pawn record. Returns null if absent.</summary>
        public PawnSetupRecord GetFor(int thingIdNumber)
        {
            if (thingIdNumber == 0) return null;
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
            Scribe_Collections.Look(ref Records, "charSetupRecords", LookMode.Value);

            // Schema-migration scaffold: if the saved SchemaVersion is
            // older, run a no-op migration path so the field is consistent
            // with current SchemaVersion after Save/Load.
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
                foreach (var s in pawn.skills.skills)
                {
                    if (s?.def == null) continue;
                    SkillDefNames.Add(s.def.defName);
                    SkillLevels.Add(s.Level);
                }
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
