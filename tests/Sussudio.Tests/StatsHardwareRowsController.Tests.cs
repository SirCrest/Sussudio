using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

static partial class Program
{
    private static Task StatsHardwareRowsBuilder_FormatsDecodeAndGpuRows()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
            var buildDecodeRows = builderType.GetMethod("BuildHardwareDecodeRows", BindingFlags.Static | BindingFlags.Public)
                                  ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareDecodeRows not found.");
            var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", BindingFlags.Static | BindingFlags.Public)
                               ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");
            var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
            var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", BindingFlags.Static | BindingFlags.Public)
                                ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

            var decodeRows = StatsHardwareRowsToMap(buildDecodeRows.Invoke(null, new object?[]
            {
                CreateStatsHardwareMjpegMetrics()
            }) ?? throw new InvalidOperationException("BuildHardwareDecodeRows returned null."));

            AssertEqual("4.00ms (250fps peak)", decodeRows["Throughput"], "decode throughput row");
            AssertEqual("8.00 / 12.25ms  avg/P95 (2T)", decodeRows["Decode"], "decode timing row");
            AssertEqual("0.75 / 1.50ms  avg/P95", decodeRows["Reorder"], "reorder timing row");
            AssertEqual("9.25 / 15.50ms  avg/P95", decodeRows["Pipeline"], "pipeline timing row");
            AssertEqual("1,234 emitted / 56 dropped", decodeRows["Frames"], "frame counters row");
            AssertEqual("compressed=4 (5.0/10.0MB)  reorder=6  preview=3  skips=2", decodeRows["Buffers"], "buffer row");
            AssertEqual("4.50 / 7.75ms", decodeRows["Thread 0"], "first decode worker row");
            AssertEqual("5.25 / 8.50ms", decodeRows["Thread 1"], "second decode worker row");

