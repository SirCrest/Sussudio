using System;
using System.Collections.Generic;

namespace Sussudio.Services.Performance;

/// <summary>
/// Result of cross-correlating the audio the app received from its capture device with the audio the Windows mixer
/// received from the app. <see cref="Status"/> is <c>ok</c>, <c>insufficient-signal</c> (the evidence cannot support a
/// latency: silence, low confidence, an ambiguous or edge-of-window peak) or <c>unavailable</c> (an input or timestamps
/// were missing). Latency and gain are null unless the status is <c>ok</c>.
/// </summary>
public sealed record AudioLatencyResult(
    string Status,
    string? Reason,
    double? LatencyMs,
    double? CoarseLatencyMs,
    string? Resolution,
    double Confidence,
    double? SecondPeakRatio,
    double? GainDifferenceDb,
    double InputPeakDbfs,
    double OutputPeakDbfs,
    double OverlapSeconds,
    double InputTimelineSpreadMs,
    string Scope);

/// <summary>
/// Latency between the capture-delivery tap and the process loopback stream. Both streams are laid on a common
/// 48 kHz QPC timeline, reduced to 1 ms RMS envelopes, and cross-correlated over lags -20 ms to +1000 ms; the best
/// lag is then refined at sample level when the raw waveforms correlate. Pure and allocation-bounded by the capture
/// length; it runs on a worker thread after the capture finished, never on an audio thread.
/// Contract: docs/architecture/performance-telemetry.md section 2.9.
/// </summary>
public static class AudioLatencyAnalyzer
{
    /// <summary>What the measured delay does and does not include. Echoed in every result.</summary>
    public const string MeasurementScope =
        "capture-delivery-to-mixer: from a capture packet being handed to the app until the same audio appears in the app's " +
        "process loopback stream; excludes device input latency and endpoint output latency";

    public const double MinLagMs = -20.0;
    public const double MaxLagMs = 1000.0;
    public const double SilentPeakDbfs = -60.0;

    /// <summary>Minimum envelope Pearson correlation at the best lag.</summary>
    public const double MinConfidence = 0.6;

    /// <summary>The second-best distinct peak may be at most this fraction of the best, or the delay is ambiguous.</summary>
    public const double MaxSecondPeakRatio = 0.9;

    public const int MinOverlapBins = 200;

    /// <summary>
    /// A lag is a separate peak from the best one only if the correlation dips below this fraction of its value on the
    /// way from the best lag. The slope of the best peak itself (broad for level steps that last 100 ms) never dips.
    /// </summary>
    public const double PeakSeparationDip = 0.8;

    public const double RefineRadiusMs = 3.0;
    public const double RefineWindowSeconds = 10.0;
    public const double RefineMinCorrelation = 0.5;
    public const double OutputSnapMs = 1.5;

    private const int MinRefineWindowFrames = 4_800;

