using System;
using System.IO;
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
    private FileStream? _audioFileStream;
    private bool _splitMode;

    public FFmpegEncoderService Encoder => _encoder;

    /// <summary>
    /// Result of the post-mux step (split mode only). <c>null</c> until <see cref="StopAsync"/> completes.
    /// </summary>
    public FinalizeResult? FinalizeResult { get; private set; }

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
        _splitMode = context.UsePostMuxAudio;

        if (_splitMode)
        {
            // Open raw audio file for writing (f32le, 48kHz, stereo)
            if (string.IsNullOrWhiteSpace(context.AudioTempPath))
            {
                throw new InvalidOperationException("Split recording mode requires AudioTempPath.");
            }

            Logger.Log($"Split recording: opening audio file {context.AudioTempPath}");
            _audioFileStream = new FileStream(
                context.AudioTempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 65536,
                useAsync: true);

            // Start encoder in video-only mode
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
                context.Settings.PipelineOptions,
                videoOnly: true);
        }
        else
        {
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
        }

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

    public async Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_started || samples.IsEmpty)
        {
            return;
        }

        var stream = _audioFileStream;
        if (_splitMode && stream != null)
        {
            await stream.WriteAsync(samples, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _encoder.EnqueueAudioSamples(samples);
        }
    }

    public async Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        var outputPath = _context?.FinalOutputPath ?? string.Empty;
        var encodedVideoPath = _context?.VideoOutputPath ?? outputPath;

        if (_disposed)
        {
            return Services.FinalizeResult.Success(outputPath, "Stopped");
        }

        if (_started)
        {
            await _encoder.StopEncodingAsync();
            _started = false;
        }

        // Close the raw audio file (DisposeAsync flushes the internal buffer)
        if (_audioFileStream != null)
        {
            await _audioFileStream.DisposeAsync().ConfigureAwait(false);
            _audioFileStream = null;
        }

        if (_encoder.LastStopTimedOut)
        {
            FinalizeResult = Services.FinalizeResult.Failure(
                outputPath,
                "Stopped (encoder stop timed out)");
            return FinalizeResult;
        }

        if (_encoder.LastWriterDrainTimedOut)
        {
            FinalizeResult = Services.FinalizeResult.Failure(
                outputPath,
                "Stopped (encoder writer drain timed out)");
            return FinalizeResult;
        }

        if (_encoder.LastExitCode is int exitCode && exitCode != 0)
        {
            FinalizeResult = Services.FinalizeResult.Failure(
                outputPath,
                $"Stopped (encoder failed: exit code {exitCode})");
            return FinalizeResult;
        }

        // Post-mux: combine video + raw audio into final output
        if (_splitMode && _context != null)
        {
            var muxResult = await RunPostMuxAsync(_context, cancellationToken).ConfigureAwait(false);
            if (!muxResult.Succeeded)
            {
                FinalizeResult = muxResult;
                return FinalizeResult;
            }
        }

        // HDR validation (runs against the encoded video, or the final muxed file)
        var validationTargetPath = _splitMode ? outputPath : encodedVideoPath;
        if (_context?.HdrPipelineActive == true)
        {
            var (validationSucceeded, validationDetail) = await HdrValidationRunner
                .RunAsync(_context, validationTargetPath, cancellationToken)
                .ConfigureAwait(false);

            if (!validationSucceeded)
            {
                if (validationDetail.Contains("validator-script-missing", StringComparison.Ordinal))
                {
                    Logger.Log($"HDR validation skipped (script not found): {validationDetail}");
                }
                else
                {
                    FinalizeResult = Services.FinalizeResult.Failure(
                        outputPath,
                        $"Stopped (hdr validation failed: {validationDetail})",
                        new[] { validationTargetPath });
                    return FinalizeResult;
                }
            }
        }

        FinalizeResult = Services.FinalizeResult.Success(outputPath, "Stopped");
        return FinalizeResult;
    }

    private async Task<FinalizeResult> RunPostMuxAsync(RecordingContext context, CancellationToken cancellationToken)
    {
        var videoPath = context.VideoOutputPath;
        var audioPath = context.AudioTempPath!;
        var finalPath = context.FinalOutputPath;

        Logger.Log($"Post-mux: combining {videoPath} + {audioPath} → {finalPath}");

        var ffmpegPath = _encoder.FfmpegPath;
        var args = $"-y " +
                   $"-i \"{videoPath}\" " +
                   $"-f f32le -ar 48000 -ac 2 -i \"{audioPath}\" " +
                   $"-c:v copy -c:a aac -b:a 320k " +
                   $"-movflags +faststart " +
                   $"\"{finalPath}\"";

        Logger.Log($"Post-mux FFmpeg args: {args}");

        var supervisor = new ProcessSupervisor();
        var spec = new ProcessSpec
        {
            FileName = ffmpegPath,
            Arguments = args,
            TimeoutMs = 120_000 // 2 minutes should be ample for a copy-mux
        };

        var result = await supervisor.RunAsync(spec, cancellationToken).ConfigureAwait(false);

        if (!result.Started)
        {
            var errorMsg = result.StartException?.Message ?? "unknown error";
            Logger.Log($"Post-mux FFmpeg failed to start: {errorMsg}");
            return Services.FinalizeResult.Failure(
                finalPath,
                $"Stopped (post-mux failed to start: {errorMsg})",
                new[] { videoPath, audioPath });
        }

        if (result.TimedOut)
        {
            Logger.Log("Post-mux FFmpeg timed out");
            return Services.FinalizeResult.Failure(
                finalPath,
                "Stopped (post-mux timed out)",
                new[] { videoPath, audioPath });
        }

        if (result.ExitCode is int muxExitCode && muxExitCode != 0)
        {
            Logger.Log($"Post-mux FFmpeg failed: exit code {muxExitCode}");
            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                Logger.Log($"Post-mux stderr: {result.StdErr}");
            }
            return Services.FinalizeResult.Failure(
                finalPath,
                $"Stopped (post-mux failed: exit code {muxExitCode})",
                new[] { videoPath, audioPath });
        }

        Logger.Log("Post-mux completed successfully");
        return Services.FinalizeResult.Success(finalPath, "Stopped");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _started = false;
        _audioFileStream?.Dispose();
        _audioFileStream = null;
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
        if (_audioFileStream != null)
        {
            await _audioFileStream.DisposeAsync().ConfigureAwait(false);
            _audioFileStream = null;
        }
        await _encoder.DisposeAsync();
    }
}
