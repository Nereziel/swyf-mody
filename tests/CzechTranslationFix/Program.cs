using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static void ValidateReference(string path)
        {
            var input = (JArray)JToken.Parse(File.ReadAllText(path));
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            string manifestPath = Path.Combine(directory, "manifest.json");
            Check(File.Exists(manifestPath), "Localization reference manifest is missing.");
            var manifest = (JObject)JToken.Parse(File.ReadAllText(manifestPath));
            Check(manifest["total_keys"]?.Type == JTokenType.Integer, "Reference manifest has no total_keys.");
            int expectedTotal = (int)manifest["total_keys"];
            Check(input.Count == expectedTotal, "Incomplete localization reference export.");

            var expectedTables = manifest["tables"] as JObject;
            Check(expectedTables != null, "Reference manifest has no table statistics.");
            var actualTableCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken token in input)
            {
                Check(token is JObject, "Reference export contains a non-object row.");
                var row = (JObject)token;
                Check(row["table"]?.Type == JTokenType.String, "Reference row has no table.");
                Check(row["id"]?.Type == JTokenType.String, "Reference row has no string ID.");
                Check(row["key"]?.Type == JTokenType.String, "Reference row has no key.");
                Check(row["en"]?.Type == JTokenType.String, "Reference row has no English text.");
                Check(row["cs"] != null && (row["cs"].Type == JTokenType.String || row["cs"].Type == JTokenType.Null),
                    "Reference row has invalid Czech text.");

                string table = (string)row["table"];
                actualTableCounts[table] = actualTableCounts.TryGetValue(table, out int tableCount) ? tableCount + 1 : 1;
                string id = table + "\u001f" + (string)row["key"];
                Check(keys.Add(id), "Duplicate reference table/key: " + table + "/" + (string)row["key"]);
            }

            Check(actualTableCounts.Count == expectedTables.Properties().Count(), "Reference table count differs from manifest.");
            foreach (JProperty tableProperty in expectedTables.Properties())
            {
                Check(tableProperty.Value["keys"]?.Type == JTokenType.Integer,
                    "Reference manifest table has no key count: " + tableProperty.Name);
                int expectedKeys = (int)tableProperty.Value["keys"];
                Check(actualTableCounts.TryGetValue(tableProperty.Name, out int actualKeys) && actualKeys == expectedKeys,
                    $"Reference table {tableProperty.Name} has {actualKeys} keys; manifest expects {expectedKeys}.");
            }
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
            if (args.Length != 2)
                throw new ArgumentException("Expected path to localization/original/en-cs.json and CzechTranslationFix directory.");

            string root = Path.Combine(Path.GetTempPath(), "swyf-path-test");
            string fallback = Path.Combine(root, "Mods", "CzechTranslationFix");
            Check(TranslationPaths.ResolveDirectory("", root) == fallback, "Byte-loaded assembly path failed.");
            Check(TranslationPaths.ResolveDirectory(null, root) == fallback, "Null assembly path failed.");
            Check(TranslationPaths.ResolveDirectory(" ", root) == fallback, "Blank assembly path failed.");
            string normal = Path.Combine(root, "BepInEx", "plugins", "translation");
            Check(TranslationPaths.ResolveDirectory(Path.Combine(normal, "CzechTranslationFix.dll"), root) == normal,
                "Normal plugin path changed.");

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

            string referenceDir = Path.Combine(Path.GetTempPath(), "swyf-reference-manifest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(referenceDir);
            try
            {
                string referencePath = Path.Combine(referenceDir, "en-cs.json");
                File.WriteAllText(referencePath,
                    "[{\"table\":\"Menu\",\"id\":\"1\",\"key\":\"one\",\"en\":\"One\",\"cs\":\"Jedna\"}]");
                File.WriteAllText(Path.Combine(referenceDir, "manifest.json"),
                    "{\"total_keys\":1,\"tables\":{\"Menu\":{\"keys\":1}}}");
                ValidateReference(referencePath);
            }
            finally
            {
                Directory.Delete(referenceDir, true);
            }

            ValidateReference(args[0]);
            int runtimeOverrides = ValidateOverrides(args[1]);
            Console.WriteLine($"PASS: {_checks} parser/reference/runtime-override checks. Runtime overrides: {runtimeOverrides}. No gameplay exercised.");
        }
    }
}
