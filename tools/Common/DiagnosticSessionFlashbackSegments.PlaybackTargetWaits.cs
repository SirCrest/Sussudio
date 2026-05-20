using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegments
{
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
}
