using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using UnityEngine.Localization.Tables;

namespace CzechTranslationFix
{
    [BepInPlugin("nereziel.swyf.czechtranslationfix", "Czech Translation Fix", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private static Plugin _instance;
        private readonly ConditionalWeakTable<StringTable, object> _processed = new ConditionalWeakTable<StringTable, object>();
        private Dictionary<string, Dictionary<string, string>> _translations;
        private Harmony _harmony;
        private static readonly FieldInfo FormatCache = AccessTools.Field(typeof(StringTableEntry), "m_FormatCache");

        private void Awake()
        {
            string directory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
            if (string.IsNullOrEmpty(directory)) directory = Path.Combine(Paths.GameRootPath, "Mods", "CzechTranslationFix");
            string file = Path.Combine(directory, "translations.json");
            try
            {
                _translations = TranslationFile.Read(file);
                int count = 0;
                foreach (var table in _translations.Values) count += table.Count;
                Logger.LogInfo($"Czech Translation Fix: loaded {count} overrides from {file}. Restart game after editing.");
                if (count == 0) return;
                if (FormatCache == null) throw new MissingFieldException(typeof(StringTableEntry).FullName, "m_FormatCache");
                _instance = this;
                _harmony = new Harmony("nereziel.swyf.czechtranslationfix");
                // This generic method may share code with asset tables on Mono.
                // Accept object and explicitly guard StringTable in the prefix.
                var target = AccessTools.DeclaredMethod(typeof(DetailedLocalizationTable<StringTableEntry>), "GetEntry", new[] { typeof(long) });
                if (target == null) throw new MissingMethodException("StringTable.GetEntry(long)");
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(Plugin), nameof(BeforeGetEntry)));
            }
            catch (Exception ex)
            {
                _instance = null;
                _harmony?.UnpatchSelf();
                Logger.LogError($"Translation overrides disabled. Original localization retained. File: {file}\n{ex}");
            }
        }

        private static void BeforeGetEntry(object __instance)
        {
            if (_instance != null && __instance is StringTable table) _instance.Apply(table);
        }

        private void Apply(StringTable table)
        {
            // Run before entry lookup, including missing entries, so Unity's fallback
            // and smart-string formatting see the new Czech value normally.
            string locale = table.LocaleIdentifier.Code;
            if (!string.Equals(locale, "cs", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(locale, "cs-CZ", StringComparison.OrdinalIgnoreCase)) return;
            if (table.SharedData == null || _processed.TryGetValue(table, out _)) return;
            _processed.Add(table, new object());
            if (!_translations.TryGetValue(table.TableCollectionName, out var entries)) return;
            try
            {
                int applied = 0;
                foreach (var pair in entries)
                {
                    var shared = table.SharedData.GetEntry(pair.Key);
                    if (shared == null)
                    {
                        Logger.LogWarning($"Unknown translation key: {table.TableCollectionName}/{pair.Key}; skipped.");
                        continue;
                    }
                    var entry = table.AddEntry(shared.Id, pair.Value);
                    // AddEntry preserves metadata but does not clear parsed smart strings.
                    FormatCache.SetValue(entry, null);
                    applied++;
                }
                Logger.LogInfo($"Czech Translation Fix: applied {applied} overrides to {table.TableCollectionName} ({locale}).");
            }
            catch (Exception ex)
            {
                // Never let an override failure break the game's localization call.
                Logger.LogError($"Could not finish applying translations to {table.TableCollectionName}: {ex}");
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            _harmony?.UnpatchSelf();
        }
    }
}
