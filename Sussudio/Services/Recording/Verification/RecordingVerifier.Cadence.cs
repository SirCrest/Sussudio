using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
    private readonly record struct CadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double MaxIntervalMs,
        double JitterStdDevMs,
        long SevereGapCount,
        double SevereGapPercent,
        long EstimatedDroppedFrames,
        double EstimatedDropPercent);

    private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(
        string outputPath,
        double? expectedFrameRate,
        CancellationToken cancellationToken)
    {
        var cadenceArgs =
            "-v error " +
            "-select_streams v:0 " +
            "-show_frames " +
            "-show_entries frame=best_effort_timestamp_time,pkt_dts_time,pkt_pts_time " +
            "-of json " +
            $"\"{outputPath}\"";

        var probe = await _processSupervisor.RunAsync(
            CreateFfprobeProcessSpec(outputPath, cadenceArgs, timeoutMs: 20000),
            cancellationToken).ConfigureAwait(false);

        if (!probe.Started || probe.TimedOut || probe.ExitCode != 0 || string.IsNullOrWhiteSpace(probe.StdOut))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(probe.StdOut);
            if (!document.RootElement.TryGetProperty("frames", out var framesElement) ||
                framesElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var intervalsMs = new List<double>(capacity: 4096);
            double? previousTimestamp = null;
            foreach (var frame in framesElement.EnumerateArray())
            {
                var timestamp = TryGetFrameTimestampSeconds(frame);
                if (!timestamp.HasValue)
                {
                    continue;
                }

                if (previousTimestamp.HasValue)
                {
                    var deltaMs = (timestamp.Value - previousTimestamp.Value) * 1000.0;
                    if (deltaMs > 0 && deltaMs < 5000)
                    {
                        intervalsMs.Add(deltaMs);
                    }
                }

                previousTimestamp = timestamp;
            }

            if (intervalsMs.Count < 1)
            {
                return null;
            }

            return ComputeCadenceMetrics(intervalsMs, expectedFrameRate);
        }
        catch (Exception ex)
        {
            Logger.Log($"AnalyzeCadenceMetricsAsync ffprobe JSON parse failed: {ex.Message}");
            return null;
        }
    }

    private static CadenceMetrics ComputeCadenceMetrics(IReadOnlyList<double> intervalsMs, double? expectedFrameRate)
    {
        var sampleCount = intervalsMs.Count;
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var value = intervalsMs[i];
            sum += value;
            if (value > max)
            {
                max = value;
            }
        }

        var average = sum / sampleCount;
        var observedFps = average > double.Epsilon ? 1000.0 / average : 0;
        var expectedIntervalMs = expectedFrameRate.HasValue && expectedFrameRate.Value > 0
            ? 1000.0 / expectedFrameRate.Value
            : average;
        var severeGapThresholdMs = expectedIntervalMs * 2.25;

        var varianceSum = 0.0;
        long severeGapCount = 0;
        long estimatedDroppedFrames = 0;
        for (var i = 0; i < sampleCount; i++)
        {
            var value = intervalsMs[i];
            var delta = value - average;
            varianceSum += delta * delta;

            if (value >= severeGapThresholdMs)
            {
                severeGapCount++;
            }

            if (expectedIntervalMs > double.Epsilon)
            {
                var missingFrames = (long)Math.Floor((value + expectedIntervalMs * 0.20) / expectedIntervalMs) - 1;
                if (missingFrames > 0)
                {
                    estimatedDroppedFrames += missingFrames;
                }
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = intervalsMs.ToArray();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var severeGapPercent = severeGapCount <= 0
            ? 0
            : (double)severeGapCount / Math.Max(1, sampleCount) * 100.0;
        var estimatedDropPercent = estimatedDroppedFrames <= 0
            ? 0
            : (double)estimatedDroppedFrames / Math.Max(1, sampleCount + estimatedDroppedFrames) * 100.0;

        return new CadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: expectedIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            MaxIntervalMs: max,
            JitterStdDevMs: jitterStdDevMs,
            SevereGapCount: severeGapCount,
            SevereGapPercent: severeGapPercent,
            EstimatedDroppedFrames: estimatedDroppedFrames,
            EstimatedDropPercent: estimatedDropPercent);
    }

    private static double? TryGetFrameTimestampSeconds(JsonElement frame)
    {
        return TryGetJsonDouble(frame, "best_effort_timestamp_time")
            ?? TryGetJsonDouble(frame, "pkt_dts_time")
            ?? TryGetJsonDouble(frame, "pkt_pts_time");
    }

    private static double? TryGetJsonDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var number) => number,
            JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }
}
