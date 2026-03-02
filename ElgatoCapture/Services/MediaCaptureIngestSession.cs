using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using WinRT;

namespace ElgatoCapture.Services;

internal sealed class MediaCaptureIngestSession : IAsyncDisposable
{
    private readonly MediaCapture _mediaCapture;
    private MediaFrameReader? _videoReader;
    private MediaFrameReader? _audioReader;
    private IRecordingSink? _sink;
    private Channel<SoftwareBitmap>? _videoIngestChannel;
    private Task? _videoIngestDrainTask;
    private bool _requireP010;
    private bool _audioEnabled;
    private string _videoStreamLabel = "unknown";
    private string _audioStreamLabel = "unknown";
    private string _videoNegotiatedSubtype = "unknown";
    private string _videoRequestedSubtype = "unknown";
    private string _audioRequestedSubtype = "unknown";
    private int _stopRequested;
    private int _loggedFirstAudioFrame;
    private int _loggedFirstVideoFrame;
    private long _videoIngestErrorCount;
    private long _lastVideoIngestErrorLogTick;
    private long _videoFrameCount;
    private long _audioLevelLastFireTick;
    private long _audioFrameCount;
    private long _audioFramesWrittenToSink;
    private long _videoFramesWrittenToSink;
    private long _lastVideoFrameArrivedTick;
    private const long AudioLevelFireIntervalMs = 66; // ~15 Hz

    // Stored for deferred reader creation (recording-only)
    private MediaFrameSource? _videoSource;
    private MediaFrameSource? _audioSource;
    private MediaSource? _previewMediaSource;
    private MediaSource? _audioPreviewMediaSource;

    public event EventHandler<MediaCaptureFailedEventArgs>? CaptureFailed;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;

    public IMediaPlaybackSource? AudioPlaybackSource => _audioPreviewMediaSource;

    public bool IsAudioReaderActive => _audioReader != null;
    public long AudioFramesArrived => Interlocked.Read(ref _audioFrameCount);
    public long AudioFramesWrittenToSink => Interlocked.Read(ref _audioFramesWrittenToSink);
    public bool IsVideoReaderActive => _videoReader != null;
    public long VideoFramesArrived => Interlocked.Read(ref _videoFrameCount);
    public long VideoFramesWrittenToSink => Interlocked.Read(ref _videoFramesWrittenToSink);
    public long LastVideoFrameArrivedTick => Interlocked.Read(ref _lastVideoFrameArrivedTick);
    public long VideoIngestErrorCount => Interlocked.Read(ref _videoIngestErrorCount);
    public string MemoryPreference => _requireP010 ? "Auto" : "Cpu";
    public string VideoRequestedSubtype => _videoRequestedSubtype;
    public string VideoNegotiatedSubtype => _videoNegotiatedSubtype;
    public bool RequireP010 => _requireP010;

    public VideoSourceProbeResult ProbeVideoSource()
    {
        var source = _videoSource;
        if (source == null)
        {
            return new VideoSourceProbeResult
            {
                SessionActive = false,
                MemoryPreference = MemoryPreference
            };
        }

        var supported = source.SupportedFormats;
        var formats = new List<VideoSourceFormatEntry>();
        var subtypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (supported != null)
        {
            foreach (var fmt in supported)
            {
                var sub = fmt.Subtype ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(sub))
                {
                    subtypes.Add(sub);
                }

                if (formats.Count < 50)
                {
                    var w = (int)(fmt.VideoFormat?.Width ?? 0);
                    var h = (int)(fmt.VideoFormat?.Height ?? 0);
                    var fps = fmt.FrameRate.Denominator > 0
                        ? (double)fmt.FrameRate.Numerator / fmt.FrameRate.Denominator
                        : 0;
                    formats.Add(new VideoSourceFormatEntry
                    {
                        Subtype = sub,
                        Width = w,
                        Height = h,
                        FrameRate = Math.Round(fps, 3),
                        Summary = $"{sub} {w}x{h}@{fps:0.###}"
                    });
                }
            }
        }

