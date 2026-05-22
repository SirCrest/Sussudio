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

        AssertCaptureHealthSnapshotDefaultsAndInheritance(diagnosticsType, healthType);
        RegisterCaptureDiagnosticsSnapshotProperties(diagnosticsType);
        AssertDeclaredProperties(healthType, CaptureHealthSnapshotPropertySpecs(detailType));
        AssertDeclaredProperties(detailType, CaptureHealthSourceTelemetryDetailPropertySpecs());
        AssertContains(healthRootText, "public sealed class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot");
        AssertContains(healthRootText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails");
        AssertContains(healthRootText, "public bool FlashbackBackendSettingsStale { get; init; }");
        AssertContains(healthRootText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertContains(healthRootText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(healthRootText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(healthRootText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(healthRootText, "public string? FlashbackExportVerificationFormat { get; init; }");
        AssertDoesNotContain(healthRootText, "partial class CaptureHealthSnapshot");

        var detailEntry = CreateSourceTelemetryDetailEntry(detailType);
        AssertSourceTelemetryDetailEntryValues(detailEntry);
        AssertSourceTelemetryDetailEntryJsonRoundTrip(detailType, detailEntry);

        var health = CreatePopulatedCaptureHealthSnapshot(healthType, detailType, detailEntry);
        AssertCaptureHealthSnapshotRoundTripValues(health);
        AssertCaptureHealthSnapshotJsonRoundTrip(healthType, health);

    }
}
