using HarmonyLib;

namespace CzechSpeechEnhanced
{
    /// <summary>
    /// Gives natural pauses inside a sentence a little more room before the game finalizes speech.
    /// Kept separate so existing 0.8 s config files are upgraded behaviorally without requiring a reset.
    /// </summary>
    [HarmonyPatch(typeof(WhisperVcPipeline), "OnEnable")]
    internal static class SpeechTimingPatch
    {
        private static readonly AccessTools.FieldRef<WhisperVcPipeline, float> EndSilence =
            AccessTools.FieldRefAccess<WhisperVcPipeline, float>("silenceToFinalizeSeconds");

        private static void Postfix(WhisperVcPipeline __instance)
        {
            const float minimum = 1.3f;
            float current = EndSilence(__instance);
            if (current < minimum)
            {
                Plugin.Log.LogInfo($"End-of-speech silence {current:F2}s -> {minimum:F2}s");
                EndSilence(__instance) = minimum;
            }
        }
    }
}
