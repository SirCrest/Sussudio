using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync()
    {
        var diagnosticsType = RequireType("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var healthRootText = ReadRepoFile("Sussudio/Models/Capture/CaptureHealthSnapshot.cs");
        var healthFlashbackText = ReadRepoFile("Sussudio/Models/Capture/CaptureHealthSnapshot.Flashback.cs");

        AssertCaptureHealthSnapshotDefaultsAndInheritance(diagnosticsType, healthType);
        RegisterCaptureDiagnosticsSnapshotProperties(diagnosticsType);
        AssertDeclaredProperties(healthType, CaptureHealthSnapshotPropertySpecs(detailType));
        AssertDeclaredProperties(detailType, CaptureHealthSourceTelemetryDetailPropertySpecs());
        AssertContains(healthRootText, "public sealed partial class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot");
        AssertContains(healthRootText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails");
        AssertDoesNotContain(healthRootText, "public string FlashbackPlaybackState { get; init; }");
        AssertContains(healthFlashbackText, "public sealed partial class CaptureHealthSnapshot");
        AssertContains(healthFlashbackText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(healthFlashbackText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(healthFlashbackText, "public string? FlashbackExportVerificationFormat { get; init; }");

        var detailEntry = CreateSourceTelemetryDetailEntry(detailType);
        AssertSourceTelemetryDetailEntryValues(detailEntry);
        AssertSourceTelemetryDetailEntryJsonRoundTrip(detailType, detailEntry);

        var health = CreatePopulatedCaptureHealthSnapshot(healthType, detailType, detailEntry);
        AssertCaptureHealthSnapshotRoundTripValues(health);
        AssertCaptureHealthSnapshotJsonRoundTrip(healthType, health);

    }
}
