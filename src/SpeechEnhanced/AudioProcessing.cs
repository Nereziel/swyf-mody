using System;

namespace CzechSpeechEnhanced
{
    internal static class Speech
    {
        internal static float[] PrepareForParakeet(float[] samples, bool normalize)
        {
            if (!normalize) return samples;
            var prepared = (float[])samples.Clone();
            NormalizeVolume(prepared);
            return prepared;
        }

        internal static void NormalizeVolume(float[] samples)
        {
            const int window = 320;
            int windows = samples.Length / window;
            if (windows == 0) return;
            var levels = new float[windows];
            float peak = 0f;
            // Include the final incomplete window when measuring the clipping limit.
            foreach (float sample in samples) peak = Math.Max(peak, Math.Abs(sample));
            for (int w = 0; w < windows; w++)
            {
                double sum = 0;
                for (int i = w * window; i < (w + 1) * window; i++)
                    sum += samples[i] * samples[i];
                levels[w] = (float)Math.Sqrt(sum / window);
            }
            Array.Sort(levels);
            float level = levels[(int)(windows * 0.9f)];
            if (level < 1e-5f || peak < 1e-5f) return;
            float gain = Math.Max(0.25f, Math.Min(12f, 0.1f / level));
            gain = Math.Min(gain, 0.97f / peak);
            if (Math.Abs(gain - 1f) < 0.05f) return;
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
        }
    }
}
