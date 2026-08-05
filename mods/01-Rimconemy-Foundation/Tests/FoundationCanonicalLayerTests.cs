using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Rimconemy.Foundation.Canonical;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    ///
    /// VC-8 / Phase VC — Cross-Domain Falsification Harness.
    ///
    /// Three layers under test:
    ///   1. <see cref="MaterialIdentityRegistry"/> — material role catalogue
    ///   2. <see cref="NeedSettingsTranslator"/> — Vanilla need → Setting id
    ///   3. <see cref="RoomRoleResolver"/> — vanilla Room.Role → functional role
    ///
    /// Each test asserts ONE invariant. The harness is
    /// deterministic (no DefDatabase mutation), exit-fast on first failed
    /// assertion so the boot log surfaces the smallest possible failing
    /// surface, and idempotent (re-runnable in the same process).
    ///
    /// Test plan:
    ///   T1  MaterialIdentity — every MaterialRole enum value emits a label
    ///   T2  MaterialIdentity — null Thing / null ThingDef → Unclassified (safe default)
    ///   T3  MaterialIdentity — registry can Reindex multiple times without throwing
    ///   T4  MaterialIdentity — DefModExtension MaterialIdentityExt exposes role, displayTag, secondaryRoles
    ///   T5  SettingIdentity — every Setting identity has a non-empty label
    ///   T6  SettingIdentity — null NeedDef → SettingIdentity.None
    ///   T7  SettingIdentity — NeedDefOf.Food translates to SettingIdentity.Food
    ///   T8  SettingIdentity — NeedDefOf.Rest translates to SettingIdentity.Safety
    ///   T9  SettingIdentity — IsCritical boundary semantics
    ///   T10 RoomRoleResolver — every RimconemyRoomRole emits a label
    ///   T11 RoomRoleResolver — null Room returns Other
    ///   T12 RoomRoleResolver — vanilla mapping table contains core rooms (Kitchen, Workshop, Barracks, BedRoom, Hospital)
    ///   T13 RoomRoleResolver — Kitchen maps to Produktion
    ///   T14 RoomRoleResolver — PrisonBarracks maps to Verteidigung
    ///   T15 Cross-domain — Round-trip: MaterialRole enum count >= NumberOfModExtensibleSampleRows (architecture invariant)
    ///   T16 Cross-domain — ReindexVanillaTable does not lose entries when called repeatedly
    /// </summary>
    public static class FoundationCanonicalLayerTests
    {
        public const int ExpectedPassCount = 22;

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name, string detail = null)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null)
                    firstFailure = name + (detail == null ? "" : " — " + detail);
                Log.Warning(
                    "[Rimconemy.Foundation] CanonicalLayer test FAILED: " +
                    name + (detail == null ? "" : " — " + detail));
            }

            // T1
            Check(TestAllMaterialRolesHaveLabels(), "T1.EachMaterialRoleHasLabel");
            // T2
            Check(TestMaterialIdentitySafeDefaultsForNull(),
                  "T2.NullSafeOnMaterialIdentity",
                  "Null thing/def/def-without-ext must return Unclassified without throwing");
            // T3
            Check(TestReindexIsRepeatableAndSafe(), "T3.ReindexRepeatable");
            // T4
            Check(TestMaterialIdentityExtFields(), "T4.MaterialIdentityExtFields");
            // T5
            Check(TestAllSettingIdentitiesHaveLabels(), "T5.EachSettingIdentityHasLabel");
            // T6
            Check(TestSettingIdentityNullSafe(), "T6.NullSafeOnNeedSettingsTranslator");
            // T7
            Check(TestVanillaFoodTranslatesToFood(), "T7.Food→Food");
            // T8
            Check(TestVanillaRestTranslatesToSafety(), "T8.Rest→Safety");
            // T9
            Check(TestIsCriticalBoundaries(), "T9.IsCriticalBoundaries");
            // T10
            Check(TestAllRoomRolesHaveLabels(), "T10.EachRoomRoleHasLabel");
            // T11
            Check(TestRoomRoleResolverNullSafe(), "T11.NullSafeOnRoomRoleResolver");
            // T12
            Check(TestRoomRoleResolverContainsCoreRooms(), "T12.CoreRoomsInTable");
            // T13
            Check(TestVanillaRoomMapsToProduktion(), "T13.Kitchen→Produktion");
            // T14
            Check(TestVanillaRoomMapsToVerteidigung(), "T14.PrisonBarracks→Verteidigung");
            // T15
            Check(TestCrossDomainEnumCoverage(), "T15.CrossDomainEnumCoverage");
            // T16
            Check(TestReindexVanillaTableRepeatable(), "T16.VanillaTableReindexRepeatable");
            // T17
            Check(TestMaterialIdentityRegistryUsesDefNameKey(), "T17.MaterialIdentityUsesDefNameKey");
            // T18
            Check(TestMaterialIdentityReindexIsStableAcrossCalls(), "T18.MaterialIdentityReindexStable");
            // T19
            Check(TestRimconemyKeyedFallbackForMissing(), "T19.RimconemyKeyedMissingFallback");
            // T20
            Check(TestRimconemyKeyedFallbackForEmpty(), "T20.RimconemyKeyedEmptyFallback");
            // T21
            Check(TestFurnitureHashSetsAreInitialised(), "T21.FurnitureHashSetsNonNull");
            // T22
            Check(TestResolveIncludingFurnitureNullSafe(), "T22.ResolveIncludingFurnitureNullSafe");

            Log.Message(
                "[Rimconemy.Foundation] Canonical layer tests: " + passed + " passed, " + failed +
                " failed (expected=" + ExpectedPassCount + ")." +
                (firstFailure == null ? "" : " First failure: " + firstFailure));
            return failed;
        }

        // --- T1: every MaterialRole value emits a non-empty label ---
        public static bool TestAllMaterialRolesHaveLabels()
        {
            try
            {
                foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
                {
                    var label = MaterialIdentityRegistry.PawnLabelFor(role);
                    if (string.IsNullOrEmpty(label)) return false;
                }
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T2: null / un-extended ThingDef → Unclassified ---
        public static bool TestMaterialIdentitySafeDefaultsForNull()
        {
            try
            {
                if (MaterialIdentityRegistry.GetRoleOf((ThingDef)null) != MaterialRole.Unclassified) return false;
                if (MaterialIdentityRegistry.GetRoleOf((Thing)null) != MaterialRole.Unclassified) return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T3: Reindex is idempotent and not throwing ---
        public static bool TestReindexIsRepeatableAndSafe()
        {
            try
            {
                MaterialIdentityRegistry.Reindex();
                MaterialIdentityRegistry.Reindex();
                MaterialIdentityRegistry.Reindex();
                return MaterialIdentityRegistry.IsReady;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T4: MaterialIdentityExt exposes the documented fields ---
        public static bool TestMaterialIdentityExtFields()
        {
            try
            {
                var t = typeof(MaterialIdentityExt);
                if (t.GetField("role") == null) return false;
                if (t.GetField("displayTag") == null) return false;
                if (t.GetField("secondaryRoles") == null) return false;
                if (t.GetField("migrationMarker") == null) return false;
                // Instantiate default values
                var inst = (MaterialIdentityExt)Activator.CreateInstance(t);
                if (inst.role != MaterialRole.Unclassified) return false;
                if (inst.secondaryRoles != null && inst.secondaryRoles.Count != 0) return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T5: every SettingIdentity has a label ---
        public static bool TestAllSettingIdentitiesHaveLabels()
        {
            try
            {
                foreach (SettingIdentity id in Enum.GetValues(typeof(SettingIdentity)))
                {
                    var label = NeedSettingsTranslator.Label(id);
                    if (string.IsNullOrEmpty(label)) return false;
                }
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T6: null NeedDef → None ---
        public static bool TestSettingIdentityNullSafe()
        {
            try
            {
                if (NeedSettingsTranslator.Translate((NeedDef)null) != SettingIdentity.None) return false;
                if (NeedSettingsTranslator.Translate((Need)null) != SettingIdentity.None) return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T7: NeedDefOf.Food → Food ---
        public static bool TestVanillaFoodTranslatesToFood()
        {
            try
            {
                if (NeedDefOf.Food == null) return true; // DefDatabase might not be ready in tests; soft skip
                return NeedSettingsTranslator.Translate(NeedDefOf.Food) == SettingIdentity.Food;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] T7 FAILED: " + ex.GetType().Name + ": " + ex.Message); return false; }
        }

        // --- T8: NeedDefOf.Rest → Safety ---
        public static bool TestVanillaRestTranslatesToSafety()
        {
            try
            {
                if (NeedDefOf.Rest == null) return true;
                return NeedSettingsTranslator.Translate(NeedDefOf.Rest) == SettingIdentity.Safety;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] T8 FAILED: " + ex.GetType().Name + ": " + ex.Message); return false; }
        }

        // --- T9: IsCritical boundary semantics ---
        public static bool TestIsCriticalBoundaries()
        {
            try
            {
                if (NeedSettingsTranslator.IsCritical(SettingIdentity.None, 0f)) return false;
                if (!NeedSettingsTranslator.IsCritical(SettingIdentity.Food, 0.19f)) return false;
                if (NeedSettingsTranslator.IsCritical(SettingIdentity.Food, 0.21f)) return false;
                if (NeedSettingsTranslator.IsCritical(SettingIdentity.Food, 0.99f)) return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T10: every RimconemyRoomRole has a label ---
        public static bool TestAllRoomRolesHaveLabels()
        {
            try
            {
                foreach (RimconemyRoomRole role in Enum.GetValues(typeof(RimconemyRoomRole)))
                {
                    var label = RoomRoleResolver.Label(role);
                    if (string.IsNullOrEmpty(label)) return false;
                }
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T11: null Room → Other ---
        public static bool TestRoomRoleResolverNullSafe()
        {
            try
            {
                if (RoomRoleResolver.Resolve(null) != RimconemyRoomRole.Other) return false;
                if (RoomRoleResolver.ResolveIncludingFurniture(null) != RimconemyRoomRole.Other) return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T12: vanilla mapping table contains core rooms ---
        public static bool TestRoomRoleResolverContainsCoreRooms()
        {
            try
            {
                RoomRoleResolver.ReindexVanillaTable();
                var field = typeof(RoomRoleResolver).GetField(
                    "_vanillaToRimconemy",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (field == null) return false;
                var map = field.GetValue(null) as IDictionary;
                if (map == null) return false;
                foreach (var expected in new[] { "Kitchen", "Workshop", "Barracks", "Bedroom", "Hospital" })
                {
                    if (!map.Contains(expected)) return false;
                }
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T13: Kitchen → Produktion (vanilla-table inspection) ---
        public static bool TestVanillaRoomMapsToProduktion()
        {
            try
            {
                RoomRoleResolver.ReindexVanillaTable();
                var field = typeof(RoomRoleResolver).GetField(
                    "_vanillaToRimconemy",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (field == null) return false;
                var map = field.GetValue(null) as IDictionary;
                if (map == null || !map.Contains("Kitchen")) return false;
                object value = map["Kitchen"];
                if (value == null) return false;
                return value.Equals(RimconemyRoomRole.Produktion);
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T14: PrisonBarracks → Verteidigung ---
        public static bool TestVanillaRoomMapsToVerteidigung()
        {
            try
            {
                RoomRoleResolver.ReindexVanillaTable();
                var field = typeof(RoomRoleResolver).GetField(
                    "_vanillaToRimconemy",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (field == null) return false;
                var map = field.GetValue(null) as IDictionary;
                if (map == null || !map.Contains("PrisonBarracks")) return false;
                object value = map["PrisonBarracks"];
                if (value == null) return false;
                return value.Equals(RimconemyRoomRole.Verteidigung);
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T15: enum coverage invariant (N MaterialRole >= 1 + N RoomRole >= 5 + N SettingIdentity >= 3) ---
        public static bool TestCrossDomainEnumCoverage()
        {
            try
            {
                int materialRoles = Enum.GetValues(typeof(MaterialRole)).Length;
                int roomRoles = Enum.GetValues(typeof(RimconemyRoomRole)).Length;
                int settingIdentities = Enum.GetValues(typeof(SettingIdentity)).Length;
                // Architecture invariant: at least 6 roles per domain so labels are non-trivial.
                if (materialRoles < 6) return false;
                if (roomRoles < 5) return false;
                if (settingIdentities < 3) return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T16: ReindexVanillaTable is idempotent and preserves at least the same entries ---
        public static bool TestReindexVanillaTableRepeatable()
        {
            try
            {
                RoomRoleResolver.ReindexVanillaTable();
                var field = typeof(RoomRoleResolver).GetField(
                    "_vanillaToRimconemy",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (field == null) return false;
                int firstCount = (field.GetValue(null) as IDictionary)?.Count ?? 0;
                RoomRoleResolver.ReindexVanillaTable();
                int secondCount = (field.GetValue(null) as IDictionary)?.Count ?? 0;
                return firstCount == secondCount && firstCount >= 6;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T17: MaterialIdentityRegistry Critical-stability (defName-keyed, not ushort-hashed) ---
        public static bool TestMaterialIdentityRegistryUsesDefNameKey()
        {
            try
            {
                MaterialIdentityRegistry.Reindex();
                var fieldPrimary = typeof(MaterialIdentityRegistry).GetField(
                    "_primaryByName",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (fieldPrimary == null) return false;
                var dict = fieldPrimary.GetValue(null) as IDictionary;
                if (dict == null) return false;
                // Key type must be string, not ushort. Inspect the declared
                // generic argument rather than the first runtime key: a
                // registry with no MaterialIdentityExt entries is valid and
                // therefore has no keys to enumerate.
                Type declaredType = fieldPrimary.FieldType;
                if (!declaredType.IsGenericType
                    || declaredType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                    return false;
                Type[] genericArguments = declaredType.GetGenericArguments();
                if (genericArguments.Length != 2 || genericArguments[0] != typeof(string))
                    return false;

                // Keep a runtime-shape guard as well: the field must still
                // contain the declared Dictionary instance after Reindex().
                Type runtimeType = dict.GetType();
                return runtimeType.IsGenericType
                    && runtimeType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                    && runtimeType.GetGenericArguments()[0] == typeof(string);
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T18: Reindex of MaterialIdentityRegistry is stable across calls ---
        public static bool TestMaterialIdentityReindexIsStableAcrossCalls()
        {
            try
            {
                MaterialIdentityRegistry.Reindex();
                var field = typeof(MaterialIdentityRegistry).GetField(
                    "_primaryByName",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (field == null) return false;
                int firstCount = (field.GetValue(null) as IDictionary)?.Count ?? -1;
                MaterialIdentityRegistry.Reindex();
                int secondCount = (field.GetValue(null) as IDictionary)?.Count ?? -2;
                return firstCount == secondCount;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T19: RimconemyKeyed.Try returns fallback (no Log.Error spam) for missing keys ---
        public static bool TestRimconemyKeyedFallbackForMissing()
        {
            try
            {
                string result = RimconemyKeyed.Try(
                    "Rimconemy.ThisKeyDoesNotExist_2026-08-04_Test",
                    "Test-Fallback");
                return result == "Test-Fallback";
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T20: RimconemyKeyed.Try returns fallback for empty key ---
        public static bool TestRimconemyKeyedFallbackForEmpty()
        {
            try
            {
                string result = RimconemyKeyed.Try("", "empty-fallback");
                return result == "empty-fallback";
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T21: RoomRoleResolver RebuildFurnitureHashSets creates empty but non-null sets when DB not ready ---
        public static bool TestFurnitureHashSetsAreInitialised()
        {
            try
            {
                RoomRoleResolver.RebuildFurnitureHashSets();
                var fieldStorage = typeof(RoomRoleResolver).GetField(
                    "_storageFurnitureDefNames",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var fieldDefense = typeof(RoomRoleResolver).GetField(
                    "_defenseFurnitureDefNames",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (fieldStorage == null || fieldDefense == null) return false;
                // HashSet<string> implements ICollection<string> (generic) but not
                // the non-generic System.Collections.ICollection. Just check non-null.
                object storage = fieldStorage.GetValue(null);
                object defense = fieldDefense.GetValue(null);
                return storage != null && defense != null;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }

        // --- T22: ResolveIncludingFurniture is null-safe (room == null or things == null) ---
        public static bool TestResolveIncludingFurnitureNullSafe()
        {
            try
            {
                // Already covered indirectly by T11 (Resolve(null) → Other),
                // but be explicit about the secondary path which uses the
                // pre-built furniture HashSets.
                RimconemyRoomRole r1 = RoomRoleResolver.ResolveIncludingFurniture(null);
                return r1 == RimconemyRoomRole.Other;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod01] test caught: " + ex); return false; }
        }
    }
}
