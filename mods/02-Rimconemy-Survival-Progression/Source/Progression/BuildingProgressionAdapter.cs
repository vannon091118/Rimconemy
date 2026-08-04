using System.Collections.Generic;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression
{
    /// <summary>
    /// Package-02 read-side boundary for Building progression.
    /// Validated construction/reparatur output supplies the idempotency key;
    /// this adapter never polls ticks and never mutates pawn state.
    /// </summary>
    public static class BuildingProgressionAdapter
    {
        public const string BuildingWorkTypeId = "Building";

        private static readonly BuildingProgressionLedger StandaloneLedger
            = new BuildingProgressionLedger();

        public static bool TryCreateAward(
            string idempotencyKey,
            string pawnId,
            int amount,
            out BuildingXpAward award)
        {
            return ResolveLedger().TryAward(idempotencyKey, pawnId, amount, 0L, out award);
        }

        /// <summary>Returns the save-owned ledger when a game exists.</summary>
        public static BuildingProgressionLedger ResolveLedger()
        {
            if (Current.Game != null)
            {
                var component = Current.Game.GetComponent<ProgressionGameComponent>();
                if (component != null)
                {
                    component.EnsureBuildingAwards();
                    return component.BuildingAwards;
                }
            }
            return StandaloneLedger;
        }
    }

    /// <summary>
    /// Persistent idempotency ledger for validated Building output.
    /// The ledger stores the accepted award record, not a second XP balance.
    /// </summary>
    public sealed class BuildingProgressionLedger : IExposable
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public List<BuildingXpAward> Awards = new List<BuildingXpAward>();

        private readonly HashSet<string> _keys
            = new HashSet<string>(System.StringComparer.Ordinal);
        private List<string> _keysForSave;
        private List<string> _pawnsForSave;
        private List<int> _amountsForSave;
        private List<long> _ticksForSave;

        public int AwardCount => Awards?.Count ?? 0;

        public bool TryAward(
            string idempotencyKey,
            string pawnId,
            int amount,
            long currentTick,
            out BuildingXpAward award)
        {
            award = default(BuildingXpAward);
            if (string.IsNullOrEmpty(idempotencyKey)
                || string.IsNullOrEmpty(pawnId)
                || amount <= 0)
                return false;

            if (_keys.Contains(idempotencyKey))
            {
                for (int i = 0; i < Awards.Count; i++)
                {
                    if (Awards[i].Key == idempotencyKey)
                    {
                        award = Awards[i];
                        break;
                    }
                }
                return false;
            }

            award = new BuildingXpAward
            {
                Key = idempotencyKey,
                PawnId = pawnId,
                Amount = amount,
                AwardTick = currentTick,
            };
            _keys.Add(idempotencyKey);
            Awards.Add(award);
            return true;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref SchemaVersion, "buildingProgressionSchema", CurrentSchemaVersion);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _keysForSave = new List<string>();
                _pawnsForSave = new List<string>();
                _amountsForSave = new List<int>();
                _ticksForSave = new List<long>();
                foreach (var award in Awards ?? new List<BuildingXpAward>())
                {
                    _keysForSave.Add(award.Key ?? "");
                    _pawnsForSave.Add(award.PawnId ?? "");
                    _amountsForSave.Add(award.Amount);
                    _ticksForSave.Add(award.AwardTick);
                }
            }

            Scribe_Collections.Look(ref _keysForSave, "buildingAwardKeys", LookMode.Value);
            Scribe_Collections.Look(ref _pawnsForSave, "buildingAwardPawns", LookMode.Value);
            Scribe_Collections.Look(ref _amountsForSave, "buildingAwardAmounts", LookMode.Value);
            Scribe_Collections.Look(ref _ticksForSave, "buildingAwardTicks", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Awards = new List<BuildingXpAward>();
                _keys.Clear();
                int count = _keysForSave?.Count ?? 0;
                for (int i = 0; i < count; i++)
                {
                    string key = _keysForSave[i];
                    if (string.IsNullOrEmpty(key) || !_keys.Add(key)) continue;
                    Awards.Add(new BuildingXpAward
                    {
                        Key = key,
                        PawnId = ValueAt(_pawnsForSave, i, ""),
                        Amount = ValueAt(_amountsForSave, i, 0),
                        AwardTick = ValueAt(_ticksForSave, i, 0L),
                    });
                }
                SchemaVersion = CurrentSchemaVersion;
                _keysForSave = null;
                _pawnsForSave = null;
                _amountsForSave = null;
                _ticksForSave = null;
            }

            if (Awards == null) Awards = new List<BuildingXpAward>();
            if (_keys.Count == 0)
                foreach (var award in Awards)
                    if (!string.IsNullOrEmpty(award.Key)) _keys.Add(award.Key);
        }

        private static T ValueAt<T>(List<T> values, int index, T fallback)
        {
            return values != null && index >= 0 && index < values.Count ? values[index] : fallback;
        }
    }

    public struct BuildingXpAward
    {
        public string Key;
        public string PawnId;
        public int Amount;
        public long AwardTick;
    }
}
