using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackPlaybackContractsTests
{
    public FlashbackPlaybackContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackPlaybackInitialStateIsLive()
        => global::Program.FlashbackPlaybackController_InitialState_IsLive();

    [Fact]
    public Task FlashbackPlaybackCommandsNoOpBeforeInitialize()
        => global::Program.FlashbackPlaybackController_CommandsNoOpBeforeInitialize();

    [Fact]
    public Task FlashbackPlaybackSuccessfulNoOpsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackCoalescedCommandsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackWorkerExitRearmsFutureCommands()
        => global::Program.FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart();

    [Fact]
    public Task FlashbackPlaybackCommandQueueAcceptsNewestControlWhenFull()
        => global::Program.FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull();

    [Fact]
    public Task FlashbackCommandPositionsClampBeforeFileLookup()
        => global::Program.FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup();

    [Fact]
    public Task FlashbackPlaybackTimestampArithmeticIsSaturating()
        => global::Program.FlashbackPlaybackController_TimestampArithmeticIsSaturating();

    [Fact]
    public Task FlashbackEndOfSegmentOpenFailuresSnapLive()
        => global::Program.FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive();

    [Fact]
    public Task FlashbackNormalPlaybackUsesTightNearLiveSnap()
        => global::Program.FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap();

    [Fact]
    public Task FlashbackSnapLiveClearsOpenFileIdentity()
        => global::Program.FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity();

    [Fact]
    public Task FlashbackPauseFromLiveDisplaysBufferedFrameBeforePaused()
        => global::Program.FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused();

    [Fact]
    public Task FlashbackPlaybackGuardsInvalidDecoderFrameRates()
        => global::Program.FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps();

    [Fact]
    public Task FlashbackPlaybackPtsCadenceTelemetryTracksMismatches()
        => global::Program.FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches();

    [Fact]
    public Task FlashbackNudgeOpensDecoderAfterPauseFromLive()
        => global::Program.FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused();

    [Fact]
    public Task FlashbackPlaybackReleasesDecodedFramesAfterSubmitFailures()
        => global::Program.FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames();

    [Fact]
    public Task FlashbackPlaybackGuardsFmp4ReopenRetries()
        => global::Program.FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded();

    [Fact]
    public Task FlashbackScrubCoalescingDoesNotRequeueControlCommands()
        => global::Program.FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands();

    [Fact]
    public Task FlashbackSeekSlotsPreserveControlCommandBarriers()
        => global::Program.FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers();

    [Fact]
    public Task FlashbackSeekSlotsPreserveSlotStateAfterRejectedBarriers()
        => global::Program.FlashbackPlaybackController_SeekSlots_PreserveSlotStateAfterRejectedBarriers();

    [Fact]
    public Task FlashbackPlaybackTransitionsUseBestEffortAudioPreviewGuards()
        => global::Program.FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards();

    [Fact]
    public Task FlashbackPlaybackMetricResetClearsDecodeTimings()
        => global::Program.FlashbackPlaybackController_ResetClearsDecodeMetrics();
}
