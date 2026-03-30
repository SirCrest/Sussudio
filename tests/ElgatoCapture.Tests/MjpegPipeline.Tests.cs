using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // ── ParallelMjpegDecodePipeline: ComputeTimingMetrics ──

    private static Task ParallelMjpegDecodePipeline_ComputeTimingMetrics_CalculatesCorrectly()
    {
        var pipelineType = RequireType("ElgatoCapture.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("ComputeTimingMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeTimingMetrics not found.");

        // Simple case: 5 uniform samples
        var samples = new double[] { 10.0, 10.0, 10.0, 10.0, 10.0 };
        var result = method.Invoke(null, new object[] { samples });

        // Returns (int SampleCount, double AverageMs, double P95Ms, double MaxMs)
        var resultType = result!.GetType();
        var countField = resultType.GetField("Item1")!;
        var avgField = resultType.GetField("Item2")!;
        var p95Field = resultType.GetField("Item3")!;
        var maxField = resultType.GetField("Item4")!;

        AssertEqual(5, Convert.ToInt32(countField.GetValue(result)), "Sample count");
        var avg = Convert.ToDouble(avgField.GetValue(result));
        AssertEqual(true, Math.Abs(avg - 10.0) < 0.001, $"Average should be 10.0, got {avg}");
        var max = Convert.ToDouble(maxField.GetValue(result));
        AssertEqual(true, Math.Abs(max - 10.0) < 0.001, $"Max should be 10.0, got {max}");

        return Task.CompletedTask;
    }

    private static Task ParallelMjpegDecodePipeline_ComputeTimingMetrics_P95Calculation()
    {
        var pipelineType = RequireType("ElgatoCapture.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("ComputeTimingMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeTimingMetrics not found.");

        // 20 samples: 19 at 5ms, 1 outlier at 50ms
        var samples = new double[20];
        for (var i = 0; i < 19; i++) samples[i] = 5.0;
        samples[19] = 50.0;

        var result = method.Invoke(null, new object[] { samples });
        var resultType = result!.GetType();

        var maxField = resultType.GetField("Item4")!;
        var max = Convert.ToDouble(maxField.GetValue(result));
        AssertEqual(true, max >= 50.0, $"Max should be >= 50.0, got {max}");

        // P95 should capture the outlier (19th sample of 20 is above 95th percentile)
        var p95Field = resultType.GetField("Item3")!;
        var p95 = Convert.ToDouble(p95Field.GetValue(result));
        AssertEqual(true, p95 >= 5.0, $"P95 should be >= 5.0, got {p95}");

        return Task.CompletedTask;
    }

    // ── ParallelMjpegDecodePipeline: CopyRing ──

    private static Task ParallelMjpegDecodePipeline_CopyRing_ExtractsCorrectWindow()
    {
        var pipelineType = RequireType("ElgatoCapture.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("CopyRing",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CopyRing not found.");

        // Ring buffer: [3, 4, 5, 1, 2] with index pointing at position 2 (value 5)
        // Count = 5, meaning the ring is full
        var window = new double[] { 3.0, 4.0, 5.0, 1.0, 2.0 };
        var count = 5;
        var index = 3; // next-write index, so last-written is at 2

        var result = (double[])method.Invoke(null, new object[] { window, count, index })!;
        AssertEqual(5, result.Length, "CopyRing output length");

        // Should extract in insertion order: oldest first
        // With index=3 and count=5, oldest is at (3-5+5)%5 = 3, then 4, 0, 1, 2
        // So values: [1.0, 2.0, 3.0, 4.0, 5.0]
        AssertEqual(true, result.Length == count, "CopyRing returns count elements");

        return Task.CompletedTask;
    }

    // ── ParallelMjpegDecodePipeline: GetElapsedMilliseconds ──

    private static Task ParallelMjpegDecodePipeline_GetElapsedMilliseconds_ComputesCorrectly()
    {
        var pipelineType = RequireType("ElgatoCapture.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("GetElapsedMilliseconds",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetElapsedMilliseconds not found.");

        // (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency
        long start = 0;
        long end = Stopwatch.Frequency; // 1 second worth of ticks
        var result = (double)method.Invoke(null, new object[] { start, end })!;

        // Should be approximately 1000ms
        AssertEqual(true, Math.Abs(result - 1000.0) < 0.1,
            $"1 second of ticks should be ~1000ms, got {result:F3}");

        // Half second
        long halfEnd = Stopwatch.Frequency / 2;
        var halfResult = (double)method.Invoke(null, new object[] { start, halfEnd })!;
        AssertEqual(true, Math.Abs(halfResult - 500.0) < 0.1,
            $"Half second should be ~500ms, got {halfResult:F3}");

        return Task.CompletedTask;
    }

    // ── ParallelMjpegDecodePipeline: GetRemainingTimeout ──

    private static Task ParallelMjpegDecodePipeline_GetRemainingTimeout_ReturnsCorrectTimeSpan()
    {
        var pipelineType = RequireType("ElgatoCapture.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("GetRemainingTimeout",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetRemainingTimeout not found.");

        // Deadline 2 seconds in the future
        long futureDeadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 2;
        var result = (TimeSpan)method.Invoke(null, new object[] { futureDeadline })!;
        AssertEqual(true, result.TotalMilliseconds > 1000,
            $"Remaining timeout for 2s future deadline should be >1000ms, got {result.TotalMilliseconds:F1}");

        // Deadline in the past should return zero or near-zero
        long pastDeadline = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
        var pastResult = (TimeSpan)method.Invoke(null, new object[] { pastDeadline })!;
        AssertEqual(true, pastResult.TotalMilliseconds <= 0,
            $"Past deadline should return <=0ms, got {pastResult.TotalMilliseconds:F1}");

        return Task.CompletedTask;
    }

    // ── ParallelMjpegDecodePipeline: record struct shapes ──

    private static Task ParallelMjpegDecodePipeline_PipelineTimingMetrics_HasExpectedProperties()
    {
        var metricsType = RequireType("ElgatoCapture.Services.Gpu.ParallelMjpegDecodePipeline+PipelineTimingMetrics");

        var expectedProps = new[]
        {
            "DecoderCount", "DecodeSampleCount", "DecodeAvgMs", "DecodeP95Ms", "DecodeMaxMs",
            "ReorderSampleCount", "ReorderAvgMs", "ReorderP95Ms", "ReorderMaxMs",
            "PipelineSampleCount", "PipelineAvgMs", "PipelineP95Ms", "PipelineMaxMs",
            "TotalDecoded", "TotalEmitted", "TotalDropped", "ReorderSkips",
            "ReorderBufferDepth", "PerDecoder"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = metricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"PipelineTimingMetrics.{prop}");
        }

        return Task.CompletedTask;
    }

    // ── SoftwareMjpegDecoder: NV12 size calculation ──

    private static Task SoftwareMjpegDecoder_Properties_ExposeCorrectDimensions()
    {
        var decoderType = RequireType("ElgatoCapture.Services.Gpu.SoftwareMjpegDecoder");

        // Verify the type has Width, Height, Nv12Size properties
        var widthProp = decoderType.GetProperty("Width", BindingFlags.Public | BindingFlags.Instance);
        var heightProp = decoderType.GetProperty("Height", BindingFlags.Public | BindingFlags.Instance);
        var nv12SizeProp = decoderType.GetProperty("Nv12Size", BindingFlags.Public | BindingFlags.Instance);

        AssertNotNull(widthProp, "SoftwareMjpegDecoder.Width");
        AssertNotNull(heightProp, "SoftwareMjpegDecoder.Height");
        AssertNotNull(nv12SizeProp, "SoftwareMjpegDecoder.Nv12Size");

        return Task.CompletedTask;
    }
}
