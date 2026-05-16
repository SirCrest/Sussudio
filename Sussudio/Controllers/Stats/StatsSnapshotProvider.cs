using System;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal sealed class StatsSnapshotProviderContext
{
    public required Func<CaptureHealthSnapshot> GetCaptureHealthSnapshot { get; init; }
    public required Func<D3D11PreviewRenderer?> GetRenderer { get; init; }
    public required Func<double> GetPreviewMinPresentationIntervalMs { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsRecording { get; init; }
}

internal sealed partial class StatsSnapshotProvider
{
    private readonly StatsSnapshotProviderContext _context;

    public StatsSnapshotProvider(StatsSnapshotProviderContext context)
    {
        _context = context;
    }

    public StatsSnapshot GetSnapshot()
    {
        var health = _context.GetCaptureHealthSnapshot();
        var renderer = BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs());
        var viewState = new StatsSnapshotViewState(_context.IsPreviewing(), _context.IsRecording());

        return StatsSnapshotBuilder.Build(health, renderer, viewState);
    }
}
