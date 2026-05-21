using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static class DiagnosticSessionSampler
{
    internal static async Task SampleLoopAsync(
        int durationSeconds,
        int sampleIntervalMs,
        List<DiagnosticSessionSample> samples,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        Func<Task>? sampleCheckpointAsync = null)
    {
        var started = Stopwatch.GetTimestamp();
        var duration = TimeSpan.FromSeconds(durationSeconds);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                samples.Add(new DiagnosticSessionSample
                {
                    OffsetMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Snapshot = snapshot.Clone()
                });
                if (sampleCheckpointAsync is not null)
                {
                    await sampleCheckpointAsync().ConfigureAwait(false);
                }
            }

            var elapsed = Stopwatch.GetElapsedTime(started);
            if (elapsed >= duration)
            {
                break;
            }

            var remaining = duration - elapsed;
            var delay = TimeSpan.FromMilliseconds(Math.Min(sampleIntervalMs, Math.Max(1, remaining.TotalMilliseconds)));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }
}
