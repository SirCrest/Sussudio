using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    internal static Task WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var fanoutSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.Fanout.cs")
            .Replace("\r\n", "\n");
        var captureLoopSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs")
            .Replace("\r\n", "\n");
        var contractsSource = (ReadRepoFile("Sussudio/Services/Recording/RecordingContracts.cs")
                + "\n"
                + ReadRepoFile("Sussudio/Services/Contracts/RecordingContracts.cs"))
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
        AssertContains(fanoutSource, "private bool DispatchConvertedAudioPacket(ConvertedAudioPacket converted)");
        AssertContains(fanoutSource, "var audioWriter = Volatile.Read(ref _audioWriter);");
        AssertContains(fanoutSource, "var sink = Volatile.Read(ref _recordingSink);");
        AssertContains(fanoutSource, "var flashbackSink = Volatile.Read(ref _flashbackSink);");
        AssertContains(fanoutSource, "var playback = Volatile.Read(ref _playback);");
        AssertContains(fanoutSource, "playback.EnqueuePooledSamples(convertedBuffer, converted.Length);");
        AssertContains(fanoutSource, "private static void InvokeHotAudioWriter(");
        AssertContains(fanoutSource, "private static void WriteAudioToSinkOnCaptureThread(");
        AssertContains(fanoutSource, "private static void CompleteHotAudioWrite(Task task, string target)");
        AssertContains(fanoutSource, "if (!task.IsCompleted)");
        AssertContains(fanoutSource, "Audio writers must copy/enqueue synchronously and return Task.CompletedTask.");
        AssertDoesNotContain(wasapiSource, "private static void CompleteHotAudioWrite(Task task, string target)");
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

    internal static Task WasapiAudioCapture_ConversionLivesInFocusedPartial()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var fanoutSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.Fanout.cs")
            .Replace("\r\n", "\n");
        var conversionSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.Conversion.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "internal sealed partial class WasapiAudioCapture");
        AssertContains(fanoutSource, "internal sealed partial class WasapiAudioCapture");
        AssertContains(conversionSource, "internal sealed partial class WasapiAudioCapture");
        AssertContains(fanoutSource, "public void AttachRecordingSink(IRecordingSink sink)");
        AssertContains(fanoutSource, "public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)");
        AssertContains(fanoutSource, "internal void SetPlayback(WasapiAudioPlayback? playback)");
        AssertContains(conversionSource, "private ConvertedAudioPacket ConvertToOutputFormat(");
        AssertContains(conversionSource, "private int ComputeResampledFrameCount(");
        AssertContains(conversionSource, "private static void ResampleStereoLinear(");
        AssertContains(conversionSource, "private static unsafe void DecodeToStereo(");
        AssertContains(conversionSource, "private static unsafe float ReadSample(");
        AssertContains(conversionSource, "private static void ReturnPacketBuffer(ConvertedAudioPacket packet)");
        AssertContains(conversionSource, "private readonly struct ConvertedAudioPacket");
        AssertDoesNotContain(wasapiSource, "private void CaptureThreadMain()");
        AssertDoesNotContain(wasapiSource, "private void DrainCapturePackets()");
        AssertDoesNotContain(wasapiSource, "private ConvertedAudioPacket ConvertToOutputFormat(");
        AssertDoesNotContain(wasapiSource, "private static void ResampleStereoLinear(");
        AssertDoesNotContain(wasapiSource, "private readonly struct ConvertedAudioPacket");
        AssertDoesNotContain(wasapiSource, "public void AttachRecordingSink(IRecordingSink sink)");
        AssertDoesNotContain(wasapiSource, "public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_InitializationLivesInFocusedPartial()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var initializationSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.Initialization.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "internal sealed partial class WasapiAudioCapture : IAsyncDisposable");
        AssertContains(initializationSource, "internal sealed partial class WasapiAudioCapture");
        AssertContains(initializationSource, "public Task InitializeAsync(string audioDeviceId, CancellationToken ct)");
        AssertContains(initializationSource, "WasapiComInterop.CreateDeviceEnumerator()");
        AssertContains(initializationSource, "audioClient.GetMixFormat(out mixFormat)");
        AssertContains(initializationSource, "WasapiComInterop.AllocFloatStereo48kFormat()");
        AssertContains(initializationSource, "audioClient.IsFormatSupported(");
        AssertContains(initializationSource, "WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, selectedFormat)");
        AssertContains(initializationSource, "\"IAudioClient.Initialize(capture)\"");
        AssertContains(initializationSource, "audioClient.SetEventHandle(captureEvent.SafeWaitHandle.DangerousGetHandle())");
        AssertContains(initializationSource, "audioClient.GetService(ref iidCaptureClient, out var captureClientObject)");
        AssertContains(initializationSource, "_fastPathCopy = _captureFormat.SampleRate == OutputSampleRate");
        AssertContains(initializationSource, "_resampleRemainderNumerator = 0;");
        AssertContains(initializationSource, "Interlocked.Exchange(ref _initialized, 1);");
        AssertContains(initializationSource, "WasapiComInterop.CoTaskMemFree(desiredFormat);");
        AssertContains(initializationSource, "WasapiComInterop.ReleaseComObject(ref audioCaptureClient);");
        AssertDoesNotContain(wasapiSource, "public Task InitializeAsync(string audioDeviceId, CancellationToken ct)");
        AssertContains(wasapiSource, "public void Start()");
        AssertContains(wasapiSource, "public Task StopAsync()");
        AssertContains(wasapiSource, "public async ValueTask DisposeAsync()");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioPlayback_InitializationLivesInFocusedPartial()
    {
        var playbackSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs")
            .Replace("\r\n", "\n");
        var initializationSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.Initialization.cs")
            .Replace("\r\n", "\n");

        AssertContains(playbackSource, "internal sealed partial class WasapiAudioPlayback : IDisposable");
        AssertContains(initializationSource, "internal sealed partial class WasapiAudioPlayback");
        AssertContains(initializationSource, "public Task InitializeAsync(CancellationToken ct)");
        AssertContains(initializationSource, "enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device)");
        AssertContains(initializationSource, "WasapiComInterop.AllocFloatStereo48kFormat()");
        AssertContains(initializationSource, "audioClient.IsFormatSupported(");
        AssertContains(initializationSource, "WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, desiredFormat)");
        AssertContains(initializationSource, "\"IAudioClient.Initialize(render)\"");
        AssertContains(initializationSource, "audioClient.GetBufferSize(out _bufferFrameCount)");
        AssertContains(initializationSource, "audioClient.GetStreamLatency(out var streamLatencyHundredNs)");
        AssertContains(initializationSource, "audioClient.SetEventHandle(renderEvent.SafeWaitHandle.DangerousGetHandle())");
        AssertContains(initializationSource, "audioClient.GetService(ref iidRenderClient, out var renderClientObject)");
        AssertContains(initializationSource, "Interlocked.Exchange(ref _renderCallbackCount, 0)");
        AssertContains(initializationSource, "Volatile.Write(ref _playbackQueueDepth, 0)");
        AssertContains(initializationSource, "Interlocked.Exchange(ref _initialized, 1)");
        AssertContains(initializationSource, "WasapiComInterop.CoTaskMemFree(desiredFormat)");
        AssertContains(initializationSource, "WasapiComInterop.ReleaseComObject(ref audioRenderClient)");
        AssertDoesNotContain(playbackSource, "public Task InitializeAsync(CancellationToken ct)");
        AssertContains(playbackSource, "public void Start()");
        AssertContains(playbackSource, "public void PauseRendering()");
        AssertContains(playbackSource, "public void ResumeRendering(double prebufferMs = 0, int prebufferTimeoutMs = 0)");
        AssertContains(playbackSource, "public void Flush()");
        AssertContains(playbackSource, "public void Stop()");
        AssertContains(playbackSource, "public void Dispose()");

        return Task.CompletedTask;
    }

    internal static Task WasapiAudioCapture_DiagnosticsLivesInFocusedPartial()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.Diagnostics.cs")
            .Replace("\r\n", "\n");
        var captureLoopSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureLoopSource, "TrackCaptureCallback(Environment.TickCount64);");
        AssertContains(captureLoopSource, "TrackCapturePacketFlags(flags);");
        AssertContains(diagnosticsSource, "public long AudioFramesArrived => Interlocked.Read(ref _audioFramesArrived);");
        AssertContains(diagnosticsSource, "public (double AvgIntervalMs, double MaxIntervalMs) GetCaptureCallbackIntervalSnapshot()");
        AssertContains(diagnosticsSource, "private void RaiseAudioLevelIfDue(ReadOnlySpan<byte> f32leBytes)");
        AssertContains(diagnosticsSource, "private void TrackCaptureCallback(long callbackTickMs)");
        AssertContains(diagnosticsSource, "private CallbackIntervalMetrics GetCaptureCallbackIntervalMetrics()");
        AssertContains(diagnosticsSource, "private void TrackCapturePacketFlags(uint flags)");
        AssertContains(diagnosticsSource, "private readonly record struct CallbackIntervalMetrics");
        AssertDoesNotContain(wasapiSource, "public long AudioFramesArrived => Interlocked.Read(ref _audioFramesArrived);");
        AssertDoesNotContain(wasapiSource, "private void RaiseAudioLevelIfDue(ReadOnlySpan<byte> f32leBytes)");
        AssertDoesNotContain(wasapiSource, "private void TrackCaptureCallback(long callbackTickMs)");
        AssertDoesNotContain(wasapiSource, "private readonly record struct CallbackIntervalMetrics");

        return Task.CompletedTask;
    }

    internal static Task WasapiComInterop_ContractsLiveInFocusedFiles()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.cs")
            .Replace("\r\n", "\n");
        var formatSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.Formats.cs")
            .Replace("\r\n", "\n");
        var deviceClientSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.DeviceClients.cs")
            .Replace("\r\n", "\n");
        var coreAudioContractsSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.CoreAudio.Contracts.cs")
            .Replace("\r\n", "\n");
        var audioClientContractsSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.AudioClient.Contracts.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootSource, "internal static partial class WasapiComInterop");
        AssertContains(rootSource, "internal static void ThrowIfFailed(int hr, string operation)");
        AssertContains(rootSource, "internal static void ReleaseComObject<T>(ref T? comObject)");
        AssertContains(formatSource, "internal static partial class WasapiComInterop");
        AssertContains(formatSource, "internal static WasapiAudioFormat ReadAudioFormat(IntPtr formatPtr)");
        AssertContains(formatSource, "private static WasapiSampleType ResolveSampleType(");
        AssertContains(deviceClientSource, "internal static partial class WasapiComInterop");
        AssertContains(deviceClientSource, "internal static IMMDeviceEnumerator CreateDeviceEnumerator()");
        AssertContains(deviceClientSource, "internal static IAudioClient ActivateAudioClient(IMMDevice device, out IAudioClient3? audioClient3)");
        AssertContains(deviceClientSource, "internal static bool TryInitializeSharedStreamWithAudioClient3(");
        AssertContains(deviceClientSource, "internal static float GetEndpointVolume(string deviceId)");
        AssertContains(deviceClientSource, "internal static void SetEndpointVolume(string deviceId, float level)");
        AssertContains(coreAudioContractsSource, "internal enum EDataFlow");
        AssertContains(coreAudioContractsSource, "internal enum WasapiSampleType");
        AssertContains(coreAudioContractsSource, "internal readonly record struct WasapiAudioFormat(");
        AssertContains(coreAudioContractsSource, "internal struct WAVEFORMATEX");
        AssertContains(coreAudioContractsSource, "internal struct WAVEFORMATEXTENSIBLE");
        AssertContains(coreAudioContractsSource, "internal struct PropVariant : IDisposable");
        AssertContains(coreAudioContractsSource, "internal interface IMMDeviceEnumerator");
        AssertContains(coreAudioContractsSource, "internal interface IMMDevice");
        AssertContains(coreAudioContractsSource, "internal interface IMMDeviceCollection");
        AssertContains(coreAudioContractsSource, "internal interface IPropertyStore");
        AssertContains(coreAudioContractsSource, "internal interface IMMNotificationClient");
        AssertContains(audioClientContractsSource, "internal interface IAudioClient");
        AssertContains(audioClientContractsSource, "internal interface IAudioClient3 : IAudioClient");
        AssertContains(audioClientContractsSource, "internal interface IAudioCaptureClient");
        AssertContains(audioClientContractsSource, "internal interface IAudioRenderClient");
        AssertContains(audioClientContractsSource, "internal interface IAudioEndpointVolume");
        AssertDoesNotContain(rootSource, "internal static WasapiAudioFormat ReadAudioFormat(IntPtr formatPtr)");
        AssertDoesNotContain(rootSource, "internal static IMMDeviceEnumerator CreateDeviceEnumerator()");
        AssertDoesNotContain(rootSource, "internal static IAudioClient ActivateAudioClient(");
        AssertDoesNotContain(rootSource, "internal readonly record struct WasapiAudioFormat(");
        AssertDoesNotContain(rootSource, "internal struct WAVEFORMATEX");
        AssertDoesNotContain(rootSource, "internal interface IAudioClient");
        AssertDoesNotContain(rootSource, "internal interface IMMDeviceEnumerator");
        AssertDoesNotContain(formatSource, "internal static float GetEndpointVolume(string deviceId)");
        AssertDoesNotContain(deviceClientSource, "private static WasapiSampleType ResolveSampleType(");
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
