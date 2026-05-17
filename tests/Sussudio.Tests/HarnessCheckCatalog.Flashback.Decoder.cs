using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddFlashbackDecoderChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback decoder calculates NV12 frame buffer sizes",
            FlashbackDecoder_CalculateFrameBufferSize_Nv12);
        await AddCheckAsync(results,
            "Flashback decoder calculates P010 frame buffer sizes",
            FlashbackDecoder_CalculateFrameBufferSize_P010);
        await AddCheckAsync(results,
            "Flashback decoder validation helpers live in focused partial",
            FlashbackDecoder_ValidationHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder lifetime cleanup lives in focused partial",
            FlashbackDecoder_LifetimeCleanupLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder diagnostics and guards live in focused partials",
            FlashbackDecoder_DiagnosticsAndGuardsLiveInFocusedPartials);
        await AddCheckAsync(results,
            "Flashback decoder output types live in focused file",
            FlashbackDecoder_OutputTypesLiveInFocusedFile);
        await AddCheckAsync(results,
            "Flashback decoder video setup lives in focused partial",
            FlashbackDecoder_VideoSetupLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder seeking lives in focused partial",
            FlashbackDecoder_SeekingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder decode loop lives in focused partial",
            FlashbackDecoder_DecodeLoopLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback decoder defaults to closed state",
            FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized);
        await AddCheckAsync(results,
            "Flashback decoder dispose before initialize is safe",
            FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow);
        await AddCheckAsync(results,
            "Flashback decoder unreferences discarded audio frames",
            FlashbackDecoder_DiscardedAudioFramesAreUnreffed);
        await AddCheckAsync(results,
            "Flashback decoder MJPEG playback uses low-latency single-thread decode",
            FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode);
        await AddCheckAsync(results,
            "Flashback decoder rejects invalid timestamps",
            FlashbackDecoder_PtsConversionRejectsInvalidTimestamps);
        await AddCheckAsync(results,
            "Flashback decoder input streams and frame sizes are bounded",
            FlashbackDecoder_InputStreamsAndFrameSizesAreBounded);
        await AddCheckAsync(results,
            "Flashback decoder audio output buffers are bounded",
            FlashbackDecoder_AudioOutputBuffersAreBounded);
        await AddCheckAsync(results,
            "Flashback decoder audio setup lives in audio output partial",
            FlashbackDecoder_AudioSetupLivesInAudioOutputPartial);
        await AddCheckAsync(results,
            "Flashback decoder software frame planes are validated",
            FlashbackDecoder_SoftwareFramePlanesAreValidated);
        await AddCheckAsync(results,
            "Flashback decoder D3D11 frames are validated",
            FlashbackDecoder_D3D11FramesAreValidated);
        await AddCheckAsync(results,
            "Flashback decoder held-frame cleanup is best effort",
            FlashbackDecoder_HeldFrameCleanupIsBestEffort);
        await AddCheckAsync(results,
            "Flashback decoder decode loops observe cancellation",
            FlashbackDecoder_DecodeLoopsObserveCancellation);
        await AddCheckAsync(results,
            "Flashback decoder rejects initialize after dispose",
            FlashbackDecoder_RejectsInitializeAfterDispose);
        await AddCheckAsync(results,
            "Flashback decoder clears audio callback on dispose",
            FlashbackDecoder_ClearsAudioCallbackOnDispose);
    }
}
