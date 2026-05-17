using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Gpu;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing capture, health, recording snapshot, and probe entry points.
/// </summary>
public partial class MainViewModel
{
    public CaptureRuntimeSnapshot GetCaptureRuntimeSnapshot() => _captureService.GetRuntimeSnapshot();
    public CaptureHealthSnapshot GetCaptureHealthSnapshot() => _captureService.GetHealthSnapshot();
    public CaptureDiagnosticsSnapshot GetCaptureDiagnosticsSnapshot() => _captureService.GetDiagnosticsSnapshot();
    public RecordingStats GetRecordingStatsSnapshot() => _captureService.GetRecordingStats();
    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
        => _captureService.GetMjpegPipelineTimingDetails();
    public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();
    public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();
    public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);
    public Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetHealthSnapshot, cancellationToken);
    public Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRecordingStats, cancellationToken);
    public Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);
    public Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);
    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)
        => _captureService.CapturePreviewFrameAsync(outputPath, cancellationToken);

    private static Task<T> FromSynchronousSnapshot<T>(Func<T> snapshotFactory, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        return Task.FromResult(snapshotFactory());
    }
    public CaptureSettings BuildCurrentSettings() => BuildCaptureSettings();

}
