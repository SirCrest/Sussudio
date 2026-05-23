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
    // See: FlashbackPlaybackController.PlaybackState.cs, .CommandQueue.cs, .CommandTelemetry.cs, .DecoderFiles.cs, .DecoderCleanup.cs,
    // .DecoderReopen.cs, .DecoderAdjacentSegmentSeek.cs, .DecoderSegmentReopen.cs,
    // .ThreadLoop.cs, .ThreadLifecycle.cs, .ThreadEndScrubCommand.cs, .PlaybackLoop.cs,
    // .PlaybackSegmentEdges.cs, .PlaybackSegmentSwitch.cs, .PlaybackTiming.cs, .PlaybackPtsCadence.cs, .SeekDisplayFrames.cs,
    // .AudioMasterClock.cs, .AudioMasterPacing.cs, .AudioMasterFallbacks.cs, .AudioCallback.cs, .AudioRouting.cs, .AudioPreviewGuards.cs, .AudioPrebuffer.cs,
    // .PreviewFrames.cs, .PreviewFrameValidation.cs, .PlaybackFrameOwnership.cs, .PlaybackLiveRecovery.cs, .PreviewDetachLifecycle.cs,
    // .Metrics.cs
}
