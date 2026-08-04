using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Building
{
    /// <summary>
    /// Phase-3.2 (2026-08-04): Vanilla UI-Hook für Bauschutt-Wall-Build.
    ///
    /// Owner: Scavenger Infrastructure (Package 03).
    ///
    /// Hook-Pfad OHNE Harmony-Transpiler: eine unmittelbare <see cref="Designator"/>
    /// ruft in <c>ProcessInput</c> direkt <see cref="BauschuttRemapApply.ApplyRemap"/>
    /// direkt auf. Das ist die kanonische vanilla-Integration: jeder Mod kann
    /// Designator-Subklassen registrieren ohne Patch-Operationen.
    ///
    /// Owner-Constraint (INTERFACE_CONTRACT §9.1 + §9.4): Paket 03 mutiert
    /// die Map und den eigenen physischen Bauschutt-Bestand; falls Wallet-Zugriff
    /// je nötig wird, muss das CapabilityAudit-Gate genutzt werden.
    /// ApplyRemap platziert Blueprints und fordert danach einen best-effort
    /// Storage-Write für den verbrauchten Bauschutt an.
    ///
    /// Screenshot-Beleg (2026-08-04): der Designator ist im Architect sichtbar.
    /// Das beweist nicht den vollständigen Vanilla-Bau-/Save-Lifecycle; dieser
    /// bleibt ein Runtime-Gate.
    ///
    /// Vanilla-Healthy-Verification: der Designator bleibt ein UI-Trigger;
    /// die tatsächliche Blueprint-Construction bleibt ein Runtime-Gate.
    /// </summary>
    public class Designator_BuildWallBauschutt : Designator
    {
        public Designator_BuildWallBauschutt() : base()
        {
            this.defaultLabel = "Rimconemy · BuildWallBauschutt";
            this.defaultDesc = "Platziert Wall-Blueprints anhand des gelesenen Bauschutt-Bestands und fordert danach einen best-effort physischen Storage-Abzug an. Teilabzug möglich; Vanilla-Bau-/Save-Lifecycle bleibt ein Runtime-Gate.";
            this.icon = ContentFinder<Texture2D>.Get("Things/Building/Misc/Campfire_MenuIcon", false);
            this.hotKey = KeyBindingDefOf.Misc1; // Beispiel-Belegung
            this.soundSucceeded = SoundDefOf.Designate_ZoneAdd;
        }

        /// <summary>
        /// Vanilla ruft <c>ProcessInput</c> auf, wenn der Spieler den Designator
        /// ausgewählt und Enter/LMB gedrückt hat. Wir umgehen den cell-pick-Selector
        /// und rufen direkt <see cref="BauschuttRemapApply.ApplyRemap"/> auf.
        /// </summary>
        public override void ProcessInput(Event ev)
        {
            // This is an immediate action designator, not a cell picker. Do not
            // call Designator.ProcessInput: that would enter vanilla selection
            // mode instead of applying the remap now.
            var result = BauschuttRemapApply.ApplyRemap();

            if (!string.IsNullOrEmpty(result.ReasonBlocked))
            {
                Messages.Message(
                    "Rimconemy Bauschutt-Remap blockiert: " + result.ReasonBlocked,
                    MessageTypeDefOf.RejectInput);
                Find.DesignatorManager.Deselect();
                return;
            }

            string placementSummary =
                "Rimconemy build: " + result.WallsPlaced + " Wall-Blueprints platziert "
                + "(Bauschutt-Write angefordert: " + result.BauschuttConsumed
                + "; best effort, Teilabzug möglich).";
            Messages.Message(placementSummary, MessageTypeDefOf.PositiveEvent);

            if (result.PlacementFailures != null && result.PlacementFailures.Count > 0)
            {
                foreach (var fail in result.PlacementFailures)
                {
                    Log.Warning("[Rimconemy.ScavengerInfrastructure] Bauschutt-Remap placement issue: " + fail);
                }
            }

            Find.DesignatorManager.Deselect();
        }

        /// <summary>
        /// Vanilla <c>CanDesignateCell</c> bleibt true (wir haben keinen Cell-Pick-Mechanismus).
        /// Notwendig, dass Player den Designator überhaupt auswählen kann.
        /// </summary>
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }
    }
}
