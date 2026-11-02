using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CzechSpeechEnhanced
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nereziel.swyf.czechspeechenhanced";
        public const string PluginName = "CzechSpeechEnhanced";
        public const string PluginVersion = "1.3.1";
        internal const string ModId = "CzechSpeechEnhanced";
        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> SpeechEngine;
        internal static ConfigEntry<string> ComputeDevice;
        internal static ConfigEntry<string> LivePreview;
        internal static ConfigEntry<bool> NormalizeVolume;
        internal static ConfigEntry<float> SpeechThreshold;
        internal static ConfigEntry<float> EndSilenceSeconds;
        internal static ConfigEntry<bool> LogTranscripts;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            string oldConfig = Path.Combine(Paths.ConfigPath, "dex.swyf.czechspeechenhanced.cfg");
            if (!File.Exists(Config.ConfigFilePath) && File.Exists(oldConfig))
            {
                File.Copy(oldConfig, Config.ConfigFilePath);
                Config.Reload();
            }

            SpeechConfiguration.Bind(Config);
            if (!Enabled.Value)
            {
                Log.LogInfo($"{PluginName} {PluginVersion} is disabled in the config.");
                return;
            }

            string originalMod = Path.Combine(Paths.GameRootPath, "Mods", "CzechSpeech");
            if (File.Exists(Path.Combine(originalMod, "mod.json")) && !File.Exists(Path.Combine(originalMod, "disabled")))
            {
                Log.LogWarning("CzechSpeech is enabled too. Disable it in SWYF Mod Hub and restart the game to avoid conflicting patches.");
                return;
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Parakeet engine={SpeechEngine.Value}, compute={ComputeDevice.Value}. " +
                        (ParakeetBridge.Available
                            ? "Runtime and model files found."
                            : "Runtime or model missing. Install the selected model with Mod Hub and restart; the game keeps its own recognition until then."));
        }

        private void OnDestroy()
        {
            ParakeetBridge.Stop();
            _harmony?.UnpatchSelf();
        }
    }
}
