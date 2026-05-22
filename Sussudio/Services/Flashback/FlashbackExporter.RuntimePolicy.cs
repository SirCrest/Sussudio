using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private const int ExportWriterYieldPacketInterval = 256;
    private const int ExportWriterThrottlePacketInterval = 4096;
    private const int ExportWriterThrottleSleepMs = 1;
    private const int ExportWriterAdaptiveThrottlePacketInterval = 4;
    private const int ExportWriterMaxAdaptiveThrottleSleepMs = 25;

    [ThreadStatic]
    private static Func<int>? s_adaptiveThrottleDelayMsProvider;

    private readonly object _adaptiveThrottleSync = new();
    private Func<int>? _nextAdaptiveThrottleDelayMsProvider;

    private static void ReportProgress(IProgress<ExportProgress>? progress, ExportProgress value, string stage)
    {
        value = NormalizeExportProgress(value, stage);
        try
        {
            progress?.Report(value);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_PROGRESS_WARN stage={stage} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static ExportProgress NormalizeExportProgress(ExportProgress value, string stage)
    {
        var totalSegments = Math.Max(0, value.TotalSegments);
        var segmentsProcessed = Math.Max(0, value.SegmentsProcessed);
        if (totalSegments > 0 && segmentsProcessed > totalSegments)
        {
            segmentsProcessed = totalSegments;
        }

        var percent = double.IsFinite(value.Percent)
            ? Math.Clamp(value.Percent, 0.0, 100.0)
            : 0.0;

        if (segmentsProcessed != value.SegmentsProcessed ||
            totalSegments != value.TotalSegments ||
            percent != value.Percent)
        {
            Logger.Log(
                $"FLASHBACK_EXPORT_PROGRESS_NORMALIZED stage={stage} " +
                $"raw_segments={value.SegmentsProcessed}/{value.TotalSegments} " +
                $"segments={segmentsProcessed}/{totalSegments} " +
                $"raw_percent={value.Percent:0.###} percent={percent:0.###}");
        }

        return new ExportProgress(segmentsProcessed, totalSegments, percent);
    }

    private static bool ShouldReportProgressHeartbeat(ref long lastHeartbeatTick)
    {
        var now = Stopwatch.GetTimestamp();
        var last = lastHeartbeatTick;
        if (last != 0 &&
            (now - last) * 1000.0 / Stopwatch.Frequency < ProgressHeartbeatIntervalMs)
        {
            return false;
        }

        lastHeartbeatTick = now;
        return true;
    }

    private void SetNextAdaptiveThrottleDelayProvider(Func<int>? adaptiveThrottleDelayMsProvider)
    {
        lock (_adaptiveThrottleSync)
        {
            _nextAdaptiveThrottleDelayMsProvider = adaptiveThrottleDelayMsProvider;
        }
    }

    private Func<int>? ConsumeNextAdaptiveThrottleDelayProvider()
    {
        lock (_adaptiveThrottleSync)
        {
            var provider = _nextAdaptiveThrottleDelayMsProvider;
            _nextAdaptiveThrottleDelayMsProvider = null;
            return provider;
        }
    }

    private static FinalizeResult RunWithAdaptiveThrottle(
        Func<int>? adaptiveThrottleDelayMsProvider,
        Func<FinalizeResult> exportWork)
    {
        var previousProvider = s_adaptiveThrottleDelayMsProvider;
        try
        {
            s_adaptiveThrottleDelayMsProvider = adaptiveThrottleDelayMsProvider;
            return exportWork();
        }
        finally
        {
            s_adaptiveThrottleDelayMsProvider = previousProvider;
        }
    }

    private static void ThrottleExportWriterIfNeeded(long packetsWritten)
    {
        if (packetsWritten <= 0)
        {
            return;
        }

        var adaptiveThrottleDelayMsProvider = s_adaptiveThrottleDelayMsProvider;
        if (adaptiveThrottleDelayMsProvider != null &&
            packetsWritten % ExportWriterAdaptiveThrottlePacketInterval == 0)
        {
            var adaptiveDelayMs = Math.Clamp(
                adaptiveThrottleDelayMsProvider(),
                0,
                ExportWriterMaxAdaptiveThrottleSleepMs);
            if (adaptiveDelayMs > 0)
            {
                Thread.Sleep(adaptiveDelayMs);
                return;
            }
        }

        if (packetsWritten % ExportWriterThrottlePacketInterval == 0)
        {
            Thread.Sleep(ExportWriterThrottleSleepMs);
            return;
        }

        if (packetsWritten % ExportWriterYieldPacketInterval == 0)
        {
            Thread.Yield();
        }
    }
}