    public static AudioLatencyResult Measure(
        ReadOnlySpan<float> inputSamples,
        ReadOnlySpan<AudioPacket> inputPackets,
        ReadOnlySpan<float> outputSamples,
        ReadOnlySpan<AudioPacket> outputPackets,
        int sampleRate = AudioSignalAnalyzer.DefaultSampleRate)
    {
        if (inputPackets.Length == 0 || inputSamples.Length < 2)
        {
            return Unavailable("The capture-delivery tap recorded no audio (is a program capture with preview audio running?).");
        }

        if (outputPackets.Length == 0 || outputSamples.Length < 2)
        {
            return Unavailable("The process loopback stream delivered no audio.");
        }

        long t0 = long.MaxValue;
        foreach (var packet in inputPackets)
        {
            if (packet.StartTime100Ns <= 0)
            {
                return Unavailable("The input tap packets carry no QPC timestamps.");
            }

            t0 = Math.Min(t0, packet.StartTime100Ns);
        }

        foreach (var packet in outputPackets)
        {
            if (packet.StartTime100Ns <= 0)
            {
                return Unavailable("The process loopback packets carry no QPC timestamps; the streams cannot be aligned.");
            }

            t0 = Math.Min(t0, packet.StartTime100Ns);
        }

        // Input: the capture delivers a gapless stream and each packet's start time is its drain time minus its duration,
        // so every packet's offset from the sample clock is the true offset plus a non-negative delivery delay. The
        // smallest offset is the least-delayed delivery and anchors the whole stream; the spread is reported as the
        // timeline quality.
        var anchorSeconds = double.MaxValue;
        var maxOffsetSeconds = double.MinValue;
        foreach (var packet in inputPackets)
        {
            var offset = (packet.StartTime100Ns - t0) / (double)QpcMath.HundredNsPerSecond - packet.FrameOffset / (double)sampleRate;
            anchorSeconds = Math.Min(anchorSeconds, offset);
            maxOffsetSeconds = Math.Max(maxOffsetSeconds, offset);
        }

        var inputFrames = inputSamples.Length / 2;
        var inputStart = (long)Math.Round(anchorSeconds * sampleRate);
        var inputEnd = inputStart + inputFrames;
        var spreadMs = (maxOffsetSeconds - anchorSeconds) * 1000.0;

        // Output: engine timestamps are placed as given, except that a packet that starts within the snap tolerance of
        // the previous packet's end continues it sample-exactly.
        var snapFrames = Math.Max(1, (int)Math.Round(sampleRate * OutputSnapMs / 1000.0));
        var outputStarts = new long[outputPackets.Length];
        long outputStart = long.MaxValue;
        long outputEnd = long.MinValue;
        var cursor = long.MinValue;
        for (var index = 0; index < outputPackets.Length; index++)
        {
            var packet = outputPackets[index];
            var start = (long)Math.Round((packet.StartTime100Ns - t0) / (double)QpcMath.HundredNsPerSecond * sampleRate);
            if (cursor != long.MinValue && Math.Abs(start - cursor) <= snapFrames)
            {
                start = cursor;
            }

            outputStarts[index] = start;
            cursor = start + packet.Frames;
            outputStart = Math.Min(outputStart, start);
            outputEnd = Math.Max(outputEnd, cursor);
        }

        var origin = Math.Min(inputStart, outputStart);
        var length = checked((int)(Math.Max(inputEnd, outputEnd) - origin));
        var inputMono = new float[length];
        var outputMono = new float[length];
        PlaceMono(inputMono, inputSamples, 0, inputFrames, inputStart - origin);
        for (var index = 0; index < outputPackets.Length; index++)
        {
            var packet = outputPackets[index];
            PlaceMono(outputMono, outputSamples, packet.FrameOffset, packet.Frames, outputStarts[index] - origin);
        }

        var inputSpan = (Start: inputStart - origin, End: inputEnd - origin);
        var outputSpan = (Start: outputStart - origin, End: outputEnd - origin);
        var inputPeakDb = AudioSignalAnalyzer.ToDbfs(Peak(inputMono, inputSpan.Start, inputSpan.End));
        var outputPeakDb = AudioSignalAnalyzer.ToDbfs(Peak(outputMono, outputSpan.Start, outputSpan.End));
        if (inputPeakDb < SilentPeakDbfs)
        {
            return Insufficient("The input is silent: there is no program audio to correlate.", 0, null, inputPeakDb, outputPeakDb, spreadMs);
        }

        if (outputPeakDb < SilentPeakDbfs)
        {
            return Insufficient("The output is silent: the input had audio but none reached the process loopback stream.", 0, null, inputPeakDb, outputPeakDb, spreadMs);
        }

        var binSamples = Math.Max(1, sampleRate / 1000);
        var inputEnvelope = Envelope(inputMono, binSamples);
        var outputEnvelope = Envelope(outputMono, binSamples);
        var inputBins =((int)((inputSpan.Start + binSamples - 1) / binSamples), (int)(inputSpan.End / binSamples));
        var outputBins = ((int)((outputSpan.Start + binSamples - 1) / binSamples), (int)(outputSpan.End / binSamples));
        var minLagBins = (int)Math.Floor(MinLagMs * sampleRate / 1000.0 / binSamples);
        var maxLagBins = (int)Math.Ceiling(MaxLagMs * sampleRate / 1000.0 / binSamples);

        var correlations = CorrelateEnvelopes(inputEnvelope, outputEnvelope, inputBins, outputBins, minLagBins, maxLagBins);
        var bestIndex = -1;
        var best = 0.0;
        for (var index = 0; index < correlations.Length; index++)
        {
            if (correlations[index] > best)
            {
                best = correlations[index];
                bestIndex = index;
            }
        }

        if (bestIndex < 0)
        {
            return Insufficient("The streams overlap for too little time (or have no level variation) to correlate.", 0, null, inputPeakDb, outputPeakDb, spreadMs);
        }

        var second = Math.Max(
            SecondPeak(correlations, bestIndex, 1),
            SecondPeak(correlations, bestIndex, -1));

        var bestLagBins = minLagBins + bestIndex;
        var ratio = best > 0 ? second / best : 1.0;
        var confidence = Math.Round(best, 4);
        if (best < MinConfidence)
        {
            return Insufficient($"Low confidence: the best envelope correlation is {best:F2} (needs {MinConfidence:F2}); use program audio with level changes.", confidence, ratio, inputPeakDb, outputPeakDb, spreadMs);
        }

        if (bestIndex == 0 || bestIndex == correlations.Length - 1)
        {
            return Insufficient($"The correlation peak sits at the edge of the {MinLagMs:F0} to {MaxLagMs:F0} ms search window, so the true delay may lie outside it.", confidence, ratio, inputPeakDb, outputPeakDb, spreadMs);
        }

        if (ratio > MaxSecondPeakRatio)
        {
            return Insufficient($"Ambiguous: a second lag correlates at {ratio:P0} of the best (repetitive program audio); use non-repeating audio.", confidence, ratio, inputPeakDb, outputPeakDb, spreadMs);
        }

        var coarseLag = bestLagBins * binSamples;
        var lag = coarseLag;
        var resolution = "envelope-1ms";
        var radius = Math.Max(1, (int)Math.Round(sampleRate * RefineRadiusMs / 1000.0));
        if (TryRefineLag(inputMono, outputMono, inputSpan, outputSpan, coarseLag, radius, sampleRate, out var refined))
        {
            lag = refined;
            resolution = "sample";
        }

        var gainStart = Math.Max(inputSpan.Start, outputSpan.Start - lag);
        var gainEnd = Math.Min(inputSpan.End, outputSpan.End - lag);
        double? gainDb = null;
        var overlapSeconds = 0.0;
        if (gainEnd > gainStart)
        {
            overlapSeconds = (gainEnd - gainStart) / (double)sampleRate;
            double inputSquares = 0, outputSquares = 0;
            for (var t = gainStart; t < gainEnd; t++)
            {
                var a = inputMono[t];
                var b = outputMono[t + lag];
                inputSquares += (double)a * a;
                outputSquares += (double)b * b;
            }

            var count = gainEnd - gainStart;
            var inputRms = Math.Sqrt(inputSquares / count);
            var outputRms = Math.Sqrt(outputSquares / count);
            if (inputRms > 1e-6 && outputRms > 1e-6)
            {
                gainDb = Math.Round(AudioSignalAnalyzer.ToDbfs(outputRms) - AudioSignalAnalyzer.ToDbfs(inputRms), 2);
            }
        }

        return new AudioLatencyResult(
            "ok",
            null,
            Math.Round(lag * 1000.0 / sampleRate, 3),
            Math.Round(coarseLag * 1000.0 / sampleRate, 3),
            resolution,
            confidence,
            Math.Round(ratio, 4),
            gainDb,
            Math.Round(inputPeakDb, 2),
            Math.Round(outputPeakDb, 2),
            Math.Round(overlapSeconds, 3),
            Math.Round(spreadMs, 3),
            MeasurementScope);
    }

