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
