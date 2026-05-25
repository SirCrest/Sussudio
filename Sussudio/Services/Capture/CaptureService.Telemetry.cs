using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// Source telemetry polling, provider reads, and capture-format runtime telemetry.
// These diagnostics are read-only from the capture pipeline's point of view, so
// they live outside the lifecycle/resource orchestration file.
public partial class CaptureService
{
    public SourceSignalTelemetrySnapshot GetLatestSourceTelemetrySnapshot() => _latestSourceTelemetry;

    private Task RefreshSourceTelemetryAsync(CancellationToken cancellationToken)
        => RefreshSourceTelemetryAsync(cancellationToken, Volatile.Read(ref _telemetryPollGeneration));

    private async Task RefreshSourceTelemetryAsync(CancellationToken cancellationToken, long pollGeneration)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fallback = BuildFallbackTelemetry();
        SourceSignalTelemetrySnapshot telemetry;
        try
        {
            telemetry = await _sourceTelemetryProvider
                .ReadAsync(_currentDevice, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"Source telemetry read failed: {ex.Message}");
            telemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("source-telemetry-exception", ex.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (pollGeneration != Volatile.Read(ref _telemetryPollGeneration))
        {
            return;
        }

        _latestSourceTelemetry = MergeTelemetryWithFallback(telemetry, fallback);
        SourceTelemetryUpdated?.Invoke(this, _latestSourceTelemetry);
    }

    private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()
    {
        return new SourceSignalTelemetrySnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Availability = SourceTelemetryAvailability.Inconclusive,
            Origin = SourceTelemetryOrigin.DeviceFormatFallback,
            OriginDetail = "CaptureSettingsFallback",
            Confidence = SourceTelemetryConfidence.Low,
            Width = (int?)_actualWidth ?? (int?)_currentSettings?.Width,
            Height = (int?)_actualHeight ?? (int?)_currentSettings?.Height,
            FrameRateExact = _actualFrameRate ?? _currentSettings?.FrameRate,
            FrameRateArg = _actualFrameRateArg ?? _currentSettings?.RequestedFrameRateArg,
            IsHdr = null,
            DiagnosticSummary = "Using capture-format fallback telemetry."
        };
    }

    private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(
        SourceSignalTelemetrySnapshot telemetry,
        SourceSignalTelemetrySnapshot fallback)
    {
        return telemetry with
        {
            Width = telemetry.Width ?? fallback.Width,
            Height = telemetry.Height ?? fallback.Height,
            FrameRateExact = telemetry.FrameRateExact ?? fallback.FrameRateExact,
            FrameRateArg = telemetry.FrameRateArg ?? fallback.FrameRateArg,
            IsHdr = telemetry.IsHdr ?? fallback.IsHdr,
            Origin = telemetry.Origin == SourceTelemetryOrigin.Unknown
                ? fallback.Origin
                : telemetry.Origin,
            OriginDetail = string.IsNullOrWhiteSpace(telemetry.OriginDetail) ||
                           string.Equals(telemetry.OriginDetail, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? fallback.OriginDetail
                : telemetry.OriginDetail,
            Confidence = telemetry.Confidence == SourceTelemetryConfidence.Unknown
                ? fallback.Confidence
                : telemetry.Confidence,
            VideoFormat = telemetry.VideoFormat ?? fallback.VideoFormat,
            Colorimetry = telemetry.Colorimetry ?? fallback.Colorimetry,
            Quantization = telemetry.Quantization ?? fallback.Quantization,
            HdrTransferFunction = telemetry.HdrTransferFunction ?? fallback.HdrTransferFunction,
            HdrTransferCode = telemetry.HdrTransferCode ?? fallback.HdrTransferCode,
            Firmware = telemetry.Firmware ?? fallback.Firmware,
            AudioFormat = telemetry.AudioFormat ?? fallback.AudioFormat,
            AudioSampleRate = telemetry.AudioSampleRate ?? fallback.AudioSampleRate,
            InputSource = telemetry.InputSource ?? fallback.InputSource,
            UsbHostProtocol = telemetry.UsbHostProtocol ?? fallback.UsbHostProtocol,
            HdcpMode = telemetry.HdcpMode ?? fallback.HdcpMode,
            HdcpVersion = telemetry.HdcpVersion ?? fallback.HdcpVersion,
            RxTxHdcpVersion = telemetry.RxTxHdcpVersion ?? fallback.RxTxHdcpVersion,
            RawTimingHex = telemetry.RawTimingHex ?? fallback.RawTimingHex,
            DetailEntries = telemetry.DetailEntries.Count > 0
                ? telemetry.DetailEntries
                : fallback.DetailEntries,
            DiagnosticSummary = string.IsNullOrWhiteSpace(telemetry.DiagnosticSummary)
                ? fallback.DiagnosticSummary
                : telemetry.DiagnosticSummary
        };
    }

    private void ResetObservedPixelTelemetry()
    {
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;
        _latestObservedSurfaceFormat = null;
        Interlocked.Exchange(ref _observedP010FrameCount, 0);
        Interlocked.Exchange(ref _observedNv12FrameCount, 0);
        Interlocked.Exchange(ref _observedOtherFrameCount, 0);
    }

    private static string? NormalizeObservedPixelFormat(string? pixelFormat)
    {
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return null;
        }

        if (pixelFormat.Contains("P010", StringComparison.OrdinalIgnoreCase))
        {
            return "P010";
        }

        if (pixelFormat.Contains("NV12", StringComparison.OrdinalIgnoreCase))
        {
            return "NV12";
        }

        return pixelFormat.Trim().ToUpperInvariant();
    }

