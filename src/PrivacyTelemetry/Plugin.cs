using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace CzechSpeechEnhanced
{
    [BepInPlugin("nereziel.swyf.privacytelemetry", "Privacy Telemetry", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> BlockAnalytics;
        internal static ConfigEntry<bool> BlockFeedbackDiagnostics;
        private Harmony _harmony;

        private void Awake()
        {
            BlockAnalytics = Config.Bind("Privacy", "BlockAnalytics", true,
                "Block the game's analytics collector from queuing and uploading analytics.");
            BlockFeedbackDiagnostics = Config.Bind("Privacy", "BlockFeedbackDiagnostics", true,
                "Exclude diagnostic metadata and logs from manually submitted feedback snapshots.");

            _harmony = new Harmony("nereziel.swyf.privacytelemetry");
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Privacy Telemetry 1.0.0 loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
