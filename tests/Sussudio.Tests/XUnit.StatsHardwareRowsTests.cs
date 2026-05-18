using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public class StatsHardwareRowsTests
{
    [Fact]
    public void HardwareRowsPresentation_FormatsDecodeAndGpuRows()
    {
        using var culture = CultureScope.Use("en-US");

        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildDecodeRows = builderType.GetMethod("BuildHardwareDecodeRows", ReflectionFlags.Static)
                              ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareDecodeRows not found.");
        var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", ReflectionFlags.Static)
                           ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        var decodeRows = StatsHardwareRowsToMap(buildDecodeRows.Invoke(null, new object?[]
        {
            CreateStatsHardwareMjpegMetrics()
        }) ?? throw new InvalidOperationException("BuildHardwareDecodeRows returned null."));

        Assert.Equal("4.00ms (250fps peak)", decodeRows["Throughput"]);
        Assert.Equal("8.00 / 12.25ms  avg/P95 (2T)", decodeRows["Decode"]);
        Assert.Equal("0.75 / 1.50ms  avg/P95", decodeRows["Reorder"]);
        Assert.Equal("9.25 / 15.50ms  avg/P95", decodeRows["Pipeline"]);
        Assert.Equal("1,234 emitted / 56 dropped", decodeRows["Frames"]);
        Assert.Equal("compressed=4 (5.0/10.0MB)  reorder=6  preview=3  skips=2", decodeRows["Buffers"]);
        Assert.Equal("4.50 / 7.75ms", decodeRows["Thread 0"]);
        Assert.Equal("5.25 / 8.50ms", decodeRows["Thread 1"]);

        var unavailableGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new object?[] { null })
                                 ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for unavailable snapshot."));
        Assert.Equal("NVML not available", unavailableGpuRows["Status"]);
        Assert.Null(buildGpuInput.Invoke(null, new object?[] { null }));

        var gpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
        {
            CreateStatsHardwareNvmlSnapshot()
        }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null."));

        Assert.Equal("RTX Test", gpuRows["GPU"]);
        Assert.Equal("41% (Mem: 52%)", gpuRows["Utilization"]);
        Assert.Equal("13%", gpuRows["NVDEC"]);
        Assert.Equal("17%", gpuRows["NVENC"]);
        Assert.Equal("1.5 MB/s", gpuRows["PCIe TX"]);
        Assert.Equal("2.0 MB/s", gpuRows["PCIe RX"]);
        Assert.Equal("3 / 12 MB", gpuRows["VRAM"]);
        Assert.Equal("66\u00B0C", gpuRows["Temperature"]);
        Assert.Equal("123.5W", gpuRows["Power"]);
        Assert.Equal("2500 MHz (Mem: 7000 MHz)", gpuRows["Clocks"]);

        var fallbackGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
        {
            CreateStatsHardwareNvmlSnapshotWithFallbacks()
        }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for fallback snapshot."));

        Assert.Equal("\u2014", fallbackGpuRows["GPU"]);
        Assert.Equal("0% (Mem: 0%)", fallbackGpuRows["Utilization"]);
        Assert.Equal("0.0 MB/s", fallbackGpuRows["PCIe TX"]);
        Assert.Equal("0 / 0 MB", fallbackGpuRows["VRAM"]);
    }

    [Fact]
    public void HardwareRowsInputProvider_PreservesSamplingPolicy()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", ReflectionFlags.Instance)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");
        var getGpuRowsInput = providerType.GetMethod("GetGpuRowsInput", ReflectionFlags.Instance)
                              ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetGpuRowsInput not found.");

        var nullMetricsProvider = CreateStatsHardwareRowsInputProvider(null, 3, null);
        Assert.Null(getDecodeRowsInput.Invoke(nullMetricsProvider, null));

        var zeroDecoderProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(decoderCount: 0),
            3,
            null);
        Assert.Null(getDecodeRowsInput.Invoke(zeroDecoderProvider, null));

        var validProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(),
            7,
            null);
        var decodeInput = getDecodeRowsInput.Invoke(validProvider, null)
                          ?? throw new InvalidOperationException("Provider returned null decode input for valid metrics.");
        Assert.Equal(7, Convert.ToInt32(GetPropertyValue(decodeInput, "PendingPreviewFrameCount"), CultureInfo.InvariantCulture));
        Assert.Null(getGpuRowsInput.Invoke(validProvider, null));
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateStatsHardwareMjpegMetrics()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildDecodeInput = inputBuilderType.GetMethod("BuildDecodeRowsInput", ReflectionFlags.Static)
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
        var property = contextType.GetProperty(propertyName, ReflectionFlags.Instance)
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
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
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
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
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
        var constructor = type.GetConstructors(ReflectionFlags.Instance)
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

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance);
        if (property?.CanWrite == true)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
                    ?? throw new InvalidOperationException($"Property or backing field '{propertyName}' was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;

        private CultureScope(CultureInfo culture)
        {
            _previousCulture = CultureInfo.CurrentCulture;
            _previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public static CultureScope Use(string cultureName)
            => new(CultureInfo.GetCultureInfo(cultureName));

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }
    }
}
