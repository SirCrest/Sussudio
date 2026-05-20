using System.Threading.Tasks;

static partial class Program
{
    // ── Integration test: HDR validation passes with correct metadata ──

    internal static async Task RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // Use hdrOutputActive=true (not requestedHdrEnabled) to trigger HDR validation
            // without the ProbeHdrSideDataAsync JSON path (avoids System.Text.Json version mismatch)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=p010le\n" +
                    "color_primaries=bt2020\n" +
                    "color_transfer=smpte2084\n" +
                    "color_space=bt2020nc\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("p010le", GetStringProperty(result, "DetectedPixelFormat"), "DetectedPixelFormat");
            AssertEqual(true, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(true, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Verified", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(true, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");
            AssertEqual("ColorimetryOnly", GetStringProperty(hdrParity, "VerificationLevel"), "HdrParity.VerificationLevel");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: HDR colorimetry mismatch ──

    internal static async Task RecordingVerifier_DetectsHdrColorimetryMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_bad_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // SDR colorimetry on an HDR-active recording (use hdrOutputActive, not requestedHdrEnabled
            // to avoid ProbeHdrSideDataAsync JSON path)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n" +
                    "color_primaries=bt709\n" +
                    "color_transfer=bt709\n" +
                    "color_space=bt709\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            // Should have multiple HDR-related mismatches
            var mismatches = GetPropertyValue(result, "Mismatches") as System.Collections.IEnumerable;
            var mismatchList = new List<string>();
            foreach (var m in mismatches!) mismatchList.Add(m?.ToString() ?? "");
            var hasPixfmtMismatch = mismatchList.Any(m => m.Contains("pixfmt-not-10bit"));
            var hasColorimetryMismatch = mismatchList.Any(m => m.Contains("colorimetry-mismatch"));
            AssertEqual(true, hasPixfmtMismatch, "Has pixfmt-not-10bit mismatch");
            AssertEqual(true, hasColorimetryMismatch, "Has colorimetry-mismatch");

            AssertEqual(false, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(false, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Mismatch", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(false, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");

            var taxonomy = GetPropertyValue(hdrParity, "MismatchTaxonomy") as System.Collections.IEnumerable;
            var taxonomyEntries = new List<object>();
            foreach (var entry in taxonomy!) taxonomyEntries.Add(entry!);
            var hasHdrError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "HDR" &&
                GetStringProperty(entry, "Code") == "pixfmt-not-10bit" &&
                GetStringProperty(entry, "Severity") == "Error");
            var hasColorimetryError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "Colorimetry" &&
                GetStringProperty(entry, "Code") == "colorimetry-mismatch" &&
                GetStringProperty(entry, "Severity") == "Error");
            AssertEqual(true, hasHdrError, "HDR mismatch taxonomy is Error severity");
            AssertEqual(true, hasColorimetryError, "Colorimetry mismatch taxonomy is Error severity");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
