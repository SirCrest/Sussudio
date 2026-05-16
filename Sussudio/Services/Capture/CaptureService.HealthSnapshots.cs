using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Health snapshot projection for diagnostics and automation health checks.
// Keep this read-only; lifecycle mutations belong in coordinator/transition paths.
public partial class CaptureService
{
    public CaptureHealthSnapshot GetHealthSnapshot()
    {
        var sink = _libavSink;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var fbSink = _flashbackSink;
        var bufMgr = _flashbackBufferManager;
        var fbPlayback = _flashbackPlaybackController;
        var fatalCleanupInProgress = Volatile.Read(ref _fatalCleanupInProgress) != 0;
        var flashbackCleanupInProgress = Volatile.Read(ref _flashbackCleanupInProgress) != 0;
        var observedTelemetry = ResolveObservedFrameTelemetry();
        var sourceTelemetry = CaptureSourceTelemetryHealthSnapshotFields(_latestSourceTelemetry);
        var captureCadence = BuildCaptureCadenceHealthSnapshotFields(unifiedVideoCapture);
        var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);
        var avSyncHealth = CaptureAvSyncHealthSnapshotFields();
        var recordingHealth = CaptureRecordingHealthSnapshotFields(sink, fbSink);
        var flashbackQueues = CaptureFlashbackQueueHealthSnapshotFields(
            fbSink,
            recordingHealth.FlashbackVideoQueueLatencyMetrics);
        var snapshotUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var flashbackExport = CaptureFlashbackExportHealthSnapshotFields(snapshotUtcUnixMs);
        var flashbackBackendSettings = _flashbackBackendSettings;
        var flashbackBuffer = CaptureFlashbackBufferHealthSnapshotFields(
            fbSink,
            bufMgr,
            flashbackBackendSettings,
            _currentSettings);

        var flashbackPlayback = CaptureFlashbackPlaybackHealthSnapshotFields(fbPlayback);

        return AssembleCaptureHealthSnapshot(
            new CaptureHealthSnapshotAssemblyFields(
                unifiedVideoCapture,
                fatalCleanupInProgress,
                flashbackCleanupInProgress,
                observedTelemetry,
                sourceTelemetry,
                captureCadence,
                mjpegHealth,
                avSyncHealth,
                recordingHealth,
                flashbackQueues,
                snapshotUtcUnixMs,
                flashbackExport,
                flashbackBuffer,
                flashbackPlayback));
    }
}
