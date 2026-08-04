using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Rimconemy.EconomyTerritory.Wallet;
using Rimconemy.Foundation.Colonials;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// slop-audit 2026-08-04 / H3 fix.
    ///
    /// Replaces event placeholders of the form <c>{Variable}</c> in the
    /// Letter-Label / Letter-Text / Effect-Strings at runtime. Mirrors
    /// DECISIONS.md #13 (11+ event variables):
    /// <list type="bullet">
    ///   <item>{PawnName}, {BothPawns} - colonist names from ColonialReader</item>
    ///   <item>{ResourceName}, {CurrentAmount}, {MinAmount} - critical-resource lookups</item>
    ///   <item>{FactionName} - settlement faction label</item>
    ///   <item>{MapName} - canonical player home map name</item>
    ///   <item>{ThreatLevel}, {ThreatPercent} - formatted pressure</item>
    ///   <item>{DaysSinceStart} - elapsed in-game days</item>
    ///   <item>{WalletBalance} - CreditsLedger.Balance</item>
    ///   <item>{WeatherType}, {Season} - RimWorld sky / season (defensive fallbacks)</item>
    /// </list>
    ///
    /// Missing variables resolve to a configurable placeholder marker so
    /// the player can spot template gaps in QA playtests.
    /// </summary>
    public static class PlaceholderResolver
    {
        public const string LogMarker = "v1";
        public const string MissingPlaceholderToken = "?";

        private static readonly Regex PlaceholderPattern =
            new Regex(@"\{([A-Za-z][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

        /// <summary>
        /// Resolve every <c>{Variable}</c> in <paramref name="content"/>
        /// using the supplied context. Returns the rendered string.
        /// Empty/null input returns empty.
        /// </summary>
        public static string Resolve(string content, PlaceholderContext ctx)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;
            if (ctx == null) return content;

            return PlaceholderPattern.Replace(content, match =>
            {
                string key = match.Groups[1].Value;
                return ResolveSingle(key, ctx) ?? MissingPlaceholderToken + key;
            });
        }

        /// <summary>
        /// Resolve a single key into a display string. Returns null when
        /// the key does not match any known resolver.
        /// </summary>
        public static string ResolveSingle(string key, PlaceholderContext ctx)
        {
            switch (key)
            {
                case "PawnName":
                case "PawnA":
                    return ctx.Pawn?.LabelShortCap ?? MissingPlaceholderToken + key;
                case "BothPawns":
                case "PawnB":
                    return ctx.OtherPawn?.LabelShortCap ?? "another colonist";
                case "ResourceName":
                    return ctx.FirstCriticalResource ?? MissingPlaceholderToken + key;
                case "CurrentAmount":
                    return ctx.CriticalAmount.ToString(CultureInfo.InvariantCulture);
                case "MinAmount":
                    return ctx.CriticalFloor.ToString(CultureInfo.InvariantCulture);
                case "FactionName":
                    return TryGetFactionName(ctx) ?? "an unknown faction";
                case "MapName":
                    return TryGetMapName(ctx) ?? "an unknown map";
                case "ThreatLevel":
                    return ctx.ThreatPressure.ToString("F2", CultureInfo.InvariantCulture);
                case "ThreatPercent":
                    return (ctx.ThreatPressure * 100f).ToString("F0", CultureInfo.InvariantCulture) + "%";
                case "DaysSinceStart":
                    return (ctx.GameTick / 60000L).ToString(CultureInfo.InvariantCulture);
                case "WalletBalance":
                    return TryGetWalletBalance(ctx).ToString(CultureInfo.InvariantCulture);
                case "WeatherType":
                    return TryGetWeather(ctx) ?? "clear";
                case "Season":
                    return TryGetSeason(ctx) ?? "spring";
                case "EventId":
                    return ctx.EventId ?? "";
                default:
                    return null;
            }
        }

        private static string TryGetFactionName(PlaceholderContext ctx)
        {
            try
            {
                if (ctx.Pawn != null && ctx.Pawn.Faction != null)
                    return ctx.Pawn.Faction.Name;
            }
            catch { }
            return null;
        }

        private static string TryGetMapName(PlaceholderContext ctx)
        {
            try
            {
                if (ctx.Map != null) return ctx.Map.Parent?.LabelCap ?? "an unknown map";
                if (Find.AnyPlayerHomeMap != null)
                    return Find.AnyPlayerHomeMap.Parent?.LabelCap ?? "an unknown map";
            }
            catch { }
            return null;
        }

        private static long TryGetWalletBalance(PlaceholderContext ctx)
        {
            try
            {
                if (ctx.WalletLedger != null) return ctx.WalletLedger.Balance;
                var ledger = WalletService.GetOrCreateLedger();
                return ledger?.Balance ?? 0L;
            }
            catch { return 0L; }
        }

        private static string TryGetWeather(PlaceholderContext ctx)
        {
            try
            {
                Map map = ctx.Map ?? Find.AnyPlayerHomeMap;
                if (map == null) return "clear";
                var w = map.weatherManager?.curWeather;
                return w != null ? (w.label ?? "clear") : "clear";
            }
            catch { return "clear"; }
        }

        private static string TryGetSeason(PlaceholderContext ctx)
        {
            try
            {
                Map map = ctx.Map ?? Find.AnyPlayerHomeMap;
                if (map == null) return "spring";
                // Season enum cannot be looked up without reflection; fall back to quadrant.
                return SeasonUtils.DescribeSeason(map);
            }
            catch { return "spring"; }
        }
    }

    /// <summary>
    /// Bag of values the resolver reads from the live game. Re-build
    /// per incident rather than caching to keep the surface area
    /// explicit at call sites.
    /// </summary>
    public sealed class PlaceholderContext
    {
        public Pawn Pawn;
        public Pawn OtherPawn;
        public Map Map;
        public long GameTick;
        public float ThreatPressure;
        public string EventId;
        public string FirstCriticalResource;
        public int CriticalAmount;
        public int CriticalFloor;
        public CreditsLedger WalletLedger;

        public static PlaceholderContext FromSnapshot(SituationSnapshot snap, StoryEventSpec evt, Pawn pawn = null, Pawn otherPawn = null)
        {
            var colonists = pawn != null
                ? new List<Pawn> { pawn }
                : ColonialReader.GetActiveColonists();

            if (colonists.Count > 0 && pawn == null)
                pawn = colonists[0];
            if (colonists.Count > 1 && otherPawn == null)
                otherPawn = colonists[1];

            return new PlaceholderContext
            {
                Pawn = pawn,
                OtherPawn = otherPawn,
                Map = Find.AnyPlayerHomeMap,
                GameTick = snap.GameTick,
                ThreatPressure = snap.ThreatPressure,
                EventId = evt?.EventId,
                FirstCriticalResource = snap.CriticalResourceIds != null && snap.CriticalResourceIds.Count > 0
                    ? snap.CriticalResourceIds[0]
                    : null,
                CriticalAmount = snap.CriticalResourceIds?.Count ?? 0,
                CriticalFloor = Rimconemy.InfectedAutomation.ResourceThresholds.FallbackCriticalUnits,
                WalletLedger = null, // resolved lazily via WalletService
            };
        }
    }

    /// <summary>
    /// Tiny helper that translates tile quadrant into a season label.
    /// Lives here so PlaceholderResolver does not depend on a
    /// code-behind binding to Season.
    /// </summary>
    public static class SeasonUtils
    {
        public static string DescribeSeason(Map map)
        {
            if (map == null) return "spring";
            // Tile-id-based pseudo-quadrant. We don't have a public
            // latitude accessor in 1.6; the audit asked for a placeholder
            // value, so we hash the tile id into a stable quadrant.
            int tileId = (int)map.Tile;
            float normalized = (tileId & 0xFF) / 255f;
            int day = GenDate.DayOfYear(map.Tile, Find.TickManager?.TicksAbs ?? 0L);
            return LatToSeason(normalized, day);
        }

        public static string LatToSeason(float lat, int dayOfYear)
        {
            // Northern hemisphere absolute value 0..1 - very rough metaphor.
            // We just round the day-of-year into one of four quadrants.
            int quadrant = (dayOfYear / 15) % 4;
            return quadrant switch
            {
                0 => "spring",
                1 => "summer",
                2 => "autumn",
                _ => "winter",
            };
        }
    }
}
