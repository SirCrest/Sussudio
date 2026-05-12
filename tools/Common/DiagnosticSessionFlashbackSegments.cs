using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

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
}
