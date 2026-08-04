using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimconemy.EconomyTerritory.Transfers
{
    public enum TransferStatus
    {
        Blocked = 0,
        Reserved = 1,
        Executed = 2,
        Cancelled = 3,
    }

    public sealed class TransferRequest
    {
        public string PackageId;
        public string RequestId;
        public string IdempotencyKey;
        public string ResourceId;
        public int Amount;
        public long CurrentTick;
        public string StableKey => (PackageId ?? "") + "|" + (RequestId ?? "");
    }

    public sealed class TransferResult
    {
        public string TransferId;
        public TransferStatus Status;
        public string ResourceId;
        public int Amount;
        public string Reason;

        public TransferResult Clone()
        {
            return new TransferResult
            {
                TransferId = TransferId,
                Status = Status,
                ResourceId = ResourceId,
                Amount = Amount,
                Reason = Reason,
            };
        }
    }

    internal sealed class TransferRecord : IExposable
    {
        public string TransferId;
        public string StableKey;
        public string IdempotencyKey;
        public string ResourceId;
        public int Amount;
        public TransferStatus Status;
        public long LastActionTick;
        public string Reason;

        public void ExposeData()
        {
            Scribe_Values.Look(ref TransferId, "transferId", "");
            Scribe_Values.Look(ref StableKey, "stableKey", "");
            Scribe_Values.Look(ref IdempotencyKey, "idempotencyKey", "");
            Scribe_Values.Look(ref ResourceId, "resourceId", "");
            Scribe_Values.Look(ref Amount, "amount", 0);
            Scribe_Values.Look(ref Status, "status", TransferStatus.Blocked);
            Scribe_Values.Look(ref LastActionTick, "lastActionTick", 0L);
            Scribe_Values.Look(ref Reason, "reason", "");
        }
    }

    /// <summary>
    /// Package-04 owner of physical transfer booking. It reserves against a
    /// supplied physical stock read, but does not mutate RimWorld Things.
    /// Execute/Cancel mutate only this booking ledger; a future owner-side
    /// Thing mutation hook must commit the actual physical stack atomically.
    /// </summary>
    public sealed class PhysicalTransferService : IExposable
    {
        public const int CurrentSchemaVersion = 1;
        public const string CapabilityId = "rimconemy.economyterritory.physical_transfer";

        public int SchemaVersion = CurrentSchemaVersion;
        private readonly Dictionary<string, int> _available
            = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _reserved
            = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, TransferRecord> _records
            = new Dictionary<string, TransferRecord>(StringComparer.Ordinal);

        private List<string> _availableKeys;
        private List<int> _availableValues;
        private List<string> _reservedKeys;
        private List<int> _reservedValues;
        private List<TransferRecord> _recordsForSave;

        public void SetAvailable(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId)) return;
            _available[resourceId] = Math.Max(0, amount);
        }

        public int GetAvailable(string resourceId) => ValueOf(_available, resourceId);
        public int GetReserved(string resourceId) => ValueOf(_reserved, resourceId);

        public TransferResult ReservePhysicalTransfer(TransferRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PackageId)
                || string.IsNullOrEmpty(request.RequestId)
                || string.IsNullOrEmpty(request.ResourceId)
                || request.Amount <= 0)
                return Blocked(null, "invalid transfer request");

            if (_records.TryGetValue(request.StableKey, out var existing))
                return ToResult(existing);

            string transferId = BuildTransferId(request.PackageId, request.RequestId, request.IdempotencyKey);
            int available = GetAvailable(request.ResourceId);
            if (available - GetReserved(request.ResourceId) < request.Amount)
            {
                var blocked = new TransferRecord
                {
                    TransferId = transferId,
                    StableKey = request.StableKey,
                    IdempotencyKey = request.IdempotencyKey ?? "",
                    ResourceId = request.ResourceId,
                    Amount = request.Amount,
                    Status = TransferStatus.Blocked,
                    LastActionTick = request.CurrentTick,
                    Reason = "insufficient physical stock",
                };
                _records[request.StableKey] = blocked;
                return ToResult(blocked);
            }

            Add(_reserved, request.ResourceId, request.Amount);
            var record = new TransferRecord
            {
                TransferId = transferId,
                StableKey = request.StableKey,
                IdempotencyKey = request.IdempotencyKey ?? "",
                ResourceId = request.ResourceId,
                Amount = request.Amount,
                Status = TransferStatus.Reserved,
                LastActionTick = request.CurrentTick,
                Reason = "reserved",
            };
            _records[request.StableKey] = record;
            return ToResult(record);
        }

        public TransferResult ExecutePhysicalTransfer(string transferId)
        {
            var record = Find(transferId);
            if (record == null) return Blocked(transferId, "transfer not found");
            if (record.Status != TransferStatus.Reserved) return ToResult(record);

            Add(_reserved, record.ResourceId, -record.Amount);
            Add(_available, record.ResourceId, -record.Amount);
            record.Status = TransferStatus.Executed;
            record.LastActionTick++;
            record.Reason = "executed";
            return ToResult(record);
        }

        public TransferResult CancelPhysicalTransfer(string transferId)
        {
            var record = Find(transferId);
            if (record == null) return Blocked(transferId, "transfer not found");
            if (record.Status != TransferStatus.Reserved) return ToResult(record);

            Add(_reserved, record.ResourceId, -record.Amount);
            record.Status = TransferStatus.Cancelled;
            record.LastActionTick++;
            record.Reason = "cancelled";
            return ToResult(record);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref SchemaVersion, "physicalTransferSchema", CurrentSchemaVersion);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _availableKeys = _available.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
                _availableValues = _availableKeys.Select(k => _available[k]).ToList();
                _reservedKeys = _reserved.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
                _reservedValues = _reservedKeys.Select(k => _reserved[k]).ToList();
                _recordsForSave = _records.Values.OrderBy(r => r.StableKey, StringComparer.Ordinal).ToList();
            }
            Scribe_Collections.Look(ref _availableKeys, "availableKeys", LookMode.Value);
            Scribe_Collections.Look(ref _availableValues, "availableValues", LookMode.Value);
            Scribe_Collections.Look(ref _reservedKeys, "reservedKeys", LookMode.Value);
            Scribe_Collections.Look(ref _reservedValues, "reservedValues", LookMode.Value);
            Scribe_Collections.Look(ref _recordsForSave, "transferRecords", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _available.Clear();
                _reserved.Clear();
                _records.Clear();
                Restore(_available, _availableKeys, _availableValues);
                Restore(_reserved, _reservedKeys, _reservedValues);
                foreach (var record in _recordsForSave ?? new List<TransferRecord>())
                    if (record != null && !string.IsNullOrEmpty(record.StableKey))
                        _records[record.StableKey] = record;
                _availableKeys = null;
                _availableValues = null;
                _reservedKeys = null;
                _reservedValues = null;
                _recordsForSave = null;
                SchemaVersion = CurrentSchemaVersion;
            }
        }

        private TransferRecord Find(string transferId)
        {
            return _records.Values.FirstOrDefault(r => r.TransferId == transferId);
        }

        private static string BuildTransferId(string packageId, string requestId, string idempotencyKey)
        {
            string canonical = (packageId ?? "") + "|" + (requestId ?? "") + "|" + (idempotencyKey ?? "");
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in canonical) { hash ^= c; hash *= 16777619; }
                return "transfer-" + hash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private static TransferResult ToResult(TransferRecord record)
        {
            return record == null ? Blocked(null, "transfer not found") : new TransferResult
            {
                TransferId = record.TransferId,
                Status = record.Status,
                ResourceId = record.ResourceId,
                Amount = record.Amount,
                Reason = record.Reason,
            };
        }

        private static TransferResult Blocked(string transferId, string reason)
        {
            return new TransferResult { TransferId = transferId, Status = TransferStatus.Blocked, Reason = reason };
        }

        private static int ValueOf(Dictionary<string, int> values, string key)
        {
            return key != null && values.TryGetValue(key, out var value) ? value : 0;
        }

        private static void Add(Dictionary<string, int> values, string key, int amount)
        {
            values[key] = Math.Max(0, ValueOf(values, key) + amount);
        }

        private static void Restore(Dictionary<string, int> target, List<string> keys, List<int> values)
        {
            int count = Math.Min(keys?.Count ?? 0, values?.Count ?? 0);
            for (int i = 0; i < count; i++)
                if (!string.IsNullOrEmpty(keys[i])) target[keys[i]] = Math.Max(0, values[i]);
        }
    }
}
