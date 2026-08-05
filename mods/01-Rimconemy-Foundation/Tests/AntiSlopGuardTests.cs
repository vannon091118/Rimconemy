using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Paket 01).
    /// Anti-Slop-Guard — scans ALL Rimconemy assemblies via Cecil IL-Walker
    /// für anti-patterns die LLMs nutzen um Tests nutzlos zu machen.
    ///
    /// DETEKTIERTE PATTERNS (IL-Ebene):
    ///   1. Empty catch handler (leerer Catch-Block im IL)
    ///      → Test verschluckt Exceptions und meldet Erfolg
    ///   2. Catch-Block der nur return true macht (ohne Assertion davor)
    ///      → Tautologie-Test der immer grün ist
    ///   3. "asserted-as-pass" / "test swallowed" im Kommentar
    ///      → Exception wird als PASS getarnt
    ///
    /// SCAN-METHODE: Cecil liest die deployeten DLLs und prüft
    /// jeden catch-Handler auf Leere oder reines return true.
    /// Funktioniert in deployed UND dev context.
    ///
    /// SKIP-COUNTER (hart, kein Flag):
    ///   - Jeder Fund ist 1 Violation — 1x erlaubt (Warnung)
    ///   - Beim 2x: HARTER BLOCK — "ROOTCAUSE FIX erforderlich"
    ///   - Keine permanenten Flags, kein Reset — der Test
    ///     scheitert bis der Code gefixt ist.
    /// </summary>
    public static class AntiSlopGuardTests
    {
        private const int MaxViolationsBeforeBlock = 1;

        private static int _violationCount;
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();
        private static readonly HashSet<string> _reportedViolations = new HashSet<string>();

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;
            _violationCount = 0;
            _failures.Clear();
            _reportedViolations.Clear();

            try
            {
                var assemblies = FindRimconemyAssemblies();
                if (assemblies.Count == 0)
                {
                    Log.Warning("[Rimconemy.Foundation] AntiSlopGuard: no Rimconemy assemblies found to scan.");
                    return true;
                }

                foreach (var asmPath in assemblies)
                {
                    ScanAssembly(asmPath);
                }

                string summary = "[Rimconemy.Foundation] AntiSlopGuard: "
                    + _passed + " assemblies clean, "
                    + _failed + " with violations, "
                    + _violationCount + " total violations.";

                if (_failed > 0)
                {
                    foreach (var f in _failures)
                        Log.Error("[Rimconemy.Foundation] AntiSlopGuard FAIL: " + f);
                    Log.Error(summary);
                    return false;
                }

                if (_violationCount > MaxViolationsBeforeBlock)
                {
                    Log.Error("[Rimconemy.Foundation] AntiSlopGuard HARD BLOCK: "
                        + _violationCount + " violations exceeded limit of " + MaxViolationsBeforeBlock
                        + ". ROOTCAUSE FIX erforderlich — keine weiteren Skips erlaubt!");
                    Log.Error(summary);
                    return false;
                }

                Log.Message(summary);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[Rimconemy.Foundation] AntiSlopGuard crashed: "
                    + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static void ScanAssembly(string asmPath)
        {
            string asmName = Path.GetFileName(asmPath);
            bool asmClean = true;

            try
            {
                using (var asmDef = AssemblyDefinition.ReadAssembly(asmPath))
                {
                    foreach (var type in asmDef.MainModule.Types)
                    {
                        ScanType(type, asmName, ref asmClean);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation] AntiSlopGuard: could not read " + asmName + ": " + ex.Message);
                return;
            }

            if (asmClean)
            {
                _passed++;
                Log.Message("[Rimconemy.Foundation] AntiSlopGuard: " + asmName + " — CLEAN");
            }
        }

        // Types whose own bodies contain anti-slop test strings that would
        // cause false positives (the guard is the guard's own test fixture).
        private static readonly HashSet<string> _exemptTypes = new HashSet<string>
        {
            "Rimconemy.Foundation.Tests.AntiSlopGuardTests",
        };

        private static void ScanType(TypeDefinition type, string asmName, ref bool asmClean)
        {
            if (type == null) return;

            // Never scan the guard itself — its test strings are the
            // detection targets, not violations.
            if (_exemptTypes.Contains(type.FullName)) return;

            // Only scan test classes: types ending with "Tests" or in a "Tests" namespace.
            // Production code is NOT scanned — the guard watches for test-slop only.
            bool isTestClass = type.FullName.EndsWith("Tests")
                || type.FullName.Contains(".Tests.")
                || type.FullName.Contains("Tests_");

            if (isTestClass)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    ScanMethod(method, type.FullName, asmName, ref asmClean);
                }
            }

            // Recurse into nested types (inherit test scope)
            foreach (var nested in type.NestedTypes)
            {
                ScanType(nested, asmName, ref asmClean);
            }
        }

        private static void ScanMethod(MethodDefinition method, string typeName, string asmName, ref bool asmClean)
        {
            var body = method.Body;
            if (body == null || body.Instructions == null) return;

            // Pattern 1: Empty catch handler — IL has a catch handler with no instructions
            // or only a rethrow. We check ExceptionHandlers for handlers with empty/short bodies.
            if (body.HasExceptionHandlers)
            {
                foreach (var handler in body.ExceptionHandlers)
                {
                    if (handler.HandlerType != ExceptionHandlerType.Catch) continue;

                    // Find the range of instructions in this catch handler
                    var handlerStart = handler.HandlerStart;
                    // HandlerEnd is null when handler extends to end of method
                    // (Cecil convention). CountInstructionsInRange handles null
                    // correctly via `current != null` — no fallback needed.
                    var handlerEnd = handler.HandlerEnd;

                    if (handlerStart == null) continue;

                    int instrCount = CountInstructionsInRange(body, handlerStart, handlerEnd);

                    // Empty catch: 0 instructions (just the handler boundary)
                    // or only a rethrow (1 instruction)
                    if (instrCount <= 1)
                    {
                        string key = typeName + "::" + method.Name + ":EMPTY_CATCH@" + handlerStart.Offset;
                        if (_reportedViolations.Add(key))
                        {
                            RecordViolation(asmName, typeName, method.Name, "EMPTY_CATCH",
                                "Leerer catch-Handler (" + instrCount + " instructions) — Exception verschluckt");
                            asmClean = false;
                        }
                    }

                    // Pattern 2: Catch that only returns true (tautology)
                    // Check if the handler body is: ldarg.0 + ret (return true) or just ret true
                    if (instrCount >= 1 && instrCount <= 4)
                    {
                        bool isReturnTrue = CheckCatchReturnsTrue(body, handlerStart, handlerEnd);
                        if (isReturnTrue)
                        {
                            string key = typeName + "::" + method.Name + ":CATCH_RETURN_TRUE@" + handlerStart.Offset;
                            if (_reportedViolations.Add(key))
                            {
                                RecordViolation(asmName, typeName, method.Name, "CATCH_RETURN_TRUE",
                                    "catch-Handler gibt nur 'return true' zurück — Tautologie-Test");
                                asmClean = false;
                            }
                        }
                    }
                }
            }

            // Pattern 3: Check IL for ldstr "asserted-as-pass" or "swallowed"
            foreach (var instr in body.Instructions)
            {
                if (instr.OpCode != OpCodes.Ldstr) continue;
                if (instr.Operand is string str)
                {
                    string lower = str.ToLowerInvariant();
                    if (lower.Contains("asserted-as-pass") || lower.Contains("test swallowed")
                        || lower.Contains("swallowed exception"))
                    {
                        string key = typeName + "::" + method.Name + ":ASSERTED_AS_PASS@" + instr.Offset;
                        if (_reportedViolations.Add(key))
                        {
                            RecordViolation(asmName, typeName, method.Name, "ASSERTED_AS_PASS",
                                "String-Literal '" + str + "' — Exception wird als PASS getarnt");
                            asmClean = false;
                        }
                    }
                }
            }

            // Pattern 4: Detect --skip / --noverify flag abuse via ldstr
            foreach (var instr in body.Instructions)
            {
                if (instr.OpCode != OpCodes.Ldstr) continue;
                if (instr.Operand is string flag)
                {
                    string lower = flag.ToLowerInvariant();
                    if (lower.Contains("--skip") || lower.Contains("--noverify")
                        || lower.Contains("skiptest") || lower.Contains("skip_test"))
                    {
                        string key = typeName + "::" + method.Name + ":SKIP_FLAG@" + instr.Offset;
                        if (_reportedViolations.Add(key))
                        {
                            RecordViolation(asmName, typeName, method.Name, "SKIP_FLAG",
                                "LLM Skip/Noverify Flag erkannt in String-Literal '" + flag + "'");
                            asmClean = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a catch handler body is a simple "return true" pattern.
        /// </summary>
        private static bool CheckCatchReturnsTrue(Mono.Cecil.Cil.MethodBody body, Instruction start, Instruction end)
        {
            if (start == null) return false;

            var instrs = new List<Instruction>();
            var current = start;
            while (current != null && current != end)
            {
                instrs.Add(current);
                current = current.Next;
            }

            if (instrs.Count == 0) return false;

            // Find the last non-nop/branch instruction
            Instruction lastMeaningful = null;
            for (int i = instrs.Count - 1; i >= 0; i--)
            {
                var op = instrs[i].OpCode;
                if (op != OpCodes.Nop && op != OpCodes.Br_S && op != OpCodes.Br)
                {
                    lastMeaningful = instrs[i];
                    break;
                }
            }
            if (lastMeaningful == null) return false;
            if (lastMeaningful.OpCode != OpCodes.Ret) return false;

            // Find the instruction before ret (skipping nops/branches)
            Instruction beforeRet = null;
            for (int i = instrs.Count - 2; i >= 0; i--)
            {
                var op = instrs[i].OpCode;
                if (op != OpCodes.Nop && op != OpCodes.Br_S && op != OpCodes.Br)
                {
                    beforeRet = instrs[i];
                    break;
                }
            }
            if (beforeRet == null) return false;

            // Check for all C# compiler variants of "ldc.i4 1" (true):
            //   ldc.i4.1  (compact)
            //   ldc.i4.m1 (compact, -1, truthy in C#)
            //   ldc.i4 1  (wide form with int operand)
            //   ldc.i4.s 1 (byte form)
            if (beforeRet.OpCode == OpCodes.Ldc_I4_1 || beforeRet.OpCode == OpCodes.Ldc_I4_M1)
                return true;
            if (beforeRet.OpCode == OpCodes.Ldc_I4 && beforeRet.Operand is int val && val != 0)
                return true;
            if (beforeRet.OpCode == OpCodes.Ldc_I4_S && beforeRet.Operand is sbyte sval && sval != 0)
                return true;

            return false;
        }

        private static int CountInstructionsInRange(Mono.Cecil.Cil.MethodBody body, Instruction start, Instruction end)
        {
            int count = 0;
            var current = start;
            while (current != null && current != end)
            {
                count++;
                current = current.Next;
            }
            return count;
        }

        private static void RecordViolation(string asmName, string typeName, string methodName,
            string patternId, string description)
        {
            _violationCount++;
            string violation = asmName + " " + typeName + "::" + methodName
                + " [" + patternId + "] " + description;

            if (_violationCount <= MaxViolationsBeforeBlock)
            {
                Log.Warning("[Rimconemy.Foundation] AntiSlopGuard WARNING " + _violationCount
                    + "/" + MaxViolationsBeforeBlock + ": " + violation);
            }
            else
            {
                _failures.Add("HARD BLOCK (violation " + _violationCount + "): " + violation
                    + "\n>>> ROOTCAUSE FIX ERFORDERLICH — keine weiteren Skips erlaubt! <<<");
                _failed++;
            }
        }

        private static List<string> FindRimconemyAssemblies()
        {
            var result = new List<string>();

            // Strategy 1: scan loaded assemblies for Rimconemy DLLs
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                string location = asm.Location;
                if (string.IsNullOrEmpty(location)) continue;
                if (Path.GetFileName(location).StartsWith("Rimconemy."))
                    result.Add(location);
            }

            if (result.Count > 0) return result;

            // Strategy 2: find Assemblies/ folders in the Mods directory
            string gameDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // Walk up from Assemblies/ to the mod folder, then to Mods/
            for (int i = 0; i < 6; i++)
            {
                string modsDir = Path.Combine(gameDir, "Mods");
                if (Directory.Exists(modsDir))
                {
                    foreach (var modDir in Directory.GetDirectories(modsDir))
                    {
                        string assemDir = Path.Combine(modDir, "Assemblies");
                        if (Directory.Exists(assemDir))
                        {
                            result.AddRange(Directory.GetFiles(assemDir, "Rimconemy.*.dll"));
                        }
                    }
                    break;
                }
                gameDir = Path.GetDirectoryName(gameDir);
                if (gameDir == null) break;
            }

            return result;
        }
    }
}
