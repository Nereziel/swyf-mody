using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CzechTranslationFix
{
    internal static class Program
    {
        private static int _checks;

        private static void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            _checks++;
        }

        private static void Reject(string json)
        {
            bool rejected = false;
            try { TranslationFile.Parse((JArray)JToken.Parse(json)); }
            catch (FormatException) { rejected = true; }
            Check(rejected, "Invalid input accepted: " + json);
        }

        private static int ValidateOverrides(string directory)
        {
            string[] files = Directory.GetFiles(directory, "overrides*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            Check(files.Length > 0, "No runtime overrides*.json files found.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            foreach (string file in files)
            {
                var loaded = TranslationFile.Read(file);
                foreach (var tablePair in loaded)
                {
                    foreach (var entryPair in tablePair.Value)
                    {
                        string id = tablePair.Key + "\u001f" + entryPair.Key;
                        Check(seen.Add(id),
                            $"Duplicate runtime override across files: {tablePair.Key}/{entryPair.Key} ({Path.GetFileName(file)})." );
                        count++;
                    }
                }
            }

            Check(count > 0, "Runtime override files contain no active translations.");
            return count;
        }

        private static void Main(string[] args)
        {
            if (args.Length != 1) throw new ArgumentException("Expected path to translations.json.");

            string root = Path.Combine(Path.GetTempPath(), "swyf-path-test");
            string fallback = Path.Combine(root, "Mods", "CzechTranslationFix");
            Check(TranslationPaths.ResolveDirectory("", root) == fallback, "Byte-loaded assembly path failed.");
            Check(TranslationPaths.ResolveDirectory(null, root) == fallback, "Null assembly path failed.");
            Check(TranslationPaths.ResolveDirectory(" ", root) == fallback, "Blank assembly path failed.");
            string normal = Path.Combine(root, "BepInEx", "plugins", "translation");
            Check(TranslationPaths.ResolveDirectory(Path.Combine(normal, "CzechTranslationFix.dll"), root) == normal,
                "Normal plugin path changed.");

            var template = TranslationFile.Read(args[0]);
            Check(template.Count == 0, "Shipped template must not change any translation.");

            var data = TranslationFile.Parse((JArray)JToken.Parse("""
                [
                  {"table":"Menu","key":"menu.play","text":"Žluťoučký {0}\n<color=red>kůň</color>","original_cs":"HRÁT"},
                  {"table":"Controls","key":"Turn Off","text":"Vypnout"},
                  {"table":"Menu","key":"ignored","text":null},
                  {"table":"Menu","key":"alsoIgnored","text":""}
                ]
                """));
            Check(data.Count == 2 && data["Menu"].Count == 1, "Partial overrides or ignored entries wrong.");
            Check(data["Menu"]["menu.play"] == "Žluťoučký {0}\n<color=red>kůň</color>", "Unicode or formatting changed.");
            Check(data["Controls"]["Turn Off"] == "Vypnout", "Missing Czech table entries unsupported.");
            Reject("[{\"table\":\"Menu\",\"key\":\"a\",\"text\":\"x\"},{\"table\":\"Menu\",\"key\":\"a\",\"text\":\"y\"}]");
            Reject("[{\"table\":\"Menu\",\"key\":\"a\",\"text\":42}]");
            Reject("[{\"table\":\"Menu\",\"key\":\"a\",\"cs\":\"x\"}]");
            Reject("[{\"table\":\"\",\"key\":\"a\",\"text\":\"x\"}]");
            Reject("[null]");

            var input = (JArray)JToken.Parse(File.ReadAllText(args[0]));
            Check(input.Count == 3821, "Incomplete template.");
            foreach (JObject row in input)
            {
                Check(row["text"].Type == JTokenType.Null && row["original_en"].Type == JTokenType.String,
                    "Template lost original English or has a prefilled translation.");
            }

            string temp = Path.Combine(AppContext.BaseDirectory, "invalid.json");
            foreach (string invalid in new[] {
                "[{\"table\":\"Menu\",\"key\":\"a\",\"text\":\"x\",\"text\":\"y\"}]",
                "[] []", "[", "{}"
            })
            {
                File.WriteAllText(temp, invalid);
                bool rejected = false;
                try { TranslationFile.Read(temp); }
                catch (Exception) { rejected = true; }
                Check(rejected, "Malformed file accepted.");
            }
            File.Delete(temp);

            string duplicateDir = Path.Combine(Path.GetTempPath(), "swyf-override-duplicate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(duplicateDir);
            try
            {
                File.WriteAllText(Path.Combine(duplicateDir, "overrides.a.json"),
                    "[{\"table\":\"Menu\",\"key\":\"same\",\"text\":\"A\"}]");
                File.WriteAllText(Path.Combine(duplicateDir, "overrides.b.json"),
                    "[{\"table\":\"Menu\",\"key\":\"same\",\"text\":\"B\"}]");
                bool duplicateRejected = false;
                try { ValidateOverrides(duplicateDir); }
                catch (Exception ex) when (ex.Message.StartsWith("Duplicate runtime override across files:", StringComparison.Ordinal))
                {
                    duplicateRejected = true;
                }
                Check(duplicateRejected, "Cross-file duplicate runtime override was accepted.");
            }
            finally
            {
                Directory.Delete(duplicateDir, true);
            }

            string overrideDirectory = Path.GetDirectoryName(Path.GetFullPath(args[0]));
            int runtimeOverrides = ValidateOverrides(overrideDirectory);
            Console.WriteLine($"PASS: {_checks} parser/template/runtime-override checks. Runtime overrides: {runtimeOverrides}. No gameplay exercised.");
        }
    }
}
