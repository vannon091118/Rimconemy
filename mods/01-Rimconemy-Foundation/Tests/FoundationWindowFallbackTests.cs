using System;
using Rimconemy.Foundation.UI;
using UnityEngine;
using Verse;
using Rimconemy.Foundation.Tests;


namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Paket 01).
    /// Regression für den Honest-Fallback-Pfad in
    /// <see cref="Rimconemy.Foundation.UI.RimconemyWindow"/> +
    /// <see cref="Rimconemy.Foundation.UI.RimconemyMainTabWindow"/>.
    ///
    /// Geprüfte Invarianten (Audit-Falsifizierung status-vs-code-audit-2026-08-04 §A1/A2):
    ///   1. Initialer Memo-Count ist 0.
    ///   2. Erster DoWindowContents-Call auf override-vergessender Subklasse
    ///      erhöht Memo auf 1; auch wenn Widgets.DrawBoxSolid außerhalb eines
    ///      aktiven OnGUI-Kontexts eine Exception wirft, darf der Memo-Pfad
    ///      dennoch nicht crashed werden.
    ///   3. Zweiter Call auf demselben Caller-Typ erhöht Memo NICHT
    ///      (Code-Review-Finding C1: Anti-Log-Spam-Disziplin).
    ///   4. DoWindowContents auf einer anderen Quelle (Window vs. MainTab)
    ///      erzeugt einen separaten Memo-Eintrag (Count == 2).
    ///   5. Clear-Hook setzt den Memo auf 0 zurück, damit wiederholte
    ///      Test-Runs deterministisch starten.
    ///
    /// Style-Hinweis: Diese Tests folgen der Foundation-`RunAll()`-Convention
    /// (statisch, ohne externe Test-Framework). Sie werden über
    /// Foundation.Bootstrap.RunAll aufgerufen. Jeder Test ist fail-soft: ein
    /// Crash im Renderer gilt nicht als Test-Fail; nur Memo-Anomalien
    /// (Count-Verstoß) failen den Test.
    /// </summary>
    public static class FoundationWindowFallbackTests
    {
        private static TestSuite ts;
        public const int ExpectedPassCount = 5;

        public static void RunAll()
        {
            ts = new TestSuite("Foundation", "Window-fallback tests");

            int passed = 0;
            try
            {
                // T1: Initialer Memo ist 0 nach Reset.
                RimconemyWindow.ClearFallbackLogMemoForTests();
                if (RimconemyWindow.MemoEntryCount == 0)
                    passed++;

                // T2: Erster Window-Fallback-Call erhöht Memo auf 1.
                SafeCall(new TestWindowUnfinished());
                if (RimconemyWindow.MemoEntryCount == 1)
                    passed++;

                // T3: Zweiter Call auf demselben Caller-Typ — Memo bleibt 1.
                SafeCall(new TestWindowUnfinished());
                if (RimconemyWindow.MemoEntryCount == 1)
                    passed++;

                // T4: Andere Caller-Quelle (MainTab) — Memo wird 2.
                SafeCall(new TestMainTabUnfinished());
                if (RimconemyWindow.MemoEntryCount == 2)
                    passed++;

                // T5: Clear-Hook setzt zurück.
                RimconemyWindow.ClearFallbackLogMemoForTests();
                if (RimconemyWindow.MemoEntryCount == 0)
                    passed++;

                Log.Message(
                    "[Rimconemy.Foundation] Window-fallback tests: "
                    + passed + "/" + ExpectedPassCount + " passed.");
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.Foundation] FoundationWindowFallbackTests.RunAll crashed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }

            ts.Check(passed >= ExpectedPassCount, "legacy assertion aggregate");
            ts.RunSummary(1);
        }

        // Widgets-Aufrufe crashen außerhalb eines aktiven OnGUI-Kontexts.
        // Wir zählen nur Memo-Disziplin, nicht Render-Output. Trotzdem
        // werfen wir die Exception nicht weiter, weil wir den Memo-Pfad
        // unabhängig vom Render-State prüfen wollen.
        //
        // Wichtig: Wir schlucken die Exception, weil die Spec unter Test
        // Memo-Disziplin ist, nicht Render-Korrektheit. Die subclass-must-
        // override-Disziplin wird in Production erzwungen, nicht hier.
        private static void SafeCall(Window window)
        {
            try
            {
                window.DoWindowContents(new Rect(0f, 0f, 80f, 60f));
            }
            catch (Exception)
            {
                // Defensive: Widgets.DrawBoxSolid/DrawBox kann in headless
                // Tests außerhalb von OnGUI NRE oder IL2CPP-Exception werfen.
                // Spec unter Test ist Memo-Disziplin, nicht Render.
            }
        }
    }

    // Anonyme Subklassen ohne DoWindowContents-Override. Diese Klassen sind
    // rein interne Test-Doubles; sie exportieren nirgendwo else und dürfen
    // nicht in Production verwendet werden.
    internal sealed class TestWindowUnfinished : Rimconemy.Foundation.UI.RimconemyWindow
    {
    }

    internal sealed class TestMainTabUnfinished : Rimconemy.Foundation.UI.RimconemyMainTabWindow
    {
    }
}
