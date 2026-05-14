using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task HdrOutputPolicy_ReturnsTrue_WhenHdrAndHdr10PqRequested()
    {
        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null);
            var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
            AssertEqual(true, result, "HDR enabled + Hdr10Pq should return true");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
        }

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenHdrDisabled()
    {
        var result = InvokeHdrOutputPolicy(hdrEnabled: false, hdrOutputMode: "Hdr10Pq");
        AssertEqual(false, result, "HDR disabled should return false");

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenNotHdr10Pq()
    {
        var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Off");
        AssertEqual(false, result, "HdrOutputMode=Off should return false");

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenForceOffEnvSet()
    {
        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", "true");
            var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
            AssertEqual(false, result, "force-off env switch should disable HDR output");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
        }

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_IgnoresLegacyEnabledEnvSwitch()
    {
        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        var previousLegacyEnabled = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_ENABLED");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null);
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_ENABLED", "false");
            var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
            AssertEqual(true, result, "legacy enabled env switch should no longer disable HDR output");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_ENABLED", previousLegacyEnabled);
        }

        return Task.CompletedTask;
    }

    private static bool InvokeHdrOutputPolicy(bool hdrEnabled, string hdrOutputMode)
    {
        var policyType = RequireType("Sussudio.Services.Capture.HdrOutputPolicy");
        var method = policyType.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("HdrOutputPolicy.IsEnabled not found");

        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "HdrEnabled", hdrEnabled);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("Sussudio.Models.HdrOutputMode", hdrOutputMode));

        return (bool)method.Invoke(null, new[] { settings })!;
    }
}
