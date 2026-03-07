using System;
using System.Diagnostics;
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
    private Process? _audioEncoderProcess;
    private Stream? _audioStdinStream;
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
            if (string.IsNullOrWhiteSpace(context.AudioTempPath))
            {
                throw new InvalidOperationException("Split recording mode requires AudioTempPath.");
            }

            // Start a lightweight FFmpeg process that encodes f32le stdin → AAC M4A in real-time.
            // This replaces the old raw f32le FileStream approach — audio is encoded during
            // recording so post-mux only needs -c:a copy (near-instant regardless of duration).
            var ffmpegPath = _encoder.FfmpegPath;
            var audioArgs = $"-y -f f32le -ar 48000 -ac 2 -i pipe:0 " +
                            $"-c:a aac -b:a 320k " +
                            $"\"{context.AudioTempPath}\"";

            Logger.Log($"Split recording: starting audio encoder → {context.AudioTempPath}");
            Logger.Log($"Audio encoder args: {audioArgs}");

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = audioArgs,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            };

            _audioEncoderProcess = Process.Start(startInfo);
            if (_audioEncoderProcess == null)
            {
                throw new InvalidOperationException("Failed to start audio encoder FFmpeg process.");
            }

            _audioStdinStream = _audioEncoderProcess.StandardInput.BaseStream;
            Logger.Log($"Audio encoder started (PID: {_audioEncoderProcess.Id})");

            // Start video encoder in video-only mode
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

        var stream = _audioStdinStream;
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

        // Close audio encoder stdin to signal EOF, then wait for it to finish
        if (_audioEncoderProcess != null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (_audioStdinStream != null)
                {
                    await _audioStdinStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    _audioStdinStream.Close();
                    _audioStdinStream = null;
                }

                using var exitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                exitCts.CancelAfter(30_000); // 30s should be ample for AAC flush
                await _audioEncoderProcess.WaitForExitAsync(exitCts.Token).ConfigureAwait(false);

                var exitCode = _audioEncoderProcess.ExitCode;
                Logger.Log($"Audio encoder exited (code={exitCode}, elapsed={sw.ElapsedMilliseconds}ms)");

                if (exitCode != 0)
                {
                    FinalizeResult = Services.FinalizeResult.Failure(
                        outputPath,
                        $"Stopped (audio encoder failed: exit code {exitCode})");
                    return FinalizeResult;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log($"Audio encoder wait timed out after {sw.ElapsedMilliseconds}ms — killing");
                try { _audioEncoderProcess.Kill(); } catch { /* best-effort */ }
                FinalizeResult = Services.FinalizeResult.Failure(
                    outputPath,
                    "Stopped (audio encoder timed out)");
                return FinalizeResult;
            }
            finally
            {
                _audioEncoderProcess.Dispose();
                _audioEncoderProcess = null;
            }
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

        if (_encoder.LastExitCode is int exitCode2 && exitCode2 != 0)
        {
            FinalizeResult = Services.FinalizeResult.Failure(
                outputPath,
                $"Stopped (encoder failed: exit code {exitCode2})");
            return FinalizeResult;
        }

        // Post-mux: combine video + audio into final output (copy-only, near-instant)
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
                   $"-i \"{audioPath}\" " +
                   $"-c:v copy -c:a copy " +
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
        _audioStdinStream?.Dispose();
        _audioStdinStream = null;
        _audioEncoderProcess?.Dispose();
        _audioEncoderProcess = null;
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
        if (_audioStdinStream != null)
        {
            await _audioStdinStream.DisposeAsync().ConfigureAwait(false);
            _audioStdinStream = null;
        }
        _audioEncoderProcess?.Dispose();
        _audioEncoderProcess = null;
        await _encoder.DisposeAsync();
    }
}
