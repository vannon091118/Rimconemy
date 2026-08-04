using System.Collections.Generic;
using Rimconemy.SurvivalProgression.Progression;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression.Unlocks
{
    /// <summary>
    /// Phase 9.1 — Unlock contract as a DefModExtension. Vanilla-recognised:
    /// every <see cref="Def"/> subclass (<see cref="ThingDef"/>,
    /// <see cref="RecipeDef"/>, <see cref="DesignationCategoryDef"/>, …)
    /// can opt-in to a Rimconemy gate by listing this extension in
    /// <c>&lt;modExtensions&gt;</c>.
    ///
    /// Vertical-Slice-Plan §Phase 9.1:
    ///   domain       — which ProgressionDomain is read
    ///   requiredLevel — minimum level required to consider the def unlocked
    ///   requiredActions — list of ActionKeys that must have been completed
    ///
    /// Reading lives in <see cref="UnlockService"/>. Writing lives in
    /// <see cref="DomainXpState.TryAward"/>.
    ///
    /// The domain is stored as a string so Defs remain stable across enum
    /// renumberings; <see cref="UnlockService"/> maps back to the enum.
    /// </summary>
    public sealed class RimconemyUnlockExtension : DefModExtension
    {
        public const string StableDomainKey = "domain";

        /// <summary>Stable string, e.g. "Building", "Machinery". Defaults to "".</summary>
        public string domain = "";

        public int requiredLevel = 1;

        public List<string> requiredActions = new List<string>();

        /// <summary>
        /// Returns true if the extension defines a usable gate
        /// (domain string resolves, level &gt;= 1, action list is
        /// non-empty or progress-only).
        /// </summary>
        public bool IsGateDefined()
        {
            if (string.IsNullOrEmpty(domain)) return false;
            if (requiredLevel < 1) return false;
            return true;
        }

        /// <summary>
        /// Returns true if <see cref="domain"/> maps to one of the seven
        /// defined <see cref="ProgressionDomain"/> values. Used by Xml
        /// defs and by the gate to reject misspelled domain strings.
        /// </summary>
        public bool IsKnownDomainString()
        {
            switch (domain)
            {
                case "Survival":
                case "Salvage":
                case "Firecraft":
                case "Building":
                case "Processing":
                case "Machinery":
                case "Defense":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Resolves <see cref="domain"/> back into the strongly typed
        /// <see cref="ProgressionDomain"/>. Returns <c>null</c> for unknown
        /// strings so callers can treat malformed extensions as closed gates.
        /// </summary>
        public ProgressionDomain? ResolveDomain()
        {
            switch (domain)
            {
                case "Survival": return ProgressionDomain.Survival;
                case "Salvage": return ProgressionDomain.Salvage;
                case "Firecraft": return ProgressionDomain.Firecraft;
                case "Building": return ProgressionDomain.Building;
                case "Processing": return ProgressionDomain.Processing;
                case "Machinery": return ProgressionDomain.Machinery;
                case "Defense": return ProgressionDomain.Defense;
                default: return null;
            }
        }
    }
}
