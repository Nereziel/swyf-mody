using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace CzechSpeechEnhanced
{
    internal static class CzechPocketText
    {
        [ThreadStatic] internal static bool Active;
    }

    [HarmonyPatch]
    internal static class CzechPocketModelScope
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("PocketTTS.PocketTTSModel"), "BuildPromptSegments");
        private static void Prefix(object __instance, out bool __state)
        {
            __state = CzechPocketText.Active;
            var field = AccessTools.Field(__instance.GetType(), "tokenizerPath");
            string path = field?.GetValue(__instance) as string;
            CzechPocketText.Active = !string.IsNullOrEmpty(path) && ("/" + path.Replace('\\', '/')).IndexOf("/czech/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private static Exception Finalizer(Exception __exception, bool __state)
        {
            CzechPocketText.Active = __state;
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class CzechPocketPrepareText
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("PocketTts.TextProcessor"), "PrepareText");
        private static bool Prefix(string text, ref string __result)
        {
            if (!CzechPocketText.Active) return true;
            __result = Regex.Replace((text ?? "").Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ");
            if (__result.Length > 0 && char.IsLetterOrDigit(__result[__result.Length - 1])) __result += ".";
            return false;
        }
    }
}
