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
        var healthFlashbackBackendText = ReadRepoFile("Sussudio/Models/Capture/CaptureHealthSnapshot.FlashbackBackend.cs");
        var healthFlashbackPlaybackText = ReadRepoFile("Sussudio/Models/Capture/CaptureHealthSnapshot.FlashbackPlayback.cs");
        var healthFlashbackExportText = ReadRepoFile("Sussudio/Models/Capture/CaptureHealthSnapshot.FlashbackExport.cs");

        AssertCaptureHealthSnapshotDefaultsAndInheritance(diagnosticsType, healthType);
        RegisterCaptureDiagnosticsSnapshotProperties(diagnosticsType);
        AssertDeclaredProperties(healthType, CaptureHealthSnapshotPropertySpecs(detailType));
        AssertDeclaredProperties(detailType, CaptureHealthSourceTelemetryDetailPropertySpecs());
        AssertContains(healthRootText, "public sealed partial class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot");
        AssertContains(healthRootText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails");
        AssertDoesNotContain(healthRootText, "public string FlashbackPlaybackState { get; init; }");
        AssertContains(healthFlashbackBackendText, "public sealed partial class CaptureHealthSnapshot");
        AssertContains(healthFlashbackBackendText, "public bool FlashbackBackendSettingsStale { get; init; }");
        AssertContains(healthFlashbackBackendText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertContains(healthFlashbackPlaybackText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(healthFlashbackPlaybackText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(healthFlashbackExportText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(healthFlashbackExportText, "public string? FlashbackExportVerificationFormat { get; init; }");
        AssertDoesNotContain(healthFlashbackBackendText, "public string FlashbackPlaybackState { get; init; }");
        AssertDoesNotContain(healthFlashbackPlaybackText, "public string FlashbackExportStatus { get; init; }");

        var detailEntry = CreateSourceTelemetryDetailEntry(detailType);
        AssertSourceTelemetryDetailEntryValues(detailEntry);
        AssertSourceTelemetryDetailEntryJsonRoundTrip(detailType, detailEntry);

        var health = CreatePopulatedCaptureHealthSnapshot(healthType, detailType, detailEntry);
        AssertCaptureHealthSnapshotRoundTripValues(health);
        AssertCaptureHealthSnapshotJsonRoundTrip(healthType, health);

    }
}
