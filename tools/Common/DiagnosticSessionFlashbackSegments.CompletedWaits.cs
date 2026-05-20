using System.Diagnostics;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegments
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
}
