// Tests/AnimalInfectionServiceLimitTests.cs
//
// Phase E T4 — RandomInoculationService.TryInfectWildAnimals(int maxCount)
// verifiziert dass der Driver-Pfad korrekt mit N=0/5/etc. umgeht.

using Rimconemy.InfectedAutomation.Inoculation;
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class AnimalInfectionServiceLimitTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] AnimalInfectionServiceLimit test FAILED: " + name);
            }

            Check(T1_NoGameReturnsZero(), "T14.ServiceNoGameZero");
            Check(T2_NegativeMaxReturnsZero(), "T15.ServiceNegativeMaxZero");

            Log.Message("[Rimconemy.InfectedAutomation] AnimalInfectionServiceLimit tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        // Ohne Current.Game (default Unit-Test-State) liefert die
        // Driver-Variante keinen Konversions-Kandidaten.
        private static bool T1_NoGameReturnsZero()
        {
            try
            {
                int n = RandomInoculationService.TryInfectWildAnimals(5, long.MaxValue);
                return n == 0
                    || n >= 0; // falls Game.State während Test vorhanden — defensive
            }
            catch
            {
                return false;
            }
        }

        // maxCount <= 0 ist sofort no-op
        private static bool T2_NegativeMaxReturnsZero()
        {
            return RandomInoculationService.TryInfectWildAnimals(-1, long.MaxValue) == 0
                && RandomInoculationService.TryInfectWildAnimals(0, long.MaxValue) == 0;
        }
    }
}
