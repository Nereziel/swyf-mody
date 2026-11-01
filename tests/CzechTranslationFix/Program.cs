using System;
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

        private static void Main(string[] args)
        {
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
            Console.WriteLine($"PASS: {_checks} parser/template checks. No gameplay exercised.");
        }
    }
}
