using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;
internal static class DiagnosticSessionFlashbackExports
{
    internal static int? TryParseFlashbackExportSegmentCount(string message)
    {
        const string marker = " from ";
        var markerIndex = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var digitsStart = markerIndex + marker.Length;
        while (digitsStart < message.Length && char.IsWhiteSpace(message[digitsStart]))
        {
            digitsStart++;
        }

        var digitsEnd = digitsStart;
        while (digitsEnd < message.Length && char.IsDigit(message[digitsEnd]))
        {
            digitsEnd++;
        }

        if (digitsEnd == digitsStart)
        {
            return null;
        }

        var suffix = message[digitsEnd..];
        if (!suffix.Contains("segment", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(
            message.AsSpan(digitsStart, digitsEnd - digitsStart),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
    }

    internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath) =>
        new()
        {
            ["filePath"] = filePath,
            ["strict"] = true,
            ["verificationProfile"] = "flashback-export"
        };

    internal static async Task CleanupFlashbackSelectionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
    }

    internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(
        Task<JsonElement> exportTask,
        JsonElement baselineSnapshot,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var baselineAudioEnabled = GetBool(baselineSnapshot, "IsAudioEnabled");
        var toggledAudioEnabled = !baselineAudioEnabled;
        var exportRequestOutstandingBeforeToggle = false;

        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            exportRequestOutstandingBeforeToggle = !exportTask.IsCompleted;
            if (exportRequestOutstandingBeforeToggle)
            {
                actions.Add("flashback range export audio switch confirmed export command outstanding before audio toggle");
            }
            else
            {
                warnings.Add("flashback range export audio switch: export completed before audio toggle");
            }

            var toggleResponse = await sendCommandAsync(
                    "SetAudioEnabled",
                    new Dictionary<string, object?> { ["enabled"] = toggledAudioEnabled },
                    10_000)
                .ConfigureAwait(false);
            if (IsSuccess(toggleResponse))
            {
                actions.Add($"flashback range export audio switch toggled audio enabled to {toggledAudioEnabled}");
            }
            else
            {
                warnings.Add(
                    "flashback range export audio switch: audio toggle failed - " +
                    Get(toggleResponse, "Message", "unknown error"));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"flashback range export audio switch: audio toggle threw {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                var restoreResponse = await sendCommandAsync(
                        "SetAudioEnabled",
                        new Dictionary<string, object?> { ["enabled"] = baselineAudioEnabled },
                        10_000)
                    .ConfigureAwait(false);
                if (IsSuccess(restoreResponse))
                {
                    actions.Add($"flashback range export audio switch restored audio enabled to {baselineAudioEnabled}");
                }
                else
                {
                    warnings.Add(
                        "flashback range export audio switch: audio restore failed - " +
                        Get(restoreResponse, "Message", "unknown error"));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add($"flashback range export audio switch: audio restore threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}

internal readonly record struct FlashbackSegmentProbe(
    int SequenceNumber,
    long StartPtsMs,
    long EndPtsMs,
    bool IsActive);

internal readonly record struct FlashbackSegmentPlaybackTarget(
    FlashbackSegmentProbe Segment,
    long ValidStartPtsMs,
    long BoundaryPositionMs,
    long BufferedDurationMs);

internal static class DiagnosticSessionFlashbackWaits
{
    internal static async Task<JsonElement?> WaitForFlashbackActiveAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        bool expectedActive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "FlashbackActive") == expectedActive)
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static async Task<JsonElement?> WaitForPreviewActiveAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        bool expectedActive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "IsPreviewing") == expectedActive)
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "IsRecording") &&
                string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase) &&
                GetBool(snapshot, "RecordingFileGrowing"))
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        int requiredBufferedDurationMs = 8_000,
        long requiredEncodedFrames = 240,
        TimeSpan? timeout = null)
    {
        var started = Stopwatch.GetTimestamp();
        var waitTimeout = timeout ?? TimeSpan.FromSeconds(30);
        while (Stopwatch.GetElapsedTime(started) < waitTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "FlashbackActive") &&
                GetInt(snapshot, "FlashbackBufferedDurationMs") >= requiredBufferedDurationMs &&
                (GetNullableLong(snapshot, "FlashbackEncodedFrames") ?? 0) >= requiredEncodedFrames)
            {
                return true;
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        string expectedState,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        JsonElement? lastSnapshot = null;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                lastSnapshot = snapshot;
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                if (string.Equals(state, expectedState, StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }

    internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long boundaryMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        JsonElement? lastSnapshot = null;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                lastSnapshot = snapshot;
                var positionMs = GetNullableLong(snapshot, "FlashbackPlaybackPositionMs") ?? 0;
                var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
                var pending = GetInt(snapshot, "FlashbackPlaybackPendingCommands");
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                if (positionMs >= boundaryMs + 1_500 &&
                    frameCount >= 180 &&
                    pending == 0 &&
                    string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }

    internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long baselineFrameCount,
        double minimumSeconds,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        JsonElement? lastSnapshot = null;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                lastSnapshot = snapshot;
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
                var sessionFrameCount = frameCount >= baselineFrameCount
                    ? frameCount - baselineFrameCount
                    : frameCount;
                var targetFps = GetDouble(snapshot, "FlashbackPlaybackTargetFps");
                if (targetFps <= 0)
                {
                    targetFps = GetDouble(snapshot, "SelectedExactFrameRate");
                }

                var minimumFrames = Math.Max(
                    240,
                    targetFps > 0
                        ? (long)Math.Ceiling(targetFps * minimumSeconds)
                        : 240);
                if (sessionFrameCount >= minimumFrames &&
                    string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }

    internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        int targetPositionMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                var position = GetInt(snapshot, "FlashbackPlaybackPositionMs");
                if (Math.Abs(position - targetPositionMs) <= 1_500)
                {
                    return true;
                }
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}

internal static class DiagnosticSessionFlashbackSegments
{
    internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("FlashbackGetSegments", null, null).ConfigureAwait(false);
            if (TryGetFlashbackSegments(response, out var segments))
            {
                var completed = segments
                    .Where(segment => !segment.IsActive && segment.EndPtsMs > segment.StartPtsMs)
                    .OrderBy(segment => segment.EndPtsMs)
                    .FirstOrDefault();
                if (completed.EndPtsMs > completed.StartPtsMs)
                {
                    return completed;
                }
            }

            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        const int requiredHeadroomMs = 8_000;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var segmentsResponse = await sendCommandAsync("FlashbackGetSegments", null, null).ConfigureAwait(false);
            var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetFlashbackSegments(segmentsResponse, out var segments) &&
                TryGetSnapshot(snapshotResponse, out var snapshot))
            {
                var bufferedDurationMs = GetNullableLong(snapshot, "FlashbackBufferedDurationMs") ?? 0;
                var latestPtsMs = segments.Count > 0
                    ? segments.Max(segment => segment.EndPtsMs)
                    : 0;
                var validStartPtsMs = Math.Max(0, latestPtsMs - bufferedDurationMs);
                var completed = segments
                    .Where(segment => !segment.IsActive && segment.EndPtsMs > segment.StartPtsMs)
                    .Select(segment => new
                    {
                        Segment = segment,
                        BoundaryPositionMs = Math.Max(0, segment.EndPtsMs - validStartPtsMs)
                    })
                    .Where(candidate =>
                        candidate.BoundaryPositionMs > 0 &&
                        candidate.BoundaryPositionMs + requiredHeadroomMs <= bufferedDurationMs)
                    .OrderByDescending(candidate => candidate.Segment.EndPtsMs)
                    .FirstOrDefault();
                if (completed is not null)
                {
                    return new FlashbackSegmentPlaybackTarget(
                        completed.Segment,
                        validStartPtsMs,
                        completed.BoundaryPositionMs,
                        bufferedDurationMs);
                }
            }

            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long boundaryMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        const int requiredHeadroomMs = 8_000;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                var bufferedDurationMs = GetNullableLong(snapshot, "FlashbackBufferedDurationMs") ?? 0;
                if (bufferedDurationMs >= boundaryMs + requiredHeadroomMs)
                {
                    return true;
                }
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    internal static bool TryGetFlashbackSegments(JsonElement response, out List<FlashbackSegmentProbe> segments)
    {
        segments = new List<FlashbackSegmentProbe>();
        if (!response.TryGetProperty("Data", out var data) ||
            !data.TryGetProperty("Segments", out var segmentsElement) ||
            segmentsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var segment in segmentsElement.EnumerateArray())
        {
            if (segment.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            segments.Add(new FlashbackSegmentProbe(
                SequenceNumber: GetInt(segment, "SequenceNumber"),
                StartPtsMs: GetNullableLong(segment, "StartPtsMs") ?? 0,
                EndPtsMs: GetNullableLong(segment, "EndPtsMs") ?? 0,
                IsActive: GetBool(segment, "IsActive")));
        }

        return true;
    }
}

internal static class DiagnosticSessionFlashbackValidation
{
    internal static void ValidateFlashbackRecordingSession(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        List<string> warnings)
    {
        var metrics = BuildFlashbackRecordingMetrics(initialSnapshot, samples);
        if (metrics.SampleCount == 0)
        {
            warnings.Add("flashback recording: no recording samples captured");
            return;
        }

        if (!metrics.BackendObserved)
        {
            warnings.Add("flashback recording: RecordingBackend never reported Flashback");
        }

        if (!metrics.FileGrowthObserved)
        {
            warnings.Add("flashback recording: recording file never reported growth");
        }

        if (metrics.VideoFramesSubmittedDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback video frames submitted to encoder");
        }

        if (metrics.VideoEncoderPacketsWrittenDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback encoder packets written");
        }

        if (metrics.IntegritySequenceGapsDelta > 0)
        {
            warnings.Add($"flashback recording: Flashback video sequence gaps increased delta={metrics.IntegritySequenceGapsDelta} end={metrics.IntegritySequenceGapsAtEnd}");
        }

        if (metrics.IntegrityQueueDroppedFramesDelta > 0)
        {
            warnings.Add($"flashback recording: Flashback dropped frames increased delta={metrics.IntegrityQueueDroppedFramesDelta} end={metrics.IntegrityQueueDroppedFramesAtEnd}");
        }
    }

    internal static void ValidateFlashbackPlaybackSession(
        JsonElement lastSnapshot,
        FlashbackPlaybackSessionMetrics metrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        int durationSeconds,
        List<string> warnings)
    {
        var targetFps = GetDouble(lastSnapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
        }

        var frameCount = Math.Max(metrics.EndSessionFrameCount, metrics.MaxSessionFrameCountObserved);
        if (frameCount <= 0)
        {
            warnings.Add("flashback playback: no playback frames were observed");
            return;
        }

        if (targetFps > 0 && durationSeconds > 0)
        {
            var minimumExpectedFrames = Math.Max(1, (long)Math.Floor(targetFps * durationSeconds * 0.80));
            if (frameCount < minimumExpectedFrames)
            {
                warnings.Add($"flashback playback: frame count below expected floor frames={frameCount} min={minimumExpectedFrames} targetFps={targetFps:0.##}");
            }

            var minimumOnePercentLow = targetFps * 0.80;
            var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);
            if (!visualCadenceHealthy &&
                metrics.MinOnePercentLowFpsObserved > 0 &&
                metrics.MinOnePercentLowFpsObserved < minimumOnePercentLow)
            {
                warnings.Add($"flashback playback: 1% low dipped below floor min={metrics.MinOnePercentLowFpsObserved:0.##} floor={minimumOnePercentLow:0.##}");
            }
        }

        if (metrics.DroppedFramesDelta > 0)
        {
            var droppedFrames = GetNullableLong(lastSnapshot, "FlashbackPlaybackDroppedFrames") ?? 0;
            warnings.Add($"flashback playback: dropped frames increased delta={metrics.DroppedFramesDelta} end={droppedFrames}");
        }

        if (metrics.SubmitFailuresDelta > 0)
        {
            var submitFailures = GetNullableLong(lastSnapshot, "FlashbackPlaybackSubmitFailures") ?? 0;
            warnings.Add($"flashback playback: submit failures increased delta={metrics.SubmitFailuresDelta} end={submitFailures}");
        }

        const double maxHealthyAudioBufferedMs = 250.0;
        if (metrics.MaxAudioBufferedDurationMsObserved > maxHealthyAudioBufferedMs)
        {
            warnings.Add($"flashback playback: audio buffered duration exceeded budget max={metrics.MaxAudioBufferedDurationMsObserved:0.##}ms budget={maxHealthyAudioBufferedMs:0.##}ms");
        }

        const double maxHealthyAvDriftMs = 250.0;
        if (metrics.MaxAbsAvDriftMsObserved > maxHealthyAvDriftMs)
        {
            warnings.Add($"flashback playback: absolute A/V drift exceeded budget max={metrics.MaxAbsAvDriftMsObserved:0.##}ms budget={maxHealthyAvDriftMs:0.##}ms");
        }
    }

    internal static void ValidateFlashbackPreviewScheduler(
        long deadlineDropsDelta,
        long underflowsDelta,
        long d3dStatsFailureDelta,
        PreviewCadenceSessionMetrics previewCadenceMetrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        PreviewD3DMetrics previewD3DMetrics,
        double targetFps,
        bool tolerateSchedulerTransitionsWithHealthyVisualCadence,
        List<string> warnings)
    {
        if (deadlineDropsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence)
        {
            warnings.Add($"flashback preview: scheduler deadline drops increased delta={deadlineDropsDelta}");
        }

        if (underflowsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence)
        {
            warnings.Add($"flashback preview: scheduler underflows increased delta={underflowsDelta}");
        }

        if (d3dStatsFailureDelta > 0)
        {
            warnings.Add($"flashback preview: D3D frame stats failures increased delta={d3dStatsFailureDelta}");
        }

        if (targetFps < 100)
        {
            return;
        }

        var targetFrameMs = 1000.0 / targetFps;
        var onePercentLowFloor = targetFps * 0.80;
        var presentP99BudgetMs = targetFrameMs * 1.25;
        var totalP99BudgetMs = targetFrameMs * 1.35;
        var onePercentLowMiss =
            previewCadenceMetrics.MinOnePercentLowFpsObserved > 0 &&
            previewCadenceMetrics.MinOnePercentLowFpsObserved < onePercentLowFloor;
        var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);
        var presentP99Miss =
            previewD3DMetrics.PresentCallP99MsAtEnd > presentP99BudgetMs;
        var totalP99Miss =
            previewD3DMetrics.TotalFrameCpuP99MsAtEnd > totalP99BudgetMs;

        if ((onePercentLowMiss && !visualCadenceHealthy) || presentP99Miss || totalP99Miss)
        {
            warnings.Add(
                "flashback preview: present/display pressure " +
                $"targetFps={targetFps:0.##} " +
                $"onePercentLowFpsMin={previewCadenceMetrics.MinOnePercentLowFpsObserved:0.##}/{onePercentLowFloor:0.##} " +
                $"visualChangeFpsMin={visualCadenceMetrics.MinChangeFpsObserved:0.##} " +
                $"visualRepeatPctMax={visualCadenceMetrics.MaxRepeatPercentObserved:0.###} " +
                $"visualLongestRepeatRun={visualCadenceMetrics.LongestRepeatRunAtEnd} " +
                $"presentCallP99Ms={previewD3DMetrics.PresentCallP99MsAtEnd:0.##}/{presentP99BudgetMs:0.##} " +
                $"totalFrameCpuP99Ms={previewD3DMetrics.TotalFrameCpuP99MsAtEnd:0.##}/{totalP99BudgetMs:0.##} " +
                $"missedRefreshDelta={previewD3DMetrics.MissedRefreshDelta} " +
                $"underflowsDelta={underflowsDelta} " +
                $"latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)} " +
                $"latestSlowPresentCallMs={previewD3DMetrics.LatestSlowFramePresentCallMs:0.##} " +
                $"latestSlowTotalFrameCpuMs={previewD3DMetrics.LatestSlowFrameTotalFrameCpuMs:0.##} " +
                $"latestSlowPending={previewD3DMetrics.LatestSlowFramePendingFrameCount}");
        }
    }
}
