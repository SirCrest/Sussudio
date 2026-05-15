using System.Collections;
using System.Globalization;
using System.Reflection;

static partial class Program
{
    private static Task StatsHardwareRowsController_FormatsDecodeAndGpuRows()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var controllerType = RequireType("Sussudio.Controllers.StatsHardwareRowsController");
            var buildDecodeRows = controllerType.GetMethod("BuildDecodeRows", BindingFlags.Static | BindingFlags.NonPublic)
                                  ?? throw new InvalidOperationException("StatsHardwareRowsController.BuildDecodeRows not found.");
            var buildGpuRows = controllerType.GetMethod("BuildGpuRows", BindingFlags.Static | BindingFlags.NonPublic)
                               ?? throw new InvalidOperationException("StatsHardwareRowsController.BuildGpuRows not found.");

            var decodeRows = StatsHardwareRowsToMap(buildDecodeRows.Invoke(null, new object?[]
            {
                CreateStatsHardwareMjpegMetrics(),
                (int?)3
            }) ?? throw new InvalidOperationException("BuildDecodeRows returned null."));

            AssertEqual("4.00ms (250fps peak)", decodeRows["Throughput"], "decode throughput row");
            AssertEqual("8.00 / 12.25ms  avg/P95 (2T)", decodeRows["Decode"], "decode timing row");
            AssertEqual("0.75 / 1.50ms  avg/P95", decodeRows["Reorder"], "reorder timing row");
            AssertEqual("9.25 / 15.50ms  avg/P95", decodeRows["Pipeline"], "pipeline timing row");
            AssertEqual("1,234 emitted / 56 dropped", decodeRows["Frames"], "frame counters row");
            AssertEqual("compressed=4 (5.0/10.0MB)  reorder=6  preview=3  skips=2", decodeRows["Buffers"], "buffer row");
            AssertEqual("4.50 / 7.75ms", decodeRows["Thread 0"], "first decode worker row");
            AssertEqual("5.25 / 8.50ms", decodeRows["Thread 1"], "second decode worker row");

            var unavailableGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new object?[] { null })
                                     ?? throw new InvalidOperationException("BuildGpuRows returned null for unavailable snapshot."));
            AssertEqual("NVML not available", unavailableGpuRows["Status"], "unavailable GPU status row");

            var gpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
            {
                CreateStatsHardwareNvmlSnapshot()
            }) ?? throw new InvalidOperationException("BuildGpuRows returned null."));

            AssertEqual("RTX Test", gpuRows["GPU"], "GPU name row");
            AssertEqual("41% (Mem: 52%)", gpuRows["Utilization"], "GPU utilization row");
            AssertEqual("13%", gpuRows["NVDEC"], "NVDEC row");
            AssertEqual("17%", gpuRows["NVENC"], "NVENC row");
            AssertEqual("1.5 MB/s", gpuRows["PCIe TX"], "PCIe TX row");
            AssertEqual("2.0 MB/s", gpuRows["PCIe RX"], "PCIe RX row");
            AssertEqual("3 / 12 MB", gpuRows["VRAM"], "VRAM row");
            AssertEqual("66\u00B0C", gpuRows["Temperature"], "temperature row");
            AssertEqual("123.5W", gpuRows["Power"], "power row");
            AssertEqual("2500 MHz (Mem: 7000 MHz)", gpuRows["Clocks"], "clock row");

            var fallbackGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
            {
                CreateStatsHardwareNvmlSnapshotWithFallbacks()
            }) ?? throw new InvalidOperationException("BuildGpuRows returned null for fallback snapshot."));

            AssertEqual("\u2014", fallbackGpuRows["GPU"], "blank GPU name fallback");
            AssertEqual("0% (Mem: 0%)", fallbackGpuRows["Utilization"], "nullable GPU utilization fallback");
            AssertEqual("0.0 MB/s", fallbackGpuRows["PCIe TX"], "nullable PCIe TX fallback");
            AssertEqual("0 / 0 MB", fallbackGpuRows["VRAM"], "nullable VRAM fallback");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        return Task.CompletedTask;
    }

    private static object CreateStatsHardwareMjpegMetrics()
    {
        var metricsType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        var perDecoder = Array.CreateInstance(perDecoderType, 2);
        perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 0, 11, 4.5d, 7.75d, 9.0d), 0);
        perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 1, 12, 5.25d, 8.5d, 10.0d), 1);

        return InvokeStatsHardwareConstructor(
            metricsType,
            2,
            23,
            8.0d,
            12.25d,
            18.0d,
            9,
            0.75d,
            1.5d,
            2.0d,
            14,
            9.25d,
            15.5d,
            22.0d,
            1300L,
            1234L,
            56L,
            1400L,
            1390L,
            4L,
            5L,
            6L,
            0L,
            1L,
            2L,
            4,
            5L * 1024L * 1024L,
            10L * 1024L * 1024L,
            2L,
            6,
            perDecoder);
    }

    private static object CreateStatsHardwareNvmlSnapshot()
        => InvokeStatsHardwareConstructor(
            RequireType("Sussudio.Services.Gpu.NvmlSnapshot"),
            "RTX Test",
            (uint?)41,
            (uint?)52,
            (uint?)13,
            (uint?)17,
            (uint?)1536,
            (uint?)2048,
            (ulong?)(3UL * 1024UL * 1024UL),
            (ulong?)(12UL * 1024UL * 1024UL),
            (uint?)66,
            (uint?)123456,
            (uint?)2500,
            (uint?)7000);

    private static object CreateStatsHardwareNvmlSnapshotWithFallbacks()
        => InvokeStatsHardwareConstructor(
            RequireType("Sussudio.Services.Gpu.NvmlSnapshot"),
            " ",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    private static object InvokeStatsHardwareConstructor(Type type, params object?[] arguments)
    {
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters().Length == arguments.Length);
        return constructor.Invoke(arguments);
    }

    private static Dictionary<string, string> StatsHardwareRowsToMap(object rows)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in (IEnumerable)rows)
        {
            if (row == null)
            {
                continue;
            }

            map[GetStringProperty(row, "Label")] = GetStringProperty(row, "Value");
        }

        return map;
    }
}
