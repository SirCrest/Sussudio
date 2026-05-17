using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Source-aware automatic resolution and frame-rate selection adapter.
/// </summary>
public partial class MainViewModel
{
    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
        => AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(
            options,
            _resolutionToFormats,
            _latestSourceTelemetry,
            IsHdrEnabled));
}
