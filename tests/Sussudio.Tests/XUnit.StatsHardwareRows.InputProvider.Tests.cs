using System.Globalization;
using System.Linq.Expressions;
using Xunit;

namespace Sussudio.Tests;

public partial class StatsHardwareRowsTests
{
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
}