    // Highest correlation among the lags on one side of the best peak that are separated from it by a real valley.
    private static double SecondPeak(double[] correlations, int bestIndex, int direction)
    {
        var second = 0.0;
        var valley = correlations[bestIndex];
        for (var index = bestIndex + direction; index >= 0 && index < correlations.Length; index += direction)
        {
            var value = correlations[index];
            if (value < valley)
            {
                valley = value;
            }

            if (valley <= value * PeakSeparationDip && value > second)
            {
                second = value;
            }
        }

        return second;
    }

    private static AudioLatencyResult Unavailable(string reason) =>
        new("unavailable", reason, null, null, null, 0, null, null, AudioSignalAnalyzer.FloorDbfs, AudioSignalAnalyzer.FloorDbfs, 0, 0, MeasurementScope);

    private static AudioLatencyResult Insufficient(string reason, double confidence, double? ratio, double inputPeakDb, double outputPeakDb, double spreadMs) =>
        new("insufficient-signal", reason, null, null, null, confidence, ratio is null ? null : Math.Round(ratio.Value, 4), null,
            Math.Round(inputPeakDb, 2), Math.Round(outputPeakDb, 2), 0, Math.Round(spreadMs, 3), MeasurementScope);

    private static void PlaceMono(float[] destination, ReadOnlySpan<float> interleaved, long frameOffset, int frames, long position)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            var index = position + frame;
            if (index < 0 || index >= destination.Length)
            {
                continue;
            }

