// Source/Story/StoryEventDef.cs
//
// Owner: Infected & Automation — Story-/Threat-Domäne (Phase 1, H2-Story-Vertrag).
// Status: SCAFFOLD — deklarative, von RimWorld ladbare Event-Defs.
//         Die Auswahl-/Ausführungs-Engine (DeterministicRng, Prerequisites-/
//         Effects-Evaluierung) ist noch PLANNED und wird erst in Phase 1 gebaut.
//
// Diese Def-Klasse macht die Event-Presets in Defs/StoryEvents/ RimWorld-
// verarbeitbar. Jedes neue Event = eine neue XML-Datei im Preset-Ordner.
// Felder entsprechen 1:1 docs/H2-story-contract.md §2 (StoryEventSpec).

using System.Collections.Generic;
using System.Globalization;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Declarative story event preset, loadable by RimWorld.
    /// H2-Feldmapping:
    ///   EventId          -> defName (geerbt von Def)
    ///   EventVersion     -> eventVersion
    ///   EventFamily      -> eventFamily
    ///   Label            -> label (geerbt von Def)
    ///   Description      -> description (geerbt von Def)
    ///   Prerequisites    -> prerequisites (Liste deklarativer Prädikate)
    ///   Exclusions       -> exclusions (Liste deklarativer Ausschlüsse)
    ///   Weights          -> weights (ProfilId -> Gewicht)
    ///   Cooldowns        -> cooldownDays (ProfilId -> Tage)
    ///   EscalationBand   -> escalationBand
    ///   EscalationModifier -> escalationModifier (Wert, der bei "Ignorieren"
    ///                        auf escalationTarget addiert wird)
    ///   TextKey          -> textKey
    ///   LetterLabel/-Text -> letterLabel / letterText
    ///   Choices          -> choices (Liste von StoryEventChoiceDef)
    ///   FollowUpIds      -> followUpIds
    ///   DeterminismKey   -> determinismKey
    /// </summary>
    public class StoryEventDef : Def
    {
        /// <summary>Schema-Version des Presets (H2: EventVersion).</summary>
        public int eventVersion = 1;

        /// <summary>Familien-ID aus H2 §3, z.B. "SupplyCrisis".</summary>
        public string eventFamily;

        /// <summary>Eskalationsstufe (1..3); muss &lt;= Profil.MaxEscalationBand sein.</summary>
        public int escalationBand;

        /// <summary>
        /// Zielgröße der Eskalation bei "Ignorieren" (H2 EscalationModifier).
        /// Werte: "ThreatPressure" oder "IdeologyTension".
        /// </summary>
        public string escalationTarget;

        /// <summary>Additiver Wert, der bei "Ignorieren" auf escalationTarget wirkt.</summary>
        public float escalationModifier;

        /// <summary>Deklarative Voraussetzungs-Prädikate (H2 Prerequisites).</summary>
        public List<string> prerequisites = new List<string>();

        /// <summary>Deklarative Ausschluss-Prädikate (H2 Exclusions).</summary>
        public List<string> exclusions = new List<string>();

        /// <summary>
        /// Gewichtung pro SettingProfileId (H2 Weights).
        /// Format: "ProfileId=Gewicht", z.B. "Rimconemy_Refuge=20".
        /// RimWorld-kompatibel als List&lt;string&gt; statt Dictionary.
        /// </summary>
        public List<string> weights = new List<string>();

        /// <summary>
        /// Cooldown pro SettingProfileId in Tagen (H2 Cooldowns; 1 Tag = 60.000 Ticks).
        /// Format: "ProfileId=Tage", z.B. "Rimconemy_Refuge=5.0".
        /// RimWorld-kompatibel als List&lt;string&gt; statt Dictionary.
        /// </summary>
        public List<string> cooldownDays = new List<string>();

        /// <summary>TextKey für Localisation (H2 TextKey).</summary>
        public string textKey;

        /// <summary>Letter-Überschrift (H2 LetterLabel).</summary>
        public string letterLabel;

        /// <summary>Letter-Text mit Platzhaltern, z.B. {ResourceName} (H2 LetterText).</summary>
        public string letterText;

        /// <summary>Entscheidungsoptionen (H2 Choices).</summary>
        public List<StoryEventChoiceDef> choices = new List<StoryEventChoiceDef>();

        /// <summary>IDs geplanter Folge-Events (H2 FollowUpIds).</summary>
        public List<string> followUpIds = new List<string>();

        /// <summary>
        /// Kanonischer Determinismus-Schlüssel-Template (H2 DeterminismKey),
        /// z.B. "ProfileId + EventId + StorageSnapshot.Hash + GameTickDay".
        /// </summary>
        public string determinismKey;
    }

    /// <summary>
    /// Eine Entscheidungsoption eines StoryEventDef (H2 Choices).
    /// Effects sind deklarative Effekt-Strings (H2 Effects), die die Phase-1-
    /// Engine interpretiert; z.B. "IdeologyTension += 0.05".
    /// </summary>
    public class StoryEventChoiceDef
    {
        /// <summary>Stabile Choice-ID (H2 ChoiceId).</summary>
        public string choiceId;

        /// <summary>UI-Label der Option (H2 Label).</summary>
        public string label;

        /// <summary>Deklarative Effekte bei Auswahl (H2 Effects).</summary>
        public List<string> effects = new List<string>();
    }
}
