using System;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Presentation-layer state machine for the Flashback timeline. It chooses
/// whether preview/audio should show live capture or decoded file playback, but
/// it never starts, stops, or throttles the capture pipeline.
/// </summary>
internal sealed partial class FlashbackPlaybackController : IDisposable
{
    // --- Dependencies ---
    private readonly FlashbackBufferManager _bufferManager;

    public FlashbackPlaybackController(FlashbackBufferManager bufferManager)
    {
        _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
        _commandChannel = CreateCommandChannel();
    }

    // (Command dispatch, decoder files, playback loop/timing, audio routing,
    // and audio prebuffer extracted to partials)
    // See: FlashbackPlaybackController.PlaybackState.cs, .CommandQueue.cs, .CommandCoalescing.cs, .CommandTelemetry.cs, .CommandFailures.cs, .DecoderFiles.cs,
    // .DecoderReopen.cs, .DecoderSegmentReopen.cs,
    // .ThreadLoop.cs, .ThreadLifecycle.cs, .ThreadCleanup.cs, .PlaybackLoop.cs,
    // .PlaybackSegmentEdges.cs, .PlaybackTiming.cs, .PlaybackSoftwareBudget.cs,
    // .AudioMasterPacing.cs, .AudioMasterFallbacks.cs, .AudioCallback.cs, .AudioRouting.cs, .AudioPreviewGuards.cs, .AudioPrebuffer.cs,
    // .PreviewFrames.cs, .PlaybackFrameOwnership.cs, .PlaybackLiveRecovery.cs, .MetricsCollection.cs
}
