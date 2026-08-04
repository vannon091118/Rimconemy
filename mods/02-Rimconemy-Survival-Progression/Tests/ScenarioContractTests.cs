using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Static contract checks for scenario XML. These checks run in RimWorld's
    /// bootstrap because the repository has no separate test runner.
    /// </summary>
    public static class ScenarioContractTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
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
    }
}
