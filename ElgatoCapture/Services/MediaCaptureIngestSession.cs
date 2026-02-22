using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using WinRT;

namespace ElgatoCapture.Services;

internal sealed class MediaCaptureIngestSession : IAsyncDisposable
{
    private readonly MediaCapture _mediaCapture;
    private readonly bool _ownsMediaCapture;
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

    public MediaCaptureIngestSession()
    {
        _mediaCapture = new MediaCapture();
        _ownsMediaCapture = true;
    }

    public MediaCaptureIngestSession(MediaCapture mediaCapture)
    {
        _mediaCapture = mediaCapture ?? throw new ArgumentNullException(nameof(mediaCapture));
        _ownsMediaCapture = false;
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

    public async Task StartAsync(
        string videoDeviceId,
        string? audioDeviceId,
        bool audioEnabled,
        bool requireP010,
        uint requestedWidth,
        uint requestedHeight,
        double requestedFps,
        IRecordingSink sink,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sink);
        if (string.IsNullOrWhiteSpace(videoDeviceId))
        {
            throw new ArgumentException("Video device id is required.", nameof(videoDeviceId));
        }

        _sink = sink;
        _requireP010 = requireP010;
        _audioEnabled = audioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId);

        if (_ownsMediaCapture)
        {
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

        var requestedVideoSubtype = requireP010 ? MediaEncodingSubtypes.P010 : MediaEncodingSubtypes.Nv12;
        _videoRequestedSubtype = requestedVideoSubtype;
        _videoNegotiatedSubtype = videoSource.CurrentFormat?.Subtype ?? "unknown";
        Logger.Log($"Video ingest reader request: subtype={requestedVideoSubtype}");
        _videoReader = await _mediaCapture.CreateFrameReaderAsync(videoSource, requestedVideoSubtype).AsTask(cancellationToken);
        _videoReader.FrameArrived += OnVideoFrameArrived;

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

            var supportedSubtypes = audioSource.SupportedFormats
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
                _audioReader = await _mediaCapture.CreateFrameReaderAsync(audioSource, requestedSubtype)
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
        if (Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        var sink = _sink;
        if (sink == null)
        {
            return;
        }

        try
        {
            using var frame = sender.TryAcquireLatestFrame();
            var video = frame?.VideoMediaFrame;
            if (video == null)
            {
                return;
            }

            Windows.Graphics.Imaging.SoftwareBitmap? bitmap = null;
            var hasD3dSurface = false;
            try
            {
                bitmap = video.SoftwareBitmap;
            }
            catch (Exception ex)
            {
                hasD3dSurface = video.Direct3DSurface != null;
                throw new InvalidOperationException(
                    $"Failed to access VideoMediaFrame.SoftwareBitmap (hasD3dSurface={hasD3dSurface}). {ex.GetType().Name}: {ex.Message} (hr=0x{ex.HResult:X8})",
                    ex);
            }

            if (bitmap == null)
            {
                var surface = video.Direct3DSurface;
                if (surface != null)
                {
                    hasD3dSurface = true;
                    try
                    {
                        // Best-effort: copy from GPU surface to CPU SoftwareBitmap for the encoder.
                        bitmap = Windows.Graphics.Imaging.SoftwareBitmap
                            .CreateCopyFromSurfaceAsync(surface)
                            .AsTask()
                            .GetAwaiter()
                            .GetResult();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"SoftwareBitmap was null and surface copy failed (hasD3dSurface={hasD3dSurface}). {ex.GetType().Name}: {ex.Message} (hr=0x{ex.HResult:X8})",
                            ex);
                    }
                }

                if (bitmap == null)
                {
                    return;
                }
            }

            if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstVideoFrame, 1) == 0)
            {
                Logger.LogVerbose(
                    $"First video frame: requireP010={_requireP010} requestedSubtype={_videoRequestedSubtype} " +
                    $"negotiatedSubtype={_videoNegotiatedSubtype} softwareBitmapFormat={bitmap.BitmapPixelFormat}");
            }

            if (_requireP010 && bitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.P010)
            {
                throw new InvalidOperationException(
                    $"HDR ingress requires P010, but received {bitmap.BitmapPixelFormat} (requestedSubtype={_videoRequestedSubtype}, negotiatedSubtype={_videoNegotiatedSubtype}).");
            }

            if (!_requireP010 && bitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Nv12)
            {
                // SDR ingest currently requires NV12 to match FFmpeg rawvideo args.
                throw new InvalidOperationException($"SDR ingress requires NV12, but received {bitmap.BitmapPixelFormat}.");
            }

            _ = sink.WriteVideoAsync(bitmap);
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
    }

    private void OnAudioFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        var sink = _sink;
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
                    var managed = new byte[capacity];
                    Marshal.Copy((IntPtr)data, managed, 0, managed.Length);
                    _ = sink.WriteAudioAsync(managed);
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

        _sink = null;
        if (_ownsMediaCapture)
        {
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
}
