using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Performance;

/// <summary>
/// Hears what this app actually plays: captures this process's rendered audio through Windows process loopback (the
/// stream the mixer receives from the app, before volume of the endpoint and before the device), analyses it, and, on
/// request, measures the delay from capture delivery to the mixer by correlating it with the capture-delivery tap.
/// All capture work happens on a dedicated MTA thread; the analysis runs on a worker after the capture stopped. The app's
/// own audio threads are never blocked or allocated on; the only hook on them is <see cref="AudioMonitorTap"/>.
/// Contract: docs/architecture/performance-telemetry.md section 2.9.
/// </summary>
public static class AudioOutputProbe
{
    public const double MinSeconds = 0.5;
    public const double MaxSeconds = 30.0;
    public const double DefaultSeconds = 5.0;

    private const int SampleRate = AudioMonitorRing.SampleRate;
    private const int Channels = AudioMonitorRing.Channels;
    private const int MaxPacketsPerDrain = 256;
    private const uint WaitSliceMs = 50;
    private const int PacketsPerSecondBudget = 500;
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromSeconds(5);

    private const string SourceDescription = "this process's rendered audio via Windows process loopback (48 kHz float stereo, process tree included)";

    public static async Task<AudioOutputProbeResult> CaptureAsync(
        double seconds,
        string? wavPath,
        bool measureLatency,
        CancellationToken cancellationToken)
    {
        var processId = Environment.ProcessId;
        if (!WasapiProcessLoopbackInterop.IsSupported)
        {
            return Failed(
                "unsupported-os",
                $"Process loopback needs Windows build {WasapiProcessLoopbackInterop.MinimumBuildNumber} or later on x64 (this is build {Environment.OSVersion.Version.Build}).",
                seconds,
                processId);
        }

        AudioMonitorRing? tapRing = null;
        if (measureLatency)
        {
            tapRing = AudioMonitorTap.TryArm(
                checked((int)((seconds + 2.0) * SampleRate)),
                checked((int)((seconds + 2.0) * PacketsPerSecondBudget)));
            if (tapRing is null)
            {
                return Failed("tap-busy", "Another latency measurement already has the capture-delivery tap armed; wait for it to finish.", seconds, processId);
            }
        }

        LoopbackCapture capture;
        try
        {
            capture = await RunCaptureThreadAsync(seconds, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (tapRing is not null)
            {
                AudioMonitorTap.Disarm(tapRing);
            }
        }

        if (capture.Failure is not null)
        {
            return Failed(capture.Failure.Status, capture.Failure.Message, seconds, processId, capture.Failure.Stage, capture.Failure.HResult);
        }

        var tap = tapRing?.Snapshot(Stopwatch.Frequency);
        var diagnostics = new AudioProbeDiagnostics(
            capture.PacketCount,
            capture.Frames,
            Math.Round(capture.ActivationMs, 2),
            Math.Round(capture.FirstPacketMs, 2),
            capture.WaitTimeouts,
            capture.Truncated,
            tap is null ? 0 : tap.Samples.Length / Channels,
            tap?.Packets.Length ?? 0,
            tapRing?.RejectedPackets ?? 0);

        if (capture.Frames == 0)
        {
            return new AudioOutputProbeResult(
                "no-audio-rendered",
                "The process loopback delivered no packets at all during the capture (the audio endpoint may be changing or unavailable).",
                seconds,
                0,
                processId,
                SourceDescription,
                null,
                0,
                null,
                null,
                diagnostics,
                null,
                null);
        }

        var framesCaptured = checked((int)capture.Frames);
        var (analysis, latency) = await Task.Run(
            () =>
            {
                var samples = capture.Samples.AsSpan(0, framesCaptured * Channels);
                var packets = capture.Packets.AsSpan(0, capture.PacketCount);
                var analysisResult = AudioSignalAnalyzer.Analyze(samples, packets, SampleRate);
                AudioLatencyResult? latencyResult = null;
                if (measureLatency)
                {
                    latencyResult = tap is null
                        ? AudioLatencyAnalyzer.Measure(ReadOnlySpan<float>.Empty, ReadOnlySpan<AudioPacket>.Empty, samples, packets, SampleRate)
                        : AudioLatencyAnalyzer.Measure(tap.Samples, tap.Packets, samples, packets, SampleRate);
                }

                return (analysisResult, latencyResult);
            },
            CancellationToken.None).ConfigureAwait(false);

        string? writtenPath = null;
        long wavBytes = 0;
        string? wavError = null;
        if (!string.IsNullOrWhiteSpace(wavPath))
        {
            try
            {
                var fullPath = Path.GetFullPath(wavPath);
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var wavSamples = capture.Samples;
                await Task.Run(
                    () =>
                    {
                        using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                        AudioWavWriter.WriteFloat(stream, wavSamples.AsSpan(0, framesCaptured * Channels), Channels, SampleRate);
                        stream.Flush();
                        wavBytes = stream.Length;
                    },
                    CancellationToken.None).ConfigureAwait(false);
                writtenPath = fullPath;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                wavError = $"The WAV could not be written to '{wavPath}': {ex.Message}";
            }
        }

        // The loopback stream delivers zero-filled packets while this process renders nothing (observed with a scratch harness on the live mixer, not
        // yet through the app: continuous 10 ms packets of exact zeros), so silence is the only evidence that no audio
        // was rendered (a capture with no packets at all is reported separately above).
        var capturedSeconds = Math.Round(capture.Frames / (double)SampleRate, 3);
        var silent = analysis.Verdict == "silent";
        var status = wavError is not null ? "wav-write-failed" : silent ? "no-audio-rendered" : "captured";
        var message = wavError ?? (silent
            ? $"No audio was rendered: the app's output was silent for the whole {capturedSeconds:F2} s capture " +
              $"(peak {Math.Max(analysis.LeftPeakDbfs, analysis.RightPeakDbfs):F1} dBFS). Preview audio is off or muted, or the source is silent."
            : DescribeCapture(capturedSeconds, analysis, latency, writtenPath));
        return new AudioOutputProbeResult(
            status,
            message,
            seconds,
            capturedSeconds,
            processId,
            SourceDescription,
            writtenPath,
            wavBytes,
            analysis,
            latency,
            diagnostics,
            wavError is null ? null : "wav",
            null);
    }

    private static string DescribeCapture(double capturedSeconds, AudioAnalysis analysis, AudioLatencyResult? latency, string? wavPath)
    {
        var text = $"Captured {capturedSeconds:F2} s of this app's output: verdict {analysis.Verdict}" +
                   (analysis.Findings.Count == 0 ? string.Empty : $" ({string.Join(", ", analysis.Findings)})") +
                   $", level L {analysis.LeftRmsDbfs:F1} / R {analysis.RightRmsDbfs:F1} dBFS RMS.";
        if (latency is not null)
        {
            text += latency.LatencyMs is { } latencyMs
                ? $" Capture-to-mixer latency {latencyMs:F2} ms (confidence {latency.Confidence:F2})."
                : $" Latency {latency.Status}: {latency.Reason}";
        }

        if (wavPath is not null)
        {
            text += $" WAV: {wavPath}.";
        }

        return text;
    }

    private static AudioOutputProbeResult Failed(
        string status,
        string message,
        double requestedSeconds,
        int processId,
        string? stage = null,
        string? hresult = null) =>
        new(status, message, requestedSeconds, 0, processId, SourceDescription, null, 0, null, null, null, stage, hresult);

    private sealed record CaptureFailure(string Status, string Message, string Stage, string? HResult);

    private sealed class LoopbackCapture
    {
        public float[] Samples = Array.Empty<float>();
        public AudioPacket[] Packets = Array.Empty<AudioPacket>();
        public int PacketCount;
        public long Frames;
        public double ActivationMs;
        public double FirstPacketMs;
        public int WaitTimeouts;
        public bool Truncated;
        public CaptureFailure? Failure;
    }

    private static Task<LoopbackCapture> RunCaptureThreadAsync(double seconds, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<LoopbackCapture>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.TrySetResult(RunCapture(seconds, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                completion.TrySetResult(new LoopbackCapture
                {
                    Failure = new CaptureFailure("capture-failed", $"Process loopback capture failed: {ex.GetType().Name}: {ex.Message}", "capture", HResultText(ex))
                });
            }
        })
        {
            IsBackground = true,
            Name = "AudioOutputProbe.Capture",
            Priority = ThreadPriority.AboveNormal
        };
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        return completion.Task;
    }

