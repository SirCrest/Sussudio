using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackFatalPathContractsTests
{
    private static string ReadCaptureServiceSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");

    [Fact]
    public void FlashbackBackendCleanup_DoesNotPurgeSegments()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = global::Program.ExtractDeclaredMemberCode(source, "private void BeginFlashbackBackendCleanup");
        Assert.Contains("purgeSegments: false", cleanup);
        Assert.DoesNotContain("purgeSegments: true", cleanup);
    }

    [Fact]
    public void FlashbackBackendCleanup_PreservesRecoverySegmentsForAllFatalErrors()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = global::Program.ExtractDeclaredMemberCode(source, "private void BeginFlashbackBackendCleanup");
        // Preserve must run unconditionally, not only inside the IsGpuDeviceLost branch.
        Assert.Contains("PreserveRecoverySegments(\"backend_fatal\")", cleanup);
    }

    [Fact]
    public void FlashbackBackendCleanup_SchedulesBoundedAutoRestart()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = global::Program.ExtractDeclaredMemberCode(source, "private void BeginFlashbackBackendCleanup");
        Assert.Contains("TryScheduleFlashbackAutoRestart", cleanup);

        var flashbackSource = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs");
        Assert.Contains("MaxFlashbackAutoRestartAttempts = 2", flashbackSource);
        Assert.Contains("FLASHBACK_AUTO_RESTART", flashbackSource);
    }
}
