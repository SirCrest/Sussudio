using System.Threading.Tasks;

static partial class Program
{
    private static async Task RecordingVerifier_DetectsResolutionMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_res_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1280\n" +
                    "height=720\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedWidth: 1920, negotiatedHeight: 1080);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "resolution-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: frame rate mismatch ──

    private static async Task RecordingVerifier_DetectsFrameRateMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_fps_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=30/1\n" +
                    "r_frame_rate=30/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "fps-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
