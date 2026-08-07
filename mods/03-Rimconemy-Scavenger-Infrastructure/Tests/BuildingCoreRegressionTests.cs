using System;
using System.IO;
using Rimconemy.ScavengerInfrastructure.Power;
using Rimconemy.ScavengerInfrastructure.Resources;
using Rimconemy.ScavengerInfrastructure.Storage;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.ScavengerInfrastructure.Tests
{
    /// <summary>
    /// Static regression gate for the Package 03 Building-Core.
    /// Source-level patch assertions run when the repository is available;
    /// DefDatabase assertions remain authoritative in a deployed mod.
    /// </summary>
    public static class BuildingCoreRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("ScavengerInfrastructure", "BuildingCore regression tests");

            _passed = 0;
            _failed = 0;

            TestResourceContract();
            TestPowerDefContract();
            TestStorageSnapshotSchema();
            TestLoadedWallDoorMaterialContract();
            TestWallDoorPatchContractWhenSourceAvailable();
            TestBauschuttUiStatusContractWhenSourceAvailable();

            string summary = "[Rimconemy.ScavengerInfrastructure] BuildingCore regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }

            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        private static void TestResourceContract()
        {
            ts.Check(Equals("Rimconemy.ConstructionDebris", ResourceCategory.ConstructionDebris), "Building: resource category remains stable");
            var debris = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_ConstructionDebris");
            ts.Check(debris != null, "Building: ConstructionDebris ThingDef is loaded");
            // D3-Harmonisierung: ConstructionDebris ist kein Stuff mehr.
            // <stuffProps> wurde bewusst aus dem Def entfernt (kein
            // Wall-Stuff, kein Door-Stuff). Der Test prüft, dass der Def
            // ohne stuffProps stabil bootet.
            ts.Check(debris != null, "Building: ConstructionDebris ThingDef is loaded");
            ts.Check(debris.stuffProps == null, "Building: ConstructionDebris is no longer Stuff (D3 — stuffProps removed)");
        }

        private static void TestPowerDefContract()
        {
            ts.Check(Equals("Rimconemy_WoodCoalGenerator", PowerChainService.SolidFuelGeneratorDefName), "Building: solid generator defName is stable");
            ts.Check(Equals("Rimconemy_WaterTurbineGenerator", PowerChainService.LiquidFuelGeneratorDefName), "Building: liquid generator defName is stable");
            ts.Check(Equals("Rimconemy_ArrowTurret_Power", PowerChainService.ArrowTurretDefName), "Building: powered turret defName is stable");
        }

        private static void TestStorageSnapshotSchema()
        {
            var snapshot = new StorageSnapshot
            {
                SchemaVersion = StorageSnapshot.CurrentSchemaVersion,
                Entries = new System.Collections.Generic.List<StorageEntry>(),
                ContentHash = "0"
            };
            ts.Check(Equals(1, snapshot.SchemaVersion), "Building: storage snapshot schema is v1");
            ts.Check(snapshot.Entries != null, "Building: storage snapshot has entries collection");
        }

        private static void TestLoadedWallDoorMaterialContract()
        {
            var wall = DefDatabase<ThingDef>.GetNamedSilentFail("Wall");
            var door = DefDatabase<ThingDef>.GetNamedSilentFail("Door");
            ts.Check(wall != null, "Building: Wall ThingDef is loaded");
            ts.Check(door != null, "Building: Door ThingDef is loaded");
            if (wall != null)
                ts.Check(wall.stuffCategories != null
                    && wall.stuffCategories.Exists(category => category != null && category.defName == "Stony"), "Building: loaded Wall accepts Stony material category");
            if (door != null)
                ts.Check(door.stuffCategories != null
                    && door.stuffCategories.Exists(category => category != null && category.defName == "Stony"), "Building: loaded Door accepts Stony material category");
        }

        private static void TestBauschuttUiStatusContractWhenSourceAvailable()
        {
            string designatorPath = FindSourceFile(
                "Source", "Building", "Designator_BuildWallBauschutt.cs");
            string applyPath = FindSourceFile(
                "Source", "Building", "BauschuttRemapApply.cs");
            string dashboardPath = FindSourceFile(
                "Source", "UI", "InfrastructureDashboard.cs");

            if (!File.Exists(designatorPath)
                || !File.Exists(applyPath)
                || !File.Exists(dashboardPath))
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] BuildingCore TEST-DEFERRED T_BauschuttUI: source unavailable in deployed mod.");
                return;
            }

            string designator = File.ReadAllText(designatorPath);
            string apply = File.ReadAllText(applyPath);
            string dashboard = File.ReadAllText(dashboardPath);

            ts.Check(designator.Contains("best-effort physischen Storage-Abzug"), "Building: Architect designator explains best-effort physical write");
            ts.Check(!(designator.Contains("physischer Storage-Verbrauch: OPEN")), "Building: Architect designator has no stale OPEN consumption claim");
            ts.Check(apply.Contains("write already requested"), "Building: unchanged snapshot guard describes completed write request");
            ts.Check(!(apply.Contains("physical storage consumption is OPEN")), "Building: apply guard has no stale OPEN consumption claim");
            ts.Check(dashboard.Contains("Bauschutt-Aktion schreibt best effort"), "Building: dashboard distinguishes read-only snapshots from write action");
            ts.Check(!(dashboard.Contains("echte Verbrauchs-, Bau- und Power-Mutationen sind noch nicht aktiv")), "Building: dashboard has no stale all-mutations-disabled banner");
        }

        private static void TestWallDoorPatchContractWhenSourceAvailable()
        {
            string path = FindSourceFile(
                "Patches", "Bauschutt_Remap_Patches.xml");
            if (!File.Exists(path))
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] BuildingCore TEST-DEFERRED T_PatchContract: source patch unavailable in deployed mod.");
                return;
            }

            string text = File.ReadAllText(path);
            ts.Check(text.Contains("ThingDef[defName=\"Wall\"]/stuffCategories"), "Building: Wall category path exists");
            ts.Check(text.Contains("ThingDef[defName=\"Door\"]/stuffCategories"), "Building: Door category path exists");
            ts.Check(text.Contains("<value><li>Stony</li></value>"), "Building: existing Wall/Door category nodes receive Stony");
            ts.Check(text.Contains("not(li[text()=\"Stony\"])"), "Building: Wall/Door patch avoids duplicate Stony entries");
            ts.Check(!(text.Contains("<value>\n      </value>")), "Building: Wall/Door patch has no empty existing-node value");
        }

        private static string FindSourceFile(params string[] parts)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                string candidate = path;
                for (int p = 0; p < parts.Length; p++) candidate = Path.Combine(candidate, parts[p]);
                if (File.Exists(candidate)) return candidate;
                path = Directory.GetParent(path)?.FullName;
                if (string.IsNullOrEmpty(path)) break;
            }
            return string.Empty;
        }


    }
}
