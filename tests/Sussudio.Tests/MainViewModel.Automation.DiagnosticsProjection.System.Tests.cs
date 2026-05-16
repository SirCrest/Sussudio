using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var processResourceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertContains(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertContains(snapshotFlatteningText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax,");

        AssertContains(processResourceProjectionText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsAvSyncProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var avSyncProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSync.CaptureDriftMs,");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,");
        AssertContains(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSync.EncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,");

        AssertContains(avSyncProjectionText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncProjection");

        return Task.CompletedTask;
    }

}
