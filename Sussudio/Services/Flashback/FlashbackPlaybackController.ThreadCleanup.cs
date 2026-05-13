using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback thread cleanup ---

    private void RestoreLiveForPlaybackThreadExit(
        ref FlashbackDecoder? decoder,
        ref bool fileOpen,
        string operation)
    {
        CleanupDecoder(ref decoder, ref fileOpen);
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SetState(FlashbackPlaybackState.Live);
    }

    private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (System.Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
