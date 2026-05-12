using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// xUnit slice for three small contract / policy types that the legacy
// reflection runner already covers (LoggingJsonContext.Tests.cs,
// AutomationPipeSecurityPolicy reachable via Common\ Compile Include,
// DiagnosticThresholds covered indirectly through snapshot tests).
// Lives here so each file is reachable through the xUnit discovery path too.
public class SmallContractsTests
{
    [Fact]
    public void Sussudio_LoggingJsonContext_ExposesSourceGeneratedTypeInfoForKnownPayloads()
    {
        var asm = SussudioAssembly.Load();
        var contextType = asm.GetType("Sussudio.LoggingJsonContext", throwOnError: true)!;

        var defaultProp = contextType.GetProperty(
            "Default",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(defaultProp);

        var defaultInstance = defaultProp!.GetValue(null);
        Assert.NotNull(defaultInstance);

        Assert.NotNull(contextType.GetProperty(
            "CaptureHealthSnapshot",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.NotNull(contextType.GetProperty(
            "CaptureDiagnosticsSnapshot",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
    }

    [Fact]
    public void Sussudio_Services_Automation_DiagnosticThresholds_ComputesPercentSafely()
    {
        var asm = SussudioAssembly.Load();
        var type = asm.GetType("Sussudio.Services.Automation.DiagnosticThresholds", throwOnError: true)!;

        var minSamples = (int)type.GetField(
            "RendererDropWarningMinSamples",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Assert.Equal(120, minSamples);

        var pctConst = (double)type.GetField(
            "RendererDropWarningPercent",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Assert.Equal(0.25, pctConst);

        var calc = type.GetMethod(
            "CalculatePercent",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            new[] { typeof(long), typeof(long) })!;

        Assert.Equal(0.0, (double)calc.Invoke(null, new object[] { 5L, 0L })!);
        Assert.Equal(25.0, (double)calc.Invoke(null, new object[] { 25L, 100L })!);
        Assert.Equal(0.0, (double)calc.Invoke(null, new object[] { -10L, 100L })!);
    }

    [Fact]
    public void Sussudio_Tools_AutomationPipeSecurityPolicy_DisablesFallbackOnlyWhenWindowsAndUnauthenticated()
    {
        // Compile-included into Sussudio.Tests so the type is reachable directly
        // (the production copy lives in tools/Common/AutomationPipeSecurityPolicy.cs).
        var localType = typeof(Sussudio.Tools.AutomationPipeSecurityPolicy);
        Assert.NotNull(localType);

        // Non-Windows: never disable, regardless of other flags.
        AssertResult(false, isWindows: false, hasExplicitSecurityDescriptor: false, explicitSecurityFailed: true, authTokenRequired: false);
        AssertResult(false, isWindows: false, hasExplicitSecurityDescriptor: true, explicitSecurityFailed: false, authTokenRequired: false);

        // Auth required: never disable, even on Windows with no explicit descriptor.
        AssertResult(false, isWindows: true, hasExplicitSecurityDescriptor: false, explicitSecurityFailed: false, authTokenRequired: true);

        // Windows, no explicit descriptor, no auth token: disable.
        AssertResult(true, isWindows: true, hasExplicitSecurityDescriptor: false, explicitSecurityFailed: false, authTokenRequired: false);

        // Windows, explicit descriptor set but failed, no auth token: disable.
        AssertResult(true, isWindows: true, hasExplicitSecurityDescriptor: true, explicitSecurityFailed: true, authTokenRequired: false);

        // Windows, explicit descriptor set and working, no auth: do NOT disable.
        AssertResult(false, isWindows: true, hasExplicitSecurityDescriptor: true, explicitSecurityFailed: false, authTokenRequired: false);
    }

    private static void AssertResult(
        bool expected,
        bool isWindows,
        bool hasExplicitSecurityDescriptor,
        bool explicitSecurityFailed,
        bool authTokenRequired)
    {
        var actual = Sussudio.Tools.AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
            isWindows,
            hasExplicitSecurityDescriptor,
            explicitSecurityFailed,
            authTokenRequired);
        Assert.Equal(expected, actual);
    }
}
