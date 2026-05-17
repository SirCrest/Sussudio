using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// Source telemetry polling and fallback merging. These diagnostics are read-only
// from the capture pipeline's point of view, so they live outside the
// lifecycle/resource orchestration file.
public partial class CaptureService
{
    public SourceSignalTelemetrySnapshot GetLatestSourceTelemetrySnapshot() => _latestSourceTelemetry;

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

}
