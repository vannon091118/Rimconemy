using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimconemy.InfectedAutomation.Mechadroids
{
    public enum MechadroidJobStatus
    {
        Queued = 0,
        Assigned = 1,
        Blocked = 2,
        Completed = 3,
        Cancelled = 4,
        Failed = 5,
    }

    public sealed class MechadroidJobRequest
    {
        public string JobId;
        public string UnitId;
        public string TargetId;
        public string InputResourceId;
        public string OutputId;
        public string IdempotencyKey;
        public float EnergyRequired;
        public float MaintenanceRequired;
        public long CurrentTick;
    }

    public sealed class MechadroidJobRecord : IExposable
    {
        public string JobId;
        public string UnitId;
        public string TargetId;
        public string InputResourceId;
        public string OutputId;
        public string IdempotencyKey;
        public float EnergyRequired;
        public float MaintenanceRequired;
        public MechadroidJobStatus Status;
        public long LastActionTick;
        public string BlockReason;

        public void ExposeData()
        {
            Scribe_Values.Look(ref JobId, "jobId", "");
            Scribe_Values.Look(ref UnitId, "unitId", "");
            Scribe_Values.Look(ref TargetId, "targetId", "");
            Scribe_Values.Look(ref InputResourceId, "inputResourceId", "");
            Scribe_Values.Look(ref OutputId, "outputId", "");
            Scribe_Values.Look(ref IdempotencyKey, "idempotencyKey", "");
            Scribe_Values.Look(ref EnergyRequired, "energyRequired", 0f);
            Scribe_Values.Look(ref MaintenanceRequired, "maintenanceRequired", 0f);
            Scribe_Values.Look(ref Status, "status", MechadroidJobStatus.Queued);
            Scribe_Values.Look(ref LastActionTick, "lastActionTick", 0L);
            Scribe_Values.Look(ref BlockReason, "blockReason", "");
        }
    }

    public sealed class MechadroidJobLedger : IExposable
    {
        public const int CurrentSchemaVersion = 1;
        public const string CapabilityId = "rimconemy.infectedautomation.mechadroid_jobs";

        public int SchemaVersion = CurrentSchemaVersion;
        private readonly Dictionary<string, MechadroidJobRecord> _jobs
            = new Dictionary<string, MechadroidJobRecord>(StringComparer.Ordinal);
        private List<MechadroidJobRecord> _jobsForSave;

        public MechadroidJobRecord Enqueue(MechadroidJobRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.JobId)
                || string.IsNullOrEmpty(request.UnitId) || string.IsNullOrEmpty(request.TargetId))
                return null;
            if (_jobs.TryGetValue(request.JobId, out var existing)) return existing;

            var record = new MechadroidJobRecord
            {
                JobId = request.JobId,
                UnitId = request.UnitId,
                TargetId = request.TargetId,
                InputResourceId = request.InputResourceId ?? "",
                OutputId = request.OutputId ?? "",
                IdempotencyKey = request.IdempotencyKey ?? request.JobId,
                EnergyRequired = Math.Max(0f, request.EnergyRequired),
                MaintenanceRequired = Math.Max(0f, request.MaintenanceRequired),
                Status = MechadroidJobStatus.Queued,
                LastActionTick = request.CurrentTick,
            };
            _jobs.Add(record.JobId, record);
            return record;
        }

        public MechadroidJobRecord Get(string jobId)
        {
            return jobId != null && _jobs.TryGetValue(jobId, out var record) ? record : null;
        }

        public bool TryAssign(string jobId, long tick)
        {
            return Transition(jobId, tick, MechadroidJobStatus.Assigned,
                MechadroidJobStatus.Queued, MechadroidJobStatus.Blocked);
        }

        public bool TryBlock(string jobId, string reason, long tick)
        {
            var record = Get(jobId);
            if (record == null || record.Status == MechadroidJobStatus.Completed
                || record.Status == MechadroidJobStatus.Cancelled
                || record.Status == MechadroidJobStatus.Failed)
                return false;
            record.Status = MechadroidJobStatus.Blocked;
            record.BlockReason = reason ?? "blocked";
            record.LastActionTick = tick;
            return true;
        }

        public bool TryComplete(string jobId, long tick)
        {
            return Transition(jobId, tick, MechadroidJobStatus.Completed,
                MechadroidJobStatus.Assigned);
        }

        public bool TryCancel(string jobId, long tick)
        {
            return Transition(jobId, tick, MechadroidJobStatus.Cancelled,
                MechadroidJobStatus.Queued, MechadroidJobStatus.Assigned, MechadroidJobStatus.Blocked);
        }

        public bool TryFail(string jobId, string reason, long tick)
        {
            var record = Get(jobId);
            if (record == null || record.Status == MechadroidJobStatus.Completed
                || record.Status == MechadroidJobStatus.Cancelled)
                return false;
            record.Status = MechadroidJobStatus.Failed;
            record.BlockReason = reason ?? "failed";
            record.LastActionTick = tick;
            return true;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref SchemaVersion, "automationJobSchema", CurrentSchemaVersion);
            if (Scribe.mode == LoadSaveMode.Saving)
                _jobsForSave = _jobs.Values.OrderBy(j => j.JobId, StringComparer.Ordinal).ToList();
            Scribe_Collections.Look(ref _jobsForSave, "mechadroidJobs", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _jobs.Clear();
                foreach (var job in _jobsForSave ?? new List<MechadroidJobRecord>())
                    if (job != null && !string.IsNullOrEmpty(job.JobId)) _jobs[job.JobId] = job;
                _jobsForSave = null;
                SchemaVersion = CurrentSchemaVersion;
            }
        }

        public static string BuildStableJobId(string unitId, string targetId, string outputId)
        {
            string canonical = (unitId ?? "") + "|" + (targetId ?? "") + "|" + (outputId ?? "");
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in canonical) { hash ^= c; hash *= 16777619; }
                return "job-" + hash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private bool Transition(string jobId, long tick, MechadroidJobStatus next, params MechadroidJobStatus[] allowed)
        {
            var record = Get(jobId);
            if (record == null || !allowed.Contains(record.Status)) return false;
            record.Status = next;
            record.LastActionTick = tick;
            if (next != MechadroidJobStatus.Blocked) record.BlockReason = "";
            return true;
        }
    }
}
