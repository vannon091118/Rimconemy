using System;

namespace Rimconemy.Foundation.DLC
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    /// Single Source of Truth für "welche RimWorld-DLC-Inhalte sind in
    /// Rimconomy aktiv?". Diese hartkodierte Tabelle ist Phase 1 der
    /// DLC-Patch-Layer-Architektur (siehe docs/DECISIONS_DLC.md).
    ///
    /// ## Phase-1 vs Phase-2
    ///
    /// Phase 1 (dieser Sprint, 2026-08-04): alle Werte sind const-bool
    /// Klassen-Fields. Konsumenten lesen sie via DLCFilter.IsContentEnabled().
    /// Das reicht für das aktuelle Architektur-Prinzip "Opt-In statt Opt-Out"
    /// ohne Runtime-Config-Overhead.
    ///
    /// Phase 2 (nach Core-Stand, später): ein DLCPolicyConfig-Loader überschreibt
    /// diese Defaults aus einer Defs/DLCContentPolicy_Default.xml. Architektur
    /// bleibt dieselbe — nur die Quelle wechselt von C#-const zu XML-Load.
    /// Ein bestehendes Verhalten ändert sich nicht, weil const-Werte als
    /// Fallback greifen wenn die XML fehlt.
    ///
    /// ## 3-Kategorien-Taxonomie (User-Confirm 2026-08-04)
    ///
    /// Die Werte folgen der mechanischen Klassifikation aus der Konzept-Diskussion:
    ///
    /// - **Cat 1 (passiv, Adapter):** Systeme die nur auf expliziten Aufruf
    ///   reagieren. `true` = kapselbar im Rimconomy-Code, kein Patch nötig.
    ///   Beispiel: Ideology-ThoughtWorker (=true), Anomaly-Shamblers (=true).
    ///
    /// - **Cat 2 (aktiv, Suppressor):** Systeme die ungefragt Events/UI/Needs
    ///   erzeugen. `false` = Patch-Layer muss sie aktiv ausschalten.
    ///   Beispiel: Anomaly-Entities, Royalty-Bestower, Biotech-Children.
    ///
    /// - **Cat 3 (selektiv, Granularität):** Mischung aus C1 und C2 pro
    ///   Subsystem. Granularität in Sub-Flag-Aufschlüsselung dokumentiert.
    ///   Beispiel: Ideology-ThoughtSystem (C1=true) vs Ideology-RitualUi (C2=false).
    ///
    /// Eine `false`-Flag bedeutet hier: "in Patches/*.xml ausschalten UND
    /// jeder C#-Konsument muss vor Aufruf DLCFilter.IsContentEnabled fragen".
    /// </summary>
    public static class DLCContentPolicy
    {
        // ──────────────────────────────────────────────────────────────
        // Royalty
        // Einstellung: keine Noblesse in der Post-Apokalypse. Alles aus.
        // ──────────────────────────────────────────────────────────────
        public static class Royalty
        {
            /// <summary>Ehren-System, Titel-Verleihung, Honor Decay. Cat 2 suppressor.</summary>
            public static readonly bool HonorSystem    = false;  // F3

            /// <summary>Bestower-Shuttle-Quests, Ehren-Botschafter. Cat 2 suppressor.</summary>
            public static readonly bool Bestower       = false;  // F3

            /// <summary>Psycasts, NeuralHeat-Need, Psylink-Trait-Generation. Cat 2 suppressor.</summary>
            public static readonly bool Psycasts       = false;  // F3

            /// <summary>Royal-Bedrooms, Impressive-Rooms, Throne-Rooms Mood-Debuffs. Cat 2.</summary>
            public static readonly bool QuestsAndShuttles = false;  // F3 (Konsolidierung)
        }

        // ──────────────────────────────────────────────────────────────
        // Ideology
        // PreceptDef / ThoughtDef / ThoughtWorker bleiben aktiv (technischer
        // Träger). Player-facing UX deaktiviert — Rimconomy weist eine
        // Setting-Ideo automatisch zu.
        // ──────────────────────────────────────────────────────────────
        public static class Ideology
        {
            /// <summary>ThoughtWorker / PreceptDef / ThoughtDef. Cat 1 adapter (passiv).</summary>
            public static readonly bool ThoughtSystem  = true;   // F5: technischer Träger

            /// <summary>Founding Ceremony &amp; Ideo-Founder-UI. Cat 2 suppressor.</summary>
            public static readonly bool PlayerFounder  = false;  // F5

            /// <summary>Ritual-Performer, Ritual-UI. Cat 2 suppressor.</summary>
            public static readonly bool RitualUi       = false;  // F5

            /// <summary>Post-creation Ideo-Edit. Cat 2 suppressor.</summary>
            public static readonly bool PlayerEdit     = false;  // F5

            /// <summary>Precept-Selection-Screen. Cat 2 suppressor.</summary>
            public static readonly bool PreceptUi      = false;  // F5 (Konsolidierung)
        }

        // ──────────────────────────────────────────────────────────────
        // Anomaly (Hard-Require gemäß F1-DECISION)
        // Shamblers sind die genetische Basis der Rimconomy-Infected PawnKinds.
        // Andere Content (Entities, VoidEvents, Hold) sind Störer.
        // ──────────────────────────────────────────────────────────────
        public static class Anomaly
        {
            /// <summary>Shambler als ParentName für Infected-PawnKinds. Cat 1 adapter (passiv).</summary>
            public static readonly bool Shamblers      = true;   // F1: genetische Basis

            /// <summary>Ghouls (langsam, infiziert). Cat 1, opt-in später.</summary>
            public static readonly bool Ghouls         = false;  // F1

            /// <summary>Noctol, Revenant, Devourer, Metal-Horror. Cat 2 suppressor.</summary>
            public static readonly bool Entities       = false;  // F1

            /// <summary>Darkness, PsychicSoothe, Void-Raids. Cat 2 Anomaly-StorytellerComp.</summary>
            public static readonly bool VoidEvents     = false;  // F1

            /// <summary>Hold-Buildings, Entity-Containment. Cat 2 compiler.</summary>
            public static readonly bool HoldBuildings  = false;  // F1
        }

        // ──────────────────────────────────────────────────────────────
        // Odyssey (Hard-Require gemäß F2-DECISION)
        // Gravship ist der Territory-Engine. Andere Odyssey-Content (Fishing,
        // Travel-Events) werden separat gehandhabt oder suppressed.
        // ──────────────────────────────────────────────────────────────
        public static class Odyssey
        {
            /// <summary>Gravship WorldObject als Territory-Engine. Cat 1 adapter (passiv).</summary>
            public static readonly bool GravShip       = true;   // F2

            /// <summary>Fishing-Dock, Fischen als Nahrungs-Quelle. Cat 2 suppressor.</summary>
            public static readonly bool Fishing        = false;  // F2

            /// <summary>Travel-Events auf Weltkarte. Cat 2 (Director-Manager).</summary>
            public static readonly bool TravelEvents   = false;  // F2
        }

        // ──────────────────────────────────────────────────────────────
        // Biotech (Suppress-Installed gemäß F4-DECISION)
        // Keine Mechanitor / Mechanoids (Rimconomy hat eigene Mechadroids).
        // Children aus (GameOver-Inkompatibilität). Pollution opt-in später.
        // ──────────────────────────────────────────────────────────────
        public static class Biotech
        {
            /// <summary>Mechanitor-Pawn-Trait + Mech-System. Cat 2 suppressor.</summary>
            public static readonly bool Mechanitor     = false;  // F4

            /// <summary>Children als PawnGeneration-Output. Cat 2 suppressor.</summary>
            public static readonly bool Children       = false;  // F4

            /// <summary>Pollution-Map-Overlay + Waste-Tile-Diffusion. Cat 2 (Visual-Optional).</summary>
            public static readonly bool Pollution      = false;  // F4 (Phase-2 toggleable)

            /// <summary>Toxifier-Generator-Building. Cat 2 (Food-Conflict).</summary>
            public static readonly bool Toxifier       = false;  // F4

            /// <summary>Mech-Gestator, Genetic-Mechanoid-Bandbreite. Cat 2 suppressor.</summary>
            public static readonly bool MechGestator   = false;  // F4
        }

        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Anzahl aller derzeit aktiven (=true) Flags. Wird im Bootstrap-Log
        /// einmal emittiert für Save-Diagnose und Profil-Detection. Cheap.
        /// </summary>
        public static int ActiveCount()
        {
            int count = 0;
            count += Royalty.HonorSystem ? 1 : 0;
            count += Royalty.Bestower ? 1 : 0;
            count += Royalty.Psycasts ? 1 : 0;
            count += Royalty.QuestsAndShuttles ? 1 : 0;
            count += Ideology.ThoughtSystem ? 1 : 0;
            count += Ideology.PlayerFounder ? 1 : 0;
            count += Ideology.RitualUi ? 1 : 0;
            count += Ideology.PlayerEdit ? 1 : 0;
            count += Ideology.PreceptUi ? 1 : 0;
            count += Anomaly.Shamblers ? 1 : 0;
            count += Anomaly.Ghouls ? 1 : 0;
            count += Anomaly.Entities ? 1 : 0;
            count += Anomaly.VoidEvents ? 1 : 0;
            count += Anomaly.HoldBuildings ? 1 : 0;
            count += Odyssey.GravShip ? 1 : 0;
            count += Odyssey.Fishing ? 1 : 0;
            count += Odyssey.TravelEvents ? 1 : 0;
            count += Biotech.Mechanitor ? 1 : 0;
            count += Biotech.Children ? 1 : 0;
            count += Biotech.Pollution ? 1 : 0;
            count += Biotech.Toxifier ? 1 : 0;
            count += Biotech.MechGestator ? 1 : 0;
            return count;
        }

        /// <summary>
        /// Total-Flag-Count in der Policy. Wird für Bootstrap-Log benutzt.
        /// </summary>
        public static readonly int TotalFlags = 21;
    }
}
