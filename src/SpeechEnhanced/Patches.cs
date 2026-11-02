using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using Whisper;

namespace CzechSpeechEnhanced
{
    // These are game integration points; no Whisper model/settings are changed.
    [HarmonyPatch(typeof(WhisperManager), nameof(WhisperManager.GetTextAsync), typeof(float[]), typeof(int), typeof(int))]
    internal static class WhisperManager_GetTextAsync_Patch
    {
        private static bool Prefix(WhisperManager __instance, float[] samples, int frequency, int channels, ref Task<WhisperResult> __result)
        {
            if (ParakeetBridge.Bypass || samples == null || !ParakeetBridge.Available) return true;
            float[] prepared = Speech.PrepareForParakeet(samples, Plugin.NormalizeVolume.Value);
            __result = ParakeetBridge.Transcribe(prepared, frequency, channels,
                () => __instance.GetTextAsync(samples, frequency, channels));
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WhisperModel_GetTextAsync_Parakeet_Patch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("WhisperModel"), "GetTextAsync");

        private static bool Prefix(object __instance, float[] samples, WhisperManager settings, ref Task<WhisperResult> __result)
        {
            if (ParakeetBridge.Bypass || samples == null || settings == null || !ParakeetBridge.Available) return true;
            float[] prepared = Speech.PrepareForParakeet(samples, Plugin.NormalizeVolume.Value);
            var method = TargetMethod();
            __result = ParakeetBridge.Transcribe(prepared, 16000, 1,
                () => (Task<WhisperResult>)method.Invoke(__instance, new object[] { samples, settings }));
            return false;
        }
    }

    [HarmonyPatch(typeof(WindowsSpeechCompatibility), nameof(WindowsSpeechCompatibility.UseMoonshine))]
    internal static class WindowsSpeechCompatibility_UseMoonshine_Patch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ParakeetBridge.Available) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(WhisperVcPipeline), "OnEnable")]
    internal static class WhisperVcPipeline_OnEnable_Patch
    {
        private sealed class OriginalSettings { internal float Threshold, EndSilence; }
        private static readonly ConditionalWeakTable<WhisperVcPipeline, OriginalSettings> Originals =
            new ConditionalWeakTable<WhisperVcPipeline, OriginalSettings>();
        private static readonly AccessTools.FieldRef<WhisperVcPipeline, float> Threshold =
            AccessTools.FieldRefAccess<WhisperVcPipeline, float>("speechRmsThreshold");
        private static readonly AccessTools.FieldRef<WhisperVcPipeline, float> EndSilence =
            AccessTools.FieldRefAccess<WhisperVcPipeline, float>("silenceToFinalizeSeconds");

        private static void Postfix(WhisperVcPipeline __instance)
        {
            Apply(__instance);
        }

        internal static void Apply(WhisperVcPipeline pipeline)
        {
            var original = Originals.GetValue(pipeline, p => new OriginalSettings
                { Threshold = Threshold(p), EndSilence = EndSilence(p) });
            bool selected = ParakeetBridge.Available;
            EndSilence(pipeline) = selected ? Plugin.EndSilenceSeconds.Value : original.EndSilence;
            Threshold(pipeline) = selected && Plugin.SpeechThreshold.Value > 0f ? Plugin.SpeechThreshold.Value : original.Threshold;
        }
    }

    [HarmonyPatch(typeof(WhisperVcPipeline), "ApplyRecognitionPreference")]
    internal static class WhisperVcPipeline_Preference_Patch
    {
        private static void Postfix(WhisperVcPipeline __instance) => WhisperVcPipeline_OnEnable_Patch.Apply(__instance);
    }

    [HarmonyPatch(typeof(WhisperVcPipeline), "QueueInference")]
    internal static class WhisperVcPipeline_QueueInference_Patch
    {
        private static bool Prefix(bool isFinal) => !ParakeetBridge.Available || isFinal || Plugin.LivePreview.Value != "off";
    }

    [HarmonyPatch(typeof(WhisperVcPipeline), "NormalizeTranscript")]
    internal static class WhisperVcPipeline_NormalizeTranscript_Patch
    {
        private static void Postfix(string transcript, string __result)
        {
            if (Plugin.LogTranscripts.Value && !string.IsNullOrWhiteSpace(transcript))
                Plugin.Log.LogInfo($"Heard: \"{transcript.Trim()}\" -> \"{__result}\"");
        }
    }
}