    private void RecordObservedPixelFormat(string? pixelFormat, bool incrementAsFrame = true)
    {
        var normalizedFormat = NormalizeObservedPixelFormat(pixelFormat);
        if (string.IsNullOrWhiteSpace(normalizedFormat))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_firstObservedFramePixelFormat))
        {
            _firstObservedFramePixelFormat = normalizedFormat;
        }

        _latestObservedFramePixelFormat = normalizedFormat;
        _latestObservedSurfaceFormat = normalizedFormat;

        if (!incrementAsFrame)
        {
            return;
        }

        if (string.Equals(normalizedFormat, "P010", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedP010FrameCount);
        }
        else if (string.Equals(normalizedFormat, "NV12", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedNv12FrameCount);
        }
        else
        {
            Interlocked.Increment(ref _observedOtherFrameCount);
        }
    }

    private void CaptureEncoderRuntimeTelemetry(LibAvRecordingSink? sink)
    {
        if (sink == null)
        {
            return;
        }

        Interlocked.Exchange(ref _videoFramesDropped, sink.DroppedVideoFrames);
    }

    /// <summary>
    /// When the driver reports integer frame rates (for example 120/1 for MJPG)
    /// but source telemetry confirms NTSC timing (for example vfreq=11987 is
    /// about 119.88fps), override the actual frame rate to the correct NTSC
    /// rational. This affects recording metadata, cadence tracking, and UI display.
    /// </summary>
    private void TryCorrectFrameRateFromTelemetry()
    {
        if (_actualFrameRateDenominator is not null and not 1)
            return; // Already fractional; no correction needed.

        var telemetry = _latestSourceTelemetry;
        if (!telemetry.HasFrameRate || !telemetry.FrameRateExact.HasValue)
            return;

        // Check if telemetry reports an NTSC rate (x000/1001 family).
        // NativeXu vfreq is in 0.01Hz, so 11987 is about 119.87Hz and close to 120000/1001.
        var telemetryFps = telemetry.FrameRateExact.Value;
        var friendlyBucket = (int)Math.Round(_actualFrameRate ?? 0, MidpointRounding.AwayFromZero);
        if (friendlyBucket <= 0)
            return;

        var expectedNtscFps = friendlyBucket * 1000.0 / 1001.0;
        if (Math.Abs(telemetryFps - expectedNtscFps) > 0.15)
            return; // Telemetry doesn't match NTSC pattern for this bucket.

        var ntscNumerator = (uint)(friendlyBucket * 1000);
        const uint ntscDenominator = 1001;
        var correctedFps = (double)ntscNumerator / ntscDenominator;

        Logger.Log(
            $"FRAMERATE_NTSC_CORRECTION driver={_actualFrameRateNumerator}/{_actualFrameRateDenominator} " +
            $"telemetry={telemetryFps:0.###} corrected={ntscNumerator}/{ntscDenominator} ({correctedFps:0.######})");

        _actualFrameRate = correctedFps;
        _actualFrameRateNumerator = ntscNumerator;
        _actualFrameRateDenominator = ntscDenominator;
        _actualFrameRateArg = $"{ntscNumerator}/{ntscDenominator}";
    }

    private static string ResolveFrameRateArg(CaptureSettings settings, double fallbackFrameRate)
    {
        if (!string.IsNullOrWhiteSpace(settings.RequestedFrameRateArg))
        {
            return settings.RequestedFrameRateArg!;
        }

        if (settings.RequestedFrameRateNumerator.HasValue &&
            settings.RequestedFrameRateDenominator.HasValue &&
            settings.RequestedFrameRateNumerator.Value > 0 &&
            settings.RequestedFrameRateDenominator.Value > 0)
        {
            return $"{settings.RequestedFrameRateNumerator.Value}/{settings.RequestedFrameRateDenominator.Value}";
        }

        return fallbackFrameRate > 0
            ? fallbackFrameRate.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : "60";
    }

    private void StartTelemetryPoll()
    {
        lock (_telemetryPollSync)
        {
            var previousTask = _telemetryPollTask;
            StopTelemetryPollLocked();
            if (previousTask != null && !previousTask.IsCompleted)
            {
                var deferredGeneration = Volatile.Read(ref _telemetryPollGeneration);
                Logger.Log("Telemetry poll start deferred until canceled poll exits");
                _telemetryPollTask = Task.Run(async () =>
                {
                    try
                    {
                        await previousTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected while draining a canceled poll.
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Telemetry poll drain failed before restart: {ex.Message}");
                    }

                    lock (_telemetryPollSync)
                    {
                        if (deferredGeneration == Volatile.Read(ref _telemetryPollGeneration))
                        {
                            StartTelemetryPollCoreLocked();
                        }
                    }
                });
                return;
            }

            StartTelemetryPollCoreLocked();
        }
    }

    private void StartTelemetryPollCore()
    {
        lock (_telemetryPollSync)
        {
            StartTelemetryPollCoreLocked();
        }
    }

    private void StartTelemetryPollCoreLocked()
    {
        var generation = Interlocked.Increment(ref _telemetryPollGeneration);
        var cts = new CancellationTokenSource();
        _telemetryPollCts = cts;
        _telemetryPollTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TelemetryPollIntervalMs, cts.Token).ConfigureAwait(false);
                    await RefreshSourceTelemetryAsync(cts.Token, generation).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Telemetry poll cycle failed: {ex.Message}");
                }
            }
        }, cts.Token);
    }

    private void StopTelemetryPoll()
    {
        lock (_telemetryPollSync)
        {
            StopTelemetryPollLocked();
        }
    }

    private void StopTelemetryPollLocked()
    {
        Interlocked.Increment(ref _telemetryPollGeneration);
        var cts = _telemetryPollCts;
        _telemetryPollCts = null;
        cts?.Cancel();
        if (_telemetryPollTask?.IsCompleted == true)
        {
            _telemetryPollTask = null;
        }
        // Do not dispose the CTS here; the poll task may still be checking
        // the token between Cancel and its own exit. Let GC finalize instead of
        // risking ObjectDisposedException in the poll loop's Task.Delay.
    }

    private async Task StopTelemetryPollAsync()
    {
        Task? task;
        lock (_telemetryPollSync)
        {
            task = _telemetryPollTask;
            StopTelemetryPollLocked();
        }
        if (task == null || task.IsCompleted)
        {
            return;
        }

        try
        {
            await task.WaitAsync(TimeSpan.FromMilliseconds(TelemetryPollStopDrainTimeoutMs)).ConfigureAwait(false);
            lock (_telemetryPollSync)
            {
                if (ReferenceEquals(_telemetryPollTask, task))
                {
                    _telemetryPollTask = null;
                }
            }
        }
        catch (TimeoutException)
        {
            Logger.Log($"Telemetry poll drain timed out after {TelemetryPollStopDrainTimeoutMs}ms");
        }
        catch (OperationCanceledException)
        {
            // Expected when the poll loop observes cancellation.
        }
    }

}
