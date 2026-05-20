namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            RequestedReaderSubtype = captureFormat.RequestedReaderSubtype,
            ReaderSourceStreamType = captureFormat.ReaderSourceStreamType,
            ReaderSourceSubtype = captureFormat.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureFormat.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureFormat.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureFormat.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureFormat.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureFormat.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureFormat.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureFormat.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureFormat.ObservedP010Likely8BitUpscaled,
            MfReadwriteDisableConverters = captureFormat.MfReadwriteDisableConverters
        };

    private readonly record struct CaptureFormatReaderObservationFlattenedProjection
    {
        public string? RequestedReaderSubtype { get; init; }
        public string? ReaderSourceStreamType { get; init; }
        public string? ReaderSourceSubtype { get; init; }
        public string? FirstObservedFramePixelFormat { get; init; }
        public string? LatestObservedFramePixelFormat { get; init; }
        public string? LatestObservedSurfaceFormat { get; init; }
        public long ObservedP010FrameCount { get; init; }
        public long ObservedNv12FrameCount { get; init; }
        public long ObservedOtherFrameCount { get; init; }
        public long ObservedP010BitDepthSampleCount { get; init; }
        public double ObservedP010Low2BitNonZeroPercent { get; init; }
        public bool? ObservedP010Likely8BitUpscaled { get; init; }
        public bool? MfReadwriteDisableConverters { get; init; }
    }
}
