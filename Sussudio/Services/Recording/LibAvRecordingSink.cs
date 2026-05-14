using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
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

    private void CompleteWriter<TPacket>(Channel<TPacket>? channel)
    {
        channel?.Writer.TryComplete();
        SignalWork("complete_writer");
    }
}
