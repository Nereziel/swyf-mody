using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UIElements;

namespace CzechSpeechEnhanced
{
    internal static class SpeechMenu
    {
        internal static string Folder => Path.Combine(Paths.GameRootPath, "Mods", Plugin.ModId, "Parakeet");
        internal static bool Installed(string engine) => ParakeetModels.Installed(Folder, engine);
        internal static string SelectedEngine => ParakeetModels.EngineFor(WindowsSpeechCompatibility.RecognitionPreference);

        internal static void Initialize(ConfigFile config)
        {
            var initialized = config.Bind("Recognition", "MenuInitialized", false,
                "První automatický výběr Parakeetu v herním menu už proběhl. Další spuštění zachovají volbu hráče.");
            string current = WindowsSpeechCompatibility.RecognitionPreference;
            string choice = ParakeetModels.FirstChoice(current, initialized.Value, Plugin.SpeechEngine.Value,
                Installed("parakeet"), Installed("ultra"));
            WindowsSpeechCompatibility.RecognitionPreference = choice;
            initialized.Value = true;
            config.Save();
            WindowsSpeechCompatibility.RecognitionPreferenceChanged += Changed;
            Changed();
        }

        internal static void Dispose() => WindowsSpeechCompatibility.RecognitionPreferenceChanged -= Changed;

        private static void Changed()
        {
            string engine = SelectedEngine;
            if (engine != null) Plugin.SpeechEngine.Value = engine;
            Plugin.Log.LogInfo("Speech recognition selected: " + WindowsSpeechCompatibility.RecognitionPreference);
            ParakeetBridge.SelectionChanged();
        }

        internal static string Label(string choice)
        {
            string engine = ParakeetModels.EngineFor(choice);
            string name = engine == "ultra" ? "Parakeet Ultra" : "Parakeet TDT v3";
            return Installed(engine) ? name : name + " (není stažený / neúplný; Mod Hub)";
        }

        internal static void AddChoices(DropdownField dropdown)
        {
            if (!dropdown.choices.Contains(ParakeetModels.TdtChoice)) dropdown.choices.Add(ParakeetModels.TdtChoice);
            if (!dropdown.choices.Contains(ParakeetModels.UltraChoice)) dropdown.choices.Add(ParakeetModels.UltraChoice);
        }
    }

    [HarmonyPatch]
    internal static class RecognitionPreferenceNormalizePatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(WindowsSpeechCompatibility), "NormalizeRecognitionPreference");
        private static bool Prefix(string __0, ref string __result)
        {
            string engine = ParakeetModels.EngineFor(__0);
            if (engine == null) return true;
            __result = SpeechMenu.Installed(engine) ? __0 : "WhisperSmall";
            return false;
        }
    }

    [HarmonyPatch]
    internal static class RecognitionDropdownBindPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("PauseMenuToolkitController"), "BindSpeechRecognition");
        private static void Postfix(object __instance)
        {
            var root = AccessTools.Field(__instance.GetType(), "root").GetValue(__instance) as VisualElement;
            var dropdown = root?.Q<DropdownField>("speech-recognition-dropdown");
            if (dropdown == null) return;
            SpeechMenu.AddChoices(dropdown);
            var listFormat = dropdown.formatListItemCallback;
            var selectedFormat = dropdown.formatSelectedValueCallback;
            dropdown.formatListItemCallback = value => ParakeetModels.EngineFor(value) != null ? SpeechMenu.Label(value) : listFormat(value);
            dropdown.formatSelectedValueCallback = value => ParakeetModels.EngineFor(value) != null ? SpeechMenu.Label(value) : selectedFormat(value);
            dropdown.SetValueWithoutNotify(WindowsSpeechCompatibility.RecognitionPreference);
            Plugin.Log.LogInfo("Parakeet choices added to the game's speech recognition dropdown.");
        }
    }

    [HarmonyPatch]
    internal static class RecognitionDropdownMenuPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("PauseMenuToolkitController"), "CreateSpeechRecognitionMenu");
        private static void Prefix(DropdownField __0)
        {
            // Preserve the original menu, including its unavailable Gladia row/tooltips.
            // Add our rows separately so missing downloads cannot be selected.
            __0.choices.Remove(ParakeetModels.TdtChoice);
            __0.choices.Remove(ParakeetModels.UltraChoice);
        }

        private static void Postfix(DropdownField __0, GenericDropdownMenu __result)
        {
            SpeechMenu.AddChoices(__0);
            foreach (string choice in new[] { ParakeetModels.TdtChoice, ParakeetModels.UltraChoice })
            {
                bool selected = WindowsSpeechCompatibility.RecognitionPreference == choice;
                string engine = ParakeetModels.EngineFor(choice);
                if (!SpeechMenu.Installed(engine)) __result.AddDisabledItem(SpeechMenu.Label(choice), selected);
                else
                {
                    string captured = choice;
                    __result.AddItem(SpeechMenu.Label(choice), selected, () =>
                    {
                        if (WindowsSpeechCompatibility.RecognitionPreference == captured)
                        {
                            Plugin.Log.LogInfo("Parakeet retry requested from recognition menu.");
                            ParakeetBridge.SelectionChanged();
                        }
                        else __0.value = captured;
                    });
                }
            }
        }

        private static void Finalizer(DropdownField __0) => SpeechMenu.AddChoices(__0);
    }
}
