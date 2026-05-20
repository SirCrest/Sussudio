using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios
{
    private static async Task<FlashbackSegmentPlaybackTarget?> AcquireFlashbackSegmentPlaybackTargetAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackTarget is not null)
        {
            return playbackTarget;
        }

        var rotationOk = await CreateFlashbackCompletedSegmentViaRecordingAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
        if (!rotationOk)
        {
            return null;
        }

        playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackTarget is null)
        {
            warnings.Add("flashback segment playback: no playable completed segment became available after recording-assisted rotation");
        }

        return playbackTarget;
    }
}
