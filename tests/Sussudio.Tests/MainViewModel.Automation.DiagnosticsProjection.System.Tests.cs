using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var processResourceFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.ProcessResources.cs")
            .Replace("\r\n", "\n");
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

        AssertContains(processResourceFlatteningText, "private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(");
        AssertContains(processResourceFlatteningText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertContains(processResourceFlatteningText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertContains(processResourceFlatteningText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax");
        AssertContains(processResourceFlatteningText, "private readonly record struct ProcessResourceFlattenedProjection");

        AssertContains(processResourceProjectionText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsAvSyncProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var avSyncFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AvSync.cs")
            .Replace("\r\n", "\n");
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

        AssertContains(avSyncFlatteningText, "private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)");
        AssertContains(avSyncFlatteningText, "CaptureDriftMs = avSync.CaptureDriftMs,");
        AssertContains(avSyncFlatteningText, "CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,");
        AssertContains(avSyncFlatteningText, "EncoderCorrectionSamples = avSync.EncoderCorrectionSamples");
        AssertContains(avSyncFlatteningText, "private readonly record struct AvSyncFlattenedProjection");

        AssertContains(avSyncProjectionText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncProjection");

        return Task.CompletedTask;
    }

}
