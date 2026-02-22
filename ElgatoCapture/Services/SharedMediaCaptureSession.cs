using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;

namespace ElgatoCapture.Services;

internal sealed class SharedMediaCaptureSession : IAsyncDisposable
{
    private MediaCapture? _mediaCapture;
    private string _videoDeviceId = string.Empty;
    private string? _audioDeviceId;
    private bool _audioEnabled;
    private MediaFrameSource? _videoSource;
    private bool _previewActive;

    public MediaCapture? MediaCapture => _mediaCapture;
    public MediaFrameSource? VideoSource => _videoSource;
    public bool PreviewActive => _previewActive;

    public async Task EnsureInitializedAsync(
        string videoDeviceId,
        string? audioDeviceId,
        bool audioEnabled,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoDeviceId))
        {
            throw new ArgumentException("Video device id is required.", nameof(videoDeviceId));
        }

        var desiredAudioEnabled = audioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId);
        var needsReinit =
            _mediaCapture == null ||
            !string.Equals(_videoDeviceId, videoDeviceId, StringComparison.Ordinal) ||
            !string.Equals(_audioDeviceId, audioDeviceId, StringComparison.Ordinal) ||
            _audioEnabled != desiredAudioEnabled;

        if (!needsReinit)
        {
            return;
        }

        await DisposeAsync().ConfigureAwait(false);

        _videoDeviceId = videoDeviceId;
        _audioDeviceId = audioDeviceId;
        _audioEnabled = desiredAudioEnabled;

        var mediaCapture = new MediaCapture();
        try
        {
            await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = videoDeviceId,
                AudioDeviceId = desiredAudioEnabled ? audioDeviceId : null,
                StreamingCaptureMode = desiredAudioEnabled ? StreamingCaptureMode.AudioAndVideo : StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            }).AsTask(cancellationToken);
        }
        catch (Exception ex)
        {
            mediaCapture.Dispose();
            throw new InvalidOperationException($"Shared MediaCapture initialization failed: {ex.Message}", ex);
        }

        _mediaCapture = mediaCapture;
        _videoSource = null;
        _previewActive = false;
    }

    public async Task<IMediaPlaybackSource> StartPreviewAsync(
        CaptureSettings settings,
        bool requireP010,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var mediaCapture = _mediaCapture ?? throw new InvalidOperationException("Shared MediaCapture is not initialized.");

        var videoSource = SelectVideoSource(mediaCapture, preferRecord: requireP010);
        if (videoSource == null)
        {
            throw new InvalidOperationException("Shared MediaCapture preview failed: no color video frame source is available.");
        }

        _videoSource = videoSource;

        await ConfigureVideoFormatAsync(
            videoSource,
            requireP010,
            settings.Width,
            settings.Height,
            settings.FrameRate,
            cancellationToken).ConfigureAwait(false);

        var mediaSource = MediaSource.CreateFromMediaFrameSource(videoSource);
        _previewActive = true;
        return mediaSource;
    }

    public Task StopPreviewAsync()
    {
        _previewActive = false;
        return Task.CompletedTask;
    }

    private static MediaFrameSource? SelectVideoSource(MediaCapture mediaCapture, bool preferRecord)
    {
        var colorSources = mediaCapture.FrameSources.Values
            .Where(source => source.Info.MediaStreamType == MediaStreamType.VideoRecord ||
                             source.Info.MediaStreamType == MediaStreamType.VideoPreview)
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

    private static bool IsSubtype(MediaFrameFormat format, string desiredSubtype)
        => string.Equals(format.Subtype, desiredSubtype, StringComparison.OrdinalIgnoreCase);

    private static double ToFps(MediaFrameFormat format)
    {
        var frameRate = format.FrameRate;
        if (frameRate.Numerator > 0 && frameRate.Denominator > 0)
        {
            return (double)frameRate.Numerator / frameRate.Denominator;
        }

        return 0;
    }

    private static async Task ConfigureVideoFormatAsync(
        MediaFrameSource frameSource,
        bool requireP010,
        uint requestedWidth,
        uint requestedHeight,
        double requestedFps,
        CancellationToken cancellationToken)
    {
        var desiredSubtype = requireP010 ? MediaEncodingSubtypes.P010 : MediaEncodingSubtypes.Nv12;
        var supported = frameSource.SupportedFormats;
        if (supported == null || supported.Count == 0)
        {
            throw new InvalidOperationException("Capture device returned no supported video formats.");
        }

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
                supported.Select(fmt => fmt.Subtype).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Take(12));
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
            $"GPU preview format select: subtype={selected.Subtype} {selected.VideoFormat.Width}x{selected.VideoFormat.Height} " +
            $"fps={ToFps(selected):0.###} (requested {requestedWidth}x{requestedHeight}@{requestedFps:0.###}, requireP010={requireP010}).");

        await frameSource.SetFormatAsync(selected).AsTask(cancellationToken);

        var activeSubtype = frameSource.CurrentFormat?.Subtype ?? string.Empty;
        if (!string.Equals(activeSubtype, desiredSubtype, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Capture requested {desiredSubtype}, but negotiated subtype is '{activeSubtype}'.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _previewActive = false;
        _videoSource = null;

        var mediaCapture = _mediaCapture;
        _mediaCapture = null;
        if (mediaCapture != null)
        {
            try
            {
                mediaCapture.Dispose();
            }
            catch
            {
                // Best-effort.
            }
        }

        _videoDeviceId = string.Empty;
        _audioDeviceId = null;
        _audioEnabled = false;
    }
}
