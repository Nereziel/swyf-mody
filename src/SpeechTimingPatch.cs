using HarmonyLib;

namespace CzechSpeechEnhanced
{
    /// <summary>
    /// Gives natural pauses inside a sentence a little more room before the game finalizes speech.
    /// Existing configs below 1.3 s are upgraded automatically.
    /// </summary>
    [HarmonyPatch(typeof(WhisperVcPipeline), "OnEnable")]
    internal static class SpeechTimingPatch
    {
        private static readonly AccessTools.FieldRef<WhisperVcPipeline, float> EndSilence =
            AccessTools.FieldRefAccess<WhisperVcPipeline, float>("silenceToFinalizeSeconds");

        private static void Postfix(WhisperVcPipeline __instance)
        {
            const float minimum = 1.3f;

            if (Plugin.EndSilenceSeconds.Value < minimum)
            {
                Plugin.EndSilenceSeconds.Value = minimum;
            }

            float target = Plugin.EndSilenceSeconds.Value;
            float current = EndSilence(__instance);
            if (current < target)
            {
                Plugin.Log.LogInfo($"End-of-speech silence {current:F2}s -> {target:F2}s");
                EndSilence(__instance) = target;
            }
        }
    }
}
