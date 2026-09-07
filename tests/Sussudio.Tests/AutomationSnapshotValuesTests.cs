using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationSnapshotValuesTests
{
    private static readonly Type HubType = SussudioAssembly.Load().GetType(
        "Sussudio.Services.Automation.AutomationDiagnosticsHub", throwOnError: true)!;
    private static readonly MethodInfo BuildSnapshot = HubType.GetMethod(
        "BuildAutomationSnapshotFromProjections", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly Type ProjectionSetType = BuildSnapshot.GetParameters()[0].ParameterType;

    [Fact]
    public void Snapshot_ForwardsEveryProjectedValueAndKeepsWireFieldsDistinct()
    {
        var expected = new Dictionary<string, (Type Type, object? Value)>();
        var sequence = 100;
        var projections = PopulateProjections(ProjectionSetType, "", expected, ref sequence);
        var snapshot = BuildSnapshot.Invoke(null, new[] { projections })!;
        var fields = snapshot.GetType().GetProperties()
            .OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();

        // A lost or duplicated mapping changes this multiset. Distinct numeric and
        // string sentinels expose those mistakes even when both fields have the same type.
        Assert.Equal(expected.Count, fields.Length);
        Assert.Equal(
            expected.Values.Select(value => Fingerprint(value.Type, value.Value)).OrderBy(value => value),
            fields.Select(field => Fingerprint(field.PropertyType, field.GetValue(snapshot))).OrderBy(value => value));

        // These pairs cross groups with similar names or previously merged records.
        // They also catch swaps that preserve the multiset above.
        foreach (var (field, source) in new[]
        {
            ("TimestampUtc", "SnapshotStatus.TimestampUtc"),
            ("SnapshotSourceTelemetryEpoch", "SnapshotStatus.SnapshotSourceTelemetryEpoch"),
            ("DiagnosticLikelyStage", "SnapshotEvaluation.DiagnosticLikelyStage"),
            ("PreviewPacingLikelySlowStage", "SnapshotEvaluation.PreviewPacingLikelySlowStage"),
            ("AudioPeak", "AudioAndIngest.Signal.Peak"),
            ("RequestedWidth", "CaptureFormat.Requested.Width"),
            ("ActualWidth", "CaptureFormat.Actual.Width"),
            ("NegotiatedWidth", "CaptureFormat.Negotiated.Width"),
            ("SelectedRecordingFormat", "UserSettings.SelectedRecordingFormat"),
            ("SelectedQuality", "UserSettings.SelectedQuality"),
            ("SelectedPreset", "UserSettings.SelectedPreset"),
            ("SelectedSplitEncodeMode", "UserSettings.SelectedSplitEncodeMode"),
            ("SelectedVideoFormat", "UserSettings.SelectedVideoFormat"),
            ("CustomBitrateMbps", "UserSettings.CustomBitrateMbps"),
            ("SourceFirmware", "SourceSignal.Firmware"),
            ("SourceTelemetryDiagnosticSummary", "SourceTelemetry.SourceTelemetryDiagnosticSummary"),
            ("RecordingVideoQueueCapacity", "RecordingPipeline.VideoQueue.Capacity"),
            ("RecordingGpuFramesEnqueued", "RecordingPipeline.HardwareQueues.GpuFramesEnqueued"),
            ("RecordingCudaFramesDropped", "RecordingPipeline.HardwareQueues.CudaFramesDropped"),
            ("RecordingRecoveryPath", "RecordingOutput.RecordingRecoveryPath"),
            ("CaptureCadenceEstimatedDroppedFrames", "CaptureCadence.EstimatedDroppedFrames"),
            ("FlashbackExportOutputPath", "FlashbackExport.OutputPath"),
            ("FlashbackExportFailureKind", "FlashbackExport.FailureKind"),
            ("LastExportPath", "FlashbackExportLastResult.LastExportPath"),
            ("LastExportMessage", "FlashbackExportLastResult.LastExportMessage"),
            ("FlashbackExportVerificationFormat", "FlashbackRecording.Backend.ExportVerificationFormat"),
            ("FlashbackCodecDowngradeReason", "FlashbackRecording.Backend.CodecDowngradeReason"),
            ("FlashbackPlaybackTargetFps", "FlashbackPlayback.Timing.TargetFps"),
            ("FlashbackPlaybackDecodeAvgMs", "FlashbackPlayback.Decode.AvgMs"),
            ("FlashbackPlaybackLastCommandFailure", "FlashbackPlayback.Commands.LastFailure")
        })
        {
            Assert.Equal(expected[source].Value, fields.Single(property => property.Name == field).GetValue(snapshot));
        }

        var json = JsonSerializer.SerializeToElement(snapshot, snapshot.GetType());
        Assert.Equal(fields.Select(field => field.Name).OrderBy(name => name),
            json.EnumerateObject().Select(field => field.Name).OrderBy(name => name));
    }

    [Fact]
    public void Snapshot_DefaultProjectionValuesDoNotAcquireDtoInitializers()
    {
        var snapshot = BuildSnapshot.Invoke(null, new[] { Activator.CreateInstance(ProjectionSetType) })!;
        foreach (var field in snapshot.GetType().GetProperties())
        {
            var expected = field.PropertyType.IsValueType ? Activator.CreateInstance(field.PropertyType) : null;
            Assert.True(Equals(expected, field.GetValue(snapshot)), field.Name + " changed its default projection value.");
        }
    }

    [Theory]
    [InlineData(null, "health")]
    [InlineData("", "")]
    [InlineData("runtime", "runtime")]
    public void FlashbackBackendDiagnostics_PreserveRuntimePrecedence(string? runtimeValue, string expected)
    {
        var builderType = HubType.Assembly.GetType(
            "Sussudio.Services.Automation.AutomationSnapshotFlashbackProjectionBuilder", throwOnError: true)!;
        var method = builderType.GetMethod("BuildFlashbackRecordingBackendProjection", BindingFlags.NonPublic | BindingFlags.Static)!;
        var runtime = Empty(method.GetParameters()[0].ParameterType);
        var health = Empty(method.GetParameters()[1].ParameterType);
        foreach (var name in new[] { "FlashbackExportVerificationFormat", "FlashbackCodecDowngradeReason" })
        {
            runtime.GetType().GetProperty(name)!.SetValue(runtime, runtimeValue);
            health.GetType().GetProperty(name)!.SetValue(health, "health");
        }

        var backend = method.Invoke(null, new[] { runtime, health })!;
        var projections = Activator.CreateInstance(ProjectionSetType)!;
        SetPath(projections, "FlashbackRecording.Backend", backend);
        var snapshot = BuildSnapshot.Invoke(null, new[] { projections })!;
        Assert.Equal(expected, snapshot.GetType().GetProperty("FlashbackExportVerificationFormat")!.GetValue(snapshot));
        Assert.Equal(expected, snapshot.GetType().GetProperty("FlashbackCodecDowngradeReason")!.GetValue(snapshot));
    }

    [Theory]
    [InlineData(12.75, 12L)]
    [InlineData(0.0, 0L)]
    public void PreviewLatency_PreservesTheWireUnitAndTruncation(double latency, long expected)
    {
        var method = HubType.GetMethod("BuildPreviewRuntimeFrameProjection", BindingFlags.NonPublic | BindingFlags.Static)!;
        var runtime = Empty(method.GetParameters()[0].ParameterType);
        runtime.GetType().GetProperty("EstimatedPipelineLatencyMs")!.SetValue(runtime, latency);
        var frame = method.Invoke(null, new[] { runtime })!;
        Assert.Equal(expected, frame.GetType().GetProperty("EstimatedPipelineLatencyMs")!.GetValue(frame));
    }

    private static object PopulateProjections(Type type, string prefix,
        Dictionary<string, (Type Type, object? Value)> leaves, ref int sequence)
    {
        var instance = Activator.CreateInstance(type)!;
        // Keep sentinel assignment stable across reflection enumeration orders.
        foreach (var property in type.GetProperties().OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var path = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            object? value;
            if (property.PropertyType.Name.EndsWith("Projection", StringComparison.Ordinal))
            {
                value = PopulateProjections(property.PropertyType, path, leaves, ref sequence);
            }
            else
            {
                value = Sentinel(property.PropertyType, path, ref sequence);
                leaves.Add(path, (property.PropertyType, value));
            }
            property.SetValue(instance, value);
        }
        return instance;
    }

    private static object Sentinel(Type type, string path, ref int sequence)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string)) return path;
        if (type == typeof(bool)) return true;
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.UnixEpoch.AddMilliseconds(++sequence);
        if (type.IsEnum) return Enum.ToObject(type, 1);
        if (type.IsPrimitive || type == typeof(decimal)) return Convert.ChangeType(++sequence, type, CultureInfo.InvariantCulture);
        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)))
        {
            var elementType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
            var array = Array.CreateInstance(elementType, 1);
            array.SetValue(Sentinel(elementType, path + "[0]", ref sequence), 0);
            return array;
        }

        var instance = Empty(type);
        foreach (var property in type.GetProperties().Where(property => property.SetMethod != null)
                     .OrderBy(property => property.Name, StringComparer.Ordinal))
            property.SetValue(instance, Sentinel(property.PropertyType, path + "." + property.Name, ref sequence));
        return instance;
    }

    private static object Empty(Type type) => type.IsValueType
        ? Activator.CreateInstance(type)!
        : RuntimeHelpers.GetUninitializedObject(type);

    private static string Fingerprint(Type type, object? value)
        => type.FullName + ":" + JsonSerializer.Serialize(value, type);

    private static void SetPath(object target, string path, object value)
    {
        var parts = path.Split('.', 2);
        var property = target.GetType().GetProperty(parts[0])!;
        if (parts.Length == 1)
        {
            property.SetValue(target, value);
            return;
        }
        var child = property.GetValue(target)!;
        SetPath(child, parts[1], value);
        property.SetValue(target, child);
    }
}
