using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CzechTranslationFix
{
    internal static class TranslationFile
    {
        internal static Dictionary<string, Dictionary<string, string>> Read(string path)
        {
            using (var reader = new JsonTextReader(File.OpenText(path)))
            {
                reader.DateParseHandling = DateParseHandling.None;
                var array = JArray.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (reader.Read()) throw new FormatException("Unexpected content after translation array.");
                return Parse(array);
            }
        }

        internal static Dictionary<string, Dictionary<string, string>> Parse(JArray array)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
            {
                if (!(array[i] is JObject row)) throw new FormatException($"Entry {i + 1}: expected an object.");
                var text = row["text"];
                if (text == null) throw new FormatException($"Entry {i + 1}: missing 'text' property.");
                if (text.Type == JTokenType.Null) continue;
                if (text.Type != JTokenType.String) throw new FormatException($"Entry {i + 1}: 'text' must be a string or null.");
                string value = (string)text;
                if (string.IsNullOrEmpty(value)) continue;
                string table = RequiredString(row, "table", i);
                string key = RequiredString(row, "key", i);
                if (!result.TryGetValue(table, out var entries))
                    result.Add(table, entries = new Dictionary<string, string>(StringComparer.Ordinal));
                if (entries.ContainsKey(key)) throw new FormatException($"Duplicate translation: {table}/{key}.");
                entries.Add(key, value);
            }
            return result;
        }

        private static string RequiredString(JObject row, string name, int index)
        {
            var token = row[name];
            if (token == null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)token))
                throw new FormatException($"Entry {index + 1}: '{name}' must be a nonempty string.");
            return (string)token;
        }
    }
}
