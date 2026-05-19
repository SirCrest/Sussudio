using System;
using Sussudio.Models;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Capture;

// Read-only reader/transport projection for runtime snapshots. It describes
// the active video transport without touching capture resource lifetimes.
public partial class CaptureService
{
    private static RuntimeReaderTransportSnapshotFields CaptureRuntimeReaderTransportSnapshotFields(
        CaptureSettings? requestedSettings,
        bool hdrRequested,
        UnifiedVideoCapture? unifiedVideoCapture,
        bool videoPreviewActive,
        bool recordingActive,
        IPreviewFrameSink? previewFrameSink,
        string? actualPixelFormat,
        string? lastMfSourceReaderNegotiatedFormat)
    {
        var requestedReaderSubtype = !string.IsNullOrWhiteSpace(requestedSettings?.RequestedPixelFormat)
            ? requestedSettings!.RequestedPixelFormat
            : hdrRequested
                ? "P010"
                : "NV12";
        var mfSourceReaderNegotiatedFormat = unifiedVideoCapture?.NegotiatedFormat ?? lastMfSourceReaderNegotiatedFormat;
        var negotiatedSubtypeFromSourceReader =
            !string.IsNullOrWhiteSpace(mfSourceReaderNegotiatedFormat) &&
            mfSourceReaderNegotiatedFormat.Contains("P010", StringComparison.OrdinalIgnoreCase)
                ? "P010"
                : !string.IsNullOrWhiteSpace(mfSourceReaderNegotiatedFormat) &&
                  mfSourceReaderNegotiatedFormat.Contains("NV12", StringComparison.OrdinalIgnoreCase)
                    ? "NV12"
                    : "unknown";
        var videoNegotiatedSubtype = unifiedVideoCapture != null
            ? (unifiedVideoCapture.IsHighFrameRateMjpegMode ? "MJPG"
                : unifiedVideoCapture.IsP010 ? "P010" : "NV12")
            : negotiatedSubtypeFromSourceReader;
        var readerSourceStreamType = (recordingActive || videoPreviewActive) && unifiedVideoCapture != null
            ? "MfSourceReader"
            : null;
        var frameLedger = unifiedVideoCapture?.GetFrameLedgerSummary() ?? FrameLedgerSummary.Empty;

        return new RuntimeReaderTransportSnapshotFields
        {
            MemoryPreference = unifiedVideoCapture?.D3DManager != null ? "Gpu" : "Cpu",
            VideoRequestedSubtype = requestedReaderSubtype ?? "unknown",
            VideoNegotiatedSubtype = videoNegotiatedSubtype,
            FrameLedgerCapacity = frameLedger.Capacity,
            FrameLedgerEventCount = frameLedger.TotalEventsRecorded,
            FrameLedgerDroppedEventCount = frameLedger.EventsDroppedByRetention,
            FrameLedgerRecentEvents = frameLedger.RecentEvents,
            PreviewColorMetadata = (previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? "None",
            MfSourceReaderNegotiatedFormat = mfSourceReaderNegotiatedFormat,
            RequestedReaderSubtype = requestedReaderSubtype,
            ReaderSourceStreamType = readerSourceStreamType,
            ReaderSourceSubtype = actualPixelFormat
        };
    }

}