            var source = (frameOffset + frame) * 2;
            var left = interleaved[(int)source];
            var right = interleaved[(int)source + 1];
            destination[index] = ((float.IsFinite(left) ? left : 0f) + (float.IsFinite(right) ? right : 0f)) * 0.5f;
        }
    }

    private static float Peak(float[] samples, long start, long end)
    {
        var peak = 0f;
        for (var index = Math.Max(0, start); index < end && index < samples.Length; index++)
        {
            var magnitude = Math.Abs(samples[index]);
            if (magnitude > peak)
            {
                peak = magnitude;
            }
        }

        return peak;
    }

    private static float[] Envelope(float[] samples, int binSamples)
    {
        var bins = samples.Length / binSamples;
        var envelope = new float[bins];
        for (var bin = 0; bin < bins; bin++)
        {
            double squares = 0;
            var offset = bin * binSamples;
            for (var index = 0; index < binSamples; index++)
            {
                var value = samples[offset + index];
                squares += (double)value * value;
            }

            envelope[bin] = (float)Math.Sqrt(squares / binSamples);
        }

        return envelope;
    }

    // Pearson correlation of the two envelopes at every lag in [minLag, maxLag] bins, using only the bins where both
    // streams have data at that lag. A lag with too little overlap or a flat envelope scores zero.
    private static double[] CorrelateEnvelopes(float[] input, float[] output, (int Start, int End) inputBins, (int Start, int End) outputBins, int minLag, int maxLag)
    {
        var lags = maxLag - minLag + 1;
        var result = new double[lags];
        var bins = input.Length;
        var inputSum = Prefix(input, false);
        var inputSquares = Prefix(input, true);
        var outputSum = Prefix(output, false);
        var outputSquares = Prefix(output, true);
        for (var index = 0; index < lags; index++)
        {
            var lag = minLag + index;
            var from = Math.Max(inputBins.Start, outputBins.Start - lag);
            var to = Math.Min(inputBins.End, outputBins.End - lag);
            from = Math.Max(from, Math.Max(0, -lag));
            to = Math.Min(to, Math.Min(bins, bins - lag));
            var count = to - from;
            if (count < MinOverlapBins)
            {
                continue;
            }

            double cross = 0;
            for (var t = from; t < to; t++)
            {
                cross += (double)input[t] * output[t + lag];
            }

            var meanInput = (inputSum[to] - inputSum[from]) / count;
            var meanOutput = (outputSum[to + lag] - outputSum[from + lag]) / count;
            var varInput = (inputSquares[to] - inputSquares[from]) / count - meanInput * meanInput;
            var varOutput = (outputSquares[to + lag] - outputSquares[from + lag]) / count - meanOutput * meanOutput;
            if (varInput <= 1e-12 || varOutput <= 1e-12)
            {
                continue;
            }

            result[index] = (cross / count - meanInput * meanOutput) / Math.Sqrt(varInput * varOutput);
        }

        return result;
    }

    private static double[] Prefix(float[] values, bool squared)
    {
        var prefix = new double[values.Length + 1];
        double running = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var value = (double)values[index];
            running += squared ? value * value : value;
            prefix[index + 1] = running;
        }

        return prefix;
    }

    // Sample-level search of +/- radius around the coarse lag, over (at most) the loudest RefineWindowSeconds of the
    // overlap. Accepted only when the raw waveforms correlate; an EQ'd, resampled or phase-shifted output keeps the
    // 1 ms envelope result, which the response labels as such.
    private static bool TryRefineLag(
        float[] input,
        float[] output,
        (long Start, long End) inputSpan,
        (long Start, long End) outputSpan,
        int coarseLag,
        int radius,
        int sampleRate,
        out int refinedLag)
    {
        refinedLag = coarseLag;
        var low = coarseLag - radius;
        var high = coarseLag + radius;
        var from = Math.Max(inputSpan.Start, outputSpan.Start - low);
        var to = Math.Min(inputSpan.End, outputSpan.End - high);
        from = Math.Max(from, Math.Max(0, -low));
        to = Math.Min(to, Math.Min(input.Length, output.Length - high));
        if (to - from < MinRefineWindowFrames)
        {
            return false;
        }

        var maxWindow = (long)(RefineWindowSeconds * sampleRate);
        var windowStart = from;
        var windowEnd = to;
        if (to - from > maxWindow)
        {
            // Slide a window in 1 s steps and keep the loudest one.
            var step = sampleRate;
            double bestEnergy = -1;
            for (var start = from; start + maxWindow <= to; start += step)
            {
                double energy = 0;
                for (var t = start; t < start + maxWindow; t += 8)
                {
                    energy += (double)input[t] * input[t];
                }

                if (energy > bestEnergy)
                {
                    bestEnergy = energy;
                    windowStart = start;
                }
            }

            windowEnd = windowStart + maxWindow;
        }

        var windowLength = (int)(windowEnd - windowStart);
        double inputEnergy = 0;
        for (var t = windowStart; t < windowEnd; t++)
        {
            inputEnergy += (double)input[t] * input[t];
        }

        if (inputEnergy <= 0)
        {
            return false;
        }

        // Prefix of the output energy over the region any lag in range can touch.
        var regionStart = windowStart + low;
        var regionLength = windowLength + (high - low);
        var energyPrefix = new double[regionLength + 1];
        double running = 0;
        for (var index = 0; index < regionLength; index++)
        {
            var value = (double)output[regionStart + index];
            running += value * value;
            energyPrefix[index + 1] = running;
        }

        var bestLag = coarseLag;
        var bestCorrelation = double.MinValue;
        for (var lag = low; lag <= high; lag++)
        {
            double cross = 0;
            for (var t = windowStart; t < windowEnd; t++)
            {
                cross += (double)input[t] * output[t + lag];
            }

            var regionOffset = (int)(windowStart + lag - regionStart);
            var outputEnergy = energyPrefix[regionOffset + windowLength] - energyPrefix[regionOffset];
            if (outputEnergy <= 0)
            {
                continue;
            }

            var correlation = cross / Math.Sqrt(inputEnergy * outputEnergy);
            if (correlation > bestCorrelation)
            {
                bestCorrelation = correlation;
                bestLag = lag;
            }
        }

        if (bestCorrelation < RefineMinCorrelation)
        {
            return false;
        }

        refinedLag = bestLag;
        return true;
    }
}

