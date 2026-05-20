using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(
        CaptureTransportProjection captureTransport)
        => new()
        {
            MemoryPreference = captureTransport.MemoryPreference,
            VideoRequestedSubtype = captureTransport.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureTransport.FrameLedgerCapacity,
            FrameLedgerEventCount = captureTransport.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureTransport.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents
        };

    private readonly record struct CaptureTransportFlattenedProjection
    {
        public string MemoryPreference { get; init; }
        public string VideoRequestedSubtype { get; init; }
        public string VideoNegotiatedSubtype { get; init; }
        public int FrameLedgerCapacity { get; init; }
        public long FrameLedgerEventCount { get; init; }
        public long FrameLedgerDroppedEventCount { get; init; }
        public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; }
    }
}