            var unavailableGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new object?[] { null })
                                     ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for unavailable snapshot."));
            AssertEqual("NVML not available", unavailableGpuRows["Status"], "unavailable GPU status row");
            AssertEqual(null, buildGpuInput.Invoke(null, new object?[] { null }), "null NVML snapshot projects to null input");

            var gpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
            {
                CreateStatsHardwareNvmlSnapshot()
            }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null."));

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
            }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for fallback snapshot."));

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

    private static Task StatsHardwareRowsInputProvider_PreservesSamplingPolicy()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", BindingFlags.Instance | BindingFlags.Public)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");
        var getGpuRowsInput = providerType.GetMethod("GetGpuRowsInput", BindingFlags.Instance | BindingFlags.Public)
                              ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetGpuRowsInput not found.");

        var nullMetricsProvider = CreateStatsHardwareRowsInputProvider(null, 3, null);
        AssertEqual<object?>(
            null,
            getDecodeRowsInput.Invoke(nullMetricsProvider, null),
            "null MJPEG metrics return null decode input");

        var zeroDecoderProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(decoderCount: 0),
            3,
            null);
        AssertEqual<object?>(
            null,
            getDecodeRowsInput.Invoke(zeroDecoderProvider, null),
            "zero decoder metrics return null decode input");

        var validProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(),
            7,
            null);
        var decodeInput = getDecodeRowsInput.Invoke(validProvider, null)
                          ?? throw new InvalidOperationException("Provider returned null decode input for valid metrics.");
        AssertEqual(
            7,
            Convert.ToInt32(GetPropertyValue(decodeInput, "PendingPreviewFrameCount")),
            "pending preview frame count");
        AssertEqual<object?>(
            null,
            getGpuRowsInput.Invoke(validProvider, null),
            "null NVML snapshot returns null GPU input");

        return Task.CompletedTask;
    }

    private static object CreateStatsHardwareMjpegMetrics()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildDecodeInput = inputBuilderType.GetMethod("BuildDecodeRowsInput", BindingFlags.Static | BindingFlags.Public)
                               ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildDecodeRowsInput not found.");

        return buildDecodeInput.Invoke(null, new object?[] { CreateStatsHardwarePipelineTimingMetrics(), (int?)3 })
               ?? throw new InvalidOperationException("BuildDecodeRowsInput returned null.");
    }

    private static object CreateStatsHardwarePipelineTimingMetrics(int decoderCount = 2)
    {
        var metricsType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        var perDecoder = Array.CreateInstance(perDecoderType, decoderCount);
        if (decoderCount > 0)
        {
            perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 0, 5, 4.5d, 7.75d, 9.5d), 0);
        }

        if (decoderCount > 1)
        {
            perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 1, 4, 5.25d, 8.5d, 10.25d), 1);
        }

        return InvokeStatsHardwareConstructor(
            metricsType,
            decoderCount,
            9,
            8.0d,
            12.25d,
            14.0d,
            7,
            0.75d,
            1.5d,
            2.25d,
            11,
            9.25d,
            15.5d,
            20.0d,
            1500L,
            1234L,
            56L,
            1240L,
            1234L,
            1L,
            2L,
            3L,
            4L,
            5L,
            6L,
            4,
            5L * 1024L * 1024L,
            10L * 1024L * 1024L,
            2L,
            6,
            perDecoder);
    }

    private static object CreateStatsHardwareRowsInputProvider(
        object? mjpegMetrics,
        int? pendingPreviewFrameCount,
        object? nvmlSnapshot)
    {
        var contextType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProviderContext");
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var context = Activator.CreateInstance(contextType)
                      ?? throw new InvalidOperationException("Failed to create StatsHardwareRowsInputProviderContext.");

        SetPropertyOrBackingField(
            context,
            "GetMjpegPipelineTimingDetails",
            CreateStatsHardwareProviderCallback(contextType, "GetMjpegPipelineTimingDetails", () => mjpegMetrics));
        SetPropertyOrBackingField(
            context,
            "GetPendingPreviewFrameCount",
            CreateStatsHardwareProviderCallback(contextType, "GetPendingPreviewFrameCount", () => pendingPreviewFrameCount));
        SetPropertyOrBackingField(
            context,
            "GetNvmlSnapshot",
            CreateStatsHardwareProviderCallback(contextType, "GetNvmlSnapshot", () => nvmlSnapshot));

        return Activator.CreateInstance(providerType, context)
               ?? throw new InvalidOperationException("Failed to create StatsHardwareRowsInputProvider.");
    }

    private static Delegate CreateStatsHardwareProviderCallback(
        Type contextType,
        string propertyName,
        Func<object?> callback)
    {
        var property = contextType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException($"Missing provider context property '{propertyName}'.");
        var invoke = typeof(Func<object?>).GetMethod(nameof(Func<object?>.Invoke))
                     ?? throw new InvalidOperationException("Missing Func<object?>.Invoke.");
        var returnType = property.PropertyType.GetMethod(nameof(Func<object?>.Invoke))?.ReturnType
                         ?? throw new InvalidOperationException($"Provider context property '{propertyName}' was not a delegate.");
        var callbackValue = Expression.Call(Expression.Constant(callback), invoke);
        var convertedValue = Expression.Convert(callbackValue, returnType);
        return Expression.Lambda(property.PropertyType, convertedValue).Compile();
    }

    private static object CreateStatsHardwareNvmlSnapshot()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        return buildGpuInput.Invoke(null, new[] { CreateStatsHardwareNvmlTelemetrySnapshot() })
               ?? throw new InvalidOperationException("BuildGpuRowsInput returned null.");
    }

    private static object CreateStatsHardwareNvmlTelemetrySnapshot()
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
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        return buildGpuInput.Invoke(null, new[] { CreateStatsHardwareNvmlTelemetrySnapshotWithFallbacks() })
               ?? throw new InvalidOperationException("BuildGpuRowsInput returned null for fallback snapshot.");
    }

    private static object CreateStatsHardwareNvmlTelemetrySnapshotWithFallbacks()
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