/// <summary>
/// Places the probe's process-loopback packets on the QPC timeline (100 ns units) for the latency analyzer. The engine's
/// position is used unless it is zero or flagged <c>AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR</c>; a flagged position can hold
/// any value, so such a packet continues the previous one sample-exactly instead. Packets before the first trusted
/// position have nothing to continue and are placed back from it by <see cref="FillLeading"/>. A capture with no trusted
/// position keeps zero starts, which the analyzer refuses rather than guess. The flag stays on each packet, so the
/// analysis still counts it.
/// </summary>
internal sealed class LoopbackPacketTimeline
{
    private readonly int _sampleRate;

    // Where the next packet starts if its own position is not trusted; zero until a trusted position arrives.
    private long _nextStart100Ns;

    public LoopbackPacketTimeline(int sampleRate) => _sampleRate = sampleRate;

    /// <summary>The start of the next packet, from the engine's position and <c>GetBuffer</c> flags.</summary>
    public long Place(ulong qpcPosition, uint flags, int frames)
    {
        var trusted = qpcPosition != 0 && (flags & AudioPacketFlags.TimestampError) == 0;
        var start = trusted ? checked((long)qpcPosition) : _nextStart100Ns;
        if (start > 0)
        {
            _nextStart100Ns = start + Duration100Ns(frames);
        }

        return start;
    }

    /// <summary>Places the packets that arrived before the first trusted position back from it. Run once, after the capture.</summary>
    public void FillLeading(Span<AudioPacket> packets)
    {
        var first = 0;
        while (first < packets.Length && packets[first].StartTime100Ns <= 0)
        {
            first++;
        }

        if (first == packets.Length)
        {
            return;
        }

        var anchor = packets[first];
        for (var index = 0; index < first; index++)
        {
            packets[index] = packets[index] with
            {
                StartTime100Ns = anchor.StartTime100Ns - Duration100Ns(anchor.FrameOffset - packets[index].FrameOffset),
            };
        }
    }

    private long Duration100Ns(long frames) => (long)Math.Round(frames * (double)QpcMath.HundredNsPerSecond / _sampleRate);
}
