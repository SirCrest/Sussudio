using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    internal Task<FinalizeResult> ExportFlashbackRangeAsync(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken,
        TimeSpan? inPointFilePts = null,
        TimeSpan? outPointFilePts = null,
        bool force = false)
    {
        ThrowIfDisposed();
        return _captureService.ExportFlashbackRangeAsync(
            inPoint,
            outPoint,
            outputPath,
            progress,
            cancellationToken,
            inPointFilePts,
            outPointFilePts,
            force);
    }

    internal Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(
        double seconds,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken,
        bool force = false)
    {
        ThrowIfDisposed();
        return _captureService.ExportFlashbackLastNSecondsAsync(seconds, outputPath, progress, cancellationToken, force);
    }

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        ThrowIfDisposed();
        return _captureService.GetFlashbackSegments();
    }
}
