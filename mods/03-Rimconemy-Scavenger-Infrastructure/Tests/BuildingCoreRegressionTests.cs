using System;
using System.IO;
using Rimconemy.ScavengerInfrastructure.Power;
using Rimconemy.ScavengerInfrastructure.Resources;
using Rimconemy.ScavengerInfrastructure.Storage;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Tests
{
    /// <summary>
    /// Static regression gate for the Package 03 Building-Core.
    /// Source-level patch assertions run when the repository is available;
    /// DefDatabase assertions remain authoritative in a deployed mod.
    /// </summary>
    public static class BuildingCoreRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
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
            return true;
        }

        private static void TestResourceContract()
        {
            AssertEqual("Rimconemy.ConstructionDebris", ResourceCategory.ConstructionDebris,
                "Building: resource category remains stable");
            var debris = DefDatabase<ThingDef>.GetNamedSilentFail("Rimconemy_ConstructionDebris");
            AssertTrue(debris != null, "Building: ConstructionDebris ThingDef is loaded");
            AssertTrue(debris == null || debris.stuffProps != null,
                "Building: ConstructionDebris exposes stuffProps");
        }

        private static void TestPowerDefContract()
        {
            AssertEqual("Rimconemy_WoodCoalGenerator", PowerChainService.SolidFuelGeneratorDefName,
                "Building: solid generator defName is stable");
            AssertEqual("Rimconemy_WaterTurbineGenerator", PowerChainService.LiquidFuelGeneratorDefName,
                "Building: liquid generator defName is stable");
            AssertEqual("Rimconemy_ArrowTurret_Power", PowerChainService.ArrowTurretDefName,
                "Building: powered turret defName is stable");
        }

        private static void TestStorageSnapshotSchema()
        {
            var snapshot = new StorageSnapshot
            {
                SchemaVersion = StorageSnapshot.CurrentSchemaVersion,
                Entries = new System.Collections.Generic.List<StorageEntry>(),
                ContentHash = "0"
            };
            AssertEqual(1, snapshot.SchemaVersion, "Building: storage snapshot schema is v1");
            AssertTrue(snapshot.Entries != null, "Building: storage snapshot has entries collection");
        }

        private static void TestLoadedWallDoorMaterialContract()
        {
            var wall = DefDatabase<ThingDef>.GetNamedSilentFail("Wall");
            var door = DefDatabase<ThingDef>.GetNamedSilentFail("Door");
            AssertTrue(wall != null, "Building: Wall ThingDef is loaded");
            AssertTrue(door != null, "Building: Door ThingDef is loaded");
            if (wall != null)
                AssertTrue(wall.stuffCategories != null
                    && wall.stuffCategories.Exists(category => category != null && category.defName == "Stony"),
                    "Building: loaded Wall accepts Stony material category");
            if (door != null)
                AssertTrue(door.stuffCategories != null
                    && door.stuffCategories.Exists(category => category != null && category.defName == "Stony"),
                    "Building: loaded Door accepts Stony material category");
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
                Log.Message("[Rimconemy.ScavengerInfrastructure] BuildingCore tests: Bauschutt UI source unavailable in deployed mod; runtime DefDatabase gates remain authoritative.");
                return;
            }

            string designator = File.ReadAllText(designatorPath);
            string apply = File.ReadAllText(applyPath);
            string dashboard = File.ReadAllText(dashboardPath);

            AssertTrue(designator.Contains("best-effort physischen Storage-Abzug"),
                "Building: Architect designator explains best-effort physical write");
            AssertFalse(designator.Contains("physischer Storage-Verbrauch: OPEN"),
                "Building: Architect designator has no stale OPEN consumption claim");
            AssertTrue(apply.Contains("write already requested"),
                "Building: unchanged snapshot guard describes completed write request");
            AssertFalse(apply.Contains("physical storage consumption is OPEN"),
                "Building: apply guard has no stale OPEN consumption claim");
            AssertTrue(dashboard.Contains("Bauschutt-Aktion schreibt best effort"),
                "Building: dashboard distinguishes read-only snapshots from write action");
            AssertFalse(dashboard.Contains("echte Verbrauchs-, Bau- und Power-Mutationen sind noch nicht aktiv"),
                "Building: dashboard has no stale all-mutations-disabled banner");
        }

        private static void TestWallDoorPatchContractWhenSourceAvailable()
        {
            string path = FindSourceFile(
                "Patches", "Bauschutt_Remap_Patches.xml");
            if (!File.Exists(path))
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] BuildingCore tests: source patch unavailable in deployed mod; DefDatabase gates remain authoritative.");
                return;
            }

            string text = File.ReadAllText(path);
            AssertTrue(text.Contains("ThingDef[defName=\"Wall\"]/stuffCategories"),
                "Building: Wall category path exists");
            AssertTrue(text.Contains("ThingDef[defName=\"Door\"]/stuffCategories"),
                "Building: Door category path exists");
            AssertTrue(text.Contains("<value><li>Stony</li></value>"),
                "Building: existing Wall/Door category nodes receive Stony");
            AssertTrue(text.Contains("not(li[text()=\"Stony\"])") ,
                "Building: Wall/Door patch avoids duplicate Stony entries");
            AssertFalse(text.Contains("<value>\n      </value>"),
                "Building: Wall/Door patch has no empty existing-node value");
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

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[BuildingCoreRegression] " + label); }
        }

        private static void AssertFalse(bool condition, string label)
        {
            AssertTrue(!condition, label);
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[BuildingCoreRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
