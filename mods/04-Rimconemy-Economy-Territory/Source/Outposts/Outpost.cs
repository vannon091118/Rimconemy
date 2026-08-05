using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Rimconemy.EconomyTerritory.Transfers;

namespace Rimconemy.EconomyTerritory.Outposts
{
    /// <summary>
    /// Owner: Economy and Territory (Package 04).
    /// Outpost planning record. The visible states are
    /// Planned, Active, Blocked, Disconnected, Ruined. No per-pawn Outpost
    /// simulation: production/consumption are aggregated per absolute tick
    /// window.
    /// </summary>
    public enum OutpostState { Planned, Active, Blocked, Disconnected, Ruined }

    /// <summary>
    /// E-T5: replaces the historical stub with a real state machine for
    /// Outposts.
    /// </summary>
    public sealed class Outpost
    {
        public const string LogMarker = "v1";
        public const long DefaultBlockedTimeoutTicks = 90000L; // 1.5 days
        public const long DefaultRuinedThresholdTicks = 240000L; // 4 days w/o repair
        public const long DefaultPlannedTimeoutTicks = 90000L; // 1.5 days without activation

        public string OutpostId;
        public string OwnerId;
        public OutpostState State = OutpostState.Planned;
        public long GrossPerTick;
        public long DefenseCostPerTick;
        public long NetPerTick;
        public long LastUpdatedTick;
        public long DisconnectDeadlineTick;
        public long StateEnteredTick;
        public long CreatedAtTick;
        public long LastSeenActiveTick;
        public List<string> RouteIds = new List<string>();
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion = CurrentSchemaVersion;
        public string CurrentReason = "Initial state";
        public string InvestmentTransferId;
        public string InvestmentResourceId;
        public int InvestmentAmount;

        public Outpost(string outpostId, string ownerId, long createdAtTick = 0L)
        {
            OutpostId = outpostId ?? "outpost";
            OwnerId = ownerId ?? "default-owner";
            StateEnteredTick = createdAtTick;
            CreatedAtTick = createdAtTick;
            LastSeenActiveTick = 0L;
        }

        public long CurrentNet => GrossPerTick - DefenseCostPerTick;

        // ── state transitions ─────────────────────────────────

