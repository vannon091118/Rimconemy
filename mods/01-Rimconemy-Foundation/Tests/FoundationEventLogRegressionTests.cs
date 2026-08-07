using System;
using System.Collections.Generic;
using System.Reflection;
using Rimconemy.Foundation.Models;
using Rimconemy.Foundation.Save;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>Regression tests for the escape-aware Foundation event envelope.</summary>
    public static class FoundationEventLogRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("Foundation", "EventLog regression tests");

            _passed = 0;
            _failed = 0;

            TestRoundtripPipeBackslashAndBackslashP();
            TestRoundtripTrailingBackslash();

            string summary = "[Rimconemy.Foundation] EventLog regression tests: "
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

        private static void TestRoundtripPipeBackslashAndBackslashP()
        {
            AssertRoundtrip(
                category: "Save|Category",
                eventType: "Path",
                source: @"rimconemy\p|foundation",
                message: @"C:\path\to\thing|with\p-marker",
                detail: "literal|pipes\\and\\backslashes",
                label: "EventLog: pipe/backslash/backslash-p roundtrip");
        }

        private static void TestRoundtripTrailingBackslash()
        {
            AssertRoundtrip(
                category: "Detail",
                eventType: "Trailing",
                source: "source",
                message: "ends-with-backslash\\",
                detail: "",
                label: "EventLog: trailing backslash roundtrip");
        }

        private static void AssertRoundtrip(string category, string eventType, string source,
            string message, string detail, string label)
        {
            var serialize = typeof(FoundationSaveData).GetMethod(
                "SerializeEvent", BindingFlags.NonPublic | BindingFlags.Static);
            var deserialize = typeof(FoundationSaveData).GetMethod(
                "DeserializeEvent", BindingFlags.NonPublic | BindingFlags.Static);
            if (serialize == null || deserialize == null)
            {
                _failed++;
                Log.Error("[FoundationEventLogRegression] " + label + ": serializer methods not found");
                return;
            }

            var original = new EventRecord(7, 42, category, eventType, source, message, detail);
            string encoded = (string)serialize.Invoke(null, new object[] { original });
            var restored = (EventRecord)deserialize.Invoke(null, new object[] { encoded });

            AssertEqual(category, restored?.Category, label + ": category");
            AssertEqual(eventType, restored?.EventType, label + ": event type");
            AssertEqual(source, restored?.SourcePackageId, label + ": source");
            AssertEqual(message, restored?.Message, label + ": message");
            AssertEqual(detail, restored?.Detail, label + ": detail");
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[FoundationEventLogRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
