using System;
using Sussudio.Services.Gpu;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsHardwareRowsInputProviderContext
{
    public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }
    public required Func<int?> GetPendingPreviewFrameCount { get; init; }
    public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }
}

internal sealed class StatsHardwareRowsInputProvider
{
    private readonly StatsHardwareRowsInputProviderContext _context;

    public StatsHardwareRowsInputProvider(StatsHardwareRowsInputProviderContext context)
    {
        _context = context;
    }

    public StatsHardwareDecodeRowsInput? GetDecodeRowsInput()
    {
        var mjpegMetrics = _context.GetMjpegPipelineTimingDetails();
        if (!mjpegMetrics.HasValue || mjpegMetrics.Value.DecoderCount <= 0)
        {
            return null;
        }

        return StatsHardwareRowsInputBuilder.BuildDecodeRowsInput(
            mjpegMetrics.Value,
            _context.GetPendingPreviewFrameCount());
    }

    public StatsHardwareGpuRowsInput? GetGpuRowsInput()
        => StatsHardwareRowsInputBuilder.BuildGpuRowsInput(_context.GetNvmlSnapshot());
}
