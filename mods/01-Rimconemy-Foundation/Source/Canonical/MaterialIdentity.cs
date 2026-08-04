using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Canonical
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    ///
    /// VC-2 / Phase VC — Canonical Material Identity Layer.
    ///
    /// Vanilla has <c>ThingDef</c> trees for every "Good" (WoodLog, ChunkSteel,
    /// GraniteBlock, …). Rimconemy must NOT clone them as <c>Rimconemy_Bauschutt</c>
    /// or <c>Rimconemy_Hemp</c> — that was the prior drift. Instead each vanilla
    /// ThingDef carries a <see cref="MaterialIdentityExt"/> <see cref="DefModExtension"/>
    /// with a <see cref="MaterialRole"/>. The same wood log can be "Baumaterial",
    /// "Bauschutt" or "Handelsware" — semantically — without inventing new
    /// ThingDefs.
    ///
    /// Anti-pattern (decommissioned 2026-08-04): Nonce-ThingDefs registered as
    /// siblings of vanilla. Drift-Audit §8 documents the migration path.
    ///
    /// Read model: callers query <see cref="MaterialIdentityRegistry.GetRoleOf"/>
    /// and <see cref="MaterialIdentityRegistry.PawnLabelFor"/> without
    /// attaching anything to the pawn.
    /// </summary>
    public enum MaterialRole
    {
        /// <summary>No role discovered — caller decides what neutral default means.</summary>
        Unclassified = 0,

        /// <summary>Wood, bricks, steel — drives construction bills (Rimconemy: Baumaterial).</summary>
        Baumaterial = 1,

        /// <summary>Recoverable debris / rubble from destroyed structures (Rimconemy: Bauschutt).</summary>
        Bauschutt = 2,

        /// <summary>Edible harvest and food products (Rimconemy: Nahrung).</summary>
        Nahrung = 3,

        /// <summary>Fuel that powers generators (Rimconemy: Brennstoff).</summary>
        Brennstoff = 4,

        /// <summary>Water / fluids carried in containers (Rimconemy: Wasser).</summary>
        Wasser = 5,

        /// <summary>Good sold on the market with no construction role (Rimconemy: Handelsware).</summary>
        Handelsware = 6,

        /// <summary>Granite / marble wool-stones for sale (Rimconemy: Konstruktion).</summary>
        Konstruktion = 7,

        /// <summary>Hemp / cotton / leather — processed base for clothing (Rimconemy: Rohstoff).</summary>
        Rohstoff = 8,

        /// <summary>Catch-all for plants that are NOT food and NOT ornamental harvests.</summary>
        Sonstiges = 9,
    }

    /// <summary>
    /// DefModExtension attached to a vanilla <c>ThingDef</c> by Foundation's
    /// PatchOperation layer. The def still belongs to vanilla (or DLC);
    /// Rimconemy only annotates the role.
    /// </summary>
    public class MaterialIdentityExt : DefModExtension
    {
        /// <summary>Canonical role assigned to this ThingDef.</summary>
        public MaterialRole role = MaterialRole.Unclassified;

        /// <summary>
        /// Optional display tag (used to drive UI labels without forcing one
        /// long German literal in C#). Examples: "Holz", "Stahl", "Stein".
        /// </summary>
        public string displayTag;

        /// <summary>
        /// If a ThingDef carries multiple valid roles (e.g. wood that is also
        /// a fuel), additional roles are listed here. Primary role stays in
        /// <see cref="role"/>; this list is iterated for snapshot fan-out.
        /// </summary>
        public List<MaterialRole> secondaryRoles;

        /// <summary>Source marker for migrations — e.g. "rubble-debris".</summary>
        public string migrationMarker;
    }

    /// <summary>
    /// Canonical registry that resolves a ThingDef to a <see cref="MaterialRole"/>.
    /// Owner: Foundation. Read-only after PostLoadInit.
    ///
    /// Why a static catalog: it is the SAME read layer for StorageSnapshot,
    /// Market, RoomRoleResolver and any future dashboard. Centralising
    /// avoids the four-way drift seen in Phase-0 (03 StorageQuery, 04 Market,
    /// 05 Threat — each had its own ad-hoc thing filters).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MaterialIdentityRegistry
    {
        // Phase-VC / 2026-08-04 Critical-Fix (Code-Reviewer): the previous
        // implementation used a ushort (15-bit masked) hash of defName +
        // shortHash to key the lookup. With ~1500 Vanilla + DLC ThingDefs
        // and only 32768 distinct slots, expected ~17 hash collisions per
        // slot, and because Dictionary indexer silently OVERWRITES on
        // duplicate key, the registry could forget any def whose hash
        // collided with a later-iterated def having a different role.
        //
        // The current implementation keys entirely by the canonical
        // .defName string. Memory cost for 1500 entries ≈ 250 KB
        // (Dictionary<int,MaterialRole> with string interning), still
        // negligible on the ≤2 ms / 1 MiB budget. Determinism is now
        // identical across sessions.
        private static Dictionary<string, MaterialRole> _primaryByName;
        private static Dictionary<string, List<MaterialRole>> _secondaryByName;
        private static Dictionary<string, string> _tagByName;
        private static bool _ready;

        static MaterialIdentityRegistry()
        {
            Reindex();
        }

        /// <summary>
        /// Re-build the look-up tables from <see cref="DefDatabase{ThingDef}"/>.
        /// Cheap (≤2 ms for the whole vanilla def set) and only called once
        /// per session — cctor-on-startup anchor. Tests can call it again
        /// after adding synthetic mocks.
        /// </summary>
        public static void Reindex()
        {
            _primaryByName = new Dictionary<string, MaterialRole>(StringComparer.Ordinal);
            _secondaryByName = new Dictionary<string, List<MaterialRole>>(StringComparer.Ordinal);
            _tagByName = new Dictionary<string, string>(StringComparer.Ordinal);
            _ready = false;

            if (DefDatabase<ThingDef>.AllDefsListForReading == null)
            {
                _ready = true;
                return;
            }

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def == null) continue;
                var ext = def.GetModExtension<MaterialIdentityExt>();
                if (ext == null) continue;

                string key = def.defName ?? string.Empty;
                if (key.Length == 0) continue;

                _primaryByName[key] = ext.role;
                _tagByName[key] = ext.displayTag ?? def.defName;

                if (ext.secondaryRoles != null && ext.secondaryRoles.Count > 0)
                {
                    var copy = new List<MaterialRole>(ext.secondaryRoles.Count);
                    foreach (var r in ext.secondaryRoles)
                        copy.Add(r);
                    _secondaryByName[key] = copy;
                }
            }

            _ready = true;
        }

        /// <summary>Resolve canonical role for a <see cref="ThingDef"/>.</summary>
        public static MaterialRole GetRoleOf(ThingDef def)
        {
            if (def == null) return MaterialRole.Unclassified;
            if (!_ready) Reindex();
            string key = def.defName ?? string.Empty;
            if (key.Length == 0) return MaterialRole.Unclassified;
            MaterialRole role;
            if (_primaryByName.TryGetValue(key, out role))
                return role;
            return MaterialRole.Unclassified;
        }

        /// <summary>Convenience overload for live <see cref="Thing"/> instances.</summary>
        public static MaterialRole GetRoleOf(Thing thing)
        {
            if (thing == null) return MaterialRole.Unclassified;
            return GetRoleOf(thing.def);
        }

        /// <summary>All roles (primary + secondary) for a ThingDef.</summary>
        public static IReadOnlyList<MaterialRole> GetAllRolesOf(ThingDef def)
        {
            if (def == null) return Array.Empty<MaterialRole>();
            if (!_ready) Reindex();
            var list = new List<MaterialRole>();
            string key = def.defName ?? string.Empty;
            if (key.Length == 0) return list;
            MaterialRole primary;
            if (_primaryByName.TryGetValue(key, out primary))
                list.Add(primary);
            List<MaterialRole> secondary;
            if (_secondaryByName.TryGetValue(key, out secondary))
                foreach (var r in secondary) list.Add(r);
            return list;
        }

        /// <summary>Returns all ThingDefs whose primary role matches <paramref name="role"/>.</summary>
        public static IReadOnlyList<ThingDef> AllDefsWithPrimaryRole(MaterialRole role)
        {
            if (!_ready) Reindex();
            var list = new List<ThingDef>();
            if (_primaryByName == null || DefDatabase<ThingDef>.AllDefsListForReading == null)
                return list;
            var defs = DefDatabase<ThingDef>.AllDefsListForReading;
            foreach (var def in defs)
            {
                if (def == null) continue;
                string k = def.defName;
                if (string.IsNullOrEmpty(k)) continue;
                MaterialRole r;
                if (_primaryByName.TryGetValue(k, out r) && r == role)
                    list.Add(def);
            }
            return list;
        }

        /// <summary>Pretty label suitable for UI display (German-friendly fallback chain).</summary>
        public static string PawnLabelFor(MaterialRole role)
        {
            switch (role)
            {
                case MaterialRole.Baumaterial: return "Rimconemy.Material.Baumaterial".TranslateOrFallback("Baumaterial");
                case MaterialRole.Bauschutt:   return "Rimconemy.Material.Bauschutt".TranslateOrFallback("Bauschutt");
                case MaterialRole.Nahrung:     return "Rimconemy.Material.Nahrung".TranslateOrFallback("Nahrung");
                case MaterialRole.Brennstoff:  return "Rimconemy.Material.Brennstoff".TranslateOrFallback("Brennstoff");
                case MaterialRole.Wasser:      return "Rimconemy.Material.Wasser".TranslateOrFallback("Wasser");
                case MaterialRole.Handelsware: return "Rimconemy.Material.Handelsware".TranslateOrFallback("Handelsware");
                case MaterialRole.Konstruktion:return "Rimconemy.Material.Konstruktion".TranslateOrFallback("Konstruktion");
                case MaterialRole.Rohstoff:    return "Rimconemy.Material.Rohstoff".TranslateOrFallback("Rohstoff");
                case MaterialRole.Sonstiges:   return "Rimconemy.Material.Sonstiges".TranslateOrFallback("Sonstiges");
                default:                       return "Rimconemy.Material.Unclassified".TranslateOrFallback("Unklassifiziert");
            }
        }

        /// <summary>Display tag for a ThingDef (ext.displayTag or fallback to defName).</summary>
        public static string DisplayTag(ThingDef def)
        {
            if (def == null) return string.Empty;
            if (!_ready) Reindex();
            string tag;
            if (_tagByName.TryGetValue(def.defName ?? string.Empty, out tag))
                return tag;
            return def.defName ?? string.Empty;
        }

        /// <summary>Visible for testing — never false in production after cctor.</summary>
        public static bool IsReady => _ready;

        /// <summary>Internal test helper: pseudo-clear for synthetic Reindex tests.</summary>
        internal static void ResetForTests()
        {
            _primaryByName = null;
            _secondaryByName = null;
            _tagByName = null;
            _ready = false;
            Reindex();
        }
    }

    /// <summary>
    /// Tiny helper that lets the resolver translate a key without crashing
    /// when the language XML is missing.
    ///
    /// Phase-VC / 2026-08-04 Medium-Fix (Code-Reviewer): the previous
    /// implementation called <c>key.Translate()</c> unconditionally.
    /// RimWorld 1.6 internally emits a red <c>Log.Error("Keyed missing…")</c>
    /// BEFORE the user's catch can intervene, so missing localized strings
    /// flooded the operator log even though the runtime value was safe.
    /// The current implementation uses reflection to query the active language
    /// dictionary directly, avoiding the Log.Error side effect entirely.
    /// </summary>
    public static class RimconemyKeyed
    {
        private static System.Reflection.FieldInfo _languageDictField;
        private static System.Reflection.PropertyInfo _activeLanguageProperty;

        private static System.Collections.Generic.Dictionary<string, string> GetLanguageDict()
        {
            // F-01 helper fix (2026-08-04): RimWorld 1.6 exposes the active
            // Language via `LanguageDatabase.ActiveLanguage` (property of
            // type `RimWorld.Language`). The dictionary lives on the
            // `Language` instance under a NonPublic field named "dictionary"
            // (per Cecil-Spike). We resolve both endpoints through reflection
            // to keep this file free of symbol dependencies on the Language
            // type — if 1.6 renames the path we still discover it at runtime
            // and degrade to "no dictionary" without a hard build break.
            if (_activeLanguageProperty == null)
            {
                _activeLanguageProperty = System.Type.GetType("RimWorld.LanguageDatabase, Assembly-CSharp", false)
                    ?.GetProperty("ActiveLanguage",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (_activeLanguageProperty == null) return null;
            }
            object active = _activeLanguageProperty.GetValue(null);
            if (active == null) return null;

            if (_languageDictField == null)
            {
                _languageDictField = active.GetType().GetField(
                    "dictionary",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            if (_languageDictField == null) return null;
            return _languageDictField.GetValue(active) as System.Collections.Generic.Dictionary<string, string>;
        }

        public static string Try(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback;

            // Query the language dictionary directly to avoid Translate() side-effect (Log.Error on missing key).
            var dict = GetLanguageDict();
            if (dict != null && dict.TryGetValue(key, out string text) && !string.IsNullOrEmpty(text))
                return text;

            // Key not found or dictionary inaccessible - return fallback without calling Translate().
            return fallback;
        }
    }

    /// <summary>
    /// Backwards-compatible alias used by the canonical-layer files
    /// (MaterialIdentity, SettingIdentity, RoomRoleResolver). Re-exported
    /// as a public extension to keep existing call sites working without
    /// churn. New code in 02/03/04/05 should prefer
    /// <see cref="RimconemyKeyed.Try"/>.
    /// </summary>
    public static class KeyedFallbackExtensions
    {
        public static string TranslateOrFallback(this string key, string fallback)
        {
            return RimconemyKeyed.Try(key, fallback);
        }
    }
}
