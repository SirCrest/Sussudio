using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ParallelMjpegDecodePipeline_ComputeTimingMetrics_CalculatesCorrectly()
    {
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("ComputeTimingMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeTimingMetrics not found.");

        var samples = new double[] { 10.0, 10.0, 10.0, 10.0, 10.0 };
        var result = method.Invoke(null, new object[] { samples });

        var resultType = result!.GetType();
        var countField = resultType.GetField("Item1")!;
        var avgField = resultType.GetField("Item2")!;
        var p95Field = resultType.GetField("Item3")!;
        var maxField = resultType.GetField("Item4")!;

        AssertEqual(5, Convert.ToInt32(countField.GetValue(result)), "Sample count");
        var avg = Convert.ToDouble(avgField.GetValue(result));
        AssertEqual(true, Math.Abs(avg - 10.0) < 0.001, $"Average should be 10.0, got {avg}");
        var p95 = Convert.ToDouble(p95Field.GetValue(result));
        AssertEqual(true, Math.Abs(p95 - 10.0) < 0.001, $"P95 should be 10.0, got {p95}");
        var max = Convert.ToDouble(maxField.GetValue(result));
        AssertEqual(true, Math.Abs(max - 10.0) < 0.001, $"Max should be 10.0, got {max}");

        return Task.CompletedTask;
    }

    private static Task ParallelMjpegDecodePipeline_ComputeTimingMetrics_P95Calculation()
    {
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("ComputeTimingMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeTimingMetrics not found.");

        var samples = new double[20];
        for (var i = 0; i < 19; i++)
        {
            samples[i] = 5.0;
        }

        samples[19] = 50.0;

        var result = method.Invoke(null, new object[] { samples });
        var resultType = result!.GetType();

        var maxField = resultType.GetField("Item4")!;
        var max = Convert.ToDouble(maxField.GetValue(result));
        AssertEqual(true, max >= 50.0, $"Max should be >= 50.0, got {max}");

        var p95Field = resultType.GetField("Item3")!;
        var p95 = Convert.ToDouble(p95Field.GetValue(result));
        AssertEqual(true, p95 >= 5.0, $"P95 should be >= 5.0, got {p95}");

        return Task.CompletedTask;
    }

    private static Task ParallelMjpegDecodePipeline_GetElapsedMilliseconds_ComputesCorrectly()
    {
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("GetElapsedMilliseconds",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetElapsedMilliseconds not found.");

        long start = 0;
        long end = Stopwatch.Frequency;
        var result = (double)method.Invoke(null, new object[] { start, end })!;

        AssertEqual(true, Math.Abs(result - 1000.0) < 0.1,
            $"1 second of ticks should be ~1000ms, got {result:F3}");

        long halfEnd = Stopwatch.Frequency / 2;
        var halfResult = (double)method.Invoke(null, new object[] { start, halfEnd })!;
        AssertEqual(true, Math.Abs(halfResult - 500.0) < 0.1,
            $"Half second should be ~500ms, got {halfResult:F3}");

        return Task.CompletedTask;
    }

    private static Task ParallelMjpegDecodePipeline_GetRemainingTimeout_ReturnsCorrectTimeSpan()
    {
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var method = pipelineType.GetMethod("GetRemainingTimeout",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetRemainingTimeout not found.");

        long futureDeadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 2;
        var result = (TimeSpan)method.Invoke(null, new object[] { futureDeadline })!;
        AssertEqual(true, result.TotalMilliseconds > 1000,
            $"Remaining timeout for 2s future deadline should be >1000ms, got {result.TotalMilliseconds:F1}");

        long pastDeadline = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
        var pastResult = (TimeSpan)method.Invoke(null, new object[] { pastDeadline })!;
        AssertEqual(true, pastResult.TotalMilliseconds <= 0,
            $"Past deadline should return <=0ms, got {pastResult.TotalMilliseconds:F1}");

        return Task.CompletedTask;
    }

    private static Task ParallelMjpegDecodePipeline_PipelineTimingMetrics_HasExpectedProperties()
    {
        var metricsType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PipelineTimingMetrics");

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

    private static Task SoftwareMjpegDecoder_Properties_ExposeCorrectDimensions()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/SoftwareMjpegDecoder.cs")
            .Replace("\r\n", "\n");
        var decodeText = ReadRepoFile("Sussudio/Services/Gpu/SoftwareMjpegDecoder.Decode.cs")
            .Replace("\r\n", "\n");
        var decoderType = RequireType("Sussudio.Services.Gpu.SoftwareMjpegDecoder");

        AssertContains(rootText, "internal sealed unsafe partial class SoftwareMjpegDecoder : IDisposable");
        AssertContains(rootText, "public void Initialize(int width, int height)");
        AssertContains(rootText, "public void Dispose()");
        AssertContains(decodeText, "internal sealed unsafe partial class SoftwareMjpegDecoder");
        AssertContains(decodeText, "public bool DecodeToNv12(ReadOnlySpan<byte> jpegData, Span<byte> nv12Destination)");
        AssertContains(decodeText, "SW_MJPEG_DECODE_DIAG");
        AssertContains(decodeText, "Buffer.MemoryCopy(");
        AssertDoesNotContain(rootText, "public bool DecodeToNv12(");

        var widthProp = decoderType.GetProperty("Width", BindingFlags.Public | BindingFlags.Instance);
        var heightProp = decoderType.GetProperty("Height", BindingFlags.Public | BindingFlags.Instance);
        var nv12SizeProp = decoderType.GetProperty("Nv12Size", BindingFlags.Public | BindingFlags.Instance);

        AssertNotNull(widthProp, "SoftwareMjpegDecoder.Width");
        AssertNotNull(heightProp, "SoftwareMjpegDecoder.Height");
        AssertNotNull(nv12SizeProp, "SoftwareMjpegDecoder.Nv12Size");

        return Task.CompletedTask;
    }
}
