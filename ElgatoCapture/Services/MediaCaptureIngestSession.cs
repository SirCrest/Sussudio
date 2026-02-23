using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
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

    // Stored for deferred reader creation (recording-only)
    private MediaFrameSource? _videoSource;
    private MediaFrameSource? _audioSource;
    private MediaSource? _previewMediaSource;

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
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            }).AsTask(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"MediaCapture ingestion initialization failed: {ex.Message}", ex);
        }

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
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Create MediaSource for GPU preview — no frame reader, no callbacks
        var mediaSource = MediaSource.CreateFromMediaFrameSource(videoSource);
        _previewMediaSource = mediaSource;
        Logger.Log($"GPU preview MediaSource created from {_videoStreamLabel} (subtype={_videoNegotiatedSubtype}).");
        return mediaSource;
    }

    /// <summary>
    /// Create and start frame readers for recording. Attaches the provided sink to receive frames.
    /// The GPU preview (MediaSource) continues unaffected.
    /// </summary>
    public async Task StartRecordingAsync(IRecordingSink sink, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sink);

        if (_videoSource == null)
        {
            throw new InvalidOperationException("Cannot start recording: video source not initialized. Call StartAsync first.");
        }

        // Attach sink before starting readers so no frames are missed
        Volatile.Write(ref _sink, sink);
        Logger.Log("Recording sink attached to ingest session.");

        var requestedVideoSubtype = _videoRequestedSubtype;
        Logger.Log($"Video ingest reader request: subtype={requestedVideoSubtype}");
        _videoReader = await _mediaCapture.CreateFrameReaderAsync(_videoSource, requestedVideoSubtype).AsTask(cancellationToken);
        _videoReader.FrameArrived += OnVideoFrameArrived;

        if (_audioEnabled && _audioSource != null)
        {
            var supportedSubtypes = _audioSource.SupportedFormats
                .Select(format => format.AudioEncodingProperties?.Subtype)
                .Where(subtype => !string.IsNullOrWhiteSpace(subtype))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Prefer Float because our FFmpeg audio pipe is configured as f32le.
            var requestedSubtype = supportedSubtypes.Any(subtype =>
                    string.Equals(subtype, MediaEncodingSubtypes.Float, StringComparison.OrdinalIgnoreCase))
                ? MediaEncodingSubtypes.Float
                : MediaEncodingSubtypes.Pcm;
            _audioRequestedSubtype = requestedSubtype;

            Logger.Log($"Audio ingest request: subtype={requestedSubtype}");
            try
            {
                _audioReader = await _mediaCapture.CreateFrameReaderAsync(_audioSource, requestedSubtype)
                    .AsTask(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Audio reader creation failed (requested subtype={requestedSubtype}). {ex.GetType().Name}: {ex.Message} (hr=0x{ex.HResult:X8})",
                    ex);
            }
            _audioReader.FrameArrived += OnAudioFrameArrived;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var videoStatus = await _videoReader.StartAsync().AsTask(cancellationToken);
        if (videoStatus != MediaFrameReaderStartStatus.Success)
        {
            throw new InvalidOperationException($"Video ingestion start failed: {videoStatus}.");
        }

        if (_audioReader != null)
        {
            var audioStatus = await _audioReader.StartAsync().AsTask(cancellationToken);
            if (audioStatus != MediaFrameReaderStartStatus.Success)
            {
                throw new InvalidOperationException(
                    $"Audio ingestion start failed: {audioStatus} (requested subtype={_audioRequestedSubtype}).");
            }
        }

        Logger.Log("Recording readers started on existing MediaCapture session.");
    }

    /// <summary>
    /// Stop and dispose recording frame readers. The MediaCapture and GPU preview remain active.
    /// </summary>
    public async Task StopRecordingAsync()
    {
        // Detach sink first to stop frame delivery
        var previous = Interlocked.Exchange(ref _sink, null);
        if (previous != null)
        {
            Logger.Log("Recording sink detached from ingest session.");
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

        // Reset per-recording counters
        Interlocked.Exchange(ref _loggedFirstVideoFrame, 0);
        Interlocked.Exchange(ref _loggedFirstAudioFrame, 0);
        Interlocked.Exchange(ref _videoFrameCount, 0);
        Interlocked.Exchange(ref _videoIngestErrorCount, 0);

        Logger.Log("Recording readers stopped. GPU preview continues.");
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

    private void OnVideoFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (Volatile.Read(ref _stopRequested) != 0) return;

        MediaFrameReference? frame = null;
        try
        {
            frame = sender.TryAcquireLatestFrame();
            var video = frame?.VideoMediaFrame;
            if (video == null) return;

            SoftwareBitmap? bitmap = null;
            try { bitmap = video.SoftwareBitmap; }
            catch { /* D3D-only frame */ }
            if (bitmap == null) return;

            if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
            {
                Logger.LogVerbose(
                    $"First video frame: requireP010={_requireP010} requestedSubtype={_videoRequestedSubtype} " +
                    $"negotiatedSubtype={_videoNegotiatedSubtype} softwareBitmapFormat={bitmap.BitmapPixelFormat}");
            }

            if (_requireP010 && bitmap.BitmapPixelFormat != BitmapPixelFormat.P010)
                throw new InvalidOperationException(
                    $"HDR ingress requires P010, but received {bitmap.BitmapPixelFormat} " +
                    $"(requestedSubtype={_videoRequestedSubtype}, negotiatedSubtype={_videoNegotiatedSubtype}).");

            if (!_requireP010 && bitmap.BitmapPixelFormat != BitmapPixelFormat.Nv12)
                throw new InvalidOperationException(
                    $"SDR ingress requires NV12, but received {bitmap.BitmapPixelFormat}.");

            // Recording-only: pipe to sink
            var sink = Volatile.Read(ref _sink);
            if (sink != null)
            {
                _ = sink.WriteVideoAsync(bitmap);
            }

            Interlocked.Increment(ref _videoFrameCount);
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

    private void OnAudioFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        var sink = Volatile.Read(ref _sink);
        if (sink == null)
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

                if (string.Equals(subtype, MediaEncodingSubtypes.Float, StringComparison.OrdinalIgnoreCase) &&
                    bitsPerSample == 32)
                {
                    var managed = new byte[capacity];
                    Marshal.Copy((IntPtr)data, managed, 0, managed.Length);
                    _ = sink.WriteAudioAsync(managed);
                    return;
                }

                if (string.Equals(subtype, MediaEncodingSubtypes.Pcm, StringComparison.OrdinalIgnoreCase) &&
                    bitsPerSample == 16)
                {
                    // Convert PCM16 to f32le to match FFmpeg audio pipe format (-f f32le)
                    var sampleCount = (int)(capacity / 2);
                    var floatBytes = new byte[sampleCount * 4];
                    for (int i = 0; i < sampleCount; i++)
                    {
                        short pcm = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                        float normalized = pcm / 32768f;
                        BitConverter.TryWriteBytes(
                            new Span<byte>(floatBytes, i * 4, 4), normalized);
                    }
                    _ = sink.WriteAudioAsync(floatBytes);
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Unsupported audio format: subtype={subtype}, bits={bitsPerSample} (requestedSubtype={_audioRequestedSubtype}).");
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio ingest error: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _stopRequested, 1);
        _sink = null;

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
