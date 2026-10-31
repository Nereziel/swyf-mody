using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace CzechSpeechEnhanced
{
    [HarmonyPatch]
    internal static class BlockAnalyticsQueue
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("Kolkata.Analytics.AnalyticsCollector"), "Enqueue");
        private static bool Prefix() => !Plugin.BlockAnalytics.Value;
    }

    [HarmonyPatch]
    internal static class BlockAnalyticsDelivery
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("Kolkata.Analytics.AnalyticsCollector"), "DriveDelivery");
        private static bool Prefix() => !Plugin.BlockAnalytics.Value;
    }

    [HarmonyPatch]
    internal static class BlockFeedbackCollection
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("FeedbackDiagnostics"), "Start");
        private static bool Prefix() => !Plugin.BlockFeedbackDiagnostics.Value;
    }

    [HarmonyPatch]
    internal static class FilterFeedbackDiagnostics
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("FeedbackDiagnostics"), "Snapshot");
        private static bool Prefix(JObject metadata, JArray recordings, ref JObject __result)
        {
            if (!Plugin.BlockFeedbackDiagnostics.Value) return true;
            metadata?.RemoveAll();
            __result = new JObject {
                ["metadata"] = new JObject(),
                ["recordings"] = recordings ?? new JArray(),
                ["recentLogs"] = new JArray(),
                ["omittedLogEntries"] = 0,
                ["truncated"] = false
            };
            return false;
        }
    }
}