        var current = source.CurrentFormat;
        var currentFps = current != null && current.FrameRate.Denominator > 0
            ? (double)current.FrameRate.Numerator / current.FrameRate.Denominator
            : 0;

        return new VideoSourceProbeResult
        {
            SessionActive = true,
            MemoryPreference = MemoryPreference,
            CurrentSubtype = current?.Subtype ?? "Unknown",
            CurrentWidth = (int)(current?.VideoFormat?.Width ?? 0),
            CurrentHeight = (int)(current?.VideoFormat?.Height ?? 0),
            CurrentFrameRate = Math.Round(currentFps, 3),
            P010Available = subtypes.Contains("P010"),
            Nv12Available = subtypes.Contains("NV12"),
            SupportedSubtypes = subtypes.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
            TotalFormatCount = supported?.Count ?? 0,
            Formats = formats
        };
    }

    public MediaCaptureIngestSession()
    {
        _mediaCapture = new MediaCapture();
    }

    private static string FormatAudioProps(AudioEncodingProperties? props)
    {
        if (props == null)
        {
            return "null";
        }

        return $"subtype={props.Subtype} sr={props.SampleRate} ch={props.ChannelCount} bits={props.BitsPerSample}";
    }

    private static void LogAudioSupportedFormats(MediaFrameSource audioSource, int maxLines = 12)
    {
        var formats = audioSource.SupportedFormats;
        if (formats == null || formats.Count == 0)
        {
            Logger.Log("Audio ingest supported formats: none reported.");
            return;
        }

        Logger.Log($"Audio ingest supported formats: count={formats.Count} (showing up to {maxLines}).");
        foreach (var format in formats.Take(Math.Max(0, maxLines)))
        {
            Logger.Log($"Audio ingest supported: {FormatAudioProps(format.AudioEncodingProperties)}");
        }

        var distinctSubtypes = formats
            .Select(fmt => fmt.AudioEncodingProperties?.Subtype)
            .Where(subtype => !string.IsNullOrWhiteSpace(subtype))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .ToList();
        if (distinctSubtypes.Count > 0)
        {
            Logger.Log($"Audio ingest supported subtypes: {string.Join(", ", distinctSubtypes)}");
        }
    }

    /// <summary>
    /// Initialize MediaCapture and configure the video source. Returns an <see cref="IMediaPlaybackSource"/>
    /// suitable for GPU-rendered preview via <see cref="MediaPlayerElement"/>. Does NOT start any frame readers —
    /// readers are created on demand by <see cref="StartRecordingAsync"/>.
    /// </summary>
    public async Task<IMediaPlaybackSource> StartAsync(
        string videoDeviceId,
        string? audioDeviceId,
        bool audioEnabled,
        bool requireP010,
        uint requestedWidth,
        uint requestedHeight,
        double requestedFps,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoDeviceId))
        {
            throw new ArgumentException("Video device id is required.", nameof(videoDeviceId));
        }

        _requireP010 = requireP010;
        _audioEnabled = audioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId);

        try
        {
            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = videoDeviceId,
                AudioDeviceId = _audioEnabled ? audioDeviceId : null,
                StreamingCaptureMode = _audioEnabled ? StreamingCaptureMode.AudioAndVideo : StreamingCaptureMode.Video,
                MemoryPreference = requireP010 ? MediaCaptureMemoryPreference.Auto : MediaCaptureMemoryPreference.Cpu
            }).AsTask(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"MediaCapture ingestion initialization failed: {ex.Message}", ex);
        }

        _mediaCapture.Failed += OnMediaCaptureFailed;
        cancellationToken.ThrowIfCancellationRequested();

        var videoSource = SelectVideoSource(_mediaCapture, preferRecord: requireP010);
        if (videoSource == null)
        {
            throw new InvalidOperationException("MediaCapture ingestion failed: no color video frame source is available.");
        }

        _videoStreamLabel = $"{videoSource.Info.MediaStreamType}";
        Logger.Log(
            "HDR_REQUEST_STATE scope=ingest-video " +
            $"require_p010={requireP010} " +
            $"stream={_videoStreamLabel} " +
            $"mode={requestedWidth}x{requestedHeight}@{requestedFps:0.###}");

        await ConfigureVideoFormatAsync(videoSource, requireP010, requestedWidth, requestedHeight, requestedFps, cancellationToken)
            .ConfigureAwait(false);

        _videoRequestedSubtype = requireP010 ? MediaEncodingSubtypes.P010 : MediaEncodingSubtypes.Nv12;
        _videoNegotiatedSubtype = videoSource.CurrentFormat?.Subtype ?? "unknown";

        // Store source references for deferred reader creation during recording
        _videoSource = videoSource;

        if (_audioEnabled)
        {
            await StartAudioReaderForMeteringAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Create MediaSource for GPU preview — no frame reader, no callbacks
        var mediaSource = MediaSource.CreateFromMediaFrameSource(videoSource);
        _previewMediaSource = mediaSource;
        Logger.Log($"GPU preview MediaSource created from {_videoStreamLabel} (subtype={_videoNegotiatedSubtype}).");
        return mediaSource;
    }

    public async Task StartAudioOnlyAsync(string audioDeviceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(audioDeviceId))
        {
            throw new ArgumentException("Audio device id is required.", nameof(audioDeviceId));
        }

        _requireP010 = false;
        _audioEnabled = true;

        try
        {
            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                AudioDeviceId = audioDeviceId,
                StreamingCaptureMode = StreamingCaptureMode.Audio,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            }).AsTask(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"MediaCapture audio-only initialization failed: {ex.Message}", ex);
        }

        _mediaCapture.Failed += OnMediaCaptureFailed;
        cancellationToken.ThrowIfCancellationRequested();

        await StartAudioReaderForMeteringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attach only the recording sink (for audio-only recording mode while video comes from external source reader).
    /// </summary>
    public void AttachRecordingSink(IRecordingSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        Volatile.Write(ref _sink, sink);
        Logger.Log("Recording sink attached (audio-only mode, video via external source reader).");
    }

    public void DetachRecordingSink()
    {
        Volatile.Write(ref _sink, null);
        Logger.Log("Recording sink detached.");
    }

    /// <summary>
    /// Create and start the video frame reader for recording. Attaches the provided sink so
    /// the already-running audio reader delivers frames to it. GPU preview continues unaffected.
    /// </summary>
    public async Task StartRecordingAsync(IRecordingSink sink, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sink);

        if (_videoSource == null)
        {
            throw new InvalidOperationException("Cannot start recording: video source not initialized. Call StartAsync first.");
        }

        if (_videoReader != null)
        {
            throw new InvalidOperationException("Recording video reader is already active for this ingest session.");
        }

        Interlocked.Exchange(ref _stopRequested, 0);

        // Attach sink before starting readers so no frames are missed.
        // The audio reader (already running for metering) will begin delivering to the sink immediately.
        Volatile.Write(ref _sink, sink);
        Logger.Log("Recording sink attached to ingest session.");

        _videoIngestChannel = Channel.CreateBounded<SoftwareBitmap>(new BoundedChannelOptions(3)
        {
            SingleWriter = true,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropNewest
        });
        _videoIngestDrainTask = Task.Run(() => DrainVideoIngestQueueAsync());

        try
        {
            var requestedVideoSubtype = _videoRequestedSubtype;
            Logger.Log($"Video ingest reader request: subtype={requestedVideoSubtype}");
            // Do NOT pass subtype to CreateFrameReaderAsync — the WinRT conversion pipeline
            // doesn't support P010 as a SoftwareBitmap output format, causing TryAcquireLatestFrame()
            // to throw E_INVALIDARG. Instead, let the reader deliver frames in the source's native
            // format (already set to P010 via SetFormatAsync during preview init).
            _videoReader = await _mediaCapture.CreateFrameReaderAsync(_videoSource).AsTask(cancellationToken);
            _videoReader.FrameArrived += OnVideoFrameArrived;

            cancellationToken.ThrowIfCancellationRequested();

            var videoStatus = await _videoReader.StartAsync().AsTask(cancellationToken);
            if (videoStatus != MediaFrameReaderStartStatus.Success)
            {
                throw new InvalidOperationException($"Video ingestion start failed: {videoStatus}.");
            }

            Logger.Log("Recording video reader started. Audio reader already active from preview.");
        }
        catch
        {
            try
            {
                await StopRecordingAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cleanupEx)
            {
                Logger.Log($"Recording reader rollback failed: {cleanupEx.Message}");
            }

            throw;
        }
    }

    /// <summary>
    /// Stop and dispose the recording video reader. The audio reader, metering, GPU preview,
    /// and audio playback all continue unaffected.
    /// </summary>
    public async Task StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        var cancellationRequested = cancellationToken.IsCancellationRequested;

        // Detach sink first — audio reader continues for metering but stops recording
        var previous = Interlocked.Exchange(ref _sink, null);
        if (previous != null)
        {
            Logger.Log("Recording sink detached from ingest session.");
        }

        var channel = _videoIngestChannel;
        _videoIngestChannel = null;
        if (channel != null)
        {
            channel.Writer.TryComplete();
        }
        var drainTask = _videoIngestDrainTask;
        _videoIngestDrainTask = null;
        if (drainTask != null)
        {
            try
            {
                await drainTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Logger.Log("Video ingest drain task timed out during stop.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Video ingest drain task error during stop: {ex.Message}");
            }
        }

        var video = _videoReader;
        _videoReader = null;
        if (video != null)
        {
            video.FrameArrived -= OnVideoFrameArrived;
            try
            {
                await video.StopAsync();
            }
            catch
            {
                // Best-effort.
            }
            video.Dispose();
        }

        // Reset per-recording counters (audio reader stays alive for metering)
        Interlocked.Exchange(ref _loggedFirstVideoFrame, 0);
        Interlocked.Exchange(ref _videoFrameCount, 0);
        Interlocked.Exchange(ref _videoFramesWrittenToSink, 0);
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, 0);
        Interlocked.Exchange(ref _videoIngestErrorCount, 0);

        Logger.Log("Recording video reader stopped. Audio reader and GPU preview continue.");

        if (cancellationRequested || cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private void OnMediaCaptureFailed(MediaCapture sender, MediaCaptureFailedEventArgs args)
    {
        Logger.Log($"MEDIACAPTURE_FAILED code=0x{args.Code:X8} message={args.Message}");
        CaptureFailed?.Invoke(this, args);
    }

    private static MediaFrameSource? SelectVideoSource(MediaCapture mediaCapture, bool preferRecord)
    {
        var colorSources = mediaCapture.FrameSources.Values
            .Where(source => source.Info.SourceKind == MediaFrameSourceKind.Color)
            .ToList();
        if (colorSources.Count == 0)
        {
            return null;
        }

        if (preferRecord)
        {
            return colorSources.FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoRecord)
                ?? colorSources.FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoPreview)
                ?? colorSources.FirstOrDefault();
        }

        return colorSources.FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoPreview)
            ?? colorSources.FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoRecord)
            ?? colorSources.FirstOrDefault();
    }

    private static async Task ConfigureVideoFormatAsync(
        MediaFrameSource frameSource,
        bool requireP010,
        uint requestedWidth,
        uint requestedHeight,
        double requestedFps,
        CancellationToken cancellationToken)
    {
        var supported = frameSource.SupportedFormats;
        if (supported == null || supported.Count == 0)
        {
            throw new InvalidOperationException("MediaCapture ingestion failed: device reports no supported formats.");
        }

        static double ToFps(MediaFrameFormat format)
            => format.FrameRate.Denominator > 0
                ? (double)format.FrameRate.Numerator / format.FrameRate.Denominator
                : 0;

        static bool IsSubtype(MediaFrameFormat format, string subtype)
            => string.Equals(format.Subtype, subtype, StringComparison.OrdinalIgnoreCase);

        var desiredSubtype = requireP010 ? MediaEncodingSubtypes.P010 : MediaEncodingSubtypes.Nv12;

        var requestedW = (int)requestedWidth;
        var requestedH = (int)requestedHeight;

        var sizeMatches = supported.Where(fmt =>
            fmt.VideoFormat.Width == requestedW &&
            fmt.VideoFormat.Height == requestedH);

        var candidates = sizeMatches.Any()
            ? sizeMatches
            : supported.AsEnumerable();

        candidates = candidates.Where(fmt => IsSubtype(fmt, desiredSubtype));

        if (!candidates.Any())
        {
            var distinctSubtypes = string.Join(
                ", ",
                supported.Select(fmt => fmt.Subtype).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Take(10));
            throw new InvalidOperationException(
                $"Capture requires {desiredSubtype}, but no {desiredSubtype} formats were found for {requestedWidth}x{requestedHeight}. " +
                $"Reported subtypes include: {distinctSubtypes}.");
        }

        var selected = candidates
            .OrderBy(fmt =>
            {
                var fps = ToFps(fmt);
                return fps > 0 ? Math.Abs(fps - requestedFps) : 9999;
            })
            .First();

        Logger.Log(
            $"Ingest video format select: subtype={selected.Subtype} {selected.VideoFormat.Width}x{selected.VideoFormat.Height} " +
            $"fps={ToFps(selected):0.###} (requested {requestedWidth}x{requestedHeight}@{requestedFps:0.###}, requireP010={requireP010}).");

        await frameSource.SetFormatAsync(selected).AsTask(cancellationToken);

        var activeSubtype = frameSource.CurrentFormat?.Subtype ?? string.Empty;
        if (!string.Equals(activeSubtype, desiredSubtype, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Capture requested {desiredSubtype}, but negotiated subtype is '{activeSubtype}'.");
        }
    }

    private async Task StartAudioReaderForMeteringAsync(CancellationToken cancellationToken)
    {
        var audioSource = _mediaCapture.FrameSources.Values.FirstOrDefault(source =>
            source.Info.SourceKind == MediaFrameSourceKind.Audio);
        if (audioSource == null)
        {
            throw new InvalidOperationException("Audio capture enabled but no audio frame source was found.");
        }

        _audioStreamLabel = $"{audioSource.Info.MediaStreamType}";
        Logger.Log($"Audio ingest source selected: stream={_audioStreamLabel} id={audioSource.Info.Id}.");
        LogAudioSupportedFormats(audioSource);
        _audioSource = audioSource;

        var supportedSubtypes = audioSource.SupportedFormats
            .Select(format => format.AudioEncodingProperties?.Subtype)
            .Where(subtype => !string.IsNullOrWhiteSpace(subtype))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var requestedAudioSubtype = supportedSubtypes.Any(subtype =>
                string.Equals(subtype, MediaEncodingSubtypes.Float, StringComparison.OrdinalIgnoreCase))
            ? MediaEncodingSubtypes.Float
            : MediaEncodingSubtypes.Pcm;
        _audioRequestedSubtype = requestedAudioSubtype;

        Logger.Log($"Audio ingest request: subtype={requestedAudioSubtype}");
        _audioReader = await _mediaCapture.CreateFrameReaderAsync(audioSource, requestedAudioSubtype)
            .AsTask(cancellationToken);
        _audioReader.FrameArrived += OnAudioFrameArrived;

        var audioStatus = await _audioReader.StartAsync().AsTask(cancellationToken);
        if (audioStatus != MediaFrameReaderStartStatus.Success)
        {
            throw new InvalidOperationException(
                $"Audio reader start failed: {audioStatus} (requested subtype={requestedAudioSubtype}).");
        }

        _audioPreviewMediaSource = MediaSource.CreateFromMediaFrameSource(audioSource);
        Logger.Log("Audio reader started for metering. Audio playback source created.");
    }

    private void OnVideoFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (Volatile.Read(ref _stopRequested) != 0) return;

        MediaFrameReference? frame = null;
        try
        {
            try { frame = sender.TryAcquireLatestFrame(); }
            catch (Exception acqEx)
            {
                if (Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
                    Logger.Log($"VIDEO_DIAG TryAcquireLatestFrame failed: {acqEx.GetType().Name} hr=0x{acqEx.HResult:X8} msg={acqEx.Message}");
                throw;
            }
            VideoMediaFrame? video = null;
            try { video = frame?.VideoMediaFrame; }
            catch (Exception vmfEx)
            {
                if (Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
                    Logger.Log($"VIDEO_DIAG VideoMediaFrame failed: {vmfEx.GetType().Name} hr=0x{vmfEx.HResult:X8} msg={vmfEx.Message}");
                throw;
            }
            if (video == null) return;

            SoftwareBitmap? bitmap = null;
            bool bitmapFromSurface = false;
            try { bitmap = video.SoftwareBitmap; }
            catch (Exception sbEx)
            {
                if (Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
                    Logger.Log($"VIDEO_DIAG SoftwareBitmap access failed: {sbEx.GetType().Name} hr=0x{sbEx.HResult:X8} msg={sbEx.Message}");
                return;
            }

            if (bitmap == null)
            {
                // MemoryPreference.Auto + P010 → frames arrive as D3D surface only
                var surface = video.Direct3DSurface;
                if (surface == null) return;

                try
                {
                    bitmap = SoftwareBitmap.CreateCopyFromSurfaceAsync(
                        surface, BitmapAlphaMode.Ignore).AsTask().GetAwaiter().GetResult();
                    bitmapFromSurface = true;
                }
                catch (Exception surfEx)
                {
                    if (Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
                        Logger.Log($"VIDEO_DIAG CreateCopyFromSurface failed: {surfEx.GetType().Name} hr=0x{surfEx.HResult:X8}");
                    return;
                }
            }

            BitmapPixelFormat pixFmt;
            try { pixFmt = bitmap.BitmapPixelFormat; }
            catch (Exception pfEx)
            {
                if (Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
                    Logger.Log($"VIDEO_DIAG BitmapPixelFormat failed: {pfEx.GetType().Name} hr=0x{pfEx.HResult:X8} msg={pfEx.Message}");
                return;
            }

            if (Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
            {
                var d3dSurface = video.Direct3DSurface;
                var d3dDesc = d3dSurface != null ? d3dSurface.Description : default;
                Logger.Log(
                    $"VIDEO_DIAG first_frame: requireP010={_requireP010} requestedSubtype={_videoRequestedSubtype} " +
                    $"negotiatedSubtype={_videoNegotiatedSubtype} pixelFormat={pixFmt} " +
                    $"width={bitmap.PixelWidth} height={bitmap.PixelHeight} " +
                    $"d3dSurface={d3dSurface != null} d3dFormat={d3dDesc.Format} d3dW={d3dDesc.Width} d3dH={d3dDesc.Height}");
            }

            if (_requireP010 && pixFmt != BitmapPixelFormat.P010)
                throw new InvalidOperationException(
                    $"HDR ingress requires P010, but received {pixFmt} " +
                    $"(requestedSubtype={_videoRequestedSubtype}, negotiatedSubtype={_videoNegotiatedSubtype}).");

            if (!_requireP010 && pixFmt != BitmapPixelFormat.Nv12)
                throw new InvalidOperationException(
                    $"SDR ingress requires NV12, but received {pixFmt}.");

            Interlocked.Increment(ref _videoFrameCount);
            Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);

            // Detach bitmap from reader's buffer pool so we can release the frame immediately
            var channel = _videoIngestChannel;
            if (channel != null)
            {
                SoftwareBitmap? copy = null;
                try
                {
                    // Surface-copied bitmaps are already detached from reader pool — skip Copy()
                    copy = bitmapFromSurface ? bitmap : SoftwareBitmap.Copy(bitmap);
                }
                catch (Exception copyEx)
                {
                    Logger.Log($"VIDEO_DIAG SoftwareBitmap.Copy failed: {copyEx.GetType().Name} hr=0x{copyEx.HResult:X8} pixFmt={pixFmt}");
                    return;
                }
                if (!channel.Writer.TryWrite(copy))
                {
                    copy.Dispose();
                }
            }
            else if (bitmapFromSurface)
            {
                // No channel — surface-copied bitmap is independent of frame, must dispose
                bitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            var errors = Interlocked.Increment(ref _videoIngestErrorCount);
            var nowTick = Environment.TickCount64;
            var lastTick = Interlocked.Read(ref _lastVideoIngestErrorLogTick);
            if (errors == 1 ||
                nowTick - lastTick > 1000 &&
                Interlocked.CompareExchange(ref _lastVideoIngestErrorLogTick, nowTick, lastTick) == lastTick)
            {
                Logger.Log(
                    "VIDEO_INGEST_EXCEPTION " +
                    $"count={errors} " +
                    $"require_p010={_requireP010} " +
                    $"requestedSubtype={_videoRequestedSubtype} " +
                    $"negotiatedSubtype={_videoNegotiatedSubtype} " +
                    $"type={ex.GetType().Name} " +
                    $"hr=0x{ex.HResult:X8} " +
                    $"msg={ex.Message}");
            }
        }
        finally
        {
            frame?.Dispose();
        }
    }

    private async Task DrainVideoIngestQueueAsync()
    {
        var channel = _videoIngestChannel;
        if (channel == null) return;

        try
        {
            await foreach (var bitmap in channel.Reader.ReadAllAsync())
            {
                try
                {
                    var sink = Volatile.Read(ref _sink);
                    if (sink != null)
                    {
                        _ = sink.WriteVideoAsync(bitmap);
                        Interlocked.Increment(ref _videoFramesWrittenToSink);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Video ingest drain error: {ex.Message}");
                }
                finally
                {
                    bitmap.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Video ingest drain task failed: {ex.Message}");
        }
    }

    private void OnAudioFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        try
        {
            using var frame = sender.TryAcquireLatestFrame();
            var audio = frame?.AudioMediaFrame;
            if (audio == null)
            {
                return;
            }

            Interlocked.Increment(ref _audioFrameCount);

            var props = audio.AudioEncodingProperties;
            var sampleRate = props?.SampleRate ?? 0;
            var channels = props?.ChannelCount ?? 0;
            var bitsPerSample = props?.BitsPerSample ?? 0;
            var subtype = props?.Subtype ?? string.Empty;

            if (sampleRate != 48000 || channels != 2)
            {
                throw new InvalidOperationException($"Audio ingest requires 48kHz stereo, but received {sampleRate}Hz ch={channels}.");
            }

            if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstAudioFrame, 1) == 0)
            {
                Logger.LogVerbose($"First audio frame: subtype={subtype} bits={bitsPerSample} requestedSubtype={_audioRequestedSubtype}");
            }

            var audioFrame = audio.GetAudioFrame();
            using var buffer = audioFrame.LockBuffer(AudioBufferAccessMode.Read);
            using var reference = buffer.CreateReference();
            unsafe
            {
                var byteAccess = reference.As<IMemoryBufferByteAccess>();
                byteAccess.GetBuffer(out var data, out var capacity);
                if (data == null || capacity == 0)
                {
                    return;
                }

                byte[] f32leBytes;

                if (string.Equals(subtype, MediaEncodingSubtypes.Float, StringComparison.OrdinalIgnoreCase) &&
                    bitsPerSample == 32)
                {
                    f32leBytes = new byte[capacity];
                    Marshal.Copy((IntPtr)data, f32leBytes, 0, f32leBytes.Length);
                }
                else if (string.Equals(subtype, MediaEncodingSubtypes.Pcm, StringComparison.OrdinalIgnoreCase) &&
                    bitsPerSample == 16)
                {
                    // Convert PCM16 to f32le to match FFmpeg audio pipe format (-f f32le)
                    var sampleCount = (int)(capacity / 2);
                    f32leBytes = new byte[sampleCount * 4];
                    for (int i = 0; i < sampleCount; i++)
                    {
                        short pcm = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                        float normalized = pcm / 32768f;
                        BitConverter.TryWriteBytes(
                            new Span<byte>(f32leBytes, i * 4, 4), normalized);
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unsupported audio format: subtype={subtype}, bits={bitsPerSample} (requestedSubtype={_audioRequestedSubtype}).");
                }

                // Always meter — works during preview and recording
                RaiseAudioLevelIfDue(f32leBytes);

                // Only write to recording sink when actively recording
                var sink = Volatile.Read(ref _sink);
                if (sink != null)
                {
                    _ = sink.WriteAudioAsync(f32leBytes);
                    Interlocked.Increment(ref _audioFramesWrittenToSink);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio ingest error: {ex.Message}");
        }
    }

    private void RaiseAudioLevelIfDue(byte[] f32leBytes)
    {
        var handler = AudioLevelUpdated;
        if (handler == null) return;

        var nowTick = Environment.TickCount64;
        var lastTick = Interlocked.Read(ref _audioLevelLastFireTick);
        if (nowTick - lastTick < AudioLevelFireIntervalMs) return;
        if (Interlocked.CompareExchange(ref _audioLevelLastFireTick, nowTick, lastTick) != lastTick) return;

        var samples = MemoryMarshal.Cast<byte, float>(f32leBytes.AsSpan());
        float peak = 0f;
        foreach (var sample in samples)
        {
            var abs = MathF.Abs(sample);
            if (abs > peak) peak = abs;
        }

        handler.Invoke(this, new AudioLevelEventArgs(peak, 0, peak >= 1.0));
    }

    public async ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _stopRequested, 1);
        _sink = null;
        var channel = _videoIngestChannel;
        _videoIngestChannel = null;
        channel?.Writer.TryComplete();
        var drainTask = _videoIngestDrainTask;
        _videoIngestDrainTask = null;
        if (drainTask != null)
        {
            try { await drainTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
            catch { /* best-effort */ }
        }
        _mediaCapture.Failed -= OnMediaCaptureFailed;

        var video = _videoReader;
        _videoReader = null;
        if (video != null)
        {
            video.FrameArrived -= OnVideoFrameArrived;
            try
            {
                await video.StopAsync();
            }
            catch
            {
                // Best-effort.
            }
            video.Dispose();
        }

        var audio = _audioReader;
        _audioReader = null;
        if (audio != null)
        {
            audio.FrameArrived -= OnAudioFrameArrived;
            try
            {
                await audio.StopAsync();
            }
            catch
            {
                // Best-effort.
            }
            audio.Dispose();
        }

        _videoSource = null;
        _audioSource = null;

        var previewSource = _previewMediaSource;
        _previewMediaSource = null;
        previewSource?.Dispose();

        var audioPreviewSource = _audioPreviewMediaSource;
        _audioPreviewMediaSource = null;
        audioPreviewSource?.Dispose();

        try
        {
            _mediaCapture.Dispose();
        }
        catch
        {
            // Best-effort.
        }
    }
}
