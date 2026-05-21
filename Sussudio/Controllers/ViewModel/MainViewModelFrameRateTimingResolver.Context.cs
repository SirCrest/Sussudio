using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sussudio.Models;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the frame-rate timing resolver.
/// </summary>
internal sealed class MainViewModelFrameRateTimingResolverContext
{
    public required Func<IReadOnlyDictionary<string, List<MediaFormat>>> GetResolutionToFormats { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required ObservableCollection<FrameRateOption> AvailableFrameRates { get; init; }
}
