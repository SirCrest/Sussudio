using System.Threading.Tasks;

static partial class Program
{
    // ── Integration test: resolution mismatch ──

    private static async Task RecordingVerifier_UsesFlashbackExportVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_{Guid.NewGuid():N}.mp4");
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
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportOutputPath: tempFile,
                flashbackExportVerificationFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    private static async Task RecordingVerifier_UsesFlashbackRecordingVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_recording_{Guid.NewGuid():N}.mp4");
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
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportVerificationFormat: "HevcMp4",
                lastOutputPath: tempFile,
                recordingIntegrityBackend: "Flashback");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
