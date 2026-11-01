using System;
using System.IO;

namespace CzechTranslationFix
{
    internal static class TranslationPaths
    {
        internal static string ResolveDirectory(string assemblyLocation, string gameRoot)
        {
            // Mod Hub loads assemblies from bytes. Mono rejects GetDirectoryName("").
            if (!string.IsNullOrWhiteSpace(assemblyLocation))
            {
                string directory = Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(directory)) return directory;
            }
            return Path.Combine(gameRoot, "Mods", "CzechTranslationFix");
        }
    }
}
