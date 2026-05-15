using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionCaptureResultProjection(
        string SelectedResolutionAtEnd,
        double SelectedFrameRateAtEnd,
        string SelectedFriendlyFrameRateAtEnd,
        string SelectedExactFrameRateArgAtEnd,
        string SelectedVideoFormatAtEnd,
        string VideoRequestedSubtypeAtEnd,
        string VideoNegotiatedSubtypeAtEnd,
        int SourceWidthAtEnd,
        int SourceHeightAtEnd,
        double DetectedSourceFrameRateAtEnd,
        string DetectedSourceFrameRateArgAtEnd,
        bool SourceIsHdrAtEnd,
        string SourceTelemetrySummaryAtEnd);

    private static DiagnosticSessionCaptureResultProjection BuildCaptureResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var lastSnapshot = analysis.LastSnapshot;

        return new DiagnosticSessionCaptureResultProjection(
            SelectedResolutionAtEnd: GetString(lastSnapshot, "SelectedResolution") ?? string.Empty,
            SelectedFrameRateAtEnd: GetDouble(lastSnapshot, "SelectedFrameRate"),
            SelectedFriendlyFrameRateAtEnd: GetString(lastSnapshot, "SelectedFriendlyFrameRate") ?? string.Empty,
            SelectedExactFrameRateArgAtEnd: GetString(lastSnapshot, "SelectedExactFrameRateArg") ?? string.Empty,
            SelectedVideoFormatAtEnd: GetString(lastSnapshot, "SelectedVideoFormat") ?? string.Empty,
            VideoRequestedSubtypeAtEnd: GetString(lastSnapshot, "VideoRequestedSubtype") ?? string.Empty,
            VideoNegotiatedSubtypeAtEnd: GetString(lastSnapshot, "VideoNegotiatedSubtype") ?? string.Empty,
            SourceWidthAtEnd: (int)(GetNullableLong(lastSnapshot, "SourceWidth") ?? 0),
            SourceHeightAtEnd: (int)(GetNullableLong(lastSnapshot, "SourceHeight") ?? 0),
            DetectedSourceFrameRateAtEnd: GetDouble(lastSnapshot, "DetectedSourceFrameRate"),
            DetectedSourceFrameRateArgAtEnd: GetString(lastSnapshot, "DetectedSourceFrameRateArg") ?? string.Empty,
            SourceIsHdrAtEnd: GetBool(lastSnapshot, "SourceIsHdr"),
            SourceTelemetrySummaryAtEnd: GetString(lastSnapshot, "SourceTelemetrySummaryText") ?? string.Empty);
    }
}
