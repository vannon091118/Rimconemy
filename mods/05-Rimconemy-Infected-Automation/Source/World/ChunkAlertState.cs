namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 1 — Chunk alert escalation levels.
    ///
    /// Each chunk cycles through these states as threats, light,
    /// or noise accumulate. ChunkAI (Sprint 2) reads this to steer
    /// infected pawns between behavioral states.
    /// </summary>
    public enum ChunkAlertState
    {
        /// <summary>No recent threats. Infected stay idle.</summary>
        Dormant = 0,

        /// <summary>Something was noticed — noise, light flicker, a
        /// distant target. Infected become alert but don't move yet.</summary>
        Suspicious = 1,

        /// <summary>Active investigation: infected move toward the
        /// chunk to check what caused the disturbance.</summary>
        Investigating = 2,

        /// <summary>Confirmed hostile contact. Infected attack
        /// anything in this chunk.</summary>
        Assault = 3,
    }
}