        /// <summary>
        /// Apply the state machine for a given current tick. Returns true
        /// when the state changed (callers can log once). Returns false when
        /// no transition occurred (state-quiescent tick).
        /// </summary>
        public bool Tick(long currentTick)
        {
            LastUpdatedTick = currentTick;
            long stallingTicks = currentTick - LastSeenActiveTick;

            switch (State)
            {
                case OutpostState.Planned:
                {
                    // Auto-fail if the operator never activated the outpost
                    // within the planned timeout. Uses CreatedAtTick (anchor
                    // set at construction) instead of LastSeenActiveTick so
                    // a fresh outpost that never reached Active still times out.
                    // CRITICAL-fix 2026-08-04: previous logic gated on
                    // LastSeenActiveTick > 0 which made the branch unreachable
                    // for fresh outposts.
                    long nowAtPlanned = currentTick - CreatedAtTick;
                    if (CreatedAtTick > 0 && nowAtPlanned >= DefaultPlannedTimeoutTicks)
                    {
                        ForceTransition(OutpostState.Disconnected,
                            $"Planned timeout after {nowAtPlanned} ticks (no activation)",
                            currentTick);
                        return true;
                    }
                    return false;
                }

                case OutpostState.Active:
                {
                    if (GrossPerTick <= 0)
                    {
                        ForceTransition(OutpostState.Blocked,
                            "Gross income collapse", currentTick);
                        return true;
                    }
                    if (DefenseCostPerTick > GrossPerTick * 2L)
                    {
                        ForceTransition(OutpostState.Blocked,
                            "Defense cost exceeds 2x gross", currentTick);
                        return true;
                    }
                    LastSeenActiveTick = currentTick; // heart beat
                    return false;
                }

                case OutpostState.Blocked:
                {
                    // Recover when the gross recovers to a positive value
                    // and defense-cost is back inside 2x gross.
                    if (GrossPerTick > 0 && DefenseCostPerTick <= GrossPerTick * 2L)
                    {
                        ForceTransition(OutpostState.Active,
                            "Threat resolved", currentTick);
                        LastSeenActiveTick = currentTick;
                        return true;
                    }
                    // Otherwise: timeout if the disconnect deadline expires.
                    if (DisconnectDeadlineTick > 0 && currentTick >= DisconnectDeadlineTick)
                    {
                        ForceTransition(OutpostState.Disconnected,
                            "Blocked timeout elapsed", currentTick);
                        return true;
                    }
                    return false;
                }

                case OutpostState.Disconnected:
                {
                    // Repair after a player-led investment (e.g. wallet cost
                    // logged via a sibling effect-handler). We trigger via
                    // <see cref="TryRepair"/>.
                    if (stallingTicks >= DefaultRuinedThresholdTicks)
                    {
                        ForceTransition(OutpostState.Ruined,
                            $"Disconnected without repair for {stallingTicks} ticks",
                            currentTick);
                        return true;
                    }
                    return false;
                }

                case OutpostState.Ruined:
                    // terminal state
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Attempt to repair a disconnected outpost by paying N credits.
        /// Caller wires the wallet-side availability check; we just apply
        /// the transition.
        /// </summary>
        public OutpostInvestmentResult TryReserveInvestment(
            PhysicalTransferService transfers,
            string packageId,
            string requestId,
            string resourceId,
            int amount,
            long currentTick)
        {
            if (transfers == null || string.IsNullOrEmpty(OutpostId))
                return OutpostInvestmentResult.Blocked("transfer service unavailable");
            var result = transfers.ReservePhysicalTransfer(new TransferRequest
            {
                PackageId = packageId,
                RequestId = requestId,
                IdempotencyKey = OutpostId + "|" + requestId,
                ResourceId = resourceId,
                Amount = amount,
                CurrentTick = currentTick,
            });
            InvestmentTransferId = result.TransferId;
            InvestmentResourceId = result.ResourceId;
            InvestmentAmount = result.Amount;
            return new OutpostInvestmentResult
            {
                Status = result.Status == TransferStatus.Reserved
                    ? OutpostInvestmentStatus.Reserved : OutpostInvestmentStatus.Blocked,
                TransferId = result.TransferId,
                Reason = result.Reason,
            };
        }

        public bool TryRepair(long repairCredits, long currentTick)
        {
            if (State != OutpostState.Disconnected) return false;
            if (repairCredits < 100L) return false; // minimum repair cost

            ForceTransition(OutpostState.Active, $"Repaired for {repairCredits} credits", currentTick);
            LastSeenActiveTick = currentTick;
            return true;
        }

        /// <summary>
        /// Update economic knobs. Triggers a re-evaluation: if the gross has
        /// collapsed while Active, the next Tick moves to Blocked.
        /// </summary>
        public void UpdateEconomy(long grossPerTick, long defenseCostPerTick, long currentTick)
        {
            GrossPerTick = Mathf.Max(0, (int)grossPerTick);
            DefenseCostPerTick = Mathf.Max(0, (int)defenseCostPerTick);
            NetPerTick = GrossPerTick - DefenseCostPerTick;
            LastUpdatedTick = currentTick;
        }

        public void ForceTransition(OutpostState next, string reason, long currentTick)
        {
            if (State == next) return;
            OutpostState previous = State;
            State = next;
            CurrentReason = reason ?? "No reason given";
            StateEnteredTick = currentTick;
            if (next == OutpostState.Blocked)
                DisconnectDeadlineTick = currentTick + DefaultBlockedTimeoutTicks;

            Log.Message(
                $"[Rimconemy.EconomyTerritory] Outpost {OutpostId}: " +
                $"{previous} -> {next} | reason={CurrentReason}");
        }
    }

    /// <summary>
    /// Static accessor + GameComponent for the player's outpost roster.
    /// </summary>
    public static class OutpostNetwork
    {
        public static Outpost Register(string outpostId, string ownerId)
        {
            var ledger = OutpostService.GetOrCreateLedger();
            var op = new Outpost(outpostId, ownerId);
            ledger.Outposts[outpostId] = op;
            return op;
        }

        public static Outpost Get(string outpostId)
        {
            var ledger = OutpostService.GetOrCreateLedger();
            if (ledger.Outposts.TryGetValue(outpostId, out var op))
                return op;
            return null;
        }

        public static int TickAll(long currentTick)
        {
            int changed = 0;
            var ledger = OutpostService.GetOrCreateLedger();
            foreach (var op in ledger.Outposts.Values)
            {
                if (op == null) continue;
                if (op.Tick(currentTick)) changed++;
            }
            return changed;
        }
    }

    /// <summary>
    /// Per-Game ledger hosting the outpost roster. Persisted deep.
    /// </summary>
    public enum OutpostInvestmentStatus { Blocked = 0, Reserved = 1 }

    public struct OutpostInvestmentResult
    {
        public OutpostInvestmentStatus Status;
        public string TransferId;
        public string Reason;

        public static OutpostInvestmentResult Blocked(string reason)
        {
            return new OutpostInvestmentResult { Status = OutpostInvestmentStatus.Blocked, Reason = reason };
        }
    }

    public sealed class OutpostLedger : IExposable
    {
        public Dictionary<string, Outpost> Outposts = new Dictionary<string, Outpost>(System.StringComparer.Ordinal);

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Outposts, "outposts", LookMode.Value, LookMode.Deep);
            if (Outposts == null) Outposts = new Dictionary<string, Outpost>();
        }
    }

    /// <summary>
    /// GameComponent persisting the player's outpost roster. Mirrors
    /// <c>Wallet-GameComponent</c>.
    /// </summary>
    public sealed class OutpostService : GameComponent
    {
        public OutpostLedger Ledger = new OutpostLedger();

        public OutpostService(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Ledger == null) Ledger = new OutpostLedger();
            Scribe_Deep.Look(ref Ledger, "outpostLedger");
            if (Ledger == null) Ledger = new OutpostLedger();
            if (Ledger.Outposts == null) Ledger.Outposts = new Dictionary<string, Outpost>();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            EnsureLedger();
        }

        public void EnsureLedger()
        {
            if (Ledger == null) Ledger = new OutpostLedger();
            if (Ledger.Outposts == null) Ledger.Outposts = new Dictionary<string, Outpost>();
        }

        public static OutpostLedger GetOrCreateLedger()
        {
            if (Current.Game == null)
            {
                // Stand-alone fallback so debug tools can run too.
                return new OutpostLedger();
            }
            var comp = Current.Game.GetComponent<OutpostService>();
            if (comp != null)
            {
                comp.EnsureLedger();
                return comp.Ledger;
            }
            return new OutpostLedger();
        }
    }
}
