using BepInEx.Configuration;

namespace CzechSpeechEnhanced
{
    internal static class SpeechConfiguration
    {
        internal static void Bind(ConfigFile config)
        {
            bool saveOnSet = config.SaveOnConfigSet;
            config.SaveOnConfigSet = false;
            try
            {
                RemoveObsolete(config);
                Plugin.Enabled = config.Bind("General", "Enabled", true,
                    "Zapnout přepis mikrofonu pomocí Parakeetu. Po změně restartuj hru.");
                Plugin.SpeechEngine = config.Bind("Recognition", "Engine", "parakeet",
                    "Model přepisu: parakeet = Parakeet TDT v3, ultra = Parakeet Ultra. Model stáhni přes Mod Hub. Po změně restartuj hru.");
                Plugin.SpeechEngine.Value = (Plugin.SpeechEngine.Value ?? "").Trim().ToLowerInvariant();
                if (Plugin.SpeechEngine.Value != "parakeet" && Plugin.SpeechEngine.Value != "ultra")
                    Plugin.SpeechEngine.Value = "parakeet";

                Plugin.ComputeDevice = config.Bind("Recognition", "ComputeDevice", "auto",
                    "Výpočet Parakeetu: auto = automaticky, gpu = Vulkan, cpu = procesor. Při chybě grafiky mod zkusí procesor. Po změně restartuj hru.");
                Plugin.ComputeDevice.Value = (Plugin.ComputeDevice.Value ?? "").Trim().ToLowerInvariant();
                if (Plugin.ComputeDevice.Value != "gpu" && Plugin.ComputeDevice.Value != "cpu")
                    Plugin.ComputeDevice.Value = "auto";

                Plugin.LivePreview = config.Bind("Recognition", "LivePreview", "on",
                    "Průběžný text během mluvení: on = zapnout, off = vypnout. Pokud náhled zdržuje hotové věty, nastav off.");
                Plugin.LivePreview.Value = (Plugin.LivePreview.Value ?? "").Trim().ToLowerInvariant() == "off" ? "off" : "on";
                Plugin.NormalizeVolume = config.Bind("Audio", "NormalizeVolume", true,
                    "Vyrovnat hlasitost mikrofonu před přepisem Parakeetem.");
                Plugin.SpeechThreshold = config.Bind("Audio", "SpeechThreshold", 0f,
                    "Práh hlasitosti pro začátek řeči (RMS). 0 = ponechat herní hodnotu. Nižší hodnota zachytí tišší řeč, ale také hluk místnosti.");
                Plugin.EndSilenceSeconds = config.Bind("Audio", "EndSilenceSeconds", 1.3f,
                    new ConfigDescription("Délka ticha před dokončením věty v sekundách. Vyšší hodnota dovolí delší pauzy, ale zpomalí odeslání věty.",
                        new AcceptableValueRange<float>(1.3f, 3f)));
                Plugin.LogTranscripts = config.Bind("Debug", "LogTranscripts", false,
                    "Zapisovat rozpoznaný text mikrofonu do BepInEx/LogOutput.log. Jen pro ladění.");
                config.Save();
            }
            finally
            {
                config.SaveOnConfigSet = saveOnSet;
            }
        }

        private static void RemoveObsolete(ConfigFile config)
        {
            // Bind consumes a persisted orphan; Remove then prevents it being saved again.
            Remove(config, "Model", "Model", "CpuModel", "Device", "FlashAttention");
            Remove(config, "Recognition", "Language", "BeamSize", "FastPartials", "InitialPrompt", "CleanTranscript");
            Remove(config, "WhisperFallback", "Model", "CpuModel", "Device", "FlashAttention", "Language", "BeamSize", "FastPartials", "InitialPrompt");
            Remove(config, "Privacy", "BlockAnalytics", "BlockFeedbackDiagnostics");
        }

        private static void Remove(ConfigFile config, string section, params string[] keys)
        {
            foreach (string key in keys)
            {
                var definition = new ConfigDefinition(section, key);
                config.Bind(definition, "");
                config.Remove(definition);
            }
        }
    }
}
