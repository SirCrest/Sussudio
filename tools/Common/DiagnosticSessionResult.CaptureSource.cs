namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Capture/source summary.
    public string SelectedResolutionAtEnd { get; init; } = string.Empty;
    public double SelectedFrameRateAtEnd { get; init; }
    public string SelectedFriendlyFrameRateAtEnd { get; init; } = string.Empty;
    public string SelectedExactFrameRateArgAtEnd { get; init; } = string.Empty;
    public string SelectedVideoFormatAtEnd { get; init; } = string.Empty;
    public string VideoRequestedSubtypeAtEnd { get; init; } = string.Empty;
    public string VideoNegotiatedSubtypeAtEnd { get; init; } = string.Empty;
    public int SourceWidthAtEnd { get; init; }
    public int SourceHeightAtEnd { get; init; }
    public double DetectedSourceFrameRateAtEnd { get; init; }
    public string DetectedSourceFrameRateArgAtEnd { get; init; } = string.Empty;
    public bool SourceIsHdrAtEnd { get; init; }
    public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;
}
