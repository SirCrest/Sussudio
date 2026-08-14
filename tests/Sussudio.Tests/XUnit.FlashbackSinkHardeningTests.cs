using System;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackSinkHardeningTests
{
    private static string Source()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs");

    [Fact]
    public void RotateSegment_EscalatesAfterConsecutiveFailures()
    {
        var source = Source();
        Assert.Contains("MaxConsecutiveRotationFailures = 3", source);
        var method = global::Program.ExtractDeclaredMemberCode(source, "private bool RotateSegment");
        Assert.Contains("_consecutiveRotationFailures", method);
        Assert.Contains("FailEncoding", method);
    }

    [Fact]
    public void ForceRotate_PreparesPathBeforeEncoderLaneFence()
    {
        var source = Source();
        var method = global::Program.ExtractDeclaredMemberCode(source, "public FlashbackForceRotateResult ForceRotateForExport");
        Assert.Contains("_bufferManager.ReserveSegmentPath()", method);
        Assert.Contains("new ForceRotateRequest(preparedPath)", method);
        Assert.True(method.IndexOf("ReserveSegmentPath", StringComparison.Ordinal) < method.IndexOf("SignalWork", StringComparison.Ordinal));
        Assert.Contains("RotateSegment(currentPts, localRequest.PreparedPath)", source);
    }

    [Fact]
    public void EndRecording_WaitsForQueueDrain_NotFixedDelay()
    {
        var method = global::Program.ExtractDeclaredMemberCode(Source(), "public async Task<FinalizeResult> EndRecordingAsync");
        Assert.DoesNotContain("Task.Delay(100", method);
        Assert.Contains("WaitForEncodeQueueDrainAsync", method);
    }

    [Fact]
    public void EncodingLoop_FailsFast_WhenDiskCriticallyLow()
    {
        var method = global::Program.ExtractDeclaredMemberCode(Source(), "private void OnVideoFrameEncoded");
        Assert.Contains("IsDiskCriticallyLow", method);
    }
}
