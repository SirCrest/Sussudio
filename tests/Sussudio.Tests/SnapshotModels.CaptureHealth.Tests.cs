using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync()
    {
        var diagnosticsType = RequireType("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");

        AssertCaptureHealthSnapshotDefaultsAndInheritance(diagnosticsType, healthType);
        RegisterCaptureDiagnosticsSnapshotProperties(diagnosticsType);
        AssertDeclaredProperties(healthType, CaptureHealthSnapshotPropertySpecs(detailType));
        AssertDeclaredProperties(detailType, CaptureHealthSourceTelemetryDetailPropertySpecs());

        var detailEntry = CreateSourceTelemetryDetailEntry(detailType);
        AssertSourceTelemetryDetailEntryValues(detailEntry);
        AssertSourceTelemetryDetailEntryJsonRoundTrip(detailType, detailEntry);

        var health = CreatePopulatedCaptureHealthSnapshot(healthType, detailType, detailEntry);
        AssertCaptureHealthSnapshotRoundTripValues(health);
        AssertCaptureHealthSnapshotJsonRoundTrip(healthType, health);

        return Task.CompletedTask;
    }
}
