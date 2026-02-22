using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ElgatoCapture.Services;

public sealed class DirectShowFfmpegRecordingSink : IRecordingSink
{
    private readonly FFmpegEncoderService _encoder;
    private readonly string _videoDeviceName;
    private readonly bool _requireHdrP010Ingress;
    private RecordingContext? _context;
    private bool _started;
    private bool _disposed;

    public DirectShowFfmpegRecordingSink(
        FFmpegEncoderService encoder,
        string videoDeviceName,
        bool requireHdrP010Ingress)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        _videoDeviceName = string.IsNullOrWhiteSpace(videoDeviceName)
            ? throw new ArgumentException("DirectShow video device name is required.", nameof(videoDeviceName))
            : videoDeviceName;
        _requireHdrP010Ingress = requireHdrP010Ingress;
    }

    public async Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);

        if (_started)
        {
            throw new InvalidOperationException("DirectShow FFmpeg recording sink has already started.");
        }

        _context = context;
        await _encoder.StartDirectShowEncodingAsync(
            context.Settings,
            context.VideoOutputPath,
            _videoDeviceName,
            context.AudioDeviceName,
            context.EffectiveFrameRate,
            context.FrameRateArg,
            context.EffectiveWidth,
            context.EffectiveHeight,
            context.HdrPipelineActive,
            _requireHdrP010Ingress).ConfigureAwait(false);

        _started = true;
    }

    public Task WriteVideoAsync(SoftwareBitmap frame, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        var outputPath = _context?.FinalOutputPath ?? string.Empty;

        if (_disposed)
        {
            return FinalizeResult.Success(outputPath, "Stopped");
        }

        if (_started)
        {
            await _encoder.StopEncodingAsync().ConfigureAwait(false);
            _started = false;
        }

        if (_encoder.LastStopTimedOut)
        {
            return FinalizeResult.Failure(
                outputPath,
                "Stopped (encoder stop timed out)");
        }

        if (_encoder.LastExitCode is int exitCode && exitCode != 0)
        {
            return FinalizeResult.Failure(
                outputPath,
                $"Stopped (encoder failed: exit code {exitCode})");
        }

        var (validationSucceeded, validationDetail) = await HdrValidationRunner
            .RunAsync(_context, outputPath, cancellationToken)
            .ConfigureAwait(false);
        if (!validationSucceeded)
        {
            return FinalizeResult.Failure(
                outputPath,
                $"Stopped (hdr validation failed: {validationDetail})",
                new[] { outputPath });
        }

        return FinalizeResult.Success(outputPath, "Stopped");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _started = false;
        _encoder.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _started = false;
        await _encoder.DisposeAsync().ConfigureAwait(false);
    }
}
