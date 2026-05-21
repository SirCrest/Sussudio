using System;
using System.Threading;

namespace Sussudio.Services.Capture;

// Observed frame projection for snapshots. The counters are explicit telemetry;
// this path must not infer fake frame counts from requested settings.
public partial class CaptureService
{
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
}
