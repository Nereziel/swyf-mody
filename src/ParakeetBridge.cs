using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Whisper;

namespace CzechSpeechEnhanced
{
    internal static class ParakeetBridge
    {
        [ThreadStatic] internal static bool Bypass;
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(60) };
        private static Process process;
        private static string endpoint;
        private static bool disabled;
        private static bool forceCpu;
        private static string Folder => Path.Combine(BepInEx.Paths.GameRootPath, "Mods", Plugin.ModId, "Parakeet");
        private static string Exe => Path.Combine(Folder, "bin", "nemo-speech.exe");
        private static string Engine => (Plugin.SpeechEngine.Value ?? "parakeet").Trim().ToLowerInvariant();
        private static string Device => (Plugin.ComputeDevice.Value ?? "auto").Trim().ToLowerInvariant();
        private static string Model => Path.Combine(Folder, Engine == "ultra" ? "parakeet-ultra.q8.gguf" : "parakeet.q8.gguf");
        private static bool UseNemotron => Engine == "nemotron";
        private static string NemotronFolder => Path.Combine(BepInEx.Paths.GameRootPath, "Mods", Plugin.ModId, "Nemotron");
        private static string NemotronExe => Path.Combine(NemotronFolder, "bin", "parakeet-server.exe");
        private static string NemotronModel => Path.Combine(NemotronFolder, "nemotron-unified.q8.gguf");
        private static string CanaryFolder => Path.Combine(BepInEx.Paths.GameRootPath, "Mods", Plugin.ModId, "Canary");
        private static bool UseCanary => Engine == "canary";
        private static string CanaryPython => Path.Combine(CanaryFolder, "python", "python.exe");
        private static string CanaryScript => Path.Combine(CanaryFolder, "canary_server.py");
        private static string CanaryModel => Path.Combine(CanaryFolder, "model");
        private static string CanaryLanguage
        {
            get
            {
                string value = Plugin.ForcedLanguage ?? "cs";
                return System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z]{2}$") ? value : "cs";
            }
        }
        internal static bool Available => !disabled && (UseCanary ? File.Exists(CanaryPython) && File.Exists(CanaryScript) && File.Exists(Path.Combine(CanaryModel, "encoder-model.int8.onnx")) && File.Exists(Path.Combine(CanaryModel, "decoder-model.int8.onnx")) && File.Exists(Path.Combine(CanaryModel, "config.json")) && File.Exists(Path.Combine(CanaryModel, "vocab.txt")) : UseNemotron ? File.Exists(NemotronExe) && File.Exists(NemotronModel) : (Engine == "parakeet" || Engine == "ultra") && File.Exists(Exe) && File.Exists(Model));

        internal static async Task<WhisperResult> Transcribe(float[] samples, int frequency, int channels, Func<Task<WhisperResult>> fallbackCall)
        {
            try
            {
                await Gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    await EnsureServer().ConfigureAwait(false);
                    if (UseCanary)
                    {
                        using (var audio = new ByteArrayContent(Wav(samples, frequency, channels)))
                        using (var response = await Client.PostAsync(endpoint + "/transcribe", audio).ConfigureAwait(false))
                        {
                            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            response.EnsureSuccessStatusCode();
                            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                            if (data == null || !data.TryGetValue("text", out string value) || value == null) throw new Exception("Missing text in Canary response");
                            data.TryGetValue("device", out string actualDevice);
                            data.TryGetValue("fallback", out string fallbackReason);
                            Plugin.Log.LogInfo("Canary completed transcription; device=" + actualDevice + (string.IsNullOrEmpty(fallbackReason) ? "" : "; GPU fallback=" + fallbackReason));
                            return new WhisperResult(new List<WhisperSegment> { new WhisperSegment(0, value, 0, 0) }, 0);
                        }
                    }
                    using (var form = new MultipartFormDataContent())
                    {
                        var audio = new ByteArrayContent(Wav(samples, frequency, channels));
                        audio.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                        form.Add(audio, "file", "speech.wav");
                        using (var response = await Client.PostAsync(endpoint + "/v1/audio/transcriptions", form).ConfigureAwait(false))
                        {
                            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            response.EnsureSuccessStatusCode();
                            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                            if (data == null || !data.TryGetValue("text", out string text) || text == null) throw new Exception("Missing text in ASR response");
                            Plugin.Log.LogInfo(Engine + " completed transcription.");
                            return new WhisperResult(new List<WhisperSegment> { new WhisperSegment(0, text, 0, 0) }, 0);
                        }
                    }
                }
                finally { Gate.Release(); }
            }
            catch (Exception e)
            {
                if (!forceCpu && Device != "cpu")
                {
                    forceCpu = true;
                    Stop();
                    Plugin.Log.LogWarning(Engine + " failed on the selected device; retrying on CPU: " + e.Message);
                    return await Transcribe(samples, frequency, channels, fallbackCall).ConfigureAwait(false);
                }
                disabled = true;
                Stop();
                Plugin.Log.LogError(Engine + " failed, switching to the game's Whisper: " + e.Message);
                Task<WhisperResult> fallback;
                try { Bypass = true; fallback = fallbackCall(); }
                finally { Bypass = false; }
                return await fallback.ConfigureAwait(false);
            }
        }

        private static async Task EnsureServer()
        {
            if (process != null && !process.HasExited) return;
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
            endpoint = "http://127.0.0.1:" + port;
            string chosen = forceCpu ? "cpu" : Device == "cpu" || Device == "gpu" ? Device : "auto";
            string nemoDevice = chosen == "cpu" ? "cpu" : chosen == "gpu" ? "vulkan:0" : "auto";
            var start = new ProcessStartInfo {
                FileName = UseCanary ? CanaryPython : UseNemotron ? NemotronExe : Exe,
                WorkingDirectory = UseCanary ? CanaryFolder : UseNemotron ? Path.GetDirectoryName(NemotronExe) : Path.GetDirectoryName(Exe),
                Arguments = UseCanary ? "\"" + CanaryScript + "\" --port " + port + " --model \"" + CanaryModel + "\" --language " + CanaryLanguage + " --device " + chosen : UseNemotron ? "--host 127.0.0.1 --port " + port + " --model \"" + NemotronModel + "\"" : "serve --host 127.0.0.1 --port " + port + " --no-ui --no-warmup --device " + nemoDevice + " --asr-model \"" + Model + "\"",
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
            };
            if (UseNemotron)
            {
                if (chosen == "auto") start.EnvironmentVariables.Remove("PARAKEET_DEVICE");
                else start.EnvironmentVariables["PARAKEET_DEVICE"] = chosen == "cpu" ? "cpu" : "Vulkan0";
            }
            process = Process.Start(start);
            if (process == null) throw new Exception("Could not start " + Engine + " server");
            for (int i = 0; i < 120; i++)
            {
                if (process.HasExited) throw new Exception(Engine + " server exited: " + process.ExitCode);
                try { using (var r = await Client.GetAsync(endpoint + "/health").ConfigureAwait(false)) if (r.IsSuccessStatusCode) { string detail = await r.Content.ReadAsStringAsync().ConfigureAwait(false); Plugin.Log.LogInfo(Engine + " server ready; requested device=" + chosen + "; health=" + detail); return; } }
                catch (HttpRequestException) { }
                await Task.Delay(500).ConfigureAwait(false);
            }
            throw new TimeoutException(Engine + " server did not start");
        }

        private static byte[] Wav(float[] samples, int rate, int channels)
        {
            if (rate <= 0 || channels <= 0) throw new ArgumentException("Invalid audio format");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                int frames = samples.Length / channels;
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + frames * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(frames * 2);
                for (int i = 0; i < frames; i++) { float v = 0; for (int c = 0; c < channels; c++) v += samples[i * channels + c] / channels; writer.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32767)); }
                return stream.ToArray();
            }
        }

        internal static void Stop()
        {
            try { if (process != null && !process.HasExited) process.Kill(); } catch (Exception) { }
            if (process != null) process.Dispose(); process = null;
        }
    }
}
