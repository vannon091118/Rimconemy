using RimWorld;
using Verse;

namespace Rimconemy.Foundation.DLC
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    ///
    /// Phase-7 DLC-Content-Policy-Layer, Phase-2 Runtime-Layer (2026-08-04):
    /// Defense-in-Depth GameComponent der die Policy-Override-Schicht
    /// zuverlässig anwendet, unabhängig vom [StaticConstructorOnStartup]
    /// Timing der Foundation Bootstrap static method.
    ///
    /// ## Timing-Race Problem
    ///
    /// In RimWorld 1.6 feuert <c>[StaticConstructorOnStartup]</c> während
    /// Mod-Init-Phase. Die DefDatabase kann zu diesem Zeitpunkt noch leer sein
    /// (vor allem für Defs aus externen ContentPacks die später laden). Das
    /// Bootstrap static-ctor ruft zwar <see cref="DLCPolicyConfig.ApplyFromLoadedDefs"/>,
    /// wenn die DefDatabase leer ist returnt das 0 und ein ContentPack-Override
    /// wird nicht angewandt — der Spieler sieht im Log "applied=0" und glaubt
    /// die Phase-2-Logik ist kaputt.
    ///
    /// ## Fix
    ///
    /// Dieser GameComponent wird von RimWorld zur Laufzeit automatisch
    /// instanziert (Reflection-Discovery via GameComponentUtility). Seine
    /// <c>FinalizeInit</c> Methode feuert zuverlässig nach Mod-Init-Phase
    /// abgeschlossen ist UND alle Mods ihre Defs geladen haben. Idempotent
    /// aufrufen ist sicher (ApplyFromLoadedDefs validiert pro-def).
    ///
    /// Der Bootstrap static-ctor bleibt als first-shot erhalten (für den
    /// Fall dass die Foundation-eigenen Defs bereits geladen sind); dieser
    /// GameComponent ist der Fallback für späte Defs.
    /// </summary>
    public class DLCPolicyComponent : GameComponent
    {
        public DLCPolicyComponent(Game game) : base() { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            try
            {
                int applied = DLCPolicyConfig.ApplyFromLoadedDefs();
                if (applied > 0)
                {
                    Log.Message(
                        $"[Rimconemy.Foundation] DLCPolicyComponent.FinalizeInit: Phase-2 policy " +
                        $"re-applied ({applied} fields overridden).");
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    $"[Rimconemy.Foundation] DLCPolicyComponent.FinalizeInit: " +
                    $"{ex.GetType().Name}: {ex.Message}. Phase-1 defaults remain active.");
            }
        }

        public override void GameComponentTick()
        {
            // No-op: nur FinalizeInit, kein Per-Tick-Arbeit nötig. Diese
            // Klasse ist ein reiner Lifecycle-Pin damit die Policy nach
            // allen Mod-Init-Phasen angewandt wird.
        }
    }
}
