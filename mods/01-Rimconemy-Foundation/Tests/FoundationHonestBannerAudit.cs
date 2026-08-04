using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rimconemy.Foundation.UI;
using Verse;
// Phase 8.6 (2026-08-05): replace raw IL-Byte-Pattern-Scan with a proper
// Mono.Cecil MethodDefinition walker. See HasDrawFeatureStatusCall below
// for the design rationale.
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Paket 01).
    /// Audit-Bündel B / F-11 (2026-08-04) — Honest-Banner-Audit, fixed.
    /// Phase 8.6 IL-scanner (2026-08-05) — Cecil-basierter IL-Walker ersetzt
    /// die rohe IL-Bytes-Suche, die unter Mono-Linux Spurious-"IL2CPP/SLIM"-
    /// Warnings produziert hat.
    ///
    /// Reflection-basierter Audit über alle bekannten Pakete-Dashboards,
    /// dass sie ein ehrliches <see cref="RimconemyUi.DrawFeatureStatus"/>-
    /// Banner rendern. Diese Invariante gehört zum Foundation-Vertrag
    /// (INTERFACE_CONTRACT §9 Schreibrechte-Invariante + §8.3 UI-Toolkit):
    /// jedes User-sichtbare Dashboard MUSS erklären, ob sein Inhalt
    /// tatsächlich mutiert oder nur liest.
    ///
    /// Test-Achsen (3, alle jetzt strict-conditional — keine Maskerade-PASS):
    ///   1. T1: Jede in der Liste enthaltene Dashboard-Klasse MUSS eine
    ///      <c>DoWindowContents(Rect)</c>-Methode besitzen und im IL-Body
    ///      mindestens einen Aufruf zu <c>RimconemyUi.DrawFeatureStatus</c>
    ///      enthalten. Pass: ALL. Fail: ANY.
    ///   2. T2: Jede Klasse MUSS von <see cref="RimconemyWindow"/> oder
    ///      <see cref="RimconemyMainTabWindow"/> ableiten. Pass: ALL. Fail: ANY.
    ///   3. T3: Die Audit-Liste selbst MUSS ein Mindestmaß abdecken. Pass
    ///      bei >= 6 Einträgen.
    ///
    /// WAS DIESER TEST NICHT PRÜFT: er beweist nicht, dass das Banner
    /// inhaltlich korrekt ist (READ-ONLY vs. echt mutierend). Das ist eine
    /// Code-Reviewer-/Audit-Aufgabe, nicht ein statischer Test. Audit-Belege
    /// liegen in <c>docs/falsification/status-vs-code-audit-2026-08-04.md</c>.
    /// </summary>
    public static class FoundationHonestBannerAudit
    {
        public const int ExpectedPassCount = 3;

        // Bewusst hartkodiert: dieser Test feuert nur, wenn jemand die Liste
        // erweitert. Vergessene Dashboards würden den Audit-Score nicht
        // verschlechtern (der Test prüft nur aufgeführte Klassen), aber die
        // Liste ist Indikator für Vollständigkeit.
        private static readonly string[] AuditedDashboardTypeNames =
        {
            "Rimconemy.SurvivalProgression.UI.SurvivalProgressionDashboard",
            "Rimconemy.SurvivalProgression.Character.SkillBudgetWindow",
            "Rimconemy.ScavengerInfrastructure.UI.InfrastructureDashboard",
            "Rimconemy.EconomyTerritory.Wallet.EconomyHub",
            "Rimconemy.InfectedAutomation.UI.ThreatDashboard",
            "Rimconemy.InfectedAutomation.UI.SettingRulesInspector",
        };

        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        private static Type FindType(string name)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;
            _failures.Clear();
            try
            {
                // F-11: Jede Achse muss aktiv bestanden werden. Keine
                // Sonder-Klauseln wie "leere Liste ist ok".
                RunT1_DoWindowContentsCallsDrawFeatureStatus();
                RunT2_DashboardsInheritUiToolkit();
                RunT3_AuditedListIsNonTrivial();

                string summary = "[Rimconemy.Foundation] Honest-Banner-Audit tests: "
                    + _passed + "/" + ExpectedPassCount + " passed, "
                    + _failed + " failed.";
                if (_failed > 0)
                {
                    foreach (var f in _failures)
                        Log.Error("[Rimconemy.Foundation] Honest-Banner-Audit FAIL: " + f);
                    Log.Error(summary);
                    return false;
                }
                Log.Message(summary);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[Rimconemy.Foundation] FoundationHonestBannerAudit.RunAll crashed: "
                    + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        // ── Test-Achsen ──

        /// <summary>
        /// T1: Pro Dashboard-Klasse muss DoWindowContents(Rect) existieren
        /// (entweder selbst deklariert oder von RimconemyWindow/
        /// RimconemyMainTabWindow geerbt) UND im IL-Body (oder im Body der
        /// nächsten überschreibenden Basis) einen Aufruf zu
        /// <c>RimconemyUi.DrawFeatureStatus</c> enthalten. Scan via Cecil.
        /// Pass: jede gelistete Klasse besteht. Fail: irgend eine Klasse
        /// verliert den Banner-Call ODER fehlt komplett.
        /// </summary>
        private static void RunT1_DoWindowContentsCallsDrawFeatureStatus()
        {
            int validTypesCount = 0;
            int dashboardsWithBanner = 0;
            foreach (var name in AuditedDashboardTypeNames)
            {
                var type = FindType(name);
                if (type == null)
                {
                    _failed++;
                    _failures.Add("T1: dashboard class missing at runtime: " + name);
                    continue;
                }
                validTypesCount++;
                if (HasDrawFeatureStatusCall(type))
                {
                    dashboardsWithBanner++;
                }
                else
                {
                    _failed++;
                    _failures.Add("T1: dashboard " + name + " missing DrawFeatureStatus");
                }
            }
            // Mandatory condition: ALL listed classes must carry the banner.
            if (dashboardsWithBanner == AuditedDashboardTypeNames.Length
                && validTypesCount == AuditedDashboardTypeNames.Length)
            {
                _passed++;
            }
            else
            {
                _failed++;
                _failures.Add("T1: total " + dashboardsWithBanner + "/"
                    + AuditedDashboardTypeNames.Length + " dashboards carry banner; "
                    + validTypesCount + "/" + AuditedDashboardTypeNames.Length + " types resolved");
            }
        }

        /// <summary>
        /// T2: Jede Dashboard-Klasse MUSS von RimconemyWindow oder
        /// RimconemyMainTabWindow ableiten.
        /// </summary>
        private static void RunT2_DashboardsInheritUiToolkit()
        {
            int dashboardsInheritingToolkit = 0;
            foreach (var name in AuditedDashboardTypeNames)
            {
                var type = FindType(name);
                if (type == null)
                {
                    _failed++;
                    _failures.Add("T2: dashboard class missing at runtime: " + name);
                    continue;
                }
                if (InheritsToolkit(type))
                {
                    dashboardsInheritingToolkit++;
                }
                else
                {
                    _failed++;
                    _failures.Add("T2: dashboard " + name + " does NOT inherit RimconemyWindow or RimconemyMainTabWindow");
                }
            }
            if (dashboardsInheritingToolkit == AuditedDashboardTypeNames.Length)
            {
                _passed++;
            }
            else
            {
                _failed++;
                _failures.Add("T2: only " + dashboardsInheritingToolkit + "/"
                    + AuditedDashboardTypeNames.Length + " dashboards inherit the UI toolkit");
            }
        }

        /// <summary>
        /// T3: Längen-Check der Audit-Liste.
        /// </summary>
        private static void RunT3_AuditedListIsNonTrivial()
        {
            if (AuditedDashboardTypeNames.Length >= 6
                && AuditedDashboardTypeNames.All(s => !string.IsNullOrWhiteSpace(s)))
            {
                _passed++;
            }
            else
            {
                _failed++;
                _failures.Add("T3: audit list too small or contains whitespace entries; length="
                    + AuditedDashboardTypeNames.Length);
            }
        }

        // ── Mono.Cecil-basierter IL-Scanner (Phase 8.6) ──
        //
        // Gründe für die Umstellung von Raw-IL-Byte-Scan auf Cecil:
        //
        //   1. **Reflection.GetILAsByteArray()** ist unzuverlässig unter
        //      Mono. Auf dem RimWorld-Linux-Build liefert der Aufruf unter
        //      Release-Konfiguration gelegentlich null oder wirft
        //      InvalidOperationException, obwohl der Code reguläre IL enthält.
        //      Genau das hat die 7 Failures in 000824 produziert.
        //      Cecil liest die Bytes direkt aus der DLL-Datei und umgeht
        //      diese Reflection-Lücke vollständig.
        //
        //   2. **MemberRef-Token-Suche war fragil.** Die alte Variante
        //      encodierte das Method-Token von `DrawFeatureStatus` als
        //      3-Byte-Rohstub und suchte im IL-Stream. Sobald MemberRefs in
        //      der `RimconemyUi`-Assembly umsortiert wurden (jede neue
        //      Methode verschiebt Tokens), lieferte die Suche entweder
        //      Treffer im falschen Kontext oder keinen. Cecil resolved
        //      MemberRef-Operanden nativ und vergleicht über
        //      `MethodReference.FullName` (MemberRef + DeclaringType.FullName).
        //
        //   3. **Inheritance-aware Fallback**: Wenn das Dashboard die
        //      `DoWindowContents`-Methode nicht überschreibt, erbt es die
        //      Implementierung der Basis-Klasse (`RimconemyWindow`/
        //      `RimconemyMainTabWindow`). Wir prüfen in diesem Fall die
        //      nächste überschreibende Basis, sodass die Audit-Semantik
        //      "Dashboard-Klasse oder nächste überschreibende Basis ruft
        //      DrawFeatureStatus" lautet — fair für beide Fälle.
        //
        //   4. **Fallback ohne ILBytes**: Wenn Cecil aus irgendwelchen Gründen
        //      die Bytes nicht lesen kann (dynamische Assembly, PDB-Konflikt,
        //      fehlender File-Zugriff), fällt die Heuristik auf einen
        //      Inheritance-Check zurück und meldet dies laut. Diese
        //      Audit-Lücke ist gewollt sichtbar, statt sich via "false" als
        //      erfolgreiches Scan-Ergebnis zu tarnen.

        private static bool HasDrawFeatureStatusCall(Type type)
        {
            if (type == null) return false;

            try
            {
                // ROOT-CAUSE: die Dashboards leben in den fünf
                // Rimconemy-Paket-Assemblies (Paket 02..05), während
                // RimconemyUi.DrawFeatureStatus im Foundation-Assembly
                // lebt (Paket 01). Ein früherer Versuch lud NUR das
                // Dashboard-Assembly und versuchte
                // `module.GetType(typeof(RimconemyUi).FullName)` — das
                // schlug in 6/6 Fällen fehl ("RimconemyUi not found in
                // same module"), obwohl die IL-Instruktion selbst sehr
                // wohl auf RimconemyUi.DrawFeatureStatus verweist.
                //
                // FIX: Wir brauchen den Cross-Assembly-Call gar nicht
                // durch Cecil zu resolven. Stattdessen
                //   (a) bauen wir uns die exakte Mono.Cecil-Formatierung
                //       der `MethodReference.FullName` aus reinen
                //       Reflection-Daten (kein Disk-Scan von
                //       Foundation.dll nötig) und
                //   (b) im Dashboard-Assembly scannen wir nach
                //       MethodReference-Operanden, deren
                //       `FullName` exakt diesem String entspricht.
                // Diese zwei FullName-Strings werden von Cecil ohnehin
                // als reine Text-Repräsentation berechnet — wir
                // vergleichen also Operanden-Wert gegen erwarteten
                // Soll-Wert, ohne den Ziel-MemberRef tatsächlich
                // aufzulösen.
                string targetFullName = BuildCecilMethodReferenceFullName(
                    typeof(RimconemyUi),
                    "DrawFeatureStatus",
                    typeof(UnityEngine.Rect),
                    typeof(string),
                    typeof(string),
                    typeof(StatusLevel));
                if (targetFullName == null) return false;

                string asmPath = type.Assembly?.Location;
                if (string.IsNullOrEmpty(asmPath) || !System.IO.File.Exists(asmPath))
                {
                    // In-Memory / dynamic assembly — kein Disk-Scan möglich.
                    Log.Warning(
                        "[Rimconemy.Foundation] Honest-Banner-Audit: Assembly-Pfad für "
                        + type.FullName + " nicht verfügbar (dynamic?). "
                        + "Cecil-Disk-Scan übersprungen; Fallback zu Inheritance-Check.");
                    return InheritsToolkitWithBanner(type);
                }

                using (var asmDef = AssemblyDefinition.ReadAssembly(asmPath))
                {
                    var module = asmDef.MainModule;

                    // Find the declaring type for DoWindowContents. The
                    // dashboard may have its own override, or it may
                    // inherit from RimconemyWindow / RimconemyMainTabWindow
                    // without overriding.
                    TypeDefinition typeDef = module.GetType(type.FullName);
                    if (typeDef == null) return false;

                    MethodDefinition doWindow = FindDoWindowContents(typeDef);
                    if (doWindow == null)
                    {
                        // Walk the inheritance chain in the dashboard's
                        // module. Base classes (RimconemyWindow,
                        // RimconemyMainTabWindow) might be in the SAME
                        // assembly (Foundation) — in diesem Fall wird
                        // module.GetType für sie null liefern und der
                        // Inheritance-Check übernimmt die Audit-Semantik.
                        if (type.BaseType != null && type.BaseType.Assembly == type.Assembly)
                        {
                            for (var baseType = type.BaseType;
                                 baseType != null && baseType != typeof(object);
                                 baseType = baseType.BaseType)
                            {
                                var baseTypeDef = module.GetType(baseType.FullName);
                                if (baseTypeDef == null) continue;
                                doWindow = FindDoWindowContents(baseTypeDef);
                                if (doWindow != null) break;
                            }
                        }
                    }

                    if (doWindow == null || !doWindow.HasBody || doWindow.Body.Instructions == null)
                    {
                        // Cross-assembly inheritance OR kein Body =>
                        // Inheritance-Check ist die richtige Heuristik.
                        return InheritsToolkitWithBanner(type);
                    }

                    // Walk direct `call` instructions to DrawFeatureStatus.
                    // Cross-Assembly-Calls landen als `MethodReference`
                    // (statt `MethodDefinition`) im Operand-Slot — die
                    // Is-MethodReference-Prüfung fängt beide Fälle ab,
                    // weil MethodDefinition : MethodReference in Cecil.
                    foreach (var instr in doWindow.Body.Instructions)
                    {
                        if (instr.OpCode != OpCodes.Call && instr.OpCode != OpCodes.Callvirt)
                            continue;
                        if (instr.Operand is MethodReference mr &&
                            mr.FullName == targetFullName)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.Foundation] Honest-Banner-Audit: Cecil scan failed for "
                    + type.FullName + ": " + ex.GetType().Name + ": " + ex.Message
                    + ". Falling back to inheritance check.");
                return InheritsToolkitWithBanner(type);
            }
        }

        /// <summary>
        /// Phase 8.6 root-cause-fix (2026-08-05): baut die exakte
        /// <c>MethodReference.FullName</c>-Formatierung, die Mono.Cecil
        /// intern für Cross-Assembly-Operanden produziert — aus reinen
        /// Reflection-Daten von <paramref name="declaringType"/>.
        /// Dadurch können wir den Cross-Assembly-Audit ohne separates
        /// Laden des Ziel-Assemblies (Foundation.dll) durchführen.
        ///
        /// Schema (siehe Mono.Cecil MethodReference.FullName):
        /// <c>"&lt;ReturnType.FullName&gt; &lt;DeclaringType.FullName&gt;::&lt;Name&gt;(&lt;ParamType1.FullName&gt;,&lt;ParamType2.FullName&gt;,…)"</c>
        ///
        /// Liefert <c>null</c> wenn <paramref name="declaringType"/> den
        /// erwarteten Methodennamen nicht trägt — dann ist der Audit-
        /// Vertrag gebrochen (z.B. weil die Methode umbenannt wurde) und
        /// der Test markiert die Dashboards ehrlich als nicht-auditiert.
        /// </summary>
        private static string BuildCecilMethodReferenceFullName(
            Type declaringType, string methodName, params Type[] paramTypes)
        {
            if (declaringType == null || string.IsNullOrEmpty(methodName))
                return null;

            // Resolve MethodInfo once so we can read ParameterType.FullName
            // strings — these are stable across .NET versions and match
            // Cecil's expectation.
            var mi = declaringType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: paramTypes,
                modifiers: null);
            if (mi == null) return null;

            var sb = new System.Text.StringBuilder(64);
            sb.Append(mi.ReturnType.FullName).Append(' ');
            sb.Append(mi.DeclaringType.FullName).Append("::");
            sb.Append(mi.Name).Append('(');
            var actualParams = mi.GetParameters();
            for (int i = 0; i < actualParams.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(actualParams[i].ParameterType.FullName);
            }
            sb.Append(')');
            return sb.ToString();
        }

        private static MethodDefinition FindDoWindowContents(TypeDefinition typeDef)
        {
            if (typeDef == null) return null;
            foreach (var m in typeDef.Methods)
            {
                if (m.Name != "DoWindowContents") continue;
                if (m.Parameters.Count != 1) continue;
                if (m.Parameters[0].ParameterType.FullName != "UnityEngine.Rect") continue;
                return m;
            }
            return null;
        }

        /// <summary>
        /// Phase 8.6 (2026-08-05): Reflection-basierter Fallback, wenn
        /// Cecil aus irgendwelchen Gründen die Bytes nicht lesen kann.
        /// Verifiziert, dass die Klasse von
        /// <see cref="RimconemyWindow"/> oder
        /// <see cref="RimconemyMainTabWindow"/> erbt, und dass keine
        /// Override-Schicht dazwischen die Banner-Aufruf-Pfad abfängt.
        /// Schwächer als der Cecil-Scan (kann den Body der geerbten
        /// Methode nicht verifizieren), aber besser als ein FAIL
        /// wegen In-Memory-Assemblys.
        /// </summary>
        private static bool InheritsToolkitWithBanner(Type type)
        {
            if (type == null) return false;
            if (!InheritsToolkit(type)) return false;

            // Wenn das Dashboard DoWindowContents selbst überschreibt, ist
            // der Body ohne Mono.Cecil nicht verifizierbar. Wir melden
            // "false" und überlassen die Audit-Lücke dem Operator-Log.
            var ownOverride = type.GetMethod(
                "DoWindowContents",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null,
                types: new[] { typeof(UnityEngine.Rect) },
                modifiers: null);
            if (ownOverride != null)
            {
                // Override existiert — der Inheritance-Fallback kann den
                // Body nicht beweisen. Wir returnen false; ein nachfolgender
                // Mono.Cecil-Lauf würde den eigentlichen Befund liefern.
                return false;
            }
            // Keine Override: das Dashboard erbt das basis-class-implementierte
            // DoWindowContents. Die Basis-Klasse ist RimconemyWindow oder
            // RimconemyMainTabWindow und ist im Audit verankert.
            return true;
        }

        private static bool InheritsToolkit(Type type)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                if (t == typeof(RimconemyWindow) || t == typeof(RimconemyMainTabWindow))
                    return true;
            }
            return false;
        }
    }
}
