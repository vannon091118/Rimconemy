// Tests/AnimalInfectionAiOverlayTests.cs
//
// Phase E T8 — AnimalInfectionAiOverlay.Marker Tests T16-T19.
//
// Pawn-Mocking ist in RimWorld 1.6 schwer ohne Verse-Map-Setup. Diese
// Tests fokussieren sich daher auf die Null-Edge-Cases, die im Worst
// Case WIRKLICH crashen würden (NRE im Render-Hook).

using Rimconemy.InfectedAutomation.Inoculation;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class AnimalInfectionAiOverlayTests
    {
        private static TestSuite ts;
        public static int RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "AnimalInfectionAiOverlay test");

            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Error("[Rimconemy.InfectedAutomation] AnimalInfectionAiOverlay test FAILED: " + name);
            }

            Check(T16_NullPawnReturnsFalse(),      "T16.OverlayNullPawn");
            Check(T17_StaticConstantsValid(),     "T17.OverlayStaticConstants");
            Check(T18_MarkerTextureIsValid(),     "T18.OverlayMarkerTexture");
            Check(T19_BrandedKindDefNameValid(),  "T19.OverlayBrandedKindName");

            Log.Message("[Rimconemy.InfectedAutomation] AnimalInfectionAiOverlay tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));

            ts.Check(failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return passed;
        }

        private static bool T16_NullPawnReturnsFalse()
        {
            try { return AnimalInfectionAiOverlay.ShouldShowInfectionMarker(null) == false; }
            catch { return false; }
        }

        private static bool T17_StaticConstantsValid()
        {
            return AnimalInfectionAiOverlay.MarkerPixelSize > 0f
                && AnimalInfectionAiOverlay.MarkerPixelSize <= 64f;
        }

        private static bool T18_MarkerTextureIsValid()
        {
            try
            {
                var tex = AnimalInfectionAiOverlay.GetOrLoadMarkerTexture();
                return tex != null && tex.width > 0 && tex.height > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool T19_BrandedKindDefNameValid()
        {
            return !string.IsNullOrEmpty(InoculationConverter.BrandedKindDefName)
                && InoculationConverter.BrandedKindDefName.StartsWith("Rimconemy_");
        }
    }
}
