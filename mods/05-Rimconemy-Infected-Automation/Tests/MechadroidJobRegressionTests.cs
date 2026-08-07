using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Mechadroids;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>Red-first gates for the Milestone-B Mechadroid job contract.</summary>
    public static class MechadroidJobRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "Mechadroid job regression tests");

            _passed = 0;
            _failed = 0;

            TestJobLifecycle();
            TestDuplicateCompletionIsRejected();
            TestBlockedJobHasVisibleReason();
            TestDeterministicJobIdentity();

            string summary = "[Rimconemy.InfectedAutomation] Mechadroid job regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        private static void TestJobLifecycle()
        {
            var ledger = new MechadroidJobLedger();
            var job = ledger.Enqueue(new MechadroidJobRequest
            {
                JobId = "job-lifecycle",
                UnitId = "unit-1",
                TargetId = "building-1",
                IdempotencyKey = "jobs|job-lifecycle",
                CurrentTick = 100L,
            });
            ts.Check(Equals(MechadroidJobStatus.Queued, job.Status), "Jobs: enqueue creates Queued state");
            ts.Check(ledger.TryAssign(job.JobId, 120L), "Jobs: Queued -> Assigned");
            ts.Check(ledger.TryBlock(job.JobId, "missing fuel", 130L), "Jobs: Assigned -> Blocked");
            ts.Check(ledger.TryAssign(job.JobId, 140L), "Jobs: Blocked -> Assigned after retry");
            ts.Check(ledger.TryComplete(job.JobId, 150L), "Jobs: Assigned -> Completed");
            ts.Check(Equals(MechadroidJobStatus.Completed, ledger.Get(job.JobId).Status), "Jobs: terminal status is Completed");
        }

        private static void TestDuplicateCompletionIsRejected()
        {
            var ledger = new MechadroidJobLedger();
            ledger.Enqueue(new MechadroidJobRequest
            {
                JobId = "job-once",
                UnitId = "unit-1",
                TargetId = "building-1",
                IdempotencyKey = "jobs|job-once",
                CurrentTick = 100L,
            });
            ledger.TryAssign("job-once", 110L);
            ts.Check(ledger.TryComplete("job-once", 120L), "Jobs: first completion succeeds");
            ts.Check(!(ledger.TryComplete("job-once", 130L)), "Jobs: duplicate completion rejected");
            ts.Check(Equals(120L, ledger.Get("job-once").LastActionTick), "Jobs: duplicate completion does not rewrite action tick");
        }

        private static void TestBlockedJobHasVisibleReason()
        {
            var ledger = new MechadroidJobLedger();
            ledger.Enqueue(new MechadroidJobRequest
            {
                JobId = "job-blocked",
                UnitId = "unit-1",
                TargetId = "building-1",
                IdempotencyKey = "jobs|job-blocked",
                CurrentTick = 100L,
            });
            ts.Check(ledger.TryBlock("job-blocked", "capability unavailable", 110L), "Jobs: queued job can be blocked");
            ts.Check(Equals("capability unavailable", ledger.Get("job-blocked").BlockReason), "Jobs: blocked reason is persisted in record");
        }

        private static void TestDeterministicJobIdentity()
        {
            string first = MechadroidJobLedger.BuildStableJobId("unit-1", "building-1", "output-1");
            string second = MechadroidJobLedger.BuildStableJobId("unit-1", "building-1", "output-1");
            ts.Check(Equals(first, second), "Jobs: stable identity is deterministic");
        }


    }
}
