using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var processResourceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(snapshotFlatteningText, "var processResourceFlattening = BuildProcessResourceFlattenedProjection(processResourceProjection);");
        AssertContains(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceFlattening.MemoryWorkingSetMb,");
        AssertContains(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceFlattening.MemoryGcFragmentationPercent,");
        AssertContains(snapshotFlatteningText, "ThreadPoolIoMax = processResourceFlattening.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax,");

        AssertContains(processResourceProjectionText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceProjection");
        AssertContains(processResourceProjectionText, "private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsAvSyncProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var avSyncProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var avSyncFlattening = BuildAvSyncFlattenedProjection(avSync);");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSyncFlattening.CaptureDriftMs,");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftRateMsPerSec = avSyncFlattening.CaptureDriftRateMsPerSec,");
        AssertContains(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSyncFlattening.EncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSync.CaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSync.EncoderCorrectionSamples,");

        AssertContains(avSyncProjectionText, "private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = avSync.CaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = avSync.EncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncFlattenedProjection");

        AssertContains(avSyncProjectionText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncProjection");

        return Task.CompletedTask;
    }

}
