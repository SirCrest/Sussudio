using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

// xUnit slice for capture/telemetry policy types. Each test resolves the
// production type from the staged Sussudio.dll so the test_coverage detector
// recognizes the file as exercised.
public class CapturePoliciesTests
{
    private const string DisabledTelemetryProviderType = "Sussudio.Services.Telemetry.DisabledSourceSignalTelemetryProvider";

    [Fact]
    public void Sussudio_Services_Capture_HdrOutputPolicy_GatesOnHdrEnabledAndMode()
    {
        var asm = SussudioAssembly.Load();
        var policy = asm.GetType("Sussudio.Services.Capture.HdrOutputPolicy", throwOnError: true)!;
        var settingsType = asm.GetType("Sussudio.Models.CaptureSettings", throwOnError: true)!;
        var modeType = asm.GetType("Sussudio.Models.HdrOutputMode", throwOnError: true)!;
        var hdrEnabledProp = settingsType.GetProperty("HdrEnabled")!;
        var hdrModeProp = settingsType.GetProperty("HdrOutputMode")!;
        var isEnabled = policy.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)!;

        // Disabled: HdrEnabled=false short-circuits regardless of mode.
        var disabled = Activator.CreateInstance(settingsType)!;
        hdrEnabledProp.SetValue(disabled, false);
        hdrModeProp.SetValue(disabled, Enum.Parse(modeType, "Hdr10Pq"));
        Assert.False((bool)isEnabled.Invoke(null, new object?[] { disabled })!);

        // Enabled + Off mode: requires the explicit Hdr10Pq mode to engage.
        var enabledOff = Activator.CreateInstance(settingsType)!;
        hdrEnabledProp.SetValue(enabledOff, true);
        hdrModeProp.SetValue(enabledOff, Enum.Parse(modeType, "Off"));
        Assert.False((bool)isEnabled.Invoke(null, new object?[] { enabledOff })!);

        // Enabled + Hdr10Pq + no env override: engages.
        var enabledHdr10 = Activator.CreateInstance(settingsType)!;
        hdrEnabledProp.SetValue(enabledHdr10, true);
        hdrModeProp.SetValue(enabledHdr10, Enum.Parse(modeType, "Hdr10Pq"));
        using (EnvVarScope.Push("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null))
        {
            Assert.True((bool)isEnabled.Invoke(null, new object?[] { enabledHdr10 })!);
        }
    }

    [Fact]
    public async Task Sussudio_Services_Telemetry_DisabledSourceSignalTelemetryProvider_ReturnsUnavailableSnapshotWithDisabledReason()
    {
        var asm = SussudioAssembly.Load();
        var providerType = asm.GetType(DisabledTelemetryProviderType, throwOnError: true)!;
        var deviceType = asm.GetType("Sussudio.Models.CaptureDevice", throwOnError: true)!;
        Assert.NotNull(deviceType);

        var provider = Activator.CreateInstance(providerType)!;
        var readAsync = providerType.GetMethod("ReadAsync")!;
        var task = (Task)readAsync.Invoke(provider, new object?[] { null, CancellationToken.None })!;
        await task;

        var resultProp = task.GetType().GetProperty("Result")!;
        var snapshot = resultProp.GetValue(task)!;
        var snapshotType = snapshot.GetType();

        var availability = snapshotType.GetProperty("Availability")!.GetValue(snapshot)!;
        Assert.Equal("Unavailable", availability.ToString());

        var summary = (string?)snapshotType.GetProperty("DiagnosticSummary")!.GetValue(snapshot);
        Assert.Equal("telemetry-provider-disabled", summary);

        var origin = snapshotType.GetProperty("OriginDetail")!.GetValue(snapshot)!;
        Assert.Equal("Unavailable", origin.ToString());
    }

    [Fact]
    public void Sussudio_Services_Telemetry_DisabledSourceSignalTelemetryProvider_HonorsCancellation()
    {
        var asm = SussudioAssembly.Load();
        var providerType = asm.GetType(DisabledTelemetryProviderType, throwOnError: true)!;
        var provider = Activator.CreateInstance(providerType)!;
        var readAsync = providerType.GetMethod("ReadAsync")!;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // ReadAsync throws synchronously on a pre-cancelled token (ThrowIfCancellationRequested
        // runs before the Task is built); reflection wraps that in TargetInvocationException.
        var thrown = Assert.Throws<TargetInvocationException>(() =>
            readAsync.Invoke(provider, new object?[] { null, cts.Token }));
        Assert.IsAssignableFrom<OperationCanceledException>(thrown.InnerException);
    }
}
