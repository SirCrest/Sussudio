using System.Threading.Tasks;

static partial class Program
{
    // ── Integration test: codec match (HEVC) ──

    internal static async Task RecordingVerifier_RunsFfprobeBelowNormalPriority()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_priority_{Guid.NewGuid():N}.mp4");
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
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            _ = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, fake.Calls.Count >= 2, "ffprobe calls recorded");
            foreach (var call in fake.Calls)
            {
                AssertEqual("BelowNormal", call.PriorityClass, $"ffprobe priority for {call.Arguments}");
            }
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
