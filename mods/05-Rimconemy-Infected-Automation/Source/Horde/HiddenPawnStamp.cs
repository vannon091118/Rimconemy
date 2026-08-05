// Source/Horde/HiddenPawnStamp.cs
//
// Phase F — Lightweight-Pawn-State für den HordeManifest. KEINE
// direkten Pawn-Objekte (Scribe-freundlich, ~250 bytes per stamp ×
// 200 Stamps = ~50 KB Save-Size). Rekonstruktion deterministisch
// via PawnGenerator + EquipmentSeedOffset (siehe HordeMaterializationService).
//
// Spec §3.2.

using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public struct HiddenPawnStamp : IExposable
    {
        public string ThingID;
        public string KindDefName;
        public string FactionDefName;
        public float HealthPercent;
        public int EquipmentSeedOffset;
        public long SpawnedAtTick;
        public int SourceCellHashHint;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ThingID, "thingId", "");
            Scribe_Values.Look(ref KindDefName, "kindDefName", "");
            Scribe_Values.Look(ref FactionDefName, "factionDefName", "");
            Scribe_Values.Look(ref HealthPercent, "healthPercent", 1.0f);
            Scribe_Values.Look(ref EquipmentSeedOffset, "equipmentSeedOffset", 0);
            Scribe_Values.Look(ref SpawnedAtTick, "spawnedAtTick", 0L);
            Scribe_Values.Look(ref SourceCellHashHint, "sourceCellHashHint", 0);
        }
    }
}
