using System.Threading.Tasks;

static partial class Program
{
    // ── Integration test: ffprobe unavailable ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ffprobe_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }); // minimal mp4 header
        try
        {
            var fake = new FakeProcessSupervisorImpl().WithFfprobeUnavailable();
            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: ffprobe exit code failure ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_exit_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithExitCode(1)
                .WithStreamInfo("");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe-failed");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
