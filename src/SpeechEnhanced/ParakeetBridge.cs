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
        private static readonly SerialWork Work = new SerialWork();
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(60) };
        private static Process process;
        private static string endpoint;
        private static volatile string disabledEngine;
        private static string processKey;
        private static string cpuEngine;
        private static volatile bool stopping;

        private static string Folder => Path.Combine(BepInEx.Paths.GameRootPath, "Mods", Plugin.ModId, "Parakeet");
        private static string Exe => Path.Combine(Folder, "bin", "nemo-speech.exe");
        private static string Device => (Plugin.ComputeDevice.Value ?? "auto").Trim().ToLowerInvariant();

        internal static bool Available
        {
            get
            {
                string engine = SpeechMenu.SelectedEngine;
                return !stopping && engine != null && disabledEngine != engine && SpeechMenu.Installed(engine);
            }
        }

        internal static async Task<WhisperResult> Transcribe(float[] samples, int frequency, int channels, Func<Task<WhisperResult>> fallbackCall)
        {
            // Snapshot before awaiting: queued requests retain the model they were issued for.
            string engine = SpeechMenu.SelectedEngine;
            WhisperResult result = await Work.Run(async () =>
            {
                if (stopping || engine == null || disabledEngine == engine) return null;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    if (stopping) return null;
                    bool cpu = Device == "cpu" || cpuEngine == engine || attempt == 1;
                    try
                    {
                        await EnsureServer(engine, cpu).ConfigureAwait(false);
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
                                if (data == null || !data.TryGetValue("text", out string text) || text == null)
                                    throw new Exception("Missing text in ASR response");
                                Plugin.Log.LogInfo(engine + " completed transcription.");
                                return new WhisperResult(new List<WhisperSegment> { new WhisperSegment(0, text, 0, 0) }, 0);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        StopCore();
                        if (stopping) return null;
                        if (!cpu)
                        {
                            cpuEngine = engine;
                            Plugin.Log.LogWarning(engine + " failed on selected device; retrying on CPU: " + e.Message);
                            continue;
                        }
                        disabledEngine = engine;
                        Plugin.Log.LogError(engine + " failed; using native Whisper for this request. Reselect Parakeet to retry: " + e.Message);
                        break;
                    }
                }
                return null;
            }).ConfigureAwait(false);
            if (result != null) return result;
            if (stopping) return new WhisperResult(new List<WhisperSegment>(), 0);
            Task<WhisperResult> fallback;
            try { Bypass = true; fallback = fallbackCall(); }
            finally { Bypass = false; }
            return await fallback.ConfigureAwait(false);
        }

        private static async Task EnsureServer(string engine, bool forceCpu)
        {
            string chosen = forceCpu ? "cpu" : Device == "gpu" ? "gpu" : "auto";
            string key = engine + ":" + chosen;
            if (process != null && !process.HasExited && processKey == key) return;
            StopCore();
            if (!SpeechMenu.Installed(engine)) throw new FileNotFoundException("Parakeet model or runtime is incomplete: " + engine);

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            endpoint = "http://127.0.0.1:" + port;

            string nemoDevice = chosen == "cpu" ? "cpu" : chosen == "gpu" ? "vulkan:0" : "auto";

            var start = new ProcessStartInfo
            {
                FileName = Exe,
                WorkingDirectory = Path.GetDirectoryName(Exe),
                Arguments = "serve --host 127.0.0.1 --port " + port +
                            " --no-ui --no-warmup --device " + nemoDevice +
                            " --asr-model \"" + ParakeetModels.ModelPath(Folder, engine) + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            process = Process.Start(start);
            processKey = key;
            if (process == null) throw new Exception("Could not start " + engine + " server");
            if (stopping) { StopCore(); throw new OperationCanceledException("Game is shutting down."); }

            for (int i = 0; i < 120; i++)
            {
                if (process.HasExited) throw new Exception(engine + " server exited: " + process.ExitCode);
                try
                {
                    using (var r = await Client.GetAsync(endpoint + "/health").ConfigureAwait(false))
                    {
                        if (r.IsSuccessStatusCode)
                        {
                            string detail = await r.Content.ReadAsStringAsync().ConfigureAwait(false);
                            Plugin.Log.LogInfo(engine + " server ready; requested device=" + chosen + "; health=" + detail);
                            return;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                }
                await Task.Delay(500).ConfigureAwait(false);
            }

            throw new TimeoutException(engine + " server did not start");
        }

        private static byte[] Wav(float[] samples, int rate, int channels)
        {
            if (rate <= 0 || channels <= 0) throw new ArgumentException("Invalid audio format");
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int frames = samples.Length / channels;
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + frames * 2);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(rate);
                writer.Write(rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(frames * 2);

                for (int i = 0; i < frames; i++)
                {
                    float v = 0;
                    for (int c = 0; c < channels; c++) v += samples[i * channels + c] / channels;
                    writer.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32767));
                }
                return stream.ToArray();
            }
        }

        internal static void Stop()
        {
            stopping = true;
            _ = Work.Run(() => { StopCore(); return Task.FromResult(0); });
        }

        internal static void Activate() => stopping = false;

        internal static void Quit()
        {
            // Application shutdown cannot wait for an outstanding HTTP request.
            stopping = true;
            StopCore();
        }

        internal static void SelectionChanged()
        {
            _ = Work.AfterCurrent(() =>
            {
                // Reset after the in-flight request; its failure must not undo a new choice.
                disabledEngine = null;
                cpuEngine = null;
                if (SpeechMenu.SelectedEngine == null) StopCore();
            });
        }

        private static void StopCore()
        {
            Process old = Interlocked.Exchange(ref process, null);
            processKey = null;
            try
            {
                if (old != null && !old.HasExited) old.Kill();
            }
            catch (Exception)
            {
            }

            old?.Dispose();
        }
    }
}
