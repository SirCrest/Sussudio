using System;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    public readonly record struct PlaybackDecodeMetrics(
        int SampleCount,
        double AvgMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    public PlaybackDecodeMetrics GetPlaybackDecodeMetrics()
    {
        double[] samples;
        lock (_playbackDecodeLock)
        {
            if (_playbackDecodeDurationCount == 0)
            {
                return new PlaybackDecodeMetrics(0, 0, 0, 0, 0);
            }

            samples = new double[_playbackDecodeDurationCount];
            var oldest = (_playbackDecodeDurationHead - _playbackDecodeDurationCount + _playbackDecodeDurationsMs.Length) % _playbackDecodeDurationsMs.Length;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = _playbackDecodeDurationsMs[(oldest + i) % _playbackDecodeDurationsMs.Length];
            }
        }

        var total = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            total += samples[i];
        }

        Array.Sort(samples);
        return new PlaybackDecodeMetrics(
            samples.Length,
            total / samples.Length,
            PercentileFromSorted(samples, 0.95),
            PercentileFromSorted(samples, 0.99),
            samples[^1]);
    }
}
