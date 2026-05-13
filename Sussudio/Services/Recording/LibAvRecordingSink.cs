using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using FFmpeg.AutoGen;
using Sussudio.Services.Capture;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

// Bounded-queue recording sink that isolates capture callbacks from libav.
// Capture threads enqueue raw/GPU/CUDA video and audio quickly; one encoding
// task drains the queues and serializes every LibAvEncoder call.
public sealed partial class LibAvRecordingSink : IRecordingSink, IRawVideoFrameEncoder, IRawVideoFrameTryEncoder, IRawVideoFrameLeaseEncoder, IRawVideoFrameLeaseTryEncoder, IGpuVideoFrameEncoder, IGpuVideoFrameTryEncoder, ICudaVideoFrameEncoder
{
    private const int VideoQueueCapacity = 360;
    private const int AudioQueueCapacity = 3600;
    private const int GpuQueueCapacity = 4;
    private const int CudaQueueCapacity = 12;
    private const int VideoDrainBatchLimit = 24;
    private const int AudioDrainBatchLimit = 128;
    private const int GpuDrainBatchLimit = 16;
    private const int CudaDrainBatchLimit = 16;
    private const int StopTimeoutMs = 30_000;
    // Emergency path uses a tighter encode-drain budget so the total emergency
    // stop fits well within App.TryEmergencyStopRecording's 8s wrapper (fix #12).
    // Normal user-stop keeps the 30s budget so saturated 4K queues can drain.
    private const int EmergencyStopTimeoutMs = 5_000;
    private const int DisposeTimeoutMs = 1_000;
    private const int VideoQueueLatencyWindowSize = 256;

    private readonly object _sync = new();
    private readonly object _videoQueueSync = new();
    private readonly LibAvEncoder _encoder = new();
    private readonly SemaphoreSlim _workAvailable = new(0, 1);
    private Channel<VideoFramePacket>? _videoQueue;
    private Channel<AudioSamplePacket>? _audioQueue;
    private Channel<AudioSamplePacket>? _microphoneQueue;
    private Channel<GpuFramePacket>? _gpuQueue;
    private Channel<CudaFramePacket>? _cudaQueue;

    // The work semaphore is a wake-up signal, not a count of queued packets.
    // The encoding loop drains audio around bounded video batches so video
    // backlog cannot starve HDMI or microphone audio.
    private CancellationTokenSource? _cts;
    private Task? _encodingTask;
    private RecordingContext? _context;
    private Exception? _encodingFailure;
    private int _width;
    private int _height;
    private bool _audioEnabled;
    private bool _microphoneEnabled;
    private bool _started;
    private bool _disposed;
    private int _disposeFinalized;
    private int _deferredDisposeScheduled;
    private long _droppedVideoFrames;
    private long _encodedVideoFrames;
    private long _videoFramesEnqueued;
    private long _videoFramesSubmittedToEncoder;
    private long _videoDropsQueueSaturated;
    private long _videoDropsBacklogEviction;
    private long _audioDropsQueueSaturated;
    private long _audioDropsBacklogEviction;
    private long _microphoneDropsQueueSaturated;
    private long _microphoneDropsBacklogEviction;
    private long _gpuFramesEnqueued;
    private long _gpuFramesDropped;
    private long _cudaFramesEnqueued;
    private long _cudaFramesDropped;
    private int _videoQueueDepth;
    private int _videoQueueMaxDepth;
    private int _audioQueueDepth;
    private int _microphoneQueueDepth;
    private int _gpuQueueDepth;
    private int _gpuQueueMaxDepth;
    private int _cudaQueueDepth;
    private int _cudaQueueMaxDepth;
    private long _lastVideoEnqueueTick;
    private long _lastVideoWriteTick;
    private readonly VideoQueueLatencyTracker _videoLatencyTracker;
    private bool _gpuEncodingEnabled;

    public LibAvRecordingSink()
    {
        _videoLatencyTracker = new VideoQueueLatencyTracker(
            "LIBAV_SINK", _videoQueueSync, VideoQueueLatencyWindowSize);
    }
    private bool _cudaEncodingEnabled;

