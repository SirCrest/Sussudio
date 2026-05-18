using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("ComputeLetterboxRect",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeLetterboxRect not found.");

        // 16:9 source into 16:9 dest → no letterbox, fills completely
        var result1 = method.Invoke(null, new object[] { 1920, 1080, 1920, 1080 })!;
        var resultType = result1.GetType();
        var left1 = (int)resultType.GetField("Left")!.GetValue(result1)!;
        var top1 = (int)resultType.GetField("Top")!.GetValue(result1)!;
        var right1 = (int)resultType.GetField("Right")!.GetValue(result1)!;
        var bottom1 = (int)resultType.GetField("Bottom")!.GetValue(result1)!;
        AssertEqual(0, left1, "Same aspect: left=0");
        AssertEqual(0, top1, "Same aspect: top=0");
        AssertEqual(1920, right1, "Same aspect: right=1920");
        AssertEqual(1080, bottom1, "Same aspect: bottom=1080");

        // 16:9 source into 4:3 dest → letterboxed (bars top/bottom)
        var result2 = method.Invoke(null, new object[] { 1920, 1080, 1024, 768 })!;
        var top2 = (int)resultType.GetField("Top")!.GetValue(result2)!;
        var bottom2 = (int)resultType.GetField("Bottom")!.GetValue(result2)!;
        var left2 = (int)resultType.GetField("Left")!.GetValue(result2)!;
        // Wider source → letterbox (top > 0, centered)
        AssertEqual(true, top2 > 0, "16:9 into 4:3 should letterbox (top > 0)");
        AssertEqual(0, left2, "16:9 into 4:3 should not pillarbox");

        // 4:3 source into 16:9 dest → pillarboxed (bars left/right)
        var result3 = method.Invoke(null, new object[] { 1024, 768, 1920, 1080 })!;
        var left3 = (int)resultType.GetField("Left")!.GetValue(result3)!;
        var top3 = (int)resultType.GetField("Top")!.GetValue(result3)!;
        AssertEqual(true, left3 > 0, "4:3 into 16:9 should pillarbox (left > 0)");
        AssertEqual(0, top3, "4:3 into 16:9 should not letterbox");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly()
    {
        // Extracted to PreviewScreenshotCapture; reflect on the new type.
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");

        var leadingMethod = captureType.GetMethod("CountLeadingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CountLeadingBlackEdges not found.");
        var trailingMethod = captureType.GetMethod("CountTrailingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CountTrailingBlackEdges not found.");

        // [true, true, false, true, false] → leading = 2, trailing = 0
        var values1 = new[] { true, true, false, true, false };
        AssertEqual(2, (int)leadingMethod.Invoke(null, new object[] { values1 })!, "Leading: 2 black edges");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { values1 })!, "Trailing: 0 black edges");

        // [false, false, true, true, true] → leading = 0, trailing = 3
        var values2 = new[] { false, false, true, true, true };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { values2 })!, "Leading: 0");
        AssertEqual(3, (int)trailingMethod.Invoke(null, new object[] { values2 })!, "Trailing: 3");

        // All true → leading = 5, trailing = 5
        var allTrue = new[] { true, true, true, true, true };
        AssertEqual(5, (int)leadingMethod.Invoke(null, new object[] { allTrue })!, "All true leading");
        AssertEqual(5, (int)trailingMethod.Invoke(null, new object[] { allTrue })!, "All true trailing");

        // All false → leading = 0, trailing = 0
        var allFalse = new[] { false, false, false };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { allFalse })!, "All false leading");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { allFalse })!, "All false trailing");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries()
    {
        // PNG chunk/CRC ownership lives in the preview PNG encoder.
        var encoderType = RequireType("Sussudio.Services.Preview.PreviewPng16Encoder");
        var method = encoderType.GetMethod("InitPngCrc32Table",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("InitPngCrc32Table not found.");

        var table = (uint[])method.Invoke(null, null)!;
        AssertEqual(256, table.Length, "CRC32 table has 256 entries");

        // Entry 0 should be 0 (no bits set → no XOR)
        AssertEqual(0u, table[0], "CRC32 table[0] = 0");

        // All entries should be unique (well-formed CRC table)
        var unique = new HashSet<uint>(table);
        AssertEqual(256, unique.Count, "All 256 entries are unique");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_PreviewPngCapture_Writes16BitRgbPng()
    {
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");
        var method = captureType.GetMethod(
            "CaptureFrameBufferTo16BitPng",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CaptureFrameBufferTo16BitPng not found.");

        var outputRoot = Path.Combine(Path.GetTempPath(), "sussudio-preview-png-test-" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputRoot, "preview", "frame.png");
        try
        {
            var format = ParseEnum("Vortice.DXGI.Format", "B8G8R8A8_UNorm");
            var result = method.Invoke(
                null,
                new object[]
                {
                    new byte[] { 0x30, 0x20, 0x10, 0xFF },
                    4,
                    1,
                    1,
                    outputPath,
                    "UnitTest",
                    format
                })
                ?? throw new InvalidOperationException("CaptureFrameBufferTo16BitPng returned null.");

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "PNG capture succeeded");
            AssertEqual(1, GetIntProperty(result, "CapturedWidth"), "PNG captured width");
            AssertEqual(1, GetIntProperty(result, "CapturedHeight"), "PNG captured height");
            AssertEqual(outputPath, GetStringProperty(result, "FilePath"), "PNG output path");

            var bytes = File.ReadAllBytes(outputPath);
            AssertEqual(137, (int)bytes[0], "PNG signature byte 0");
            AssertEqual(80, (int)bytes[1], "PNG signature byte 1");
            AssertEqual(78, (int)bytes[2], "PNG signature byte 2");
            AssertEqual(71, (int)bytes[3], "PNG signature byte 3");
            AssertEqual((byte)'I', bytes[12], "PNG IHDR I");
            AssertEqual((byte)'H', bytes[13], "PNG IHDR H");
            AssertEqual((byte)'D', bytes[14], "PNG IHDR D");
            AssertEqual((byte)'R', bytes[15], "PNG IHDR R");
            AssertEqual(1, (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19], "PNG IHDR width");
            AssertEqual(1, (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23], "PNG IHDR height");
            AssertEqual(16, (int)bytes[24], "PNG bit depth");
            AssertEqual(2, (int)bytes[25], "PNG color type");
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }

        return Task.CompletedTask;
    }
}
