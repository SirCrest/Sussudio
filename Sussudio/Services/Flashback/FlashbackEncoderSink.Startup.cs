using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private static FlashbackSessionContext CreateSessionContext(RecordingContext context)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveFrameRateParts(context.FrameRateArg);
        return new FlashbackSessionContext
        {
            Width = checked((int)context.EffectiveWidth),
            Height = checked((int)context.EffectiveHeight),
            FrameRate = context.EffectiveFrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.Settings.GetTargetBitrate(),
            IsP010 = context.HdrPipelineActive,
            CodecName = MapCodecName(context.Settings.Format),
            NvencPreset = context.Settings.NvencPreset.ToString(),
            SplitEncodeMode = SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode),
            HdrEnabled = context.HdrPipelineActive,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            AudioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName),
            MicrophoneEnabled = !string.IsNullOrWhiteSpace(context.MicrophoneDeviceName)
        };
    }

    public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        ValidateSessionContext(context);
        if (ptsBaseOffset < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ptsBaseOffset), "PTS base offset must not be negative.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        string? startupGeneratedSegmentPath = null;

        lock (_sync)
        {
            if (_started || _encodingTask is { IsCompleted: false })
            {
                throw new InvalidOperationException("Flashback encoder sink has already started.");
            }
            _started = true;
        }

        try
        {
            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);
            var sessionFrameRate = ResolveSessionFrameRate(context.FrameRate);
            var sessionContext = context with { FrameRate = sessionFrameRate };

            var sessionId = _bufferManager.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = CreateSessionId();
                _bufferManager.Initialize(sessionId);
            }
            _bufferManager.SetSegmentExtension(GetSegmentExtension(sessionContext.CodecName));

            var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);
            if (startupGeneratedSegment)
            {
                startupGeneratedSegmentPath = tsPath;
            }
            _tsFilePath = tsPath;
            _recordingOutputPath = string.Empty;

            _encoder.Initialize(CreateOptions(sessionContext, tsPath));
            InitializeStartupQueues(sessionContext);

            _cts = new CancellationTokenSource();
            _sessionContext = sessionContext;
            _encodingFailure = null;
            _width = sessionContext.Width;
            _height = sessionContext.Height;
            _audioEnabled = sessionContext.AudioEnabled;
            _microphoneEnabled = sessionContext.MicrophoneEnabled;
            ResetEncodingCounters();
            Volatile.Write(ref _recordingActive, 0);
            // When continuing after a sink-only cycle (ptsBaseOffset > 0), we offset
            // the encoder's file-level PTS directly so segment timestamps continue from
            // the previous session. _ptsBaseOffset stays Zero because the buffer PTS
            // formula is: _ptsBaseOffset + encoder.NextVideoPts / frameRate - and the
            // encoder PTS already includes the offset.
            _ptsBaseOffset = TimeSpan.Zero;
            _segmentStartPts = ptsBaseOffset;
            _segmentDuration = _bufferManager.Options.SegmentDuration;
            _bufferManager.MarkActiveSegmentStart(tsPath, _segmentStartPts);

            if (ptsBaseOffset > TimeSpan.Zero)
            {
                var initialVideoPts = ToNonNegativeLongSaturated(ptsBaseOffset.TotalSeconds * sessionFrameRate);
                var initialAudioPts = ToNonNegativeLongSaturated(ptsBaseOffset.TotalSeconds * 48_000);
                _encoder.SetInitialPts(initialVideoPts, initialAudioPts);
                Logger.Log($"FLASHBACK_SINK_PTS_CONTINUE v_pts={initialVideoPts} a_pts={initialAudioPts} offset_s={ptsBaseOffset.TotalSeconds:F1}");
            }

            Logger.Log($"FLASHBACK_SINK_INIT_COMPLETE session='{sessionId}' gpu_encoding={_gpuEncodingEnabled} segment_duration_s={_segmentDuration.TotalSeconds:F0}");

            // Publish the encoder's frame rate as ground truth for playback pacing.
            _bufferManager.EncodeFrameRate = sessionFrameRate;

            _encodingTask = Task.Factory.StartNew(
                () => EncodingLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Logger.Log(
                $"FLASHBACK_SINK_START session='{sessionId}' output='{tsPath}' codec='{sessionContext.CodecName}' " +
                $"width={_width} height={_height} fps={sessionFrameRate:0.###} " +
                $"buffer_ms={(long)_bufferManager.Options.BufferDuration.TotalMilliseconds} " +
                $"audio={_audioEnabled} microphone={_microphoneEnabled} p010={sessionContext.IsP010}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            RollBackStartFailure(ex, startupGeneratedSegmentPath);
            throw;
        }
    }

    private static string CreateSessionId()
    {
        return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// MPEG-TS supports H.264 and HEVC natively. AV1 in MPEG-TS requires newer ffmpeg builds
    /// (libavformat 61.7+) and is not widely supported. Use fMP4 for AV1.
    /// </summary>
    private static bool SupportsTransportStream(string codecName) =>
        codecName.Contains("264", StringComparison.OrdinalIgnoreCase) ||
        codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase) ||
        codecName.Contains("265", StringComparison.OrdinalIgnoreCase);

    internal static string GetSegmentExtension(string codecName) =>
        SupportsTransportStream(codecName) ? ".ts" : ".mp4";

    private static LibAvEncoderOptions CreateOptions(FlashbackSessionContext context, string outputPath)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveSessionFrameRateParts(
            context.FrameRateNumerator,
            context.FrameRateDenominator);

        return new LibAvEncoderOptions
        {
            OutputPath = outputPath,
            ContainerFormat = SupportsTransportStream(context.CodecName) ? "mpegts" : "mp4",
            FragmentedMp4 = !SupportsTransportStream(context.CodecName),
            CodecName = context.CodecName,
            Width = context.Width,
            Height = context.Height,
            FrameRate = context.FrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.BitRate,
            IsP010 = context.IsP010,
            NvencPreset = context.NvencPreset,
            SplitEncodeMode = context.SplitEncodeMode,
            // 1-second GOP for fast interactive seeking. The default (2x frame rate)
            // means up to 2 seconds of decode-forward on every pause/scrub.
            // 1x frame rate halves worst-case seek latency with minimal bitrate impact.
            GopSize = (int)Math.Max(1, Math.Round(context.FrameRate)),
            HdrEnabled = context.HdrEnabled,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.HdrMasterDisplayMetadata,
            HdrMaxCll = context.HdrMaxCll,
            HdrMaxFall = context.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            AudioEnabled = context.AudioEnabled,
            AudioSampleRate = 48_000,
            AudioChannels = 2,
            AudioBitRate = 320_000,
            MicrophoneEnabled = context.MicrophoneEnabled,
            MicrophoneSampleRate = 48_000,
            MicrophoneChannels = 2,
            MicrophoneBitRate = 320_000
        };
    }

    private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)
    {
        if (!numerator.HasValue || !denominator.HasValue || numerator <= 0 || denominator <= 0)
        {
            return (null, null);
        }

        var fps = (double)numerator.Value / denominator.Value;
        if (!double.IsFinite(fps) || fps <= 0 || fps > MaxSessionFrameRate)
        {
            return (null, null);
        }

        return (numerator, denominator);
    }

    private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)
    {
        if (string.IsNullOrWhiteSpace(frameRateArg) || !frameRateArg.Contains('/', StringComparison.Ordinal))
        {
            return (null, null);
        }

        var parts = frameRateArg.Split('/', 2, StringSplitOptions.TrimEntries);
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

    private static string MapCodecName(RecordingFormat format)
        => MediaFormat.MapNvencCodecName(format);

    private void ResetEncodingCounters()
    {
        Interlocked.Exchange(ref _droppedVideoFrames, 0);
        Interlocked.Exchange(ref _encodedVideoFrames, 0);
        Interlocked.Exchange(ref _videoFramesEnqueued, 0);
        Interlocked.Exchange(ref _videoFramesSubmittedToEncoder, 0);
        Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _videoQueueRejectedFrames, 0);
        Volatile.Write(ref _lastVideoQueueRejectReason, null);
        Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _droppedAudioSamplesCount, 0);
        Interlocked.Exchange(ref _microphoneDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _microphoneDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _gpuFramesEnqueued, 0);
        Interlocked.Exchange(ref _gpuFramesDropped, 0);
        Interlocked.Exchange(ref _gpuQueueRejectedFrames, 0);
        Volatile.Write(ref _lastGpuQueueRejectReason, null);
        Interlocked.Exchange(ref _videoQueueMaxDepth, 0);
        Interlocked.Exchange(ref _gpuQueueMaxDepth, 0);
        Interlocked.Exchange(ref _audioSamplesReceived, 0);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _audioQueueDepth, 0);
        Interlocked.Exchange(ref _microphoneQueueDepth, 0);
        Interlocked.Exchange(ref _gpuQueueDepth, 0);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);
        Interlocked.Exchange(ref _lastVideoWriteTick, 0);
        ResetVideoDiagnostics();
        Interlocked.Exchange(ref _segmentStartBytes, 0);
    }

    private void ResetVideoDiagnostics()
    {
        _videoLatencyTracker.ResetAll();
    }

    private void InitializeStartupQueues(FlashbackSessionContext sessionContext)
    {
        // FullMode = Wait only affects WriteAsync (which we never call).
        // TryWrite returns false immediately when full regardless of FullMode,
        // allowing manual eviction paths to clean up resources before dropping.
        var videoQueueCapacity = ResolveVideoQueueCapacity(sessionContext, _encoder.UseHardwareFrames);
        Volatile.Write(ref _videoQueueCapacity, videoQueueCapacity);
        if (!_encoder.UseHardwareFrames && IsHighResolutionFrame(sessionContext))
        {
            Logger.Log($"FLASHBACK_SINK_WARN_CPU_ENCODING width={sessionContext.Width} height={sessionContext.Height} â€” GPU encoding unavailable, performance will be severely degraded");
        }

        if (_encoder.UseHardwareFrames)
        {
            _gpuQueue = Channel.CreateBounded<GpuFramePacket>(new BoundedChannelOptions(GpuQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });
            _gpuEncodingEnabled = true;
            Logger.Log($"FLASHBACK_SINK_GPU_QUEUE_INIT capacity={GpuQueueCapacity}");
        }

        _videoQueue = Channel.CreateBounded<VideoFramePacket>(new BoundedChannelOptions(videoQueueCapacity)
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
        if (sessionContext.MicrophoneEnabled)
        {
            _microphoneQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }
    }

    private void RollBackStartFailure(Exception ex, string? startupGeneratedSegmentPath)
    {
        Logger.Log($"FLASHBACK_SINK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        _videoQueue = null;
        _audioQueue = null;
        _microphoneQueue = null;
        _gpuQueue = null;
        _gpuEncodingEnabled = false;
        lock (_sync)
        {
            _started = false;
        }
        DisposeCtsBestEffort(_cts, "start_fail");
        _cts = null;
        _encodingTask = null;
        _sessionContext = null;
        _width = 0;
        _height = 0;
        _audioEnabled = false;
        _microphoneEnabled = false;
        _tsFilePath = null;
        _recordingOutputPath = string.Empty;
        _segmentStartPts = TimeSpan.Zero;
        _segmentDuration = TimeSpan.Zero;
        _ptsBaseOffset = TimeSpan.Zero;
        Interlocked.Exchange(ref _segmentStartBytes, 0);

        DisposeEncoderBestEffort("start_fail");
        if (_ownsBufferManager)
        {
            _bufferManager.PurgeAllSegments();
        }
        else if (startupGeneratedSegmentPath != null)
        {
            _bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);
        }
    }

    private static int ResolveVideoQueueCapacity(FlashbackSessionContext context, bool useHardwareFrames)
        => !useHardwareFrames && IsHighResolutionFrame(context)
            ? HighResolutionCpuVideoQueueCapacity
            : DefaultVideoQueueCapacity;

    private static bool IsHighResolutionFrame(FlashbackSessionContext context)
        => context.Width >= 2560 || context.Height >= 1440;

    private static double ResolveSessionFrameRate(double frameRate)
    {
        if (!double.IsFinite(frameRate) || frameRate <= 0)
        {
            return FallbackSessionFrameRate;
        }

        return Math.Min(frameRate, MaxSessionFrameRate);
    }

    private static void ValidateSessionContext(FlashbackSessionContext context)
    {
        if (context.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), context.Width, "Flashback session width must be positive.");
        }

        if (context.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), context.Height, "Flashback session height must be positive.");
        }

        if (string.IsNullOrWhiteSpace(context.CodecName))
        {
            throw new ArgumentException("Flashback session codec name is required.", nameof(context));
        }
    }
}
