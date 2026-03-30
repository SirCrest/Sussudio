using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // ── D3D11PreviewRenderer: ComputeLetterboxRect ──

    private static Task D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly()
    {
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");
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

    // ── D3D11PreviewRenderer: CountLeadingBlackEdges / CountTrailingBlackEdges ──

    private static Task D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly()
    {
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");

        var leadingMethod = rendererType.GetMethod("CountLeadingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CountLeadingBlackEdges not found.");
        var trailingMethod = rendererType.GetMethod("CountTrailingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic)
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

    // ── D3D11PreviewRenderer: IsDeviceLostException ──

    private static Task D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly()
    {
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("IsDeviceLostException",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("IsDeviceLostException not found.");

        // Regular exception → false
        var regularEx = new InvalidOperationException("test");
        AssertEqual(false, (bool)method.Invoke(null, new object[] { regularEx })!, "Regular exception is not device lost");

        // COMException with DeviceRemoved HRESULT → true
        var deviceRemovedEx = new System.Runtime.InteropServices.COMException("Device removed", unchecked((int)0x887A0005));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceRemovedEx })!, "DeviceRemoved COMException is device lost");

        // COMException with DeviceReset HRESULT → true
        var deviceResetEx = new System.Runtime.InteropServices.COMException("Device reset", unchecked((int)0x887A0007));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceResetEx })!, "DeviceReset COMException is device lost");

        // COMException with other HRESULT → false
        var otherComEx = new System.Runtime.InteropServices.COMException("Other", unchecked((int)0x80004005));
        AssertEqual(false, (bool)method.Invoke(null, new object[] { otherComEx })!, "Other COMException is not device lost");

        return Task.CompletedTask;
    }

    // ── D3D11PreviewRenderer: PresentCadenceMetrics struct shape ──

    private static Task D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties()
    {
        var metricsType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer+PresentCadenceMetrics");

        var expectedProps = new[]
        {
            "SampleCount", "ObservedFps", "ExpectedIntervalMs", "AverageIntervalMs",
            "P95IntervalMs", "MaxIntervalMs", "JitterStdDevMs", "SlowFrameCount", "SlowFramePercent"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = metricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"PresentCadenceMetrics.{prop}");
        }

        return Task.CompletedTask;
    }

    // ── D3D11PreviewRenderer: InitPngCrc32Table ──

    private static Task D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries()
    {
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("InitPngCrc32Table",
            BindingFlags.Static | BindingFlags.NonPublic)
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
}
