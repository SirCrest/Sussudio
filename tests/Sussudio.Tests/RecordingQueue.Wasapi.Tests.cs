using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    internal static Task WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var captureLoopSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs")
            .Replace("\r\n", "\n");
        var contractsSource = ReadRepoFile("Sussudio/Services/Contracts/RecordingContracts.cs")
            .Replace("\r\n", "\n");
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();

        var drainBlock = ExtractSourceBlock(
            captureLoopSource,
            "private void DrainCapturePackets()",
            "private void OnCaptureFailed");
        AssertContains(drainBlock, "handoffToPlayback = DispatchConvertedAudioPacket(converted);");
        AssertDoesNotContain(drainBlock, "InvokeHotAudioWriter(");
        AssertDoesNotContain(drainBlock, "WriteAudioToSinkOnCaptureThread(");
        AssertDoesNotContain(drainBlock, ".GetAwaiter()");
        AssertDoesNotContain(drainBlock, "Volatile.Read(ref _recordingSink)");
        AssertContains(captureLoopSource, "private void CaptureThreadMain()");
        AssertContains(captureLoopSource, "private bool DispatchConvertedAudioPacket(ConvertedAudioPacket converted)");
        AssertContains(captureLoopSource, "var audioWriter = Volatile.Read(ref _audioWriter);");
        AssertContains(captureLoopSource, "var sink = Volatile.Read(ref _recordingSink);");
        AssertContains(captureLoopSource, "var flashbackSink = Volatile.Read(ref _flashbackSink);");
        AssertContains(captureLoopSource, "var playback = Volatile.Read(ref _playback);");
        AssertContains(captureLoopSource, "playback.EnqueuePooledSamples(convertedBuffer, converted.Length);");
        AssertContains(captureLoopSource, "private static void InvokeHotAudioWriter(");
        AssertContains(captureLoopSource, "private static void WriteAudioToSinkOnCaptureThread(");
        AssertContains(captureLoopSource, "private static void CompleteHotAudioWrite(Task task, string target)");
        AssertContains(captureLoopSource, "if (!task.IsCompleted)");
        AssertContains(captureLoopSource, "Audio writers must copy/enqueue synchronously and return Task.CompletedTask.");
        AssertDoesNotContain(wasapiSource, "private static void CompleteHotAudioWrite(Task task, string target)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Fanout.cs")),
            "converted audio fan-out lives with the WASAPI capture loop");
        AssertContains(contractsSource, "Hot WASAPI callback write.");
        AssertContains(contractsSource, "must not do blocking/async work");

        var libAvAudioWrite = ExtractSourceBlock(
            libAvSource,
            "public Task WriteAudioAsync",
            "public Task WriteMicrophoneAudioAsync");
        var flashbackAudioWrite = ExtractSourceBlock(
            flashbackSource,
            "public Task WriteAudioAsync",
            "public Task WriteMicrophoneAudioAsync");
        AssertContains(libAvAudioWrite, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(libAvAudioWrite, "return Task.CompletedTask;");
        AssertContains(flashbackAudioWrite, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(flashbackAudioWrite, "return Task.CompletedTask;");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_ConversionLivesWithCaptureLoop()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var captureLoopSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "internal sealed partial class WasapiAudioCapture");
        AssertContains(captureLoopSource, "internal sealed partial class WasapiAudioCapture");
        AssertContains(captureLoopSource, "public void AttachRecordingSink(IRecordingSink sink)");
        AssertContains(captureLoopSource, "public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)");
        AssertContains(captureLoopSource, "internal void SetPlayback(WasapiAudioPlayback? playback)");
        AssertContains(captureLoopSource, "private ConvertedAudioPacket ConvertToOutputFormat(");
        AssertContains(captureLoopSource, "private int ComputeResampledFrameCount(");
        AssertContains(captureLoopSource, "private static void ResampleStereoLinear(");
        AssertContains(captureLoopSource, "private static unsafe void DecodeToStereo(");
        AssertContains(captureLoopSource, "private static unsafe float ReadSample(");
        AssertContains(captureLoopSource, "private static void ReturnPacketBuffer(ConvertedAudioPacket packet)");
        AssertContains(captureLoopSource, "private readonly struct ConvertedAudioPacket");
        AssertDoesNotContain(wasapiSource, "private void CaptureThreadMain()");
        AssertDoesNotContain(wasapiSource, "private void DrainCapturePackets()");
        AssertDoesNotContain(wasapiSource, "private ConvertedAudioPacket ConvertToOutputFormat(");
        AssertDoesNotContain(wasapiSource, "private static void ResampleStereoLinear(");
        AssertDoesNotContain(wasapiSource, "private readonly struct ConvertedAudioPacket");
        AssertDoesNotContain(wasapiSource, "public void AttachRecordingSink(IRecordingSink sink)");
        AssertDoesNotContain(wasapiSource, "public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Conversion.cs")),
            "WASAPI capture conversion folded into capture loop owner");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_InitializationLivesWithLifecycleRoot()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "internal sealed partial class WasapiAudioCapture : IAsyncDisposable");
        AssertContains(wasapiSource, "public Task InitializeAsync(string audioDeviceId, CancellationToken ct)");
        AssertContains(wasapiSource, "WasapiComInterop.CreateDeviceEnumerator()");
        AssertContains(wasapiSource, "audioClient.GetMixFormat(out mixFormat)");
        AssertContains(wasapiSource, "WasapiComInterop.AllocFloatStereo48kFormat()");
        AssertContains(wasapiSource, "audioClient.IsFormatSupported(");
        AssertContains(wasapiSource, "WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, selectedFormat)");
        AssertContains(wasapiSource, "\"IAudioClient.Initialize(capture)\"");
        AssertContains(wasapiSource, "audioClient.SetEventHandle(captureEvent.SafeWaitHandle.DangerousGetHandle())");
        AssertContains(wasapiSource, "audioClient.GetService(ref iidCaptureClient, out var captureClientObject)");
        AssertContains(wasapiSource, "_fastPathCopy = _captureFormat.SampleRate == OutputSampleRate");
        AssertContains(wasapiSource, "_resampleRemainderNumerator = 0;");
        AssertContains(wasapiSource, "Interlocked.Exchange(ref _initialized, 1);");
        AssertContains(wasapiSource, "WasapiComInterop.CoTaskMemFree(desiredFormat);");
        AssertContains(wasapiSource, "WasapiComInterop.ReleaseComObject(ref audioCaptureClient);");
        AssertContains(wasapiSource, "public void Start()");
        AssertContains(wasapiSource, "public Task StopAsync()");
        AssertContains(wasapiSource, "public async ValueTask DisposeAsync()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Initialization.cs")),
            "WASAPI capture initialization stays folded into capture lifecycle root");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioPlayback_InitializationLivesWithLifecycleRoot()
    {
        var playbackSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(playbackSource, "internal sealed class WasapiAudioPlayback : IDisposable");
        AssertContains(playbackSource, "public Task InitializeAsync(CancellationToken ct)");
        AssertContains(playbackSource, "enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device)");
        AssertContains(playbackSource, "WasapiComInterop.AllocFloatStereo48kFormat()");
        AssertContains(playbackSource, "audioClient.IsFormatSupported(");
        AssertContains(playbackSource, "WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, desiredFormat)");
        AssertContains(playbackSource, "\"IAudioClient.Initialize(render)\"");
        AssertContains(playbackSource, "audioClient.GetBufferSize(out _bufferFrameCount)");
        AssertContains(playbackSource, "audioClient.GetStreamLatency(out var streamLatencyHundredNs)");
        AssertContains(playbackSource, "audioClient.SetEventHandle(renderEvent.SafeWaitHandle.DangerousGetHandle())");
        AssertContains(playbackSource, "audioClient.GetService(ref iidRenderClient, out var renderClientObject)");
        AssertContains(playbackSource, "Interlocked.Exchange(ref _renderCallbackCount, 0)");
        AssertContains(playbackSource, "Volatile.Write(ref _playbackQueueDepth, 0)");
        AssertContains(playbackSource, "Interlocked.Exchange(ref _initialized, 1)");
        AssertContains(playbackSource, "WasapiComInterop.CoTaskMemFree(desiredFormat)");
        AssertContains(playbackSource, "WasapiComInterop.ReleaseComObject(ref audioRenderClient)");
        AssertContains(playbackSource, "internal void EnqueuePooledSamples(byte[] pooledBuffer, int validLength, long ptsTicks = 0)");
        AssertContains(playbackSource, "private bool TryWriteChunk(PlaybackChunk chunk)");
        AssertContains(playbackSource, "private bool TryDequeueChunk(out PlaybackChunk chunk)");
        AssertContains(playbackSource, "private readonly record struct PlaybackChunk");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Initialization.cs")),
            "WASAPI playback initialization stays folded into playback lifecycle root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Queue.cs")),
            "WASAPI playback queue state stays folded into playback lifecycle root");
        AssertContains(playbackSource, "public void Start()");
        AssertContains(playbackSource, "public void PauseRendering()");
        AssertContains(playbackSource, "public void ResumeRendering(double prebufferMs = 0, int prebufferTimeoutMs = 0)");
        AssertContains(playbackSource, "public void Flush()");
        AssertContains(playbackSource, "public void Stop()");
        AssertContains(playbackSource, "public void Dispose()");
        AssertContains(playbackSource, "private void RenderThreadMain()");
        AssertContains(playbackSource, "private unsafe void RenderAvailableFrames()");
        AssertContains(playbackSource, "private void ApplyVolume(Span<byte> buffer)");
        AssertContains(playbackSource, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.RenderThread.cs")),
            "WASAPI playback render thread stays folded into playback lifecycle root");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_DiagnosticsLivesWithLifecycleRoot()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var captureLoopSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureLoopSource, "TrackCaptureCallback(Environment.TickCount64);");
        AssertContains(captureLoopSource, "TrackCapturePacketFlags(flags);");
        AssertContains(wasapiSource, "public long AudioFramesArrived => Interlocked.Read(ref _audioFramesArrived);");
        AssertContains(wasapiSource, "public (double AvgIntervalMs, double MaxIntervalMs) GetCaptureCallbackIntervalSnapshot()");
        AssertContains(wasapiSource, "private void RaiseAudioLevelIfDue(ReadOnlySpan<byte> f32leBytes)");
        AssertContains(wasapiSource, "private void TrackCaptureCallback(long callbackTickMs)");
        AssertContains(wasapiSource, "private CallbackIntervalMetrics GetCaptureCallbackIntervalMetrics()");
        AssertContains(wasapiSource, "private void TrackCapturePacketFlags(uint flags)");
        AssertContains(wasapiSource, "private readonly record struct CallbackIntervalMetrics");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioCapture.Diagnostics.cs")),
            "WASAPI capture diagnostics folded into capture lifecycle root");

        return Task.CompletedTask;
    }

    internal static Task WasapiComInterop_ContractsLiveWithInteropOwner()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootSource, "internal static class WasapiComInterop");
        AssertContains(rootSource, "internal static void ThrowIfFailed(int hr, string operation)");
        AssertContains(rootSource, "internal static void ReleaseComObject<T>(ref T? comObject)");
        AssertContains(rootSource, "internal static WasapiAudioFormat ReadAudioFormat(IntPtr formatPtr)");
        AssertContains(rootSource, "private static WasapiSampleType ResolveSampleType(");
        AssertContains(rootSource, "internal static IMMDeviceEnumerator CreateDeviceEnumerator()");
        AssertContains(rootSource, "internal static IAudioClient ActivateAudioClient(IMMDevice device, out IAudioClient3? audioClient3)");
        AssertContains(rootSource, "internal static bool TryInitializeSharedStreamWithAudioClient3(");
        AssertContains(rootSource, "internal static float GetEndpointVolume(string deviceId)");
        AssertContains(rootSource, "internal static void SetEndpointVolume(string deviceId, float level)");
        AssertContains(rootSource, "internal enum EDataFlow");
        AssertContains(rootSource, "internal enum WasapiSampleType");
        AssertContains(rootSource, "internal readonly record struct WasapiAudioFormat(");
        AssertContains(rootSource, "internal struct WAVEFORMATEX");
        AssertContains(rootSource, "internal struct WAVEFORMATEXTENSIBLE");
        AssertContains(rootSource, "internal struct PropVariant : IDisposable");
        AssertContains(rootSource, "internal interface IMMDeviceEnumerator");
        AssertContains(rootSource, "internal interface IMMDevice");
        AssertContains(rootSource, "internal interface IMMDeviceCollection");
        AssertContains(rootSource, "internal interface IPropertyStore");
        AssertContains(rootSource, "internal interface IMMNotificationClient");
        AssertContains(rootSource, "internal interface IAudioClient");
        AssertContains(rootSource, "internal interface IAudioClient3 : IAudioClient");
        AssertContains(rootSource, "internal interface IAudioCaptureClient");
        AssertContains(rootSource, "internal interface IAudioRenderClient");
        AssertContains(rootSource, "internal interface IAudioEndpointVolume");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.CoreAudio.Contracts.cs")),
            "Core Audio contract shard folded into the single WASAPI interop owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.AudioClient.Contracts.cs")),
            "AudioClient contract shard folded into the single WASAPI interop owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.Formats.cs")),
            "WASAPI format helpers stay with the implementation root instead of a tiny partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.DeviceClients.cs")),
            "WASAPI device helpers stay with the implementation root instead of a tiny partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.Contracts.cs")),
            "old combined WASAPI COM contract file removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiComInterop.CommonContracts.cs")),
            "shared WASAPI contracts stay with Core Audio contracts instead of a tiny file");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_StopUsesBoundedThreadJoin()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "private static readonly TimeSpan CaptureThreadJoinTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(wasapiSource, "JoinCaptureThread(_captureThread, \"WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT_START_FAILURE\")");
        AssertContains(wasapiSource, "JoinCaptureThread(thread, \"WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT_STOP\")");
        AssertContains(wasapiSource, "thread.Join(CaptureThreadJoinTimeout)");
        AssertDoesNotContain(wasapiSource, "_captureThread.Join();");
        AssertDoesNotContain(wasapiSource, "thread.Join();");
        return Task.CompletedTask;
    }

}
