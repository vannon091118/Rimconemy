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
    /// die Map; falls Wallet-Zugriff je nötig wird, muss CapabilityAudit-Gate
    /// genutzt werden (rimconemy.economyterritory.wallet check). Aktuell wird
    /// kein Wallet-Zugriff gebraucht — ApplyRemap platziert nur Blueprints.
    ///
    /// Vanilla-Healthy-Verification: der Designator bleibt ein UI-Trigger;
    /// die tatsächliche Blueprint-Construction bleibt ein Runtime-Gate.
    /// </summary>
    public class Designator_BuildWallBauschutt : Designator
    {
        public Designator_BuildWallBauschutt() : base()
        {
            this.defaultLabel = "Rimconemy · BuildWallBauschutt";
            this.defaultDesc = "Platziert Wall-Blueprints anhand des gelesenen Bauschutt-Bestands. Der physische Verbrauch bleibt bis zum Storage-Write-Gate offen.";
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
                + "(Bauschutt logisch zugeordnet: " + result.BauschuttConsumed + "; physischer Storage-Verbrauch: OPEN).";
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
