using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rimconemy.Foundation.UI;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Paket 01).
    /// Audit-Bündel B / F-11 (2026-08-04) — Honest-Banner-Audit, fixed.
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
    ///      bei >= 6 Einträgen, damit der Test überhaupt sinnvoll aussage-
    ///      fähig ist. (Rein statischer Längen-Check, der verhindert, dass
    ///      das Audit durch Leeren der Liste trivial-grün wird.)
    ///
    /// WAS DIESER TEST NICHT PRÜFT: er beweist nicht, dass das Banner
    /// inhaltlich korrekt ist (READ-ONLY vs. echt mutierend). Das ist eine
    /// Code-Reviewer-/Audit-Aufgabe, nicht ein statischer Test. Audit-Belege
    /// liegen in <c>docs/falsification/status-vs-code-audit-2026-08-04.md</c>.
    ///
    /// F-11-Fix-Historie (Audit 2026-08-04):
    ///   - Vorher: T1/`passed++` lief auch dann, wenn 0 Banner gefunden
    ///     wurden (Klausel <c>dashboardsWithBanner &gt; 0 || validTypesCount ==
    ///     0</c>). T2 lief unbedingt (<c>passed++</c> ohne Kondition). T3
    ///     war zirkulär (Liste prüfte nur ihre eigene Mindestlänge).
    ///   - Nachher: jede Achse muss aktiv bestanden werden. Wenn die Liste
    ///     leer ist ODER Klassen fehlen ODER Banner-Calls fehlen ODER keine
    ///     Vererbung vorhanden ist, meldet der Test <c>failed &gt; 0</c>.
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
        /// UND im IL-Body einen <c>RimconemyUi.DrawFeatureStatus</c>-Aufruf
        /// enthalten. IL2CPP/SLIM-Builds reduzieren den Test auf den
        /// Existenz-Beweis und melden das Defizit laut.
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
                    _failures.Add("T1: dashboard " + name + " missing DrawFeatureStatus(IL2CPP fallback noted)");
                }
            }
            // Mandatory condition: ALL listed classes must carry the banner.
            // (Previously this pass could award a success with 0 banners via
            // the cold-start escape; that loophole is closed in F-11.)
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
        /// RimconemyMainTabWindow ableiten. Pass: alle Klassen bestehen.
        /// Fail: irgend eine Klasse erfüllt die Toolkit-Anker-Bedingung nicht.
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
        /// T3: Längen-Check der Audit-Liste. Verhindert, dass jemand die
        /// Liste auf 0 trimmt und grün bekommt — die Mindestlänge bleibt
        /// eine Voraussetzung für die Aussagefähigkeit der anderen Achsen.
        ///
        /// F-11-Doku: dieser Check bleibt absichtlich ein Längen-Check.
        /// Er prüft nicht den Inhalt (das tun T1+T2); er prüft nur, dass
        /// die Audit-Definition nicht durch Leeren trivialisiert wird.
        /// </summary>
        private static void RunT3_AuditedListIsNonTrivial()
        {
            if (AuditedDashboardTypeNames.Length >= 6
                // Defensive: keine null- oder Leerstring-Einträge erlaubt
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

        // ── IL-/Reflection-Helper ──

        private static bool HasDrawFeatureStatusCall(Type type)
        {
            if (type == null) return false;
            try
            {
                var method = type.GetMethod(
                    "DoWindowContents",
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    types: new[] { typeof(UnityEngine.Rect) },
                    modifiers: null);
                if (method == null) return false;

                var body = method.GetMethodBody();
                if (body == null) return false;
                byte[] il = body.GetILAsByteArray();
                if (il == null || il.Length == 0) return false;

                // Wir suchen roh nach einem Method-Token, der auf
                // DrawFeatureStatus zeigt.
                var target = typeof(RimconemyUi).GetMethod(
                    "DrawFeatureStatus",
                    BindingFlags.Public | BindingFlags.Static);
                if (target == null) return false;

                int targetToken = target.MetadataToken;
                int tokenBytes = (targetToken & 0xFFFFFF);
                byte b0 = (byte)((tokenBytes >> 16) & 0xFF);
                byte b1 = (byte)((tokenBytes >> 8) & 0xFF);
                byte b2 = (byte)(tokenBytes & 0xFF);
                for (int i = 0; i + 3 < il.Length; i++)
                {
                    if (il[i] == b0 && il[i + 1] == b1 && il[i + 2] == b2)
                        return true;
                }
                return false;
            }
            catch (InvalidOperationException)
            {
                // IL2CPP/SLIM-Builds liefern keine IL-Bytes. Wir melden den
                // Fall explizit im Log und melden die Klasse als nicht-auditiert
                // zurück (return false), damit niemand annimmt, IL-Scan hätte
                // ihre Konformität bestätigt. In IL2CPP-Builds muss der
                // Operator die Konformität per Source-Read oder manuell
                // verifizieren — diese Audit-Lücke ist gewollt sichtbar.
                Log.Warning(
                    "[Rimconemy.Foundation] Honest-Banner-Audit: IL-Bytes für "
                    + type.FullName + " nicht verfügbar (IL2CPP/SLIM-Build?). "
                    + "Konformität muss manuell verifiziert werden.");
                return false;
            }
            catch (ReflectionTypeLoadException rtle)
            {
                Log.Warning(
                    "[Rimconemy.Foundation] Honest-Banner-Audit: Dashboard-Klasse "
                    + type.FullName + " konnte nicht geladen werden: "
                    + rtle.Message);
                return false;
            }
            catch
            {
                return false;
            }
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
