using System;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace ElgatoCapture.Services;

public sealed class MediaCaptureFallbackSink : IRecordingSink
{
    private readonly MediaCapture _mediaCapture;
    private RecordingContext? _context;
    private bool _isRecording;
    private bool _disposed;

    public MediaCaptureFallbackSink(MediaCapture mediaCapture)
    {
        _mediaCapture = mediaCapture ?? throw new ArgumentNullException(nameof(mediaCapture));
    }

    public async Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);

        var profile = MediaEncodingProfile.CreateAvi(VideoEncodingQuality.HD1080p);
        if (profile.Video != null)
        {
            profile.Video.Width = context.Settings.Width;
            profile.Video.Height = context.Settings.Height;
            profile.Video.FrameRate.Numerator = (uint)Math.Max(1, Math.Round(context.Settings.FrameRate));
            profile.Video.FrameRate.Denominator = 1;
            profile.Video.Bitrate = 200_000_000;
        }

        if (!context.Settings.AudioEnabled)
        {
            profile.Audio = null;
        }

        var outputFile = await StorageFile.GetFileFromPathAsync(context.VideoOutputPath);
        await _mediaCapture.StartRecordToStorageFileAsync(profile, outputFile);
        _context = context;
        _isRecording = true;
    }

    public Task WriteVideoAsync(SoftwareBitmap frame, CancellationToken cancellationToken = default)
    {
        // MediaCapture handles frame writes internally in fallback mode.
        return Task.CompletedTask;
    }

    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // MediaCapture handles audio internally in fallback mode.
        return Task.CompletedTask;
    }

    public async Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        if (_isRecording)
        {
            await _mediaCapture.StopRecordAsync();
            _isRecording = false;
        }

        return FinalizeResult.Success(_context?.FinalOutputPath ?? string.Empty, "Stopped");
    }

    public void Dispose()
    {
        _disposed = true;
        _isRecording = false;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
