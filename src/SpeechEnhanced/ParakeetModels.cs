using System;
using System.IO;

namespace CzechSpeechEnhanced
{
    internal static class ParakeetModels
    {
        internal const string TdtChoice = "Parakeet";
        internal const string UltraChoice = "ParakeetUltra";
        internal static string FirstChoice(string saved, bool initialized, string configured, bool tdt, bool ultra)
        {
            if (initialized) return saved;
            if (configured == "ultra" && ultra) return UltraChoice;
            if (tdt) return TdtChoice;
            return ultra ? UltraChoice : saved;
        }

        internal static string EngineFor(string choice) => choice == TdtChoice ? "parakeet" : choice == UltraChoice ? "ultra" : null;

        internal static string ModelPath(string folder, string engine) => Path.Combine(folder,
            engine == "ultra" ? "parakeet-ultra.q8.gguf" : "parakeet.q8.gguf");

        internal static bool Installed(string folder, string engine)
        {
            if (engine != "parakeet" && engine != "ultra") return false;
            try
            {
                foreach (string name in new[] { "nemo-speech.exe", "nemo_speech_asr.dll", "nemo_speech_asr_c.dll", "ggml.dll", "ggml-base.dll", "ggml-cpu.dll" })
                {
                    var file = new FileInfo(Path.Combine(folder, "bin", name));
                    if (!file.Exists || file.Length == 0) return false;
                }
                return ValidModel(ModelPath(folder, engine), engine == "ultra" ? 714456704L : 713975456L);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        internal static bool ValidModel(string path, long expectedSize)
        {
            try
            {
                var file = new FileInfo(path);
                if (expectedSize < 4 || !file.Exists || file.Length != expectedSize) return false;
                using (var input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    return input.ReadByte() == 'G' && input.ReadByte() == 'G' && input.ReadByte() == 'U' && input.ReadByte() == 'F';
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
