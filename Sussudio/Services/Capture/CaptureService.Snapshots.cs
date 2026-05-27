using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// Snapshot projection for diagnostics, automation, and verification. These
// methods translate live service objects into immutable DTOs without mutating
// capture state, so they can be polled frequently by ssctl/MCP.
public partial class CaptureService
{
    private const int PreviewFrameCaptureRendererWaitTimeoutMs = 2000;
    private const int PreviewFrameCaptureRendererPollMs = 50;

    private string ResolveRecordingBackendName()
    {
        if (IsFlashbackRecordingBackendOwnedByRecording())
            return "Flashback";
        return _isRecording && _recordingBackend.LibAvSink != null ? "LibAv" : "None";
    }

    public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        // CaptureHealthSnapshot inherits from CaptureDiagnosticsSnapshot,
        // so the full snapshot satisfies the diagnostics contract directly.
        return GetHealthSnapshot();
    }

    public VideoSourceProbeResult ProbeVideoSource()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture == null)
        {
            return new VideoSourceProbeResult
            {
                SessionActive = false,
                MemoryPreference = "Unknown"
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        var fps = Math.Round(unifiedVideoCapture.Fps, 3);
        return new VideoSourceProbeResult
        {
            SessionActive = true,
            MemoryPreference = unifiedVideoCapture.IsP010 ? "Auto" : "Cpu",
            CurrentSubtype = subtype,
            CurrentWidth = unifiedVideoCapture.Width,
            CurrentHeight = unifiedVideoCapture.Height,
            CurrentFrameRate = fps,
            P010Available = unifiedVideoCapture.IsP010,
            Nv12Available = !unifiedVideoCapture.IsP010,
            SupportedSubtypes = new[] { subtype },
            TotalFormatCount = 1,
            Formats = new[]
            {
                new VideoSourceFormatEntry
                {
                    Subtype = subtype,
                    Width = unifiedVideoCapture.Width,
                    Height = unifiedVideoCapture.Height,
                    FrameRate = fps,
                    Summary = $"{subtype} {unifiedVideoCapture.Width}x{unifiedVideoCapture.Height}@{fps:0.###}"
                }
            }
        };
    }

    public PreviewColorProbeResult ProbePreviewColor()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        var d3dSink = _videoPipeline.PreviewFrameSink as D3D11PreviewRenderer;
        var d3dInputColor = d3dSink?.InputColorSpaceLabel ?? "None";
        var d3dOutputColor = d3dSink?.OutputColorSpaceLabel ?? "None";
        if (unifiedVideoCapture == null)
        {
            return new PreviewColorProbeResult
            {
                SessionActive = false,
                D3DInputColorSpace = d3dInputColor,
                D3DOutputColorSpace = d3dOutputColor
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        return new PreviewColorProbeResult
        {
            SessionActive = true,
            RendererMode = d3dSink?.RendererMode ?? "None",
            NegotiatedSubtype = subtype,
            SourceWidth = unifiedVideoCapture.Width,
            SourceHeight = unifiedVideoCapture.Height,
            SourceFrameRate = Math.Round(unifiedVideoCapture.Fps, 3),
            D3DInputColorSpace = d3dInputColor,
            D3DOutputColorSpace = d3dOutputColor
        };
    }

    public async Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var waitStartedAt = Stopwatch.GetTimestamp();
        while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)
        {
            var d3dSink = _videoPipeline.PreviewFrameSink as D3D11PreviewRenderer;
            if (d3dSink is { IsRendering: true })
            {
                return await d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);
            }

            if (Stopwatch.GetElapsedTime(waitStartedAt).TotalMilliseconds >= PreviewFrameCaptureRendererWaitTimeoutMs)
            {
                break;
            }

            try
            {
                await Task.Delay(PreviewFrameCaptureRendererPollMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return new PreviewFrameCaptureResult
        {
            Succeeded = false,
            Message = "No active preview renderer."
        };
    }

    private static long ComputeTickAge(long tick)
    {
        if (tick == 0) return -1;
        return Math.Max(0, Environment.TickCount64 - tick);
    }

    public RecordingStats GetRecordingStats()
    {
        try
        {
            if (_isRecording && _recordingBackend.LibAvSink != null)
            {
                return new RecordingStats(_recordingBackend.LibAvSink.OutputBytes, 0);
            }

            // Flashback recording: the output file doesn't exist until export-on-stop.
            // Report estimated size from the flashback buffer bytes written since recording start.
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                var bufferManager = _flashbackBackend.BufferManager;
                if (bufferManager != null)
                {
                    return new RecordingStats(bufferManager.TotalBytesWritten - _flashbackRecordingStartBytes, 0, isFlashbackEstimate: true);
                }
            }

            var path = _recordingBackend.Context?.VideoOutputPath ?? _lastOutputPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return new RecordingStats(0, 0);
            }

            try
            {
                return new RecordingStats(new FileInfo(path).Length, 0);
            }
            catch (FileNotFoundException)
            {
                return new RecordingStats(0, 0);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"GetRecordingStats failed: {ex.Message}");
            return new RecordingStats(0, 0, isFailure: true);
        }
    }

    private static string? ResolveEncoderCodecName(CaptureSettings? settings)
        => settings == null ? null : MediaFormat.MapNvencCodecName(settings.Format);

    private static string? ResolveEncoderOutputPixelFormat(RecordingContext? context, CaptureSettings? settings)
    {
        if (context?.HdrPipelineActive == true)
        {
            return "yuv420p10le";
        }

        return settings == null ? null : "yuv420p";
    }

    private static string? ResolveEncoderVideoProfile(RecordingContext? context, CaptureSettings? settings)
    {
        if (settings == null)
        {
            return null;
        }

        if (context?.HdrPipelineActive == true)
        {
            return "main10";
        }

        return settings.Format switch
        {
            RecordingFormat.H264Mp4 => "high",
            _ => "main"
        };
    }

    private static string? ResolveRequestedFrameRateArg(CaptureSettings? settings, string? fallbackArg)
    {
        if (!string.IsNullOrWhiteSpace(settings?.RequestedFrameRateArg))
        {
            return settings.RequestedFrameRateArg;
        }

        if (settings?.RequestedFrameRateNumerator is uint numerator &&
            settings.RequestedFrameRateDenominator is uint denominator &&
            numerator > 0 &&
            denominator > 0)
        {
            return $"{numerator}/{denominator}";
        }

        return fallbackArg;
    }

    private ObservedFrameSnapshotFields ResolveObservedFrameTelemetry()
    {
        var expectedFormat = _recordingBackend.Context?.HdrPipelineActive == true ? "P010" : _recordingBackend.Context != null ? "NV12" : null;
        var firstObserved = _firstObservedFramePixelFormat ?? expectedFormat;
        var latestObserved = _latestObservedFramePixelFormat ?? expectedFormat;
        var latestSurface = _latestObservedSurfaceFormat ?? latestObserved;

        return new ObservedFrameSnapshotFields(
            FirstObservedFramePixelFormat: firstObserved,
            LatestObservedFramePixelFormat: latestObserved,
            LatestObservedSurfaceFormat: latestSurface,
            ObservedP010FrameCount: Math.Max(0, Interlocked.Read(ref _observedP010FrameCount)),
            ObservedNv12FrameCount: Math.Max(0, Interlocked.Read(ref _observedNv12FrameCount)),
            ObservedOtherFrameCount: Math.Max(0, Interlocked.Read(ref _observedOtherFrameCount)),
            ObservedP010BitDepthSampleCount: 0,
            ObservedP010Low2BitNonZeroPercent: 0,
            ObservedP010Likely8BitUpscaled: null);
    }

    private static string ResolveSourceTelemetryBackend(SourceSignalTelemetrySnapshot telemetry)
        => telemetry.Origin switch
        {
            SourceTelemetryOrigin.DeviceFormatFallback => "DeviceFormatFallback",
            SourceTelemetryOrigin.NativeXu => "NativeXu",
            _ => "Unknown"
        };

    private static string? ResolveSourceTelemetrySuppressedReason(SourceSignalTelemetrySnapshot telemetry)
    {
        if (string.IsNullOrWhiteSpace(telemetry.DiagnosticSummary))
        {
            return null;
        }

        if (telemetry.DiagnosticSummary.Contains("suppressed", StringComparison.OrdinalIgnoreCase) ||
            telemetry.DiagnosticSummary.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            return telemetry.DiagnosticSummary;
        }

        return null;
    }

    private static string ResolveSourceTelemetryCircuitState(
        SourceTelemetryAvailability availability,
        bool telemetrySuppressed)
    {
        if (telemetrySuppressed)
        {
            return "Open";
        }

        return availability switch
        {
            SourceTelemetryAvailability.Unavailable => "Open",
            SourceTelemetryAvailability.Stale => "Open",
            _ => "Closed"
        };
    }

    private void ResetAvSyncDriftBaseline()
    {
        _avSyncBaselineDriftMs = double.NaN;
    }

    private (double? DriftMs, double? RateMsPerSec) ComputeAvSyncDrift()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        var wasapiCapture = _previewAudioGraph.ProgramCapture;
        if (unifiedVideoCapture == null || wasapiCapture == null)
        {
            return (null, null);
        }

        var videoFrames = unifiedVideoCapture.VideoFramesArrived;
        var audioFrames = wasapiCapture.AudioFramesArrived;
        var negotiatedFps = unifiedVideoCapture.Fps;

        if (videoFrames <= 0 || audioFrames <= 0 || negotiatedFps <= 0)
        {
            return (null, null);
        }

        var rawDriftMs = (audioFrames / 48000.0 - videoFrames / negotiatedFps) * 1000.0;

        if (double.IsNaN(_avSyncBaselineDriftMs))
        {
            _avSyncBaselineDriftMs = rawDriftMs;
            _avSyncPrevDriftMs = 0.0;
            _avSyncPrevDriftTick = Environment.TickCount64;
            return (0.0, 0.0);
        }

        var correctedDrift = rawDriftMs - _avSyncBaselineDriftMs;
        var now = Environment.TickCount64;
        var elapsedMs = now - _avSyncPrevDriftTick;

        if (elapsedMs >= 5000)
        {
            var elapsedSec = elapsedMs / 1000.0;
            _avSyncDriftRateMsPerSec = (correctedDrift - _avSyncPrevDriftMs) / elapsedSec;
            _avSyncPrevDriftMs = correctedDrift;
            _avSyncPrevDriftTick = now;
        }

        return (correctedDrift, _avSyncDriftRateMsPerSec);
    }

    private (double? EncoderDriftMs, long? EncoderCorrectionSamples) GetEncoderAvSyncDrift()
    {
        var sink = _recordingBackend.LibAvSink;
        if (sink != null && sink.TryGetEncoderAvSyncDrift(out var driftMs, out var correctionSamples))
        {
            return (driftMs, correctionSamples);
        }

        return (null, null);
    }

    private AvSyncHealthSnapshotFields CaptureAvSyncHealthSnapshotFields()
    {
        var (captureDriftMs, captureDriftRateMsPerSec) = ComputeAvSyncDrift();
        var (encoderDriftMs, encoderCorrectionSamples) = GetEncoderAvSyncDrift();

        return new AvSyncHealthSnapshotFields(
            captureDriftMs,
            captureDriftRateMsPerSec,
            encoderDriftMs,
            encoderCorrectionSamples);
    }

    private double _avSyncBaselineDriftMs = double.NaN;
    private double _avSyncPrevDriftMs;
    private long _avSyncPrevDriftTick;
    private double _avSyncDriftRateMsPerSec;

    private readonly record struct ObservedFrameSnapshotFields(
        string? FirstObservedFramePixelFormat,
        string? LatestObservedFramePixelFormat,
        string? LatestObservedSurfaceFormat,
        long ObservedP010FrameCount,
        long ObservedNv12FrameCount,
        long ObservedOtherFrameCount,
        long ObservedP010BitDepthSampleCount,
        double ObservedP010Low2BitNonZeroPercent,
        bool? ObservedP010Likely8BitUpscaled);

    private readonly record struct AvSyncHealthSnapshotFields(
        double? CaptureDriftMs,
        double? CaptureDriftRateMsPerSec,
        double? EncoderDriftMs,
        long? EncoderCorrectionSamples);

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

        var telemetryFps = telemetry.FrameRateExact.Value;
        var friendlyBucket = (int)Math.Round(_actualFrameRate ?? 0, MidpointRounding.AwayFromZero);
        if (friendlyBucket <= 0)
            return;

        var expectedNtscFps = friendlyBucket * 1000.0 / 1001.0;
        if (Math.Abs(telemetryFps - expectedNtscFps) > 0.15)
            return;

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
