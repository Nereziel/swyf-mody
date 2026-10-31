using BepInEx;
using HarmonyLib;

namespace CzechSpeechEnhanced
{
    [BepInPlugin("nereziel.swyf.czechttsfix", "Czech TTS Fix", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        private void Awake()
        {
            _harmony = new Harmony("nereziel.swyf.czechttsfix");
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Czech TTS Fix 1.0.0 loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
