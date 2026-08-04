using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Power
{
    /// <summary>
    /// Owner: Scavenger and Infrastructure (Package 03).
    /// Backward-compatibility aliases for the historical stub.
    /// The real implementation lives in <see cref="PowerChainService"/>.
    /// C-T2: replaced with live DefDatabase reads + FuelClass classification.
    /// </summary>
    public static class PowerChainStub
    {
        public const string LogMarker = "v1";

        // Forwarded for legacy callers. New code MUST use PowerChainService
        // directly. These aliases are kept alive so older modloaders that
        // reference the old consts still compile.
        public const string SolidFuelGeneratorDefName = PowerChainService.SolidFuelGeneratorDefName;
        public const string LiquidFuelGeneratorDefName = PowerChainService.LiquidFuelGeneratorDefName;
        public const string TurbineWaterPump = PowerChainService.TurbineWaterPump;
        public const string ArrowTurretDefName = PowerChainService.ArrowTurretDefName;

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                PowerChainService.Resolve();
                Log.Message(
                    "[Rimconemy.ScavengerInfrastructure] PowerChainStub deprecated; " +
                    "use PowerChainService for live reads. Stub aliases kept for compile-time compatibility.");
            }
        }
    }
}
