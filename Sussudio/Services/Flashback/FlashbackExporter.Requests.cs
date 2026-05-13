using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    /// <summary>
    /// Exports a flashback range to .mp4 based on the request parameters.
    /// Uses multi-segment export when <see cref="FlashbackExportRequest.Segments"/> or
    /// <see cref="FlashbackExportRequest.SegmentPaths"/> is set,
    /// otherwise falls back to single-file export from <see cref="FlashbackExportRequest.InputTsPath"/>.
    /// </summary>
    public Task<FinalizeResult> ExportAsync(
        FlashbackExportRequest request,
        IProgress<ExportProgress>? progress,
        CancellationToken ct)
    {
        if (request == null)
        {
            return Task.FromResult(FinalizeResult.Failure(
                string.Empty,
                "Flashback export failed: request is required."));
        }

        lock (_lifetimeSync)
        {
            if (_disposed)
            {
                return Task.FromResult(CreateDisposedExportResult(request.OutputPath));
            }
        }

        if (request.Segments is { Count: > 0 })
        {
            SetNextAdaptiveThrottleDelayProvider(request.AdaptiveThrottleDelayMsProvider);
            return ExportSegmentsAsync(request.Segments, request.InPoint, request.OutPoint,
                request.OutputPath, request.FastStart, request.Force, progress, ct);
        }

        if (request.SegmentPaths is { Count: > 0 })
        {
            SetNextAdaptiveThrottleDelayProvider(request.AdaptiveThrottleDelayMsProvider);
            return ExportSegmentsAsync(
                request.SegmentPaths.Select(path => new FlashbackExportSegment { Path = path }).ToArray(),
                request.InPoint,
                request.OutPoint,
                request.OutputPath,
                request.FastStart,
                request.Force,
                progress,
                ct);
        }

        SetNextAdaptiveThrottleDelayProvider(request.AdaptiveThrottleDelayMsProvider);
        return ExportSingleAsync(request.InputTsPath!, request.InPoint, request.OutPoint,
            request.OutputPath, request.FastStart, request.Force, progress, ct);
    }
}
