namespace Rimconemy.SurvivalProgression
{
    /// <summary>
    /// Package-02 integration entry point for optional tutorial forwarding.
    /// </summary>
    public static class SurvivalIntegration
    {
        public static void Initialize()
        {
            Bridge.SurvivalTutorialBridge.Initialize();
        }
    }
}
