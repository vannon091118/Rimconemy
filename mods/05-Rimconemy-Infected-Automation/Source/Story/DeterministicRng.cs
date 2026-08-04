using System;
using System.Globalization;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// Deterministic pseudo-random number generator using splitmix64.
    /// Pure function: given the same seed, always produces the same
    /// sequence. No System.Random, no system time, no external state.
    ///
    /// Gate G2: gleicher Snapshot + Profil + Seed → gleiche Auswahl.
    /// </summary>
    public struct DeterministicRng
    {
        private ulong _state;

        public DeterministicRng(int seed)
        {
            _state = (ulong)seed;
            // Advance once to avoid degenerate seed=0
            if (_state == 0)
                _state = 0xDEADBEEFCAFEBABEUL;
        }

        /// <summary>Returns a float in [0, 1).</summary>
        public float NextFloat()
        {
            // Extract upper 24 bits for good uniformity
            return (Next() >> 40) / (float)(1 << 24);
        }

        /// <summary>Returns a non-negative integer.</summary>
        public int NextInt()
        {
            return (int)(Next() >> 1);
        }

        /// <summary>Returns an integer in [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            // Rejection sampling to avoid modulo bias.
            // We reject values in the "tail" that would make some
            // remainders more probable than others.
            uint m = (uint)maxExclusive;
            uint threshold = uint.MaxValue - uint.MaxValue % m;
            uint r;
            do
            {
                r = (uint)(Next() >> 1);
            } while (r >= threshold);
            return (int)(r % m);
        }

        /// <summary>Splitmix64 core.</summary>
        private ulong Next()
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>
        /// Builds a deterministic seed from a determinism key template
        /// and concrete values. Replaces the canonical placeholders
        /// documented in docs/H2-story-contract.md §1 (SeedRule):
        /// {ProfileId}, {MapID}, {GameTickDay}, plus the event-specific
        /// {EventId}, {StorageHash}, {IdeologyTension}, {ThreatPressure},
        /// {PawnId}. Unknown placeholders pass through unchanged.
        /// </summary>
        public static int BuildSeed(string template, StoryEventSpec spec, SettingProfile profile, SituationSnapshot snapshot)
        {
            // {PawnId} substitution carries both the per-tick chosen pawn
            // and the FNV-1a roster fingerprint. Joining them with "|"
            // bakes roster shape into every pawn-anchored determinism key,
            // so a save→load that lost or gained a colonist still produces
            // a stable key for the surviving ring layout and a *different*
            // key for the changed one. Gate G2 stays satisfied because
            // identical (snapshot, profile, spec) produce identical seeds.
            string pawnId = string.IsNullOrEmpty(snapshot?.DeterministicTargetPawnId)
                ? ""
                : $"{snapshot.DeterministicTargetPawnId}|{snapshot.PawnRosterFingerprint ?? ""}";

            string resolved = template
                .Replace("{ProfileId}", profile?.ProfileId ?? "")
                .Replace("{EventId}", spec?.EventId ?? "")
                .Replace("{MapID}", (snapshot?.MapID ?? -1).ToString(CultureInfo.InvariantCulture))
                .Replace("{StorageHash}", snapshot?.StorageHash ?? "")
                .Replace("{IdeologyTension}", (snapshot?.IdeologyTension ?? 0f).ToString("F4", CultureInfo.InvariantCulture))
                .Replace("{ThreatPressure}", (snapshot?.ThreatPressure ?? 0f).ToString("F4", CultureInfo.InvariantCulture))
                .Replace("{PawnId}", pawnId)
                .Replace("{GameTickDay}", ((snapshot?.GameTick ?? 0) / 60000L).ToString(CultureInfo.InvariantCulture));

            return GetStableHashCode(resolved);
        }

        /// <summary>
        /// Stable string hash that does NOT depend on .NET's randomized
        /// String.GetHashCode. Uses FNV-1a.
        /// </summary>
        public static int GetStableHashCode(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash;
            }
        }
    }
}
