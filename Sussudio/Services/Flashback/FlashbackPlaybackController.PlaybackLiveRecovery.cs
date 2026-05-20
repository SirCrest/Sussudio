using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, operation, resumeRendering: true);

    private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, operation, resumeRendering: true);

    private void RestoreLiveAfterDecoderPlaybackFailure(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        string operation,
        bool resumeRendering)
    {
        CloseDecoderFileBestEffort(decoder, operation);
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        if (resumeRendering)
        {
            SafeResumeRendering(operation);
        }

        SetState(FlashbackPlaybackState.Live);
    }
}
