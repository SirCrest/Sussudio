using Sussudio.Services.Performance;

namespace Sussudio.Tests;

// Deterministic synthetic audio for the audio-analysis tests. Everything is interleaved 48 kHz float stereo; noise comes
// from a fixed-seed LCG so a failing case reproduces exactly.
internal static class AudioTestSignals
{
    public const int Rate = AudioSignalAnalyzer.DefaultSampleRate;

    public static float[] Silence(int frames) => new float[frames * 2];

    public static float[] Sine(int frames, double frequency, double amplitude)
    {
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = (float)(amplitude * Math.Sin(2 * Math.PI * frequency * frame / Rate));
            samples[frame * 2] = value;
            samples[frame * 2 + 1] = value;
        }

        return samples;
    }

    public static float[] WhiteNoise(int frames, double amplitude, int seed)
    {
        var random = new Lcg(seed);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = (float)(amplitude * random.NextSigned());
            samples[frame * 2] = value;
            samples[frame * 2 + 1] = value;
        }

        return samples;
    }

    /// <summary>Noise whose level steps between segments of 40-300 ms, so its 1 ms envelope is highly distinctive.</summary>
    public static float[] BurstNoise(int frames, int seed)
    {
        var random = new Lcg(seed);
        var levels = new[] { 0.03, 0.12, 0.3, 0.6 };
        var samples = new float[frames * 2];
        var frame = 0;
        while (frame < frames)
        {
            var length = (int)(Rate * (0.04 + 0.26 * random.NextUnit()));
            var level = levels[(int)(random.NextUnit() * levels.Length) % levels.Length];
            for (var index = 0; index < length && frame < frames; index++, frame++)
            {
                var value = (float)(level * random.NextSigned());
                samples[frame * 2] = value;
                samples[frame * 2 + 1] = value;
            }
        }

        return samples;
    }

    /// <summary>One burst-noise period repeated exactly, so every multiple of the period is an equally good lag.</summary>
    public static float[] RepeatedBurstNoise(int frames, int periodFrames, int seed)
    {
        var period = BurstNoise(periodFrames, seed);
        var samples = new float[frames * 2];
        for (var frame = 0; frame < frames; frame++)
        {
            var source = frame % periodFrames;
            samples[frame * 2] = period[source * 2];
            samples[frame * 2 + 1] = period[source * 2 + 1];
        }

        return samples;
    }

    public static float[] Delayed(float[] samples, int delayFrames, float gain)
    {
        var output = new float[samples.Length];
        var frames = samples.Length / 2;
        for (var frame = delayFrames; frame < frames; frame++)
        {
            output[frame * 2] = samples[(frame - delayFrames) * 2] * gain;
            output[frame * 2 + 1] = samples[(frame - delayFrames) * 2 + 1] * gain;
        }

        return output;
    }

    /// <summary>
    /// Splits <paramref name="frames"/> into packets of <paramref name="packetFrames"/>. The start time of packet n is
    /// <c>originHundredNs + n * packetDuration + offsetHundredNs(n)</c>.
    /// </summary>
    public static AudioPacket[] Packets(int frames, int packetFrames, long originHundredNs, Func<int, long>? offsetHundredNs = null, uint flags = 0)
    {
        var count = (frames + packetFrames - 1) / packetFrames;
        var packets = new AudioPacket[count];
        for (var index = 0; index < count; index++)
        {
            var offset = (long)index * packetFrames;
            var packetLength = Math.Min(packetFrames, frames - (int)offset);
            var start = originHundredNs + (long)Math.Round(offset * 10_000_000.0 / Rate) + (offsetHundredNs?.Invoke(index) ?? 0);
            packets[index] = new AudioPacket(offset, packetLength, start, flags);
        }

        return packets;
    }

    public static double Ms(double milliseconds) => milliseconds * 10_000.0;

    public sealed class Lcg
    {
        private uint _state;

        public Lcg(int seed) => _state = (uint)seed * 2654435761u + 12345u;

        private uint Next()
        {
            _state = _state * 1664525u + 1013904223u;
            return _state;
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextUnit() => (Next() >> 8) / (double)(1 << 24);

        /// <summary>Uniform in [-1, 1).</summary>
        public double NextSigned() => NextUnit() * 2.0 - 1.0;
    }
}
