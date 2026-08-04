using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.Foundation.Canonical
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    ///
    /// VC-3 / Phase VC — Canonical Need/Setting Translation Layer.
    ///
    /// Vanilla has three core needs every survival game uses: Food, Rest,
    /// (Social/Recreation). Rimconemy MUST NOT register <c>Rimconemy_Need_Food</c>
    /// as an extra need hanging off each pawn — that doubles Mood conflict
    /// rate and breaks DLC compatibility (Anomaly/Royalty). The right model
    /// is: read vanilla needs, expose them via Rimconemy's *Setting
    /// Identity* vocabulary.
    ///
    /// Anti-pattern (decommissioned 2026-08-04): Setting-NeedDefs attached
    /// to pawns. Migration evidence: <see cref="NeedMapping"/> in Paket 02
    /// honoriert Spike API-NEED-01 (Setting-NeedDefs un-attached).
    /// </summary>
    public enum SettingIdentity
    {
        /// <summary>No Setting identity — explicit neutral default for callers.</summary>
        None = 0,

        /// <summary>Food / Hunger / Hunger-rate-of-change (Rimconemy: Nahrung).</summary>
        Food = 1,

        /// <summary>Rest + Composite Health (Rimconemy: Sicherheit).</summary>
        Safety = 2,

        /// <summary>Social / Recreation / Joy (Rimconemy: Sozial).</summary>
        Social = 3,
    }

    /// <summary>
    /// Canonical translator between Vanilla <see cref="NeedDef"/> (or
    /// <see cref="Need"/>) and a Rimconemy <see cref="SettingIdentity"/>.
    /// Read-only — does not mutate pawn state, does not attach needdefs.
    /// Pure mapping + display, two decimals deep.
    ///
    /// Owner: Foundation (canonical projection). Paket 02 owns the runtime
    /// sampler (NeedMappingService) — this layer is the display side.
    /// </summary>
    public static class NeedSettingsTranslator
    {
        /// <summary>
        /// Translate a Vanilla <see cref="NeedDef"/> to its Setting identity.
        /// Returns <see cref="SettingIdentity.None"/> for unrelated Vanilla
        /// needs so callers can decide what their neutral default is.
        /// </summary>
        public static SettingIdentity Translate(NeedDef def)
        {
            if (def == null) return SettingIdentity.None;
            if (def == NeedDefOf.Food) return SettingIdentity.Food;
            if (def == NeedDefOf.Rest) return SettingIdentity.Safety;

            // RimWorld 1.6 prefers "Recreation" but legacy Core uses "Joy".
            string name = def.defName;
            if (string.Equals(name, "Recreation", StringComparison.OrdinalIgnoreCase)) return SettingIdentity.Social;
            if (string.Equals(name, "Joy", StringComparison.OrdinalIgnoreCase)) return SettingIdentity.Social;

            return SettingIdentity.None;
        }

        /// <summary>Shortcut — translate a live <see cref="Need"/> via its def.</summary>
        public static SettingIdentity Translate(Need need)
        {
            if (need == null) return SettingIdentity.None;
            return Translate(need.def);
        }

        /// <summary>
        /// German-friendly label for a Setting identity. Translation lookup
        /// is keyed first; fallbacks are inline so missing XML degrades
        /// gracefully.
        /// </summary>
        public static string Label(SettingIdentity id)
        {
            switch (id)
            {
                case SettingIdentity.Food:    return "Rimconemy.Need.Food".TranslateOrFallback("Nahrung");
                case SettingIdentity.Safety:  return "Rimconemy.Need.Safety".TranslateOrFallback("Sicherheit");
                case SettingIdentity.Social:  return "Rimconemy.Need.Social".TranslateOrFallback("Sozial");
                default:                      return "Rimconemy.Need.None".TranslateOrFallback("—");
            }
        }

        /// <summary>
        /// Display color for a percentage within a Setting identity's range.
        /// Thresholds: 0..0.35 → Error, 0.35..0.65 → Warn, 0.65..1.0 → Success.
        /// Centralised here so every dashboard (Paket 02/03/04/05) emits
        /// the same color line.
        /// </summary>
        public static Color DisplayColor(SettingIdentity id, float percent, Color successColor, Color warnColor, Color errorColor)
        {
            float p = Mathf.Clamp01(percent);
            if (p <= 0.35f) return errorColor;
            if (p <= 0.65f) return warnColor;
            return successColor;
        }

        /// <summary>Helper: True if the setting is in CRITICAL range — UI may flash.</summary>
        public static bool IsCritical(SettingIdentity id, float percent)
        {
            float p = Mathf.Clamp01(percent);
            return p <= 0.20f && id != SettingIdentity.None;
        }
    }
}
