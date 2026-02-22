using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

public sealed class DirectShowPreviewService : IDisposable, IAsyncDisposable
{
    private readonly object _sync = new();
    private Process? _process;
    private CancellationTokenSource? _cts;
    private Task? _readerTask;
    private Task? _stderrTask;
    private bool _running;
    private int _frameSizeBytes;
    private uint _width;
    private uint _height;
    private int _stride;
    private long _frameIndex;
    private string _rendererMode = "None";

    public event EventHandler<PreviewFrame>? FrameReady;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? StatusChanged;

    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _running;
            }
        }
    }

    public string RendererMode
    {
        get
        {
            lock (_sync)
            {
                return _rendererMode;
            }
        }
    }

    public async Task StartAsync(
        string ffmpegPath,
        string videoDeviceName,
        uint width,
        uint height,
        string frameRateArg,
        bool hdrRequested,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ffmpegPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoDeviceName);

        await StopAsync(cancellationToken).ConfigureAwait(false);

        var escapedDeviceName = EscapeDshowDeviceName(videoDeviceName);
        var inputPixelFormatArg = hdrRequested ? "-pixel_format p010le " : string.Empty;
        var args =
            "-hide_banner -loglevel warning -fflags nobuffer -flags low_delay " +
            "-f dshow -rtbufsize 256M " +
            $"-video_size {width}x{height} -framerate {frameRateArg} " +
            $"{inputPixelFormatArg}" +
            $"-i video=\"{escapedDeviceName}\" " +
            "-an -vf format=bgra -pix_fmt bgra -f rawvideo -";

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        Process? process = null;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start preview ingest process: {ex.Message}", ex);
        }

        if (process == null)
        {
            throw new InvalidOperationException("Failed to start preview ingest process.");
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var frameSize = checked((int)(width * height * 4));

        lock (_sync)
        {
            _process = process;
            _cts = linkedCts;
            _width = width;
            _height = height;
            _stride = checked((int)width * 4);
            _frameSizeBytes = frameSize;
            _frameIndex = 0;
            _running = true;
            _rendererMode = "DirectShowRawPipe";
        }

        StatusChanged?.Invoke(this, $"Preview ingest started (pid={process.Id}, {width}x{height}@{frameRateArg}, hdr={hdrRequested}).");

        _readerTask = Task.Run(() => ReadFramesAsync(process, linkedCts.Token));
        _stderrTask = Task.Run(() => ReadStderrAsync(process, linkedCts.Token));
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Process? process;
        CancellationTokenSource? cts;
        Task? readerTask;
        Task? stderrTask;

        lock (_sync)
        {
            process = _process;
            cts = _cts;
            readerTask = _readerTask;
            stderrTask = _stderrTask;
            _process = null;
            _cts = null;
            _readerTask = null;
            _stderrTask = null;
            _running = false;
            _rendererMode = "None";
        }

        if (process == null)
        {
            cts?.Dispose();
            return;
        }

        var stopSucceeded = false;
        string? stopStatusDetail = null;
        try
        {
            cts?.Cancel();

            try
            {
                if (!process.HasExited)
                {
                    await process.StandardInput.WriteLineAsync("q").ConfigureAwait(false);
                    await process.StandardInput.FlushAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                // Best effort: process may have already exited.
            }

            var waitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            var completed = await Task.WhenAny(waitTask, timeoutTask).ConfigureAwait(false);
            if (completed != waitTask && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            if (readerTask != null)
            {
                await AwaitIgnoringCancellationAsync(readerTask).ConfigureAwait(false);
            }

            if (stderrTask != null)
            {
                await AwaitIgnoringCancellationAsync(stderrTask).ConfigureAwait(false);
            }

            stopSucceeded = true;
            stopStatusDetail = "Preview ingest stopped.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopStatusDetail = "Preview ingest stop cancelled.";
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview ingest stop warning: {ex.Message}");
            ErrorOccurred?.Invoke(this, $"Preview ingest stop failed: {ex.Message}");
            stopStatusDetail = $"Preview ingest stop warning: {ex.Message}";
        }
        finally
        {
            try
            {
                process.Dispose();
            }
            catch
            {
                // No-op
            }

            cts?.Dispose();
            if (!string.IsNullOrWhiteSpace(stopStatusDetail))
            {
                StatusChanged?.Invoke(this, stopStatusDetail);
            }
            else if (stopSucceeded)
            {
                StatusChanged?.Invoke(this, "Preview ingest stopped.");
            }
        }
    }

    private async Task ReadFramesAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            var stream = process.StandardOutput.BaseStream;
            var scratch = new byte[_frameSizeBytes];
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = 0;
                while (bytesRead < scratch.Length && !cancellationToken.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(
                        scratch.AsMemory(bytesRead, scratch.Length - bytesRead),
                        cancellationToken).ConfigureAwait(false);
                    if (read <= 0)
                    {
                        if (!process.HasExited && !cancellationToken.IsCancellationRequested)
                        {
                            throw new IOException("Preview ingest stream ended unexpectedly.");
                        }

                        return;
                    }

                    bytesRead += read;
                }

                if (bytesRead != scratch.Length)
                {
                    continue;
                }

                var framePayload = new byte[scratch.Length];
                Buffer.BlockCopy(scratch, 0, framePayload, 0, scratch.Length);
                var frameNumber = Interlocked.Increment(ref _frameIndex);
                FrameReady?.Invoke(this, new PreviewFrame
                {
                    Width = _width,
                    Height = _height,
                    Stride = _stride,
                    PixelFormat = "BGRA8",
                    Buffer = framePayload,
                    FrameIndex = frameNumber,
                    TimestampUtc = DateTimeOffset.UtcNow
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Preview ingest failed: {ex.Message}");
        }
    }

    private async Task ReadStderrAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await process.StandardError.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                Logger.Log($"[FFmpeg Preview] {trimmed}");
                if (trimmed.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorOccurred?.Invoke(this, trimmed);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Preview stderr reader failed: {ex.Message}");
        }
    }

    private static async Task AwaitIgnoringCancellationAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // No-op.
        }
        catch
        {
            // No-op.
        }
    }

    private static string EscapeDshowDeviceName(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return string.Empty;
        }

        var escaped = new StringBuilder(deviceName.Length);
        foreach (var ch in deviceName)
        {
            if (ch == '"')
            {
                escaped.Append("\\\"");
                continue;
            }

            escaped.Append(ch);
        }

        return escaped.ToString();
    }

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // No-op.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
