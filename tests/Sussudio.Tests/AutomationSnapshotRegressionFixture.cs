using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Sussudio.Tests;

// Supplies captured domain inputs, without starting the hub or reading devices.
// Expected JSON was captured from the original builder before removing projections.
internal static class AutomationSnapshotRegressionFixture
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly DateTimeOffset FixtureTime = new(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);

    internal static JsonElement BuildResult(Assembly assembly, bool populated, object? healthOverride = null)
    {
        var hubType = assembly.GetType("Sussudio.Services.Automation.AutomationDiagnosticsHub", throwOnError: true)!;
        var method = hubType.GetMethod("BuildAutomationSnapshot", InstanceMembers)!;
        var hub = RuntimeHelpers.GetUninitializedObject(hubType);
        var parameters = method.GetParameters();
        if (parameters.Length != 18)
            throw new InvalidOperationException("Review the snapshot fixture after changing the captured input contract.");

        var sequence = 100;
        var inputs = parameters.ToDictionary(parameter => parameter.Name!, parameter =>
            populated
                ? Populate(parameter.ParameterType, parameter.Name!, ref sequence)
                : parameter.Name == "lastVerification" ? null : Empty(parameter.ParameterType),
            StringComparer.Ordinal);

        if (populated)
        {
            hubType.GetField("_verificationInProgress", InstanceMembers)!.SetValue(hub, 1);
            foreach (var (field, value) in new[]
            {
                ("_perfectionCaptureDropPercentThreshold", 0.125),
                ("_perfectionCaptureP95MultiplierThreshold", 1.875),
                ("_perfectionPreviewSlowPercentThreshold", 2.625),
                ("_perfectionVerificationDropPercentThreshold", 3.375)
            })
                hubType.GetField(field, InstanceMembers)!.SetValue(hub, value);

            var viewModel = inputs["viewModelSnapshot"]!;
            Set(viewModel, "SelectedFrameRate", 119.88);
            Set(viewModel, "SelectedFriendlyFrameRate", null);
            Set(viewModel, "SelectedExactFrameRate", null);
            Set(viewModel, "SourceFrameRateOrigin", "Unknown");
            Set(viewModel, "SourceWidth", null);
            Set(viewModel, "DetectedSourceFrameRate", null);
            Set(viewModel, "SourceTelemetryAvailability", "Unknown");
            Set(viewModel, "SourceTelemetryOriginDetail", " ");
            Set(viewModel, "SourceTelemetryAgeSeconds", 37);

            var runtime = inputs["captureRuntime"]!;
            Set(runtime, "NegotiatedWidth", null);
            Set(runtime, "NegotiatedFrameRate", null);
            Set(runtime, "NegotiatedFrameRateArg", null);
            Set(runtime, "MuxSucceeded", false);
            Set(runtime, "FlashbackExportVerificationFormat", null);
            Set(runtime, "FlashbackCodecDowngradeReason", string.Empty);
            Set(runtime, "RecordingIntegrityStatus", Enum.Parse(
                assembly.GetType("Sussudio.Models.RecordingIntegrityStatus", throwOnError: true)!, "Complete"));
            Set(runtime, "RecordingIntegrityAudioStatus", Enum.Parse(
                assembly.GetType("Sussudio.Models.RecordingIntegrityAudioStatus", throwOnError: true)!, "Clean"));

            var health = inputs["health"]!;
            Set(health, "FlashbackPlaybackState", Enum.Parse(assembly.GetType("Sussudio.Models.FlashbackPlaybackState", throwOnError: true)!, "Paused"));
            Set(health, "FlashbackExportVerificationFormat", "health-export-verification");
            Set(health, "FlashbackCodecDowngradeReason", "health-codec-downgrade");
            Set(health, "AudioDropsQueueSaturated", 11L);
            Set(health, "AudioDropsBacklogEviction", 17L);
            Set(health, "AudioChunksDropped", 19L);
            Set(health, "VideoFramesConverted", 25011L);
            Set(health, "VideoFramesEnqueued", 17007L);
            Set(health, "LastExportSuccess", false);

            var preview = inputs["previewRuntime"]!;
            Set(preview, "EstimatedPipelineLatencyMs", 12.75);
            Set(preview, "D3DFrameStatsMissedRefreshCount", 59L);
            Set(preview, "D3DFrameStatsFailureCount", 61L);
            inputs["recentD3DMissedRefreshes"] = 41L;
            inputs["recentD3DStatsFailures"] = 43L;
        }

        if (healthOverride != null)
            inputs["health"] = healthOverride;

        var snapshot = method.Invoke(hub, parameters.Select(parameter => inputs[parameter.Name!]).ToArray())!;
        return JsonSerializer.SerializeToElement(snapshot, snapshot.GetType());
    }

    private static object? Populate(Type type, string path, ref int sequence)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        var ordinal = ++sequence;
        if (type == typeof(string)) return path;
        if (type == typeof(bool)) return ordinal % 2 == 0;
        if (type == typeof(DateTimeOffset)) return FixtureTime.AddMilliseconds(ordinal);
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(ordinal % values.Length);
        }
        if (type == typeof(double)) return ordinal + 0.25;
        if (type == typeof(float)) return ordinal + 0.5f;
        if (type.IsPrimitive || type == typeof(decimal))
            return Convert.ChangeType(ordinal, type, CultureInfo.InvariantCulture);

        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)))
        {
            var elementType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
            var array = Array.CreateInstance(elementType, 2);
            for (var i = 0; i < array.Length; i++)
                array.SetValue(Populate(elementType, $"{path}[{i}]", ref sequence), i);
            return array;
        }

        if (type.FullName == "Sussudio.Models.RecordingStats")
            return Activator.CreateInstance(type, 11001L, 22003L, false, false, FixtureTime, 31009L)!;

        var instance = Empty(type)!;
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();
        foreach (var property in properties)
        {
            if (property.SetMethod == null)
                throw new InvalidOperationException($"Fixture input {path}.{property.Name} requires an explicit value factory.");
            property.SetValue(instance, Populate(property.PropertyType, path + "." + property.Name, ref sequence));
        }
        return instance;
    }

    private static object? Empty(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : RuntimeHelpers.GetUninitializedObject(type);

    private static void Set(object target, string propertyName, object? value)
        => target.GetType().GetProperty(propertyName)!.SetValue(target, value);
}
