namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Requested = BuildCaptureFormatRequestedFlattenedProjection(captureFormat),
            HdrRequest = BuildCaptureFormatHdrRequestFlattenedProjection(captureFormat),
            Actual = BuildCaptureFormatActualFlattenedProjection(captureFormat),
            Negotiated = BuildCaptureFormatNegotiatedFlattenedProjection(captureFormat),
            ReaderObservation = BuildCaptureFormatReaderObservationFlattenedProjection(captureFormat),
            Encoder = BuildCaptureFormatEncoderFlattenedProjection(captureFormat)
        };

    private readonly record struct CaptureFormatFlattenedProjection
    {
        public CaptureFormatRequestedFlattenedProjection Requested { get; init; }
        public CaptureFormatHdrRequestFlattenedProjection HdrRequest { get; init; }
        public CaptureFormatActualFlattenedProjection Actual { get; init; }
        public CaptureFormatNegotiatedFlattenedProjection Negotiated { get; init; }
        public CaptureFormatReaderObservationFlattenedProjection ReaderObservation { get; init; }
        public CaptureFormatEncoderFlattenedProjection Encoder { get; init; }
    }
}
