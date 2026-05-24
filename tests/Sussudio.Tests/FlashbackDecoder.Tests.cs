using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // ── FlashbackDecoder: CalculateFrameBufferSize ──

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_Nv12()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // NV12: width * height + width * (height / 2)
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(1920 * 1080 + 1920 * (1080 / 2), size1080, "NV12 1080p buffer size");

        var size720 = (int)method.Invoke(null, new object[] { 1280, 720, false })!;
        AssertEqual(1280 * 720 + 1280 * (720 / 2), size720, "NV12 720p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, false })!;
        AssertEqual(3840 * 2160 + 3840 * (2160 / 2), size4k, "NV12 4K buffer size");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_P010()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // P010: width * height * 2 + width * (height / 2) * 2
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, true })!;
        AssertEqual(1920 * 1080 * 2 + 1920 * (1080 / 2) * 2, size1080, "P010 1080p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, true })!;
        AssertEqual(3840 * 2160 * 2 + 3840 * (2160 / 2) * 2, size4k, "P010 4K buffer size");

        // P010 should be exactly 2x NV12
        var nv12Size = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(nv12Size * 2, size1080, "P010 is 2x NV12");

        return Task.CompletedTask;
    }

    // ── FlashbackDecoder: state guard properties ──

    internal static Task FlashbackDecoder_ValidationHelpersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Validation.cs")
            .Replace("\r\n", "\n");
        var videoOutputText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs")
            .Replace("\r\n", "\n");
        var videoConversionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoConversion.cs")
            .Replace("\r\n", "\n");

        AssertContains(validationText, "private static int CalculateFrameBufferSize(int width, int height, bool isHdr)");
        AssertContains(validationText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(validationText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(validationText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(validationText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(validationText, "private static bool TryGetInputStreamCount(AVFormatContext* formatCtx, out int streamCount, out string failureMessage)");
        AssertContains(validationText, "private static bool IsValidStreamIndex(int streamIndex, int streamCount)");
        AssertDoesNotContain(rootText, "private static int CalculateFrameBufferSize(int width, int height, bool isHdr)");
        AssertDoesNotContain(rootText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertDoesNotContain(videoOutputText, "private static void InterleaveUvRow(");
        AssertContains(videoConversionText, "private void CopyFramePlanesToBuffer(");
        AssertContains(videoConversionText, "private void ConvertYuv420pToNv12(");
        AssertContains(videoConversionText, "private void ConvertYuv420p10leToP010(");
        AssertContains(videoConversionText, "private static void InterleaveUvRow(");
        AssertDoesNotContain(rootText, "private static bool TryGetInputStreamCount(AVFormatContext* formatCtx, out int streamCount, out string failureMessage)");
        AssertDoesNotContain(rootText, "private static bool IsValidStreamIndex(int streamIndex, int streamCount)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_LifetimeCleanupLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifetimeText, "private void CloseFileCore()");
        AssertContains(lifetimeText, "internal static void ReleaseHeldFrame(DecodedVideoFrame frame)");
        AssertContains(lifetimeText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertDoesNotContain(rootText, "private void CloseFileCore()");
        AssertDoesNotContain(rootText, "internal static void ReleaseHeldFrame(DecodedVideoFrame frame)");
        AssertDoesNotContain(rootText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_StateGuardsAndTimingLiveWithOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var decodeLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs")
            .Replace("\r\n", "\n");

        AssertContains(decodeLoopText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertContains(decodeLoopText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertContains(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(rootText, "private static string GetErrorString(int errorCode)");
        AssertContains(rootText, "private static InvalidOperationException CreateException(string message)");
        AssertContains(rootText, "private void ThrowIfNotInitialized()");
        AssertContains(rootText, "private void ThrowIfNotOpen()");
        AssertContains(rootText, "private void ThrowIfDisposed()");
        AssertDoesNotContain(rootText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertDoesNotContain(rootText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertDoesNotContain(decodeLoopText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertDoesNotContain(decodeLoopText, "private void ThrowIfNotInitialized()");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_OutputTypesLiveInFocusedFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var outputTypesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.OutputTypes.cs")
            .Replace("\r\n", "\n");

        AssertContains(outputTypesText, "internal readonly struct DecodedVideoFrame");
        AssertContains(outputTypesText, "internal readonly struct DecodedAudioChunk");
        AssertDoesNotContain(rootText, "internal readonly struct DecodedVideoFrame");
        AssertDoesNotContain(rootText, "internal readonly struct DecodedAudioChunk");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_VideoSetupLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var videoSetupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs")
            .Replace("\r\n", "\n");

        AssertContains(videoSetupText, "private void InitializeVideoDecoder()");
        AssertContains(videoSetupText, "private void AllocateVideoOutputBuffers()");
        AssertDoesNotContain(rootText, "private void InitializeVideoDecoder()");
        AssertDoesNotContain(rootText, "private void AllocateVideoOutputBuffers()");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_SeekingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var seekingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs")
            .Replace("\r\n", "\n");

        AssertContains(seekingText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(seekingText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(seekingText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(seekingText, "FLASHBACK_DECODER_SEEK_CAP_HIT");
        AssertDoesNotContain(rootText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DecodeLoopLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var decodeLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs")
            .Replace("\r\n", "\n");

        AssertContains(decodeLoopText, "private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;");
        AssertContains(decodeLoopText, "public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;");
        AssertContains(decodeLoopText, "public readonly record struct PlaybackDecodePhaseTimings(");
        AssertContains(decodeLoopText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(decodeLoopText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(decodeLoopText, "ffmpeg.av_read_frame(_formatCtx, _packet)");
        AssertContains(decodeLoopText, "DecodeAndDeliverAudioPacket(_packet);");
        AssertDoesNotContain(rootText, "private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;");
        AssertDoesNotContain(rootText, "public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;");
        AssertDoesNotContain(rootText, "public readonly record struct PlaybackDecodePhaseTimings(");
        AssertDoesNotContain(rootText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        var isOpenProp = decoderType.GetProperty("IsOpen",
            BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(isOpenProp, "FlashbackDecoder.IsOpen");
        AssertEqual(false, (bool)isOpenProp!.GetValue(decoder)!, "IsOpen default");

        return Task.CompletedTask;
    }

    // ── FlashbackDecoder: Dispose is safe when not initialized ──

    internal static Task FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        // Dispose via IDisposable
        if (decoder is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else
        {
            var disposeMethod = decoderType.GetMethod("Dispose",
                BindingFlags.Public | BindingFlags.Instance);
            disposeMethod?.Invoke(decoder, null);
        }

        return Task.CompletedTask;
    }
}
