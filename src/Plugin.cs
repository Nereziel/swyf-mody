using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CzechSpeechEnhanced
{
    /// <summary>
    /// Better Czech speech recognition: swaps the game's small Whisper model (ggml-base) for a big one
    /// from Mods/CzechSpeechEnhanced, forces Czech, uses beam search and cleans up typical Whisper nonsense.
    /// Only changes your own microphone transcription, so it is safe in any lobby.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nereziel.swyf.czechspeechenhanced";
        public const string PluginName = "CzechSpeechEnhanced";
        internal const string ModId = "CzechSpeechEnhanced";
        internal const string DefaultEngine = "parakeet";
        public const string PluginVersion = "1.3.1";

        private static readonly string[] PreferredModels =
        {
            "ggml-large-v3.bin",
            "ggml-large-v3-turbo.bin",
            "ggml-large-v3-q5_0.bin",
            "ggml-large-v3-turbo-q8_0.bin",
            "ggml-large-v3-turbo-q5_0.bin",
        };

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> ModelFile;
        internal static ConfigEntry<string> SpeechEngine;
        internal static ConfigEntry<string> ComputeDevice;
        internal static ConfigEntry<bool> FlashAttention;
        internal static ConfigEntry<string> Language;
        internal static ConfigEntry<int> BeamSize;
        internal static ConfigEntry<bool> FastPartials;
        internal static ConfigEntry<string> LivePreview;
        internal static ConfigEntry<string> CpuModel;
        internal static ConfigEntry<string> Device;
        internal static ConfigEntry<string> InitialPrompt;
        internal static ConfigEntry<bool> NormalizeVolume;
        internal static ConfigEntry<float> SpeechThreshold;
        internal static ConfigEntry<float> EndSilenceSeconds;
        internal static ConfigEntry<bool> CleanTranscript;
        internal static ConfigEntry<bool> LogTranscripts;

        internal static string ModelFolder => Path.Combine(Paths.GameRootPath, "Mods", ModId);

        internal static string ForcedLanguage
        {
            get
            {
                string code = Language.Value?.Trim().ToLowerInvariant();
                return string.IsNullOrEmpty(code) || code == "auto" ? null : code;
            }
        }

        private void Awake()
        {
            Log = Logger;
            string oldConfig = Path.Combine(Paths.ConfigPath, "dex.swyf.czechspeechenhanced.cfg");
            string newConfig = Path.Combine(Paths.ConfigPath, PluginGuid + ".cfg");
            if (!File.Exists(newConfig) && File.Exists(oldConfig))
            {
                File.Copy(oldConfig, newConfig);
                Config.Reload();
                Log.LogInfo("Migrated CzechSpeechEnhanced settings to the new configuration file.");
            }

            Enabled = Config.Bind("General", "Enabled", true, "Use the better speech recognition.");
            SpeechEngine = Config.Bind("Recognition", "Engine", DefaultEngine, "Speech recognition engine. Restart after changing it.");
            string savedEngine = SpeechEngine.Value?.Trim().ToLowerInvariant();
            if (savedEngine != "parakeet" && savedEngine != "ultra")
            {
                Log.LogWarning("Unsupported speech engine '" + savedEngine + "'; switching to Parakeet.");
                SpeechEngine.Value = "parakeet";
            }

            ComputeDevice = Config.Bind("Recognition", "ComputeDevice", "auto",
                "Speech computation device: auto, gpu, or cpu. Restart after changing it. GPU uses Vulkan; unsupported operations can fall back to CPU.");
            ModelFile = Config.Bind("Model", "Model", "large-v3",
                "Which model from Mods/CzechSpeechEnhanced to use: large-v3 (best, ~4 GB VRAM), turbo (faster, for weaker PCs, ~2 GB VRAM), " +
                "or a file name / full path. If it is missing, the best model found there is used.");
            FlashAttention = Config.Bind("Model", "FlashAttention", false,
                "Whisper flash attention. The game turns it on, but with its Vulkan build it makes recognition ~10x slower. Leave off.");
            Device = Config.Bind("Model", "Device", "auto",
                "Where speech recognition runs. The game keeps some graphics cards on the processor (RTX 5090, GTX 1650, " +
                "RX 470-580...), which costs ~20 s per sentence; the mod ignores that list. " +
                "auto = graphics card, but if the game ever ends while trying it, the processor from then on (see " + GpuFailedFile +
                " in Mods/CzechSpeechEnhanced); gpu = graphics card always, even after such a failure; cpu = always the processor.");
            Language = Config.Bind("Recognition", "Language", "cs",
                "Language Whisper listens for (cs, sk, en...). Empty = the game's speech language from Audio settings.");
            BeamSize = Config.Bind("Recognition", "BeamSize", 5,
                "Beam search width for finished sentences. 1 = greedy like the base game, 5 = more accurate.");
            FastPartials = Config.Bind("Recognition", "FastPartials", true,
                "Live preview while you are still talking uses fast greedy decoding; the final sentence uses beam search.");
            LivePreview = Config.Bind("Recognition", "LivePreview", "auto",
                "Text appearing while you are still talking. auto = only when recognition is fast (GPU); on a slow PC the preview " +
                "would hold up the finished sentence. on / off = always / never.");
            CpuModel = Config.Bind("Model", "CpuModel", "turbo",
                "Model used when Whisper has to run on the processor (the game disables the GPU for some graphics cards, " +
                "or there is no Vulkan). turbo = several times faster than large-v3 there. Empty = same as Model.");
            InitialPrompt = Config.Bind("Recognition", "InitialPrompt",
                "Přepis telefonního hovoru v češtině s diakritikou a interpunkcí.",
                "Text Whisper sees before your speech. Steers it to Czech spelling and punctuation. Use a sentence nobody would say out loud: " +
                "on noise Whisper may repeat it, and such echoes are thrown away. Empty = none.");
            NormalizeVolume = Config.Bind("Audio", "NormalizeVolume", true,
                "Even out loudness before recognition, so whispering and shouting both come through.");
            SpeechThreshold = Config.Bind("Audio", "SpeechThreshold", 0f,
                "Microphone level (RMS) that counts as speech. 0 = the game's value (0.002). Lower catches quieter whispering, " +
                "but must stay above your room noise, otherwise sentences never end.");
            EndSilenceSeconds = Config.Bind("Audio", "EndSilenceSeconds", 1.3f,
                "Seconds of silence before finishing a sentence. Lower is faster but may split natural pauses. Minimum used by this mod is 1.3 s.");
            CleanTranscript = Config.Bind("Recognition", "CleanTranscript", true,
                "Remove typical Whisper nonsense (\"Titulky vytvořil JohnyX\", repeated words, the prompt echoed back).");
            LogTranscripts = Config.Bind("Debug", "LogTranscripts", false,
                "Write what was recognized into BepInEx/LogOutput.log (for testing).");

            if (!Enabled.Value)
            {
                Log.LogInfo($"{PluginName} {PluginVersion} is disabled in the config.");
                return;
            }

            string originalMod = Path.Combine(Paths.GameRootPath, "Mods", "CzechSpeech");
            if (File.Exists(Path.Combine(originalMod, "mod.json")) && !File.Exists(Path.Combine(originalMod, "disabled")))
            {
                Log.LogWarning("CzechSpeech is enabled too. Disable the original mod in SWYF Mod Hub and restart the game; CzechSpeechEnhanced will stay inactive to prevent conflicting patches.");
                return;
            }

            ResolveDevice();
            Speech.GraphicsCard = $"{UnityEngine.SystemInfo.graphicsDeviceName} " +
                                  $"({UnityEngine.SystemInfo.graphicsDeviceVendorID:X4}:{UnityEngine.SystemInfo.graphicsDeviceID:X4})";
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            WarnIfModelAlreadyLoaded();
            Log.LogInfo($"Selected speech engine: {SpeechEngine.Value}. " +
                        (ParakeetBridge.Available
                            ? "Runtime and model files found."
                            : "Runtime or model files missing; using the game's Whisper. Install all files for the selected engine and restart."));

            string[] models = Directory.Exists(ModelFolder)
                ? Directory.GetFiles(ModelFolder, "*.bin").Select(f => $"{Path.GetFileName(f)} ({new FileInfo(f).Length / 1048576} MiB)").ToArray()
                : new string[0];
            Log.LogInfo(models.Length > 0
                ? $"{PluginName} {PluginVersion} loaded. Models: {string.Join(", ", models)}; Model = {ModelFile.Value}, CpuModel = {CpuModel.Value}, Device = {Device.Value}, " +
                  $"language: {ForcedLanguage ?? "game setting"}, beam: {BeamSize.Value}."
                : $"{PluginName} {PluginVersion} loaded without a model in {ModelFolder}; using the game's model with better settings.");
        }

        private Harmony _harmony;

        internal const string GpuTryFile = "gpu-zkouska.txt";
        internal const string GpuFailedFile = "gpu-selhala.txt";
        internal static string EffectiveDevice = "auto";

        private static void ResolveDevice()
        {
            string device = Device.Value?.Trim().ToLowerInvariant();
            if (device != "gpu" && device != "cpu")
            {
                device = "auto";
            }
            string tryPath = Path.Combine(ModelFolder, GpuTryFile);
            string failedPath = Path.Combine(ModelFolder, GpuFailedFile);
            try
            {
                if (File.Exists(tryPath))
                {
                    File.WriteAllText(failedPath,
                        "Pri minulem spusteni hra skoncila, zatimco rozpoznavani reci zkouselo grafickou kartu.\r\n" +
                        "Mod proto pocita na procesoru. Smaz tento soubor (nebo v SWYF Mod Hubu znovu vyber Grafika), " +
                        "aby to zkusil znovu.\r\n" + File.ReadAllText(tryPath));
                    File.Delete(tryPath);
                }
                if (device == "auto" && File.Exists(failedPath))
                {
                    Log.LogWarning($"The game ended while trying the graphics card before ({failedPath}). Using the processor; " +
                                   "delete that file (or pick Grafika in the SWYF Mod Hub, Device = gpu) to try the graphics card again.");
                    device = "cpu";
                }
            }
            catch (Exception e)
            {
                Log.LogWarning("Could not check the GPU try files: " + e.Message);
            }
            if (device == "auto" && Environment.GetCommandLineArgs().Any(a => a.Equals("-whisper-cpu", StringComparison.OrdinalIgnoreCase)))
            {
                device = "cpu";
            }
            EffectiveDevice = device == "auto" ? "gpu" : device;
            if (EffectiveDevice == "gpu")
            {
                Environment.SetEnvironmentVariable("KOLKATA_WHISPER_CPU_ONLY", null);
            }
        }

        private static void WarnIfModelAlreadyLoaded()
        {
            foreach (var manager in UnityEngine.Resources.FindObjectsOfTypeAll<Whisper.WhisperManager>())
            {
                if (manager != null && manager.IsLoaded && !manager.ModelPath.EndsWith(".en.bin", StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogWarning($"The game already loaded {manager.ModelPath} before this mod started. Restart the game to use the Czech model.");
                    Speech.ShowStatus(manager, "Lepší čeština se zapne až po restartu hry.");
                }
            }
        }

        private void OnDestroy() { ParakeetBridge.Stop(); _harmony?.UnpatchSelf(); }

        internal static string FindModel()
        {
            if (Speech.GameForcesCpu)
            {
                string cpuModel = ResolveModel(CpuModel.Value);
                if (cpuModel != null)
                {
                    return cpuModel;
                }
            }

            string configured = ResolveModel(ModelFile.Value);
            if (configured != null)
            {
                return configured;
            }
            if (!string.IsNullOrWhiteSpace(ModelFile.Value))
            {
                Log.LogWarning($"Configured model {ModelFile.Value} does not exist in {ModelFolder}, looking for another one.");
            }

            if (!Directory.Exists(ModelFolder))
            {
                return null;
            }
            foreach (string name in PreferredModels)
            {
                string path = Path.Combine(ModelFolder, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return Directory.GetFiles(ModelFolder, "*.bin")
                .OrderByDescending(f => new FileInfo(f).Length)
                .FirstOrDefault();
        }

        internal static string ResolveModel(string value)
        {
            string name = value?.Trim();
            switch (name?.ToLowerInvariant())
            {
                case "large-v3": name = "ggml-large-v3.bin"; break;
                case "turbo":
                case "large-v3-turbo": name = "ggml-large-v3-turbo.bin"; break;
            }
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            string path = Path.IsPathRooted(name) ? name : Path.Combine(ModelFolder, name);
            return File.Exists(path) ? path : null;
        }
    }
}
