using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ElgatoCapture.Services;

public sealed class FfmpegRecordingSink : IRecordingSink
{
    private readonly FFmpegEncoderService _encoder;
    private RecordingContext? _context;
    private bool _started;
    private bool _disposed;

    public FFmpegEncoderService Encoder => _encoder;

    public FfmpegRecordingSink(FFmpegEncoderService encoder)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
    }

    public async Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);

        if (_started)
        {
            throw new InvalidOperationException("FFmpeg recording sink has already started.");
        }

        _context = context;
        await _encoder.StartEncodingAsync(
            context.Settings,
            context.VideoOutputPath,
            context.AudioDeviceName,
            context.EffectiveFrameRate,
            context.FrameRateArg,
            context.EffectiveWidth,
            context.EffectiveHeight,
            context.VideoInputPixelFormat,
            context.HdrPipelineActive,
            context.Settings.PipelineOptions);

        _started = true;
    }

    public Task WriteVideoAsync(SoftwareBitmap frame, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started)
        {
            return Task.CompletedTask;
        }

        _encoder.EnqueueVideoFrame(frame);
        return Task.CompletedTask;
    }

    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started || samples.IsEmpty)
        {
            return Task.CompletedTask;
        }

        _encoder.EnqueueAudioSamples(samples);
        return Task.CompletedTask;
    }

    public async Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        var outputPath = _context?.FinalOutputPath ?? string.Empty;

        if (_disposed)
        {
            return FinalizeResult.Success(outputPath, "Stopped");
        }

        if (_started)
        {
            await _encoder.StopEncodingAsync();
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

        if (_context?.HdrPipelineActive == true)
        {
            var (validationSucceeded, validationDetail) = await HdrValidationRunner
                .RunAsync(_context, outputPath, cancellationToken)
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
        await _encoder.DisposeAsync();
    }
}
