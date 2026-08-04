using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.DLC
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    /// Single Source of Truth für welche RimWorld-DLC-Inhalte in Rimconomy
    /// aktiv sind. Liest die Policy aus <see cref="DLCContentPolicy"/>.
    ///
    /// Architektur-Prinzip (2026-08-04, "Opt-In statt Opt-Out"):
    /// Rimconomy kontrolliert was DLC-Content sichtbar wird, nicht RimWorld.
    /// Diese Filter-Klasse ist der thin Wrapper zwischen der Policy-Tabelle
    /// (in DLCContentPolicy) und den Konsumenten in Mod 02..05.
    ///
    /// Konsumenten-Pattern:
    ///   CapabilityAudit.HasCapabilityOrWarn(
    ///       packageId: "rimconemy.foundation",
    ///       capabilityId: DLCFilter.CapabilityId,
    ///       minVersion: DLCFilter.CapabilityMinVersion);
    ///   if (!DLCFilter.IsContentEnabled(DLCFilter.RoyaltyPsycasts)) {
    ///       return;  // don't touch Royalty content
    ///   }
    ///
    /// Siehe docs/DECISIONS_DLC.md für die vollständige Matrix.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DLCFilter
    {
        public const string CapabilityId = "rimconemy.foundation.dlc_filter";
        public const int CapabilityMinVersion = 1;

        // ── Canonical Content-ID strings (Phase 7 Patch-Layer-Contract) ─────
        // Diese Strings sind Teil des öffentlichen API. Andere Pakete UND
        // XML-Patch-Files verwenden exakt diese Konstanten. Umbenennen ist
        // ein Breaking Change. Sub-Flags bekommen zusätzliche IDs.

        // Royalty (Cat 2 — alle aus per F3)
        public const string RoyaltyPsycasts        = "royalty.psycasts";
        public const string RoyaltyShuttles        = "royalty.shuttles";
        public const string RoyaltyImperial        = "royalty.imperial";
        public const string RoyaltyQuestsAndShuttles = "royalty.quests_and_shuttles";
        public const string RoyaltyHonorSystem     = "royalty.honor_system";

        // Biotech (Cat 2 — alle aus per F4)
        public const string BiotechMechanitor      = "biotech.mechanitor";
        public const string BiotechChildren        = "biotech.children";
        public const string BiotechPollution       = "biotech.pollution";
        public const string BiotechToxifier        = "biotech.toxifier";
        public const string BiotechMechGestator    = "biotech.mech_gestator";

        // Anomaly (Cat 1/2 — Shamblers an, Rest aus per F1)
        public const string AnomalyShamblers       = "anomaly.shamblers";
        public const string AnomalyGhouls          = "anomaly.ghouls";
        public const string AnomalyEntities        = "anomaly.entities";
        public const string AnomalyVoidEvents      = "anomaly.void_events";
        public const string AnomalyHold            = "anomaly.hold";

        // Odyssey (Cat 1/2 — Gravship an, Rest aus per F2)
        public const string OdysseyGravShip        = "odyssey.gravship";
        public const string OdysseyFishing         = "odyssey.fishing";
        public const string OdysseyTravelEvents    = "odyssey.travel_events";

        // Ideology (Cat 1 an, Cat 2 aus per F5)
        public const string IdeologyThoughtSystem  = "ideology.thought_system";
        public const string IdeologyPlayerFounder  = "ideology.player_founder";
        public const string IdeologyRitualUi       = "ideology.ritual_ui";
        public const string IdeologyPlayerEdit     = "ideology.player_edit";
        public const string IdeologyPreceptUi      = "ideology.precept_ui";

        /// <summary>One-time content state hash for save-diagnostics. Cheap.</summary>
        private static readonly Dictionary<string, bool> _cache
            = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// Returns true iff the given RimWorld-DLC content is enabled under
        /// the Rimconomy Opt-In policy. Default-false for unknown IDs
        /// (fail-safe: better to silently suppress a feature than to let a
        /// half-implemented DLC content through).
        ///
        /// Decision tree (post Phase-5 DLC-audit, 2026-08-04):
        ///   1. Royalty-* → <see cref="DLCContentPolicy.Royalty"/> flags
        ///   2. Biotech-* → <see cref="DLCContentPolicy.Biotech"/> flags
        ///   3. Anomaly-* → <see cref="DLCContentPolicy.Anomaly"/> flags
        ///      (Hard-Require: gated additionally on IsAnomalyLoaded())
        ///   4. Odyssey-* → <see cref="DLCContentPolicy.Odyssey"/> flags
        ///      (Hard-Require: gated additionally on IsOdysseyLoaded())
        ///   5. Ideology-* → <see cref="DLCContentPolicy.Ideology"/> flags
        ///   6. Unknown content ID → false (Phase-1 default-denied)
        /// </summary>
        public static bool IsContentEnabled(string contentId)
        {
            if (string.IsNullOrEmpty(contentId))
                return false;

            if (_cache.TryGetValue(contentId, out bool cached))
                return cached;

            bool enabled = Evaluate(contentId);
            _cache[contentId] = enabled;
            return enabled;
        }

        private static bool Evaluate(string contentId)
        {
            // ── Royalty ─────────────────────────────────────
            // Post-apocalypse has no nobility. All sub-flags consolidated to false.
            if (contentId == RoyaltyPsycasts)
                return DLCContentPolicy.Royalty.Psycasts;
            if (contentId == RoyaltyShuttles)
                return DLCContentPolicy.Royalty.Bestower;
            if (contentId == RoyaltyImperial)
                return DLCContentPolicy.Royalty.HonorSystem;
            if (contentId == RoyaltyQuestsAndShuttles)
                return DLCContentPolicy.Royalty.QuestsAndShuttles;
            if (contentId == RoyaltyHonorSystem)
                return DLCContentPolicy.Royalty.HonorSystem;

            // ── Biotech ─────────────────────────────────────
            if (contentId == BiotechMechanitor)
                return DLCContentPolicy.Biotech.Mechanitor;
            if (contentId == BiotechChildren)
                return DLCContentPolicy.Biotech.Children;
            if (contentId == BiotechPollution)
                return DLCContentPolicy.Biotech.Pollution;
            if (contentId == BiotechToxifier)
                return DLCContentPolicy.Biotech.Toxifier;
            if (contentId == BiotechMechGestator)
                return DLCContentPolicy.Biotech.MechGestator;

            // ── Anomaly ─────────────────────────────────────
            // Shamblers-Gate ist zweistufig: Policy + DLC-Install.
            // Anomaly ist Hard-Require in About.xml; defensive null-check für
            // Deactivate-mid-session edge-cases.
            if (contentId == AnomalyShamblers)
                return DLCContentPolicy.Anomaly.Shamblers && IsAnomalyLoaded();
            if (contentId == AnomalyGhouls)
                return DLCContentPolicy.Anomaly.Ghouls && IsAnomalyLoaded();
            if (contentId == AnomalyEntities)
                return DLCContentPolicy.Anomaly.Entities;
            if (contentId == AnomalyVoidEvents)
                return DLCContentPolicy.Anomaly.VoidEvents;
            if (contentId == AnomalyHold)
                return DLCContentPolicy.Anomaly.HoldBuildings;

            // ── Odyssey ─────────────────────────────────────
            // Gravship analog: Policy + DLC-Install.
            if (contentId == OdysseyGravShip)
                return DLCContentPolicy.Odyssey.GravShip && IsOdysseyLoaded();
            if (contentId == OdysseyFishing)
                return DLCContentPolicy.Odyssey.Fishing && IsOdysseyLoaded();
            if (contentId == OdysseyTravelEvents)
                return DLCContentPolicy.Odyssey.TravelEvents;

            // ── Ideology ────────────────────────────────────
            // ThoughtSystem bleibt true (technischer Träger). UX aus.
            if (contentId == IdeologyThoughtSystem)
                return DLCContentPolicy.Ideology.ThoughtSystem && IsIdeologyLoaded();
            if (contentId == IdeologyPlayerFounder)
                return DLCContentPolicy.Ideology.PlayerFounder && IsIdeologyLoaded();
            if (contentId == IdeologyRitualUi)
                return DLCContentPolicy.Ideology.RitualUi && IsIdeologyLoaded();
            if (contentId == IdeologyPlayerEdit)
                return DLCContentPolicy.Ideology.PlayerEdit && IsIdeologyLoaded();
            if (contentId == IdeologyPreceptUi)
                return DLCContentPolicy.Ideology.PreceptUi && IsIdeologyLoaded();

            // Unknown content: default-denied (Phase-1 fail-safe).
            return false;
        }

        /// <summary>Clear the cache (call from Bootstrap after DLC state changes).</summary>
        public static void InvalidateCache()
        {
            _cache.Clear();
        }

        // ── Hard-Require detection (LoadedModManager scan) ──────────────────────
        // ModsConfig.RoyaltyActive-style properties sind instabil — Anomaly und
        // Odyssey wurden später eingeführt und Property-Namen sind nicht Teil
        // desselben stabilen Vertrags. LoadedModManager.RunningMods ist
        // kanonisch seit 1.0 und funktioniert für alle DLCs.

        private static bool IsAnomalyLoaded()
        {
            return IsDlcLoaded("Anomaly", "Ludeon.RimWorld.Anomaly");
        }

        private static bool IsOdysseyLoaded()
        {
            return IsDlcLoaded("Odyssey", "Ludeon.RimWorld.Odyssey");
        }

        private static bool IsIdeologyLoaded()
        {
            return IsDlcLoaded("Ideology", "Ludeon.RimWorld.Ideology");
        }

        private static bool IsDlcLoaded(string dlcName, string packageId)
        {
            if (LoadedModManager.RunningMods == null)
                return false;
            foreach (var mod in LoadedModManager.RunningMods)
            {
                if (mod == null) continue;
                if (mod.Name == dlcName) return true;
                if (mod.PackageId == packageId) return true;
                if (mod.PackageId == dlcName) return true;
            }
            return false;
        }

        // ── one-shot diagnostic for the Bootstrap log ──────────────────────────
        // Logs sind teuer im Bootstrap-Pfad; wir emittieren maximal einmal pro
        // Session, pro Content-ID, nur für IDs die Konsumenten tatsächlich
        // abfragen.

        private static readonly HashSet<string> _loggedKeys
            = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Cheap query wrapper der "ON/OFF" einmal pro session protokolliert.
        /// Konsumenten-Pakete verwenden dies statt IsContentEnabled direkt,
        /// damit der Player.log die tatsächlichen Gate-Antworten zeigt.
        /// </summary>
        public static bool IsContentEnabledLogging(string contentId)
        {
            bool enabled = IsContentEnabled(contentId);
            if (_loggedKeys.Add(contentId))
            {
                Log.Message($"[Rimconemy.Foundation] DLCFilter: {contentId} = {enabled}");
            }
            return enabled;
        }

        /// <summary>
        /// Bootstrap-time summary — emittiert eine einzelne Log-Zeile mit
        /// aktiver und totaler Anzahl. Wird vom Foundation-Bootstrap einmal
        /// aufgerufen. Erspart dem Operator das Suchen in F-V4-Logs.
        /// </summary>
        public static void EmitBootstrapSummary()
        {
            int active = DLCContentPolicy.ActiveCount();
            int total = DLCContentPolicy.TotalFlags;
            Log.Message(
                $"[Rimconemy.Foundation] DLCFilter bootstrap: {active}/{total} content flags active " +
                $"(Anomaly={IsAnomalyLoaded()}, Odyssey={IsOdysseyLoaded()}, " +
                $"Ideology={IsIdeologyLoaded()}, Royalty={IsDlcLoaded("Royalty", "Ludeon.RimWorld.Royalty")}, " +
                $"Biotech={IsDlcLoaded("Biotech", "Ludeon.RimWorld.Biotech")}). " +
                $"See docs/DECISIONS_DLC.md for the matrix.");
        }
    }
}
