using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackDecoderContractsTests
{
    public FlashbackDecoderContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackDecoderCalculatesNv12FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_Nv12();

    [Fact]
    public Task FlashbackDecoderCalculatesP010FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_P010();

    [Fact]
    public Task FlashbackDecoderValidationHelpersLiveInFocusedPartial()
        => global::Program.FlashbackDecoder_ValidationHelpersLiveInFocusedPartial();

    [Fact]
    public Task FlashbackDecoderLifetimeCleanupLivesWithRootLifecycle()
        => global::Program.FlashbackDecoder_LifetimeCleanupLivesWithRootLifecycle();

    [Fact]
    public Task FlashbackDecoderStateGuardsAndTimingLiveWithOwners()
        => global::Program.FlashbackDecoder_StateGuardsAndTimingLiveWithOwners();

    [Fact]
    public Task FlashbackDecoderOutputTypesLiveWithDecoderRoot()
        => global::Program.FlashbackDecoder_OutputTypesLiveWithDecoderRoot();

    [Fact]
    public Task FlashbackDecoderVideoSetupLivesInFocusedPartial()
        => global::Program.FlashbackDecoder_VideoSetupLivesInFocusedPartial();

    [Fact]
    public Task FlashbackDecoderSeekingLivesInFocusedPartial()
        => global::Program.FlashbackDecoder_SeekingLivesInFocusedPartial();

    [Fact]
    public Task FlashbackDecoderDecodeLoopLivesInFocusedPartial()
        => global::Program.FlashbackDecoder_DecodeLoopLivesInFocusedPartial();

    [Fact]
    public Task FlashbackDecoderDefaultsToClosedState()
        => global::Program.FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized();

    [Fact]
    public Task FlashbackDecoderDisposeBeforeInitializeIsSafe()
        => global::Program.FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow();

    [Fact]
    public Task FlashbackDecoderUnreferencesDiscardedAudioFrames()
        => global::Program.FlashbackDecoder_DiscardedAudioFramesAreUnreffed();

    [Fact]
    public Task FlashbackDecoderMjpegPlaybackUsesLowLatencySingleThreadDecode()
        => global::Program.FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode();

    [Fact]
    public Task FlashbackDecoderRejectsInvalidTimestamps()
        => global::Program.FlashbackDecoder_PtsConversionRejectsInvalidTimestamps();

    [Fact]
    public Task FlashbackDecoderInputStreamsAndFrameSizesAreBounded()
        => global::Program.FlashbackDecoder_InputStreamsAndFrameSizesAreBounded();

    [Fact]
    public Task FlashbackDecoderAudioOutputBuffersAreBounded()
        => global::Program.FlashbackDecoder_AudioOutputBuffersAreBounded();

    [Fact]
    public Task FlashbackDecoderAudioSetupLivesInAudioOutputPartial()
        => global::Program.FlashbackDecoder_AudioSetupLivesInAudioOutputPartial();

    [Fact]
    public Task FlashbackDecoderSoftwareFramePlanesAreValidated()
        => global::Program.FlashbackDecoder_SoftwareFramePlanesAreValidated();

    [Fact]
    public Task FlashbackDecoderD3D11FramesAreValidated()
        => global::Program.FlashbackDecoder_D3D11FramesAreValidated();

    [Fact]
    public Task FlashbackDecoderHeldFrameCleanupIsBestEffort()
        => global::Program.FlashbackDecoder_HeldFrameCleanupIsBestEffort();

    [Fact]
    public Task FlashbackDecoderDecodeLoopsObserveCancellation()
        => global::Program.FlashbackDecoder_DecodeLoopsObserveCancellation();

    [Fact]
    public Task FlashbackDecoderRejectsInitializeAfterDispose()
        => global::Program.FlashbackDecoder_RejectsInitializeAfterDispose();

    [Fact]
    public Task FlashbackDecoderClearsAudioCallbackOnDispose()
        => global::Program.FlashbackDecoder_ClearsAudioCallbackOnDispose();
}
