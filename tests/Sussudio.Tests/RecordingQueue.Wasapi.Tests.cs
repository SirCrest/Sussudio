using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    private static Task WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var fanoutSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.Fanout.cs")
            .Replace("\r\n", "\n");
        var contractsSource = (ReadRepoFile("Sussudio/Services/Recording/RecordingContracts.cs")
                + "\n"
                + ReadRepoFile("Sussudio/Services/Contracts/RecordingContracts.cs"))
            .Replace("\r\n", "\n");
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();

        var drainBlock = ExtractSourceBlock(
            wasapiSource,
            "private void DrainCapturePackets()",
            "private void OnCaptureFailed");
        AssertContains(drainBlock, "InvokeHotAudioWriter(");
        AssertContains(drainBlock, "WriteAudioToSinkOnCaptureThread(");
        AssertDoesNotContain(drainBlock, ".GetAwaiter()");
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

    private static Task WasapiAudioCapture_ConversionLivesInFocusedPartial()
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
        AssertDoesNotContain(wasapiSource, "private ConvertedAudioPacket ConvertToOutputFormat(");
        AssertDoesNotContain(wasapiSource, "private static void ResampleStereoLinear(");
        AssertDoesNotContain(wasapiSource, "private readonly struct ConvertedAudioPacket");
        AssertDoesNotContain(wasapiSource, "public void AttachRecordingSink(IRecordingSink sink)");
        AssertDoesNotContain(wasapiSource, "public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)");

        return Task.CompletedTask;
    }

    private static Task WasapiAudioCapture_DiagnosticsLivesInFocusedPartial()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.Diagnostics.cs")
            .Replace("\r\n", "\n");

        AssertContains(wasapiSource, "TrackCaptureCallback(Environment.TickCount64);");
        AssertContains(wasapiSource, "TrackCapturePacketFlags(flags);");
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

    private static Task WasapiComInterop_ContractsLiveInFocusedFile()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.cs")
            .Replace("\r\n", "\n");
        var contractsSource = ReadRepoFile("Sussudio/Services/Audio/WasapiComInterop.Contracts.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootSource, "internal static class WasapiComInterop");
        AssertContains(rootSource, "internal static WasapiAudioFormat ReadAudioFormat(IntPtr formatPtr)");
        AssertContains(rootSource, "private static WasapiSampleType ResolveSampleType(");
        AssertContains(contractsSource, "internal enum EDataFlow");
        AssertContains(contractsSource, "internal enum WasapiSampleType");
        AssertContains(contractsSource, "internal readonly record struct WasapiAudioFormat(");
        AssertContains(contractsSource, "internal struct WAVEFORMATEX");
        AssertContains(contractsSource, "internal struct WAVEFORMATEXTENSIBLE");
        AssertContains(contractsSource, "internal struct PropVariant : IDisposable");
        AssertContains(contractsSource, "internal interface IMMDeviceEnumerator");
        AssertContains(contractsSource, "internal interface IAudioClient");
        AssertContains(contractsSource, "internal interface IAudioClient3 : IAudioClient");
        AssertContains(contractsSource, "internal interface IAudioCaptureClient");
        AssertContains(contractsSource, "internal interface IAudioRenderClient");
        AssertContains(contractsSource, "internal interface IMMNotificationClient");
        AssertDoesNotContain(rootSource, "internal readonly record struct WasapiAudioFormat(");
        AssertDoesNotContain(rootSource, "internal struct WAVEFORMATEX");
        AssertDoesNotContain(rootSource, "internal interface IAudioClient");
        AssertDoesNotContain(rootSource, "internal interface IMMDeviceEnumerator");

        return Task.CompletedTask;
    }

    private static Task WasapiAudioCapture_StopUsesBoundedThreadJoin()
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
