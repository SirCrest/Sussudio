namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static SourceFlattenedProjection BuildSourceFlattenedProjection(
        SourceSignalProjection sourceSignal,
        SourceTelemetryProjection sourceTelemetry)
        => new()
        {
            Signal = BuildSourceSignalFlattenedProjection(sourceSignal),
            Telemetry = BuildSourceTelemetryFlattenedProjection(sourceTelemetry)
        };

    private readonly record struct SourceFlattenedProjection
    {
        public SourceSignalFlattenedProjection Signal { get; init; }
        public SourceTelemetryFlattenedProjection Telemetry { get; init; }
    }
}
