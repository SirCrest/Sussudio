using System.Threading.Tasks;

static partial class Program
{
    private static async Task RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hevc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
            AssertEqual((uint)1920, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedWidth")), "DetectedWidth");
            AssertEqual((uint)1080, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedHeight")), "DetectedHeight");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: codec mismatch ──

    private static async Task RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_codec_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "codec-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: H264 codec match ──

    private static async Task RecordingVerifier_PassesVerification_ForH264Format()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_h264_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("h264", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
