using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rimconemy.Foundation;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    /// Phase F / slop-audit-fix (2026-08-04): regression-suite for <see cref="TimeConstants"/>.
    ///
    /// Verifies three classes of invariant:
    ///
    /// 1. **Value invariants** — the three constants are exactly the
    ///    RimWorld-canonical tick rates (60_000 day / 2_500 hour / 60
    ///    real-second). A typo at the binding site trips this group.
    ///
    /// 2. **Cross-constant invariants** — <c>TicksPerHour × 24 == TicksPerDay</c>
    ///    and <c>TicksPerRealSecond × 1000 == TicksPerDay</c>. Catches
    ///    any future tweak that breaks the time arithmetic without
    ///    re-aligning the dependencies.
    ///
    /// 3. **Drift-guard (IL scan)** — every loaded Rimconemy.* assembly
    ///    (Mod 01-05) is scanned for the IEEE-754 LE encoding of
    ///    60000f. The literal may appear in exactly one place: the
    ///    static field initializer of <see cref="TimeConstants"/>.
    ///    Any other occurrence — caller, helper, copy-pasted constant —
    ///    trips the test. This is what makes "all call sites route
    ///    through the constant" a mechanical invariant rather than a
    ///    policy document.
    ///
    /// The drift-guard relies on TimeConstants being declared as
    /// <c>static readonly</c> rather than <c>const</c>; see the
    /// TimeConstants class doc for the rationale.
    /// </summary>
    public static class FoundationTimeConstantsRegressionTests
    {
        private static readonly float TicksPerDayExpected = 60000f;
        private static readonly float TicksPerHourExpected = 2500f;
        private static readonly float TicksPerRealSecondExpected = 60f;

        private const string TimeConstantsFullName = "Rimconemy.Foundation.TimeConstants";

        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;
            _failures.Clear();

            // (1) Value invariants.
            AssertFloatEquals(TicksPerDayExpected, TimeConstants.TicksPerDay,
                "Value: TimeConstants.TicksPerDay == 60000f");
            AssertFloatEquals(TicksPerHourExpected, TimeConstants.TicksPerHour,
                "Value: TimeConstants.TicksPerHour == 2500f");
            AssertFloatEquals(TicksPerRealSecondExpected, TimeConstants.TicksPerRealSecond,
                "Value: TimeConstants.TicksPerRealSecond == 60f");

            // (2) Cross-constant invariants.
            AssertFloatEquals(TicksPerDayExpected, TimeConstants.TicksPerHour * 24f,
                "Invariant: TicksPerHour × 24 == TicksPerDay");
            AssertFloatEquals(TicksPerDayExpected, TimeConstants.TicksPerRealSecond * 1000f,
                "Invariant: TicksPerRealSecond × 1000 == TicksPerDay");

            // (3) Drift-guard: IL scan.
            AssertNoForbiddenTickLiterals();

            string summary = "[Rimconemy.Foundation] TimeConstants regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                foreach (var f in _failures)
                    Log.Error("[Rimconemy.Foundation] TEST FAILED: " + f);
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        // ── helpers ─────────────────────────────────────────────

        private static void AssertFloatEquals(float expected, float actual, string label)
        {
            if (expected == actual)
            {
                _passed++;
            }
            else
            {
                _failed++;
                _failures.Add(label + ": expected " + expected + ", got " + actual);
            }
        }

        /// <summary>
        /// Walks every loaded Rimconemy.* assembly, then every type, every
        /// declared method body, and every static-constructor (`.cctor`)
        /// body, flagging any occurrence of the IEEE-754 encoding of 60000f
        /// that lives outside <see cref="TimeConstants"/>. Inside
        /// TimeConstants we expect at least one occurrence — its own static
        /// initializer for <see cref="TimeConstants.TicksPerDay"/>; that is
        /// the canonical, authentic home of the literal.
        ///
        /// The scan uses <see cref="BitConverter.GetBytes(float)"/> and
        /// accepts either byte order, so it works on x86_64 LE and on any BE
        /// host. IL stores <c>ldc.r4</c> literals in the host's native order.
        ///
        /// Exemptions:
        /// - This test class and its siblings (anything inside a
        ///   <c>Rimconemy.&lt;X&gt;.Tests</c> namespace) carry the literal
        ///   for comparison purposes; they are the test harness, not the
        ///   production code under test.
        /// </summary>
        private static void AssertNoForbiddenTickLiterals()
        {
            byte[] needle = BitConverter.GetBytes(TicksPerDayExpected);
            byte[] reversed = new byte[needle.Length];
            for (int i = 0; i < needle.Length; i++)
            {
                reversed[i] = needle[needle.Length - 1 - i];
            }
            if (needle == null || needle.Length != 4)
            {
                _failed++;
                _failures.Add("BitConverter produced != 4 bytes for float32 60000f; cannot drift-scan.");
                return;
            }

            int foundInsideTimeConstants = 0;
            var rimconemyAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => (a.GetName().Name ?? string.Empty).StartsWith("Rimconemy.", StringComparison.Ordinal))
                .ToArray();

            foreach (var asm in rimconemyAssemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    if (IsTestHarnessType(type)) continue; // exempt the test harness itself

                    bool isTimeConstants = type.FullName == TimeConstantsFullName;

                    // (a) const fields — any const float with value 60000f anywhere
                    // is a hard regression, including a future "private const float Foo = 60000f".
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (!field.IsLiteral) continue;
                        if (field.FieldType != typeof(float)) continue;
                        var v = (float)field.GetRawConstantValue();
                        if (v == TicksPerDayExpected)
                        {
                            _failed++;
                            _failures.Add("Forbidden 60000f const field: " + type.FullName + "." + field.Name);
                        }
                    }

                    // (b) declared instance/static method bodies — the static initializer
                    // runs in the .cctor, which we scan separately below.
                    foreach (var method in type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Static | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly))
                    {
                        if (ScanMethodBodyForForbiddenLiteral(method, isTimeConstants, needle, reversed,
                                out bool matchedInsideTimeConstants))
                        {
                            // already recorded
                        }
                        else if (matchedInsideTimeConstants)
                        {
                            foundInsideTimeConstants++;
                        }
                    }

                    // (c) static constructor (.cctor) — `Type.GetMethods` excludes it,
                    // so we scan it explicitly. This is where the canonical literal
                    // for TimeConstants.TicksPerDay lives.
                    foreach (var ctor in type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Static | BindingFlags.Instance))
                    {
                        ScanCtorBodyForForbiddenLiteral(ctor, isTimeConstants, needle, reversed,
                            out bool matchedInsideTimeConstants);
                        if (matchedInsideTimeConstants) foundInsideTimeConstants++;
                    }
                }
            }

            if (foundInsideTimeConstants < 1)
            {
                _failed++;
                _failures.Add(
                    "Expected >= 1 IL occurrence of 60000f inside TimeConstants (the static-field " +
                    "initialiser), found " + foundInsideTimeConstants + ". Did TimeConstants go away?");
            }
            else
            {
                _passed++;
            }
        }

        /// <summary>True for any type whose namespace ends in <c>.Tests</c> — exempt from drift-scan.</summary>
        private static bool IsTestHarnessType(Type type)
        {
            string ns = type.Namespace ?? string.Empty;
            return ns.EndsWith(".Tests", StringComparison.Ordinal)
                || ns == "Rimconemy.Foundation.Tests"; // defensive for edge namespaces
        }

        /// <summary>
        /// Scans a single method's IL bytes for the 60000f float encoding in
        /// either byte order. Records a failure on matches outside
        /// TimeConstants; counts matches inside.
        /// </summary>
        private static bool ScanMethodBodyForForbiddenLiteral(
            MethodBase method,
            bool isTimeConstants,
            byte[] needle,
            byte[] reversed,
            out bool matchedInsideTimeConstants)
        {
            matchedInsideTimeConstants = false;
            var body = method.GetMethodBody();
            if (body == null) return false;
            byte[] il = body.GetILAsByteArray();
            if (il == null || il.Length < 4) return false;

            for (int i = 0; i <= il.Length - 4; i++)
            {
                bool native =
                    il[i] == needle[0] && il[i + 1] == needle[1]
                    && il[i + 2] == needle[2] && il[i + 3] == needle[3];
                bool swapped =
                    il[i] == reversed[0] && il[i + 1] == reversed[1]
                    && il[i + 2] == reversed[2] && il[i + 3] == reversed[3];
                if (!native && !swapped) continue;

                if (isTimeConstants)
                {
                    matchedInsideTimeConstants = true;
                    return false; // recorded by counter at caller
                }
                _failed++;
                _failures.Add("Forbidden 60000f IL literal: " + method.DeclaringType?.FullName
                    + "." + method.Name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Same scan as <see cref="ScanMethodBodyForForbiddenLiteral"/>,
        /// but for a static constructor's <see cref="MethodBase"/> (the
        /// .cctor). Both <c>MethodInfo</c> and <c>ConstructorInfo</c>
        /// derive from <see cref="MethodBase"/>, so we keep the helper
        /// signature uniform.
        /// </summary>
        private static bool ScanCtorBodyForForbiddenLiteral(
            MethodBase ctor,
            bool isTimeConstants,
            byte[] needle,
            byte[] reversed,
            out bool matchedInsideTimeConstants)
        {
            matchedInsideTimeConstants = false;
            var body = ctor.GetMethodBody();
            if (body == null) return false;
            byte[] il = body.GetILAsByteArray();
            if (il == null || il.Length < 4) return false;

            for (int i = 0; i <= il.Length - 4; i++)
            {
                bool native =
                    il[i] == needle[0] && il[i + 1] == needle[1]
                    && il[i + 2] == needle[2] && il[i + 3] == needle[3];
                bool swapped =
                    il[i] == reversed[0] && il[i + 1] == reversed[1]
                    && il[i + 2] == reversed[2] && il[i + 3] == reversed[3];
                if (!native && !swapped) continue;

                if (isTimeConstants)
                {
                    matchedInsideTimeConstants = true;
                    return false;
                }
                _failed++;
                _failures.Add("Forbidden 60000f IL literal in .cctor: "
                    + ctor.DeclaringType?.FullName);
                return true;
            }
            return false;
        }
    }
}
