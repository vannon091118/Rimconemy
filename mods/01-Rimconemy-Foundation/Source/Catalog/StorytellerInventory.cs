using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Catalog
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    /// Phase 5 / Vanilla-/DLC-Storyteller-Probe.
    ///
    /// Enumerates <see cref="DefDatabase{T}"/> of <see cref="StorytellerDef"/>
    /// at boot (after Defs are fully parsed) and writes a structured log
    /// summary. The probe deliberately stays off the spike-bound decision:
    ///   - we do NOT register a custom <see cref="StorytellerDef"/>;
    ///   - we only LIST the rival-ready defs + their <see cref="DifficultyDef"/>
    ///     applicability range, so Storyteller-Spike (DECISIONS §21) has a
    ///     one-stop source for `RimWorld_storyteller_cassandra`,
    ///     `Rimconemy_Storyteller`, etc.
    ///
    /// Vanilla-API surface used:
    ///   - <c>DefDatabase&lt;StorytellerDef&gt;.AllDefsListForReading</c> (1.5+)
    ///   - <c>StorytellerDef.defName</c>, <c>label</c>, <c>modContentPack.PackageId</c>
    /// Note: <c>storytellerComps</c> lives on the Storyteller runtime instance,
    /// not on StorytellerDef in 1.6 — comp inspection deferred to a future spike.
    ///
    /// SPIKE: API-FOUNDATION-STORYTELLER-01 — verified-compiling against local
    /// RimWorld 1.6.4566.
    /// </summary>
    public static class StorytellerInventory
    {
        private static readonly object _lock = new object();
        private static bool _populated;

        public struct StorytellerBucket
        {
            public string DefName;
            public string PackageId;
            public string Label;
        }

        private static readonly List<StorytellerBucket> _buckets
            = new List<StorytellerBucket>();

        public static IReadOnlyList<StorytellerBucket> Buckets
        {
            get { lock (_lock) return new List<StorytellerBucket>(_buckets); }
        }

        /// <summary>
        /// Captures exactly once. Safe to call multiple times.
        /// Returns true if a new capture ran this call.
        /// </summary>
        public static bool EnsureCaptured()
        {
            lock (_lock)
            {
                if (_populated) return false;
            }

            try
            {
                CaptureInternal();
                lock (_lock)
                {
                    _populated = _buckets.Count > 0;
                }
                return _buckets.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation] StorytellerInventory capture failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>Forces a re-capture (test-only).</summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _populated = false;
                _buckets.Clear();
            }
        }

        private static void CaptureInternal()
        {
            var defs = DefDatabase<StorytellerDef>.AllDefsListForReading;
            if (defs == null) return;

            foreach (var def in defs)
            {
                if (def == null) continue;
                var owner = def.modContentPack != null ? def.modContentPack.PackageId : "<none>";
                _buckets.Add(new StorytellerBucket
                {
                    DefName = def.defName,
                    PackageId = owner,
                    Label = def.label?.CapitalizeFirst() ?? def.defName,
                });
            }
        }

        /// <summary>Log a one-line summary at boot (count by package + vanilla casing).</summary>
        public static void LogBootstrapSummary()
        {
            var snap = Buckets;
            if (snap == null) return;
            int rimconemy = 0, vanilla = 0, dlcOrQuest = 0;
            for (int i = 0; i < snap.Count; i++)
            {
                var b = snap[i];
                if (b.PackageId != null && b.PackageId.StartsWith("rimconemy.")) rimconemy++;
                else if (b.PackageId != null
                    && (b.PackageId.Contains("Ideology") || b.PackageId.Contains("Biotech")
                        || b.PackageId.Contains("Anomaly") || b.PackageId.Contains("Royalty")
                        || b.PackageId.Contains("Odyssey")))
                    dlcOrQuest++;
                else vanilla++;
            }
            Log.Message("[Rimconemy.Foundation] StorytellerInventory: total="
                + snap.Count + ", Rimconemy=" + rimconemy
                + ", Vanilla=" + vanilla
                + ", DLC/Quest=" + dlcOrQuest
                + " (Phase-5 Probe; no custom StorytellerDef registered — DECISIONS §21).");
        }
    }
}
