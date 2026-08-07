using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Static contract checks for scenario XML. These checks run in RimWorld's
    /// bootstrap because the repository has no separate test runner.
    /// </summary>
    public static class ScenarioContractTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("SurvivalProgression", "Scenario contract tests");

            _passed = 0;
            _failed = 0;

            // The loaded DefDatabase is the authoritative runtime contract.
            // This catches null playerFaction/surfaceLayer/parts after XML
            // inheritance and def loading, not just well-formed source text.
            AssertTrue(typeof(Scenarios.ScenPart_StartInSandbox) != null,
                "Sandbox scenario: custom ScenPart type is compiled");
            var sandboxPartDef = DefDatabase<RimWorld.ScenPartDef>
                .GetNamedSilentFail("Rimconemy_StartInSandbox");
            AssertTrue(sandboxPartDef != null,
                "Sandbox scenario: ScenPartDef is loaded");

            var loadedDef = DefDatabase<RimWorld.ScenarioDef>
                .GetNamedSilentFail("Rimconemy_SandboxScenario");
            AssertTrue(loadedDef != null,
                "Sandbox scenario: ScenarioDef is loaded");
            if (loadedDef != null)
            {
                AssertTrue(loadedDef.scenario != null,
                    "Sandbox scenario: scenario payload is loaded");
                if (loadedDef.scenario != null)
                {
                    object playerFaction = ReadScenarioMember(loadedDef.scenario, "playerFaction");
                    object surfaceLayer = ReadScenarioMember(loadedDef.scenario, "surfaceLayer");
                    object parts = ReadScenarioMember(loadedDef.scenario, "parts");
                    AssertTrue(playerFaction != null,
                        "Sandbox scenario: loaded playerFaction is non-null");
                    AssertTrue(surfaceLayer != null,
                        "Sandbox scenario: loaded surfaceLayer is non-null");
                    AssertTrue(parts != null,
                        "Sandbox scenario: loaded parts list is non-null");
                    object sandboxPart = FindSandboxPart(parts);
                    AssertTrue(sandboxPart != null,
                        "Sandbox scenario: loaded parts contain sandbox ScenPart");
                    if (sandboxPart != null)
                    {
                        AssertTrue(ReadMember(sandboxPart, "def") != null
                            || ReadMember(sandboxPart, "Def") != null,
                            "Sandbox scenario: loaded ScenPart has a non-null def");
                    }
                    // Deployed-mod safety net: even when source XML is absent
                    // (rsync excludes source trees), the live ScenPart instance
                    // in DefDatabase must still have both pawnCount AND
                    // pawnChoiceCount set > 0. See HasConfigureStartingPawnsWithChoiceAtRuntime.
                    AssertTrue(HasConfigureStartingPawnsWithChoiceAtRuntime(parts),
                        "Sandbox scenario (deployed-runtime guard): ScenPart_ConfigPage_ConfigureStartingPawns has both pawnCount > 0 AND pawnChoiceCount > 0");
                }
            }

            string sourcePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "Defs", "Scenarios", "Rimconemy_SandboxScenario.xml");
            if (File.Exists(sourcePath))
            {
                var scenario = XElement.Load(sourcePath).Element("ScenarioDef");
                AssertEqual("ScenarioBase", scenario?.Attribute("ParentName")?.Value,
                    "Sandbox scenario: inherits ScenarioBase");
                AssertTrue(scenario?.Element("scenario")?.Element("playerFaction") != null,
                    "Sandbox scenario: playerFaction is present");
                AssertTrue(scenario?.Element("scenario")?.Element("surfaceLayer") != null,
                    "Sandbox scenario: surfaceLayer is present");
                AssertTrue(scenario?.Element("scenario")?.Element("parts")?.Element("li")?.Element("def") != null,
                    "Sandbox scenario: custom part has a def");
                AssertTrue(HasConfigureStartingPawnsWithChoice(scenario),
                    "Sandbox scenario: ScenPart_ConfigPage_ConfigureStartingPawns has both pawnCount AND pawnChoiceCount>0");
            }
            else
            {
                // In a deployed mod, source files are intentionally excluded.
                // DefDatabase runtime validation is then the only applicable gate.
                Log.Message("[Rimconemy.SurvivalProgression] ScenarioContractTests: source XML unavailable in deployed mod; DefDatabase runtime gate remains authoritative.");
            }

            string summary = "[Rimconemy.SurvivalProgression] Scenario contract tests: "
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

        private static object ReadScenarioMember(object scenario, string name)
        {
            const BindingFlags flags = BindingFlags.Instance
                | BindingFlags.Public | BindingFlags.NonPublic;
            for (var type = scenario.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(scenario);
                var property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null) return property.GetValue(scenario, null);
            }
            return null;
        }

        private static object FindSandboxPart(object parts)
        {
            var enumerable = parts as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            foreach (object part in enumerable)
            {
                if (part is Scenarios.ScenPart_StartInSandbox)
                    return part;
            }
            return null;
        }

        private static object ReadMember(object instance, string name)
        {
            if (instance == null) return null;
            const BindingFlags flags = BindingFlags.Instance
                | BindingFlags.Public | BindingFlags.NonPublic;
            for (var type = instance.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(instance);
                var property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null) return property.GetValue(instance, null);
            }
            return null;
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.SurvivalProgression] ScenarioContractTests FAILED: " + label);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.SurvivalProgression] ScenarioContractTests FAILED: "
                    + label + ": expected " + expected + ", got " + actual);
            }
        }

        /// <summary>
        /// Returns true if the scenario contains a ScenPart_ConfigPage_ConfigureStartingPawns
        /// that has both <pawnCount> AND <pawnChoiceCount> set to positive values.
        /// This is the Phase 4 systematic-debugging regression guard: omitting
        /// either field defaults the candidate pool to 0 and triggers
        /// "Could not generate starting map because there is no any player faction base"
        /// at Verse.Game.InitNewGame. The guard converts that runtime NRE into a
        /// static-gate catch before the user sees it.
        /// </summary>
        private static bool HasConfigureStartingPawnsWithChoice(XElement scenario)
        {
            if (scenario == null) return false;
            foreach (var li in scenario.Element("scenario")?.Element("parts")?.Elements("li") ?? Array.Empty<XElement>())
            {
                if (li.Attribute("Class")?.Value != "ScenPart_ConfigPage_ConfigureStartingPawns")
                    continue;
                var pawnCount = li.Element("pawnCount");
                var pawnChoiceCount = li.Element("pawnChoiceCount");
                if (pawnCount == null || pawnChoiceCount == null) return false;
                int pc1, pc2;
                if (!int.TryParse(pawnCount.Value.Trim(), out pc1)) return false;
                if (!int.TryParse(pawnChoiceCount.Value.Trim(), out pc2)) return false;
                if (pc1 <= 0 || pc2 <= 0) return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deployed-mod runtime mirror of <see cref="HasConfigureStartingPawnsWithChoice"/>.
        /// Walks the runtime-loaded <c>List&lt;ScenPart&gt;</c> in <c>ScenarioDef.scenario.parts</c>,
        /// finds the <c>ScenPart_ConfigPage_ConfigureStartingPawns</c> instance (or a
        /// subclass of it - inheritance walk via <see cref="IsOrSubclassesConfigureStartingPawns"/>),
        /// and asserts that BOTH pawnCount and pawnChoiceCount are positive integers
        /// via reflection on the live Def. This guard catches the regression even
        /// in deployed mods where source XML is absent (File.Exists returns false).
        /// </summary>
        private static bool HasConfigureStartingPawnsWithChoiceAtRuntime(object parts)
        {
            var enumerable = parts as System.Collections.IEnumerable;
            if (enumerable == null) return false;
            foreach (var part in enumerable)
            {
                if (part == null) continue;
                if (!IsOrSubclassesConfigureStartingPawns(part.GetType())) continue;
                int pawnCount = ReadIntMember(part, "pawnCount");
                int pawnChoiceCount = ReadIntMember(part, "pawnChoiceCount");
                // ReadIntMember returns -1 on missing/incompatible members so a
                // scenario missing the property entirely yields a different signal
                // from one that explicitly set pawnCount=pawnChoiceCount=0.
                if (pawnCount <= 0 || pawnChoiceCount <= 0) return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the candidate Type is <c>ScenPart_ConfigPage_ConfigureStartingPawns</c>
        /// or ANY subclass of it. Uses <see cref="Type.IsAssignableFrom"/> which handles
        /// the full class hierarchy AND interface hierarchy in a single call. Robust against
        /// future Rimconemy subclassing or decorator patterns. Mod 02 already references
        /// RimWorld's Verse assembly so the vanilla type is reachable directly.
        /// </summary>
        private static bool IsOrSubclassesConfigureStartingPawns(Type t)
        {
            if (t == null) return false;
            return typeof(RimWorld.ScenPart_ConfigPage_ConfigureStartingPawns).IsAssignableFrom(t);
        }

        /// <summary>
        /// Reflection-based int reader for live ScenPart instances.
        /// Tries property-get-value-then-field-get-value; returns -1 on missing
        /// member OR on any conversion failure. Sentinel -1 means "property absent";
        /// 0 means "property present and explicitly set to 0" (semantically distinct).
        /// </summary>
        private static int ReadIntMember(object instance, string name)
        {
            if (instance == null) return -1;
            const BindingFlags flags = BindingFlags.Instance
                | BindingFlags.Public | BindingFlags.NonPublic;
            var t = instance.GetType();
            var prop = t.GetProperty(name, flags);
            if (prop != null && prop.PropertyType == typeof(int))
            {
                try { return (int)prop.GetValue(instance, null); }
                catch { return -1; /* property present but GetValue threw */ }
            }
            var field = t.GetField(name, flags);
            if (field != null && field.FieldType == typeof(int))
            {
                try { return (int)field.GetValue(instance); }
                catch { return -1; }
            }
            return -1; // neither property nor field found
        }
    }
}
