using System.Threading.Tasks;

static partial class Program
{
    // ── Integration test: NTSC frame rate tolerance ──

    internal static async Task RecordingVerifier_PassesNtscFrameRateWithinTolerance()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ntsc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // 59.94 fps (60000/1001) vs expected 60 fps — within 0.75 tolerance
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60000/1001\n" +
                    "r_frame_rate=60000/1001\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            // 60 - 59.94 = 0.06 which is within 0.75 tolerance
            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