    public event EventHandler<long>? FrameEncoded;

    /// <summary>
    /// Invoked on the encoding thread when the encoding loop fails fatally.
    /// Allows CaptureService to immediately surface the failure to the UI
    /// instead of silently dropping all subsequent frames until Stop is called.
    /// </summary>
    public Action<Exception>? OnEncodingFailed { get; set; }

    public long DroppedVideoFrames =>
        Interlocked.Read(ref _droppedVideoFrames) +
        Interlocked.Read(ref _gpuFramesDropped) +
        Interlocked.Read(ref _cudaFramesDropped) +
        _encoder.DroppedFrameCount;
    public long EncodedVideoFrames => Interlocked.Read(ref _encodedVideoFrames);
    public long AudioSamplesReceived => _encoder.AudioSamplesReceived;
    public long MicrophoneSamplesReceived => _encoder.MicrophoneSamplesReceived;
    public long OutputBytes => _encoder.TotalBytesWritten;
    public string OutputPath => _context?.FinalOutputPath ?? _encoder.OutputPath;
    public int VideoQueueCount => Volatile.Read(ref _videoQueueDepth);
    public int VideoQueueCapacityFrames => VideoQueueCapacity;
    public int VideoQueueMaxDepth => Volatile.Read(ref _videoQueueMaxDepth);
    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);
    public int MicrophoneQueueCount => Volatile.Read(ref _microphoneQueueDepth);
    public long VideoFramesEnqueuedCount => Interlocked.Read(ref _videoFramesEnqueued);
    public long VideoFramesSubmittedToEncoder => Interlocked.Read(ref _videoFramesSubmittedToEncoder);
    public long VideoEncoderPts => _encoder.NextVideoPts;
    public long VideoEncoderPacketsWritten => _encoder.VideoPacketsWritten;
    public long VideoEncoderDroppedFrames => _encoder.DroppedFrameCount;
    public long VideoSequenceGaps => _videoLatencyTracker.SequenceGaps;
    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);
    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);
    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);
    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);
    public long LastVideoEnqueueTick => Interlocked.Read(ref _lastVideoEnqueueTick);
    public long LastVideoWriteTick => Interlocked.Read(ref _lastVideoWriteTick);
    public long LastVideoQueueLatencyMs => _videoLatencyTracker.LastLatencyMs;
    public long VideoQueueOldestFrameAgeMs => _videoLatencyTracker.GetOldestFrameAgeMs(Volatile.Read(ref _videoQueueDepth));
    public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics => _videoLatencyTracker.GetMetrics();
    public int VideoQueueLatencySampleCount => _videoLatencyTracker.GetMetrics().SampleCount;
    public double VideoQueueLatencyAvgMs => _videoLatencyTracker.GetMetrics().AverageMs;
    public double VideoQueueLatencyP95Ms => _videoLatencyTracker.GetMetrics().P95Ms;
    public double VideoQueueLatencyP99Ms => _videoLatencyTracker.GetMetrics().P99Ms;
    public double VideoQueueLatencyMaxMs => _videoLatencyTracker.GetMetrics().MaxMs;
    public long VideoBackpressureWaitMs => _videoLatencyTracker.BackpressureWaitMs;
    public long VideoBackpressureEvents => _videoLatencyTracker.BackpressureEvents;
    public long LastVideoBackpressureWaitMs => _videoLatencyTracker.LastBackpressureWaitMs;
    public long MaxVideoBackpressureWaitMs => _videoLatencyTracker.MaxBackpressureWaitMs;
    public bool GpuEncodingEnabled => Volatile.Read(ref _gpuEncodingEnabled);
    public bool CudaEncodingEnabled => Volatile.Read(ref _cudaEncodingEnabled);
    public int GpuQueueCount => Volatile.Read(ref _gpuQueueDepth);
    public int GpuQueueCapacityFrames => GpuQueueCapacity;
    public int GpuQueueMaxDepth => Volatile.Read(ref _gpuQueueMaxDepth);
    public long GpuFramesEnqueued => Interlocked.Read(ref _gpuFramesEnqueued);
    public long GpuFramesDropped => Interlocked.Read(ref _gpuFramesDropped);
    public int CudaQueueCount => Volatile.Read(ref _cudaQueueDepth);
    public int CudaQueueCapacityFrames => CudaQueueCapacity;
    public int CudaQueueMaxDepth => Volatile.Read(ref _cudaQueueMaxDepth);
    public long CudaFramesEnqueued => Interlocked.Read(ref _cudaFramesEnqueued);
    public long CudaFramesDropped => Interlocked.Read(ref _cudaFramesDropped);
    public bool EncodingFailed => Volatile.Read(ref _encodingFailure) != null;
    public string? EncodingFailureType => Volatile.Read(ref _encodingFailure)?.GetType().Name;
    public string? EncodingFailureMessage => Volatile.Read(ref _encodingFailure)?.Message;
    internal Task EncodingCompletionTask => _encodingTask ?? Task.CompletedTask;

    public bool TryGetEncoderAvSyncDrift(out double driftMs, out long correctionSamples)
        => _encoder.TryGetCurrentAvSyncDrift(out driftMs, out correctionSamples);

    public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_started)
            {
                throw new InvalidOperationException("LibAv recording sink has already started.");
            }
        }

        try
        {
            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

            _microphoneEnabled = !string.IsNullOrWhiteSpace(context.MicrophoneDeviceName);
            var options = CreateOptions(context);
            _encoder.Initialize(options);
            if (_encoder.UseCudaHardwareFrames)
            {
                _cudaQueue = Channel.CreateBounded<CudaFramePacket>(new BoundedChannelOptions(CudaQueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true
                });
                _cudaEncodingEnabled = true;
                Logger.Log("LIBAV_SINK_CUDA_QUEUE_INIT capacity=" + CudaQueueCapacity);
            }
            else if (_encoder.UseHardwareFrames)
            {
                _gpuQueue = Channel.CreateBounded<GpuFramePacket>(new BoundedChannelOptions(GpuQueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true
                });
                _gpuEncodingEnabled = true;
                Logger.Log("LIBAV_SINK_GPU_QUEUE_INIT capacity=" + GpuQueueCapacity);
            }

            _videoQueue = Channel.CreateBounded<VideoFramePacket>(new BoundedChannelOptions(VideoQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            _audioQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            if (_microphoneEnabled)
            {
                _microphoneQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
            }
            _cts = new CancellationTokenSource();
            _context = context;
            _encodingFailure = null;
            _width = checked((int)context.EffectiveWidth);
            _height = checked((int)context.EffectiveHeight);
            _audioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName);
            Interlocked.Exchange(ref _droppedVideoFrames, 0);
            Interlocked.Exchange(ref _encodedVideoFrames, 0);
            Interlocked.Exchange(ref _videoFramesEnqueued, 0);
            Interlocked.Exchange(ref _videoFramesSubmittedToEncoder, 0);
            Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
            Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
            Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
            Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
            Interlocked.Exchange(ref _microphoneDropsQueueSaturated, 0);
            Interlocked.Exchange(ref _microphoneDropsBacklogEviction, 0);
            Interlocked.Exchange(ref _gpuFramesEnqueued, 0);
            Interlocked.Exchange(ref _gpuFramesDropped, 0);
            Interlocked.Exchange(ref _cudaFramesEnqueued, 0);
            Interlocked.Exchange(ref _cudaFramesDropped, 0);
            Interlocked.Exchange(ref _videoQueueMaxDepth, 0);
            Interlocked.Exchange(ref _gpuQueueMaxDepth, 0);
            Interlocked.Exchange(ref _cudaQueueMaxDepth, 0);
            Interlocked.Exchange(ref _videoQueueDepth, 0);
            Interlocked.Exchange(ref _audioQueueDepth, 0);
            Interlocked.Exchange(ref _microphoneQueueDepth, 0);
            Interlocked.Exchange(ref _gpuQueueDepth, 0);
            Interlocked.Exchange(ref _cudaQueueDepth, 0);
            Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);
            Interlocked.Exchange(ref _lastVideoWriteTick, 0);
            ResetVideoDiagnostics();
            _encodingTask = Task.Factory.StartNew(
                () => EncodingLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            lock (_sync)
            {
                _started = true;
            }

            Logger.Log(
                $"LIBAV_SINK_START output='{context.FinalOutputPath}' codec='{options.CodecName}' " +
                $"width={_width} height={_height} fps={context.EffectiveFrameRate:0.###} p010={context.HdrPipelineActive} audio={_audioEnabled} microphone={_microphoneEnabled}");
            return Task.CompletedTask;
        }
        catch
        {
            /* Cleanup must not throw — tear down partially-initialized queues/state before re-throwing */
            CompleteWriter(_videoQueue);
            CompleteWriter(_audioQueue);
            CompleteWriter(_microphoneQueue);
            CompleteWriter(_gpuQueue);
            CompleteWriter(_cudaQueue);
            _videoQueue = null;
            _audioQueue = null;
            _microphoneQueue = null;
            _gpuQueue = null;
            _cudaQueue = null;
            _gpuEncodingEnabled = false;
            _cudaEncodingEnabled = false;
            _cts?.Dispose();
            _cts = null;
            _encodingTask = null;
            _context = null;
            _width = 0;
            _height = 0;
            _audioEnabled = false;
            _microphoneEnabled = false;
            lock (_sync)
            {
                _started = false;
            }

            _encoder.Dispose();
            throw;
        }
    }

    // Public path used by normal recording-stop (UI Stop button, automation StopRecording).
    // Keeps the 30s StopTimeoutMs drain budget so saturated 4K 60fps queues can drain
    // cleanly without triggering the fix #11 emergency-flush fallback.
    public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
        => StopCoreAsync(emergency: false, cancellationToken);

    // Internal overload used by the emergency-stop path (CaptureService.StopRecordingAsync
    // when called from CaptureSessionCoordinator.StopRecordingForEmergencyAsync).
    // Uses EmergencyStopTimeoutMs (5s) so the encode-drain fits inside App.xaml.cs's 8s
    // emergency-stop wrapper (fix #12).
    internal Task<FinalizeResult> StopAsync(bool emergency, CancellationToken cancellationToken = default)
        => StopCoreAsync(emergency, cancellationToken);

    private async Task<FinalizeResult> StopCoreAsync(bool emergency, CancellationToken cancellationToken)
    {
        var context = _context;
        var outputPath = context?.FinalOutputPath ?? OutputPath;

        if (_disposed)
        {
            return FinalizeResult.Success(outputPath, "Stopped");
        }

        lock (_sync)
        {
            _started = false;
        }

        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CompleteWriter(_cudaQueue);

        if (_encodingTask != null)
        {
            var drainTimeoutMs = emergency ? EmergencyStopTimeoutMs : StopTimeoutMs;
            var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(drainTimeoutMs, cancellationToken)).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _encodingTask))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Cancel the encoding loop so it stops processing new frames and
                // exits via OperationCanceledException — this must happen before
                // FlushAndClose so the two don't race on _encoder state.
                _cts?.Cancel();

                // Give the encoding task a brief window to unblock from its
                // cancellation-token wait and exit cleanly.  DisposeTimeoutMs (1 s)
                // is sufficient when the encoding loop is parked at its token-aware
                // wait site (_workAvailable.Wait), but the loop does NOT poll
                // cancellation inside the inner drain loops — if it's wedged in a
                // native libav call (avcodec_send_frame, av_interleaved_write_frame),
                // the grace expires while the loop is still touching _encoder /
                // _videoCodecCtx / _formatCtx. Flushing concurrently in that case
                // races on unsynchronized native state and can corrupt the file or
                // raise an SEH access violation that managed `catch` cannot intercept.
                // Gate the flush on _encodingTask having actually completed; if it
                // hasn't, skip the flush and accept a cleanly-truncated output.
                var graceResult = await Task.WhenAny(_encodingTask, Task.Delay(DisposeTimeoutMs)).ConfigureAwait(false);
                if (ReferenceEquals(graceResult, _encodingTask))
                {
                    // Encoder loop has exited — safe to flush. Wrap in try/catch
                    // since FlushAndClose can itself throw if the encoder is in a
                    // broken state; in that case the file is still truncated but
                    // the error has been surfaced to the caller via Failure below.
                    try
                    {
                        _encoder.FlushAndClose();
                    }
                    catch (Exception flushEx)
                    {
                        Logger.Log($"LIBAV_SINK_STOP_DRAIN_FLUSH_FAIL type={flushEx.GetType().Name} msg={flushEx.Message}");
                    }
                }
                else
                {
                    Logger.Log("LIBAV_SINK_STOP_DRAIN_FLUSH_SKIPPED reason=encoder_task_still_running");
                }

                return FinalizeResult.Failure(outputPath, "Stopped (libav encode drain timed out; emergency flush attempted)");
            }

            try
            {
                await _encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure = ex;
            }
        }
        else
        {
            _encoder.FlushAndClose();
        }

        if (_encodingFailure != null)
        {
            Logger.Log($"LIBAV_SINK_STOP_FAIL type={_encodingFailure.GetType().Name} msg={_encodingFailure.Message}");
            return FinalizeResult.Failure(outputPath, $"Stopped (libav encode failed: {_encodingFailure.Message})");
        }

        if (!TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure))
        {
            Logger.Log($"LIBAV_SINK_STOP_OUTPUT_INVALID output='{outputPath}' reason='{outputFailure}'");
            return FinalizeResult.Failure(outputPath, $"Stopped (output file invalid: {outputFailure})");
        }

        if (context?.HdrPipelineActive == true)
        {
            var (validationSucceeded, validationDetail) = await HdrValidationRunner
                .RunAsync(context, outputPath, cancellationToken)
                .ConfigureAwait(false);

            if (!validationSucceeded)
            {
                if (validationDetail.Contains("validator-script-missing", StringComparison.Ordinal))
                {
                    Logger.Log($"HDR validation skipped (script not found): {validationDetail}");
                }
                else
                {
                    return FinalizeResult.Failure(
                        outputPath,
                        $"Stopped (hdr validation failed: {validationDetail})",
                        new[] { outputPath });
                }
            }
        }

        Logger.Log(
            $"LIBAV_SINK_STOP output='{outputPath}' bytes={outputBytes} frames={EncodedVideoFrames} dropped={DroppedVideoFrames} audio_samples={AudioSamplesReceived} mic_samples={MicrophoneSamplesReceived}");
        return FinalizeResult.Success(outputPath, "Stopped");
    }

    // REVIEWED 2026-04-07: IDisposable fallback only — all callers use DisposeAsync.
    // CaptureService cleanup awaits StopAsync/DisposeAsync on background thread.
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_sync)
        {
            _started = false;
        }

        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CompleteWriter(_cudaQueue);
        _cts?.Cancel();

        if (_encodingTask == null)
        {
            FinalizeDisposeCore();
            return;
        }

        var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(DisposeTimeoutMs)).ConfigureAwait(false);
        if (ReferenceEquals(completedTask, _encodingTask))
        {
            ObserveEncodingTaskCompletion(_encodingTask);
            FinalizeDisposeCore();
            return;
        }

        Logger.Log($"LIBAV_SINK_DISPOSE_DEFERRED timeout_ms={DisposeTimeoutMs}");
        ScheduleDeferredDisposeCleanup(_encodingTask);
    }

    private void ScheduleDeferredDisposeCleanup(Task encodingTask)
    {
        if (Interlocked.CompareExchange(ref _deferredDisposeScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure ??= ex;
            }
            finally
            {
                FinalizeDisposeCore();
                Logger.Log("LIBAV_SINK_DISPOSE_DEFERRED_COMPLETE");
            }
        });
    }

    private void ObserveEncodingTaskCompletion(Task encodingTask)
    {
        try
        {
            encodingTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _encodingFailure ??= ex;
        }
    }

    private void FinalizeDisposeCore()
    {
        if (Interlocked.CompareExchange(ref _disposeFinalized, 1, 0) != 0)
        {
            return;
        }

        ReturnRemainingVideoBuffers(_videoQueue);
        ReturnRemainingBuffers(_audioQueue);
        ReturnRemainingBuffers(_microphoneQueue);
        ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);
        ReturnRemainingCudaFrames(_cudaQueue, ref _cudaQueueDepth);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _audioQueueDepth, 0);
        Interlocked.Exchange(ref _microphoneQueueDepth, 0);
        Interlocked.Exchange(ref _gpuQueueDepth, 0);
        Interlocked.Exchange(ref _cudaQueueDepth, 0);

        _cts?.Dispose();
        _cts = null;
        _videoQueue = null;
        _audioQueue = null;
        _microphoneQueue = null;
        _gpuQueue = null;
        _cudaQueue = null;
        _gpuEncodingEnabled = false;
        _cudaEncodingEnabled = false;
        _microphoneEnabled = false;
        _encodingTask = null;
        _workAvailable.Dispose();

        try
        {
            _encoder.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"LIBAV_SINK_DISPOSE_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static string MapCodecName(RecordingFormat format)
        => MediaFormat.MapNvencCodecName(format);

    private static (int? Numerator, int? Denominator) ResolveFrameRateParts(RecordingContext context)
    {
        if (string.IsNullOrWhiteSpace(context.FrameRateArg) || !context.FrameRateArg.Contains('/', StringComparison.Ordinal))
        {
            return (null, null);
        }

        var parts = context.FrameRateArg.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var numerator) ||
            !int.TryParse(parts[1], out var denominator) ||
            numerator <= 0 ||
            denominator <= 0)
        {
            return (null, null);
        }

        return (numerator, denominator);
    }

    private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)
    {
        outputBytes = 0;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            failureMessage = "output path is empty";
            return false;
        }

        try
        {
            if (!File.Exists(outputPath))
            {
                failureMessage = "output file is missing";
                return false;
            }

            outputBytes = new FileInfo(outputPath).Length;
            if (outputBytes <= 0)
            {
                failureMessage = "output file is empty";
                return false;
            }
        }
        catch (Exception ex)
        {
            failureMessage = $"output file length unavailable: {ex.Message}";
            Logger.Log($"LIBAV_SINK_STOP_OUTPUT_VALIDATE_WARN output='{outputPath}' type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private void CompleteWriter<TPacket>(Channel<TPacket>? channel)
    {
        channel?.Writer.TryComplete();
        SignalWork("complete_writer");
    }

    private LibAvEncoderOptions CreateOptions(RecordingContext context)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveFrameRateParts(context);
        return new LibAvEncoderOptions
        {
            OutputPath = context.VideoOutputPath,
            CodecName = MapCodecName(context.Settings.Format),
            Width = checked((int)context.EffectiveWidth),
            Height = checked((int)context.EffectiveHeight),
            FrameRate = context.EffectiveFrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.Settings.GetTargetBitrate(),
            IsP010 = context.HdrPipelineActive,
            NvencPreset = context.Settings.NvencPreset.ToString(),
            SplitEncodeMode = SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode),
            HdrEnabled = context.HdrPipelineActive,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            CudaHwDeviceCtxPtr = context.CudaHwDeviceCtxPtr,
            CudaHwFramesCtxPtr = context.CudaHwFramesCtxPtr,
            AudioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName),
            AudioSampleRate = 48_000,
            AudioChannels = 2,
            AudioBitRate = 320_000,
            MicrophoneEnabled = _microphoneEnabled,
            MicrophoneSampleRate = 48_000,
            MicrophoneChannels = 2,
            MicrophoneBitRate = 320_000
        };
    }

    private void EncodingLoop(CancellationToken cancellationToken)
    {
        try
        {
            var videoQueue = _videoQueue ?? throw new InvalidOperationException("Video queue is not initialized.");
            var audioQueue = _audioQueue ?? throw new InvalidOperationException("Audio queue is not initialized.");
            var microphoneQueue = _microphoneQueue;
            var gpuQueue = _gpuQueue;
            var cudaQueue = _cudaQueue;

            while (true)
            {
                var madeProgress = false;

                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                if (cudaQueue != null)
                {
                    madeProgress = DrainCudaPackets(cudaQueue.Reader, CudaDrainBatchLimit) || madeProgress;
                }
                if (gpuQueue != null)
                {
                    madeProgress = DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit) || madeProgress;
                }

                madeProgress = DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit) || madeProgress;

                // Audio again catches samples that arrived while video encoding
                // was consuming its bounded batch.
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                if (videoQueue.Reader.Completion.IsCompleted &&
                    audioQueue.Reader.Completion.IsCompleted &&
                    (microphoneQueue == null || microphoneQueue.Reader.Completion.IsCompleted) &&
                    (gpuQueue == null || gpuQueue.Reader.Completion.IsCompleted) &&
                    (cudaQueue == null || cudaQueue.Reader.Completion.IsCompleted) &&
                    Volatile.Read(ref _videoQueueDepth) == 0 &&
                    Volatile.Read(ref _audioQueueDepth) == 0 &&
                    Volatile.Read(ref _microphoneQueueDepth) == 0 &&
                    Volatile.Read(ref _gpuQueueDepth) == 0 &&
                    Volatile.Read(ref _cudaQueueDepth) == 0)
                {
                    break;
                }

                if (madeProgress)
                {
                    continue;
                }

                _workAvailable.Wait(cancellationToken);
            }

            _encoder.FlushAndClose();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ReturnRemainingVideoBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);
            ReturnRemainingCudaFrames(_cudaQueue, ref _cudaQueueDepth);
        }
        catch (Exception ex)
        {
            _encodingFailure = ex;
            lock (_sync) { _started = false; }
            Logger.Log($"LIBAV_SINK_ENCODING_LOOP_FAIL type={ex.GetType().Name} msg={ex.Message}");
            ReturnRemainingVideoBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);
            ReturnRemainingCudaFrames(_cudaQueue, ref _cudaQueueDepth);
            try
            {
                _encoder.Dispose();
            }
            catch
            {
                // Preserve the original failure.
            }

            try
            {
                OnEncodingFailed?.Invoke(ex);
            }
            catch
            {
                // Best effort — callback must not mask the original failure.
            }
        }
    }

    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets)
        {
            VideoFramePacket packet;
            lock (_videoQueueSync)
            {
                if (!reader.TryRead(out packet))
                {
                    break;
                }

                _videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick);
                DecrementQueueDepth(ref _videoQueueDepth, "video");
            }

            _videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber);
            try
            {
                var frameData = packet.Lease != null
                    ? packet.Lease.Memory.Span
                    : packet.Buffer!.AsSpan(0, packet.Length);
                _encoder.SendVideoFrame(frameData, _width, _height);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            finally
            {
                ReturnVideoPacket(packet);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _gpuQueueDepth, "gpu");
            try
            {
                _encoder.SendGpuVideoFrame(packet.Texture, packet.Subresource);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            finally
            {
                Marshal.Release(packet.Texture);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _cudaQueueDepth, "cuda");
            var frame = (AVFrame*)packet.Frame;
            try
            {
                _encoder.SendCudaVideoFrame(frame);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
                var encoded = Interlocked.Increment(ref _encodedVideoFrames);
                try
                {
                    FrameEncoded?.Invoke(this, encoded);
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"LIBAV_SINK_CUDA_DRAIN_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                if (frame != null)
                {
                    ffmpeg.av_frame_free(&frame);
                }
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _audioQueueDepth, "audio");
            try
            {
                _encoder.SendAudioSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
        }

        return drainedAny;
    }

    private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader)
    {
        var drainedAny = false;
        while (reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _microphoneQueueDepth, "microphone");
            try
            {
                _encoder.SendMicrophoneSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
        }

        return drainedAny;
    }


}