    private static LoopbackCapture RunCapture(double seconds, CancellationToken cancellationToken)
    {
        var result = new LoopbackCapture();
        var activationClock = Stopwatch.StartNew();
        IAudioClient? client = null;
        IAudioCaptureClient? captureClient = null;
        AutoResetEvent? captureEvent = null;
        var formatPtr = IntPtr.Zero;
        var started = false;
        try
        {
            try
            {
                client = WasapiProcessLoopbackInterop.Activate(Environment.ProcessId, ActivationTimeout);
            }
            catch (WasapiProcessLoopbackActivationException ex)
            {
                result.Failure = new CaptureFailure("activation-failed", $"Process loopback activation failed at stage '{ex.Stage}': {ex.Message}", ex.Stage, $"0x{ex.HResult:X8}");
                return result;
            }

            formatPtr = WasapiComInterop.AllocFloatStereo48kFormat();
            var hr = client.Initialize(
                WasapiComInterop.AUDCLNT_SHAREMODE_SHARED,
                WasapiComInterop.AUDCLNT_STREAMFLAGS_LOOPBACK |
                WasapiComInterop.AUDCLNT_STREAMFLAGS_EVENTCALLBACK |
                WasapiComInterop.AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM,
                200_000, // 20 ms, the documented process-loopback sample value
                0,
                formatPtr,
                IntPtr.Zero);
            if (hr < 0)
            {
                result.Failure = new CaptureFailure("initialize-failed", $"IAudioClient.Initialize on the process loopback client failed with HRESULT 0x{hr:X8}.", "initialize", $"0x{hr:X8}");
                return result;
            }

            captureEvent = new AutoResetEvent(false);
            hr = client.SetEventHandle(captureEvent.SafeWaitHandle.DangerousGetHandle());
            if (hr < 0)
            {
                result.Failure = new CaptureFailure("initialize-failed", $"IAudioClient.SetEventHandle failed with HRESULT 0x{hr:X8}.", "event-handle", $"0x{hr:X8}");
                return result;
            }

            var captureIid = WasapiComInterop.IID_IAudioCaptureClient;
            hr = client.GetService(ref captureIid, out var service);
            if (hr < 0 || service is not IAudioCaptureClient resolved)
            {
                result.Failure = new CaptureFailure("initialize-failed", $"IAudioClient.GetService(IAudioCaptureClient) failed with HRESULT 0x{hr:X8}.", "capture-client", $"0x{hr:X8}");
                return result;
            }

            captureClient = resolved;
            result.ActivationMs = activationClock.Elapsed.TotalMilliseconds;

            var capacityFrames = checked((int)((seconds + 1.0) * SampleRate));
            result.Samples = new float[capacityFrames * Channels];
            result.Packets = new AudioPacket[checked((int)((seconds + 1.0) * PacketsPerSecondBudget) + 64)];

            hr = client.Start();
            if (hr < 0)
            {
                result.Failure = new CaptureFailure("capture-failed", $"IAudioClient.Start failed with HRESULT 0x{hr:X8}.", "start", $"0x{hr:X8}");
                return result;
            }

            started = true;
            var waitHandle = captureEvent.SafeWaitHandle.DangerousGetHandle();
            var captureClock = Stopwatch.StartNew();
            var previousEnd100Ns = 0L;
            while (captureClock.Elapsed.TotalSeconds < seconds && !result.Truncated)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (WasapiComInterop.WaitForSingleObject(waitHandle, WaitSliceMs) == WasapiComInterop.WaitTimeout)
                {
                    result.WaitTimeouts++;
                }

                DrainPackets(captureClient, result, captureClock, ref previousEnd100Ns);
            }

            return result;
        }
        finally
        {
            if (started)
            {
                try
                {
                    client?.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Log($"AudioOutputProbe: loopback stop warning - {ex.Message}");
                }
            }

            if (formatPtr != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(formatPtr);
            }

            captureEvent?.Dispose();
            WasapiComInterop.ReleaseComObject(ref captureClient);
            WasapiComInterop.ReleaseComObject(ref client);
        }
    }

    private static unsafe void DrainPackets(IAudioCaptureClient captureClient, LoopbackCapture result, Stopwatch captureClock, ref long previousEnd100Ns)
    {
        for (var pass = 0; pass < MaxPacketsPerDrain; pass++)
        {
            WasapiComInterop.ThrowIfFailed(captureClient.GetNextPacketSize(out var packetFrames), "IAudioCaptureClient.GetNextPacketSize");
            if (packetFrames == 0)
            {
                return;
            }

            WasapiComInterop.ThrowIfFailed(
                captureClient.GetBuffer(out var data, out var frames, out var flags, out _, out var qpcPosition),
                "IAudioCaptureClient.GetBuffer");
            try
            {
                if (frames == 0)
                {
                    continue;
                }

                if (result.PacketCount >= result.Packets.Length || (result.Frames + frames) * Channels > result.Samples.Length)
                {
                    result.Truncated = true;
                    return;
                }

                if (result.Frames == 0)
                {
                    result.FirstPacketMs = captureClock.Elapsed.TotalMilliseconds;
                }

                var destination = result.Samples.AsSpan(checked((int)result.Frames * Channels), (int)frames * Channels);
                if ((flags & WasapiComInterop.AUDCLNT_BUFFERFLAGS_SILENT) != 0)
                {
                    destination.Clear();
                }
                else
                {
                    new ReadOnlySpan<float>((void*)data, destination.Length).CopyTo(destination);
                }

                // A packet whose timestamp the engine flagged as unreliable (reported as zero) continues the previous one;
                // the flag stays on the packet so the analysis still counts it. A stream with no timestamps at all is
                // refused by the latency analyzer, not guessed here.
                var start100Ns = checked((long)qpcPosition);
                if (start100Ns == 0 && previousEnd100Ns > 0)
                {
                    start100Ns = previousEnd100Ns;
                }

                result.Packets[result.PacketCount++] = new AudioPacket(result.Frames, (int)frames, start100Ns, flags);
                result.Frames += frames;
                if (start100Ns > 0)
                {
                    previousEnd100Ns = start100Ns + (long)Math.Round(frames * (double)QpcMath.HundredNsPerSecond / SampleRate);
                }
            }
            finally
            {
                WasapiComInterop.ThrowIfFailed(captureClient.ReleaseBuffer(frames), "IAudioCaptureClient.ReleaseBuffer");
            }
        }
    }

    private static string? HResultText(Exception ex) => ex is COMException com ? $"0x{com.HResult:X8}" : null;
}
