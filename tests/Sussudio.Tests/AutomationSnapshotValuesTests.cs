using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationSnapshotValuesTests
{
    private static readonly Type HubType = SussudioAssembly.Load().GetType(
        "Sussudio.Services.Automation.AutomationDiagnosticsHub", throwOnError: true)!;
    private static readonly MethodInfo BuildSnapshot = HubType.GetMethod(
        "BuildAutomationSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [Theory]
    [InlineData(null, "health")]
    [InlineData("", "")]
    [InlineData("runtime", "runtime")]
    public void FlashbackBackendDiagnostics_PreserveRuntimePrecedence(string? runtimeValue, string expected)
    {
        var inputs = CreateInputs();
        foreach (var name in new[] { "FlashbackExportVerificationFormat", "FlashbackCodecDowngradeReason" })
        {
            Set(inputs["captureRuntime"]!, name, runtimeValue);
            Set(inputs["health"]!, name, "health");
        }

        var snapshot = Build(inputs);
        Assert.Equal(expected, Get(snapshot, "FlashbackExportVerificationFormat"));
        Assert.Equal(expected, Get(snapshot, "FlashbackCodecDowngradeReason"));
    }

    [Theory]
    [InlineData(12.75, 12L)]
    [InlineData(0.0, 0L)]
    public void PreviewLatency_PreservesTheWireUnitAndTruncation(double latency, long expected)
    {
        var inputs = CreateInputs();
        Set(inputs["previewRuntime"]!, "EstimatedPipelineLatencyMs", latency);

        Assert.Equal(expected, Get(Build(inputs), "EstimatedPipelineLatencyMs"));
    }

    [Fact]
    public void Snapshot_ForwardsCapturedArraysAndVerdictWithoutCopying()
    {
        var inputs = CreateInputs();
        var captureIntervals = new[] { 8.25, 9.75 };
        var previewIntervals = new[] { 10.25, 11.75 };
        Set(inputs["health"]!, "CaptureCadenceRecentIntervalsMs", captureIntervals);
        Set(inputs["previewRuntime"]!, "DisplayCadenceRecentIntervalsMs", previewIntervals);
        var snapshot = Build(inputs);

        Assert.Same(captureIntervals, Get(snapshot, "CaptureCadenceRecentIntervalsMs"));
        Assert.Same(previewIntervals, Get(snapshot, "PreviewCadenceRecentIntervalsMs"));
        Assert.Same(inputs["hdrTruthVerdict"], Get(snapshot, "HdrTruthVerdict"));
    }

    [Fact]
    public void DiagnosticEvaluation_CreatePreservesVerdictAndSelectsEachLane()
    {
        var lanesType = HubType.GetNestedType("DiagnosticEvaluationLanes", BindingFlags.NonPublic)!;
        var lanes = Activator.CreateInstance(lanesType)!;
        foreach (var property in lanesType.GetProperties().Where(property => property.PropertyType == typeof(string)))
            Set(lanes, property.Name, "lane." + property.Name);

        var evaluationType = HubType.Assembly.GetType("Sussudio.Services.Automation.DiagnosticEvaluation", throwOnError: true)!;
        var create = evaluationType.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static)!;
        var evaluation = create.Invoke(null, new[] { "health", "stage", "summary", "evidence", lanes })!;

        Assert.Equal("health", Get(evaluation, "HealthStatus"));
        Assert.Equal("stage", Get(evaluation, "LikelyStage"));
        Assert.Equal("summary", Get(evaluation, "Summary"));
        Assert.Equal("evidence", Get(evaluation, "Evidence"));
        foreach (var lane in new[] { "Source", "Decode", "Preview", "Render", "Present", "Recording", "Audio" })
            Assert.Equal("lane." + lane, Get(evaluation, lane + "Lane"));
    }

    private static Dictionary<string, object?> CreateInputs()
        => BuildSnapshot.GetParameters().ToDictionary(parameter => parameter.Name!, parameter =>
            parameter.Name == "lastVerification" ? null : Empty(parameter.ParameterType));

    private static object Build(Dictionary<string, object?> inputs)
        => BuildSnapshot.Invoke(RuntimeHelpers.GetUninitializedObject(HubType),
            BuildSnapshot.GetParameters().Select(parameter => inputs[parameter.Name!]).ToArray())!;

    private static object? Empty(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : RuntimeHelpers.GetUninitializedObject(type);

    private static object? Get(object target, string propertyName)
        => target.GetType().GetProperty(propertyName)!.GetValue(target);

    private static void Set(object target, string propertyName, object? value)
        => target.GetType().GetProperty(propertyName)!.SetValue(target, value);
}
