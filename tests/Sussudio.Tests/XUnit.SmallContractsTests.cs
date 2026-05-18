using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// xUnit slice for three small contract / policy types that the legacy
// reflection runner already covers (LoggingJsonContext.Tests.cs,
// AutomationPipeSecurityPolicy reachable via Sussudio.Automation.Contracts,
// DiagnosticThresholds covered indirectly through snapshot tests).
// Lives here so each file is reachable through the xUnit discovery path too.
public class SmallContractsTests
{
    [Fact]
    public void Sussudio_Models_AudioInputDevice_DisplayNameUsesNameOrUnknownFallback()
    {
        var asm = SussudioAssembly.Load();
        var deviceType = asm.GetType("Sussudio.Models.AudioInputDevice", throwOnError: true)!;
        var idProperty = RequireProperty(deviceType, "Id", typeof(string), canWrite: true);
        var nameProperty = RequireProperty(deviceType, "Name", typeof(string), canWrite: true);
        var displayNameProperty = RequireProperty(deviceType, "DisplayName", typeof(string), canWrite: false);
        var device = Activator.CreateInstance(deviceType)!;

        Assert.Equal(string.Empty, idProperty.GetValue(device));
        Assert.Equal(string.Empty, nameProperty.GetValue(device));
        Assert.Equal("Unknown Audio Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Audio Device", device.ToString());

        nameProperty.SetValue(device, "   ");
        Assert.Equal("Unknown Audio Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Audio Device", device.ToString());

        idProperty.SetValue(device, "audio-1");
        nameProperty.SetValue(device, "Wave Link Microphone");
        Assert.Equal("audio-1", idProperty.GetValue(device));
        Assert.Equal("Wave Link Microphone", displayNameProperty.GetValue(device));
        Assert.Equal("Wave Link Microphone", device.ToString());
    }

    [Fact]
    public void Sussudio_Models_AudioLevelEventArgs_ExposesPeakRmsAndClippedState()
    {
        var asm = SussudioAssembly.Load();
        var argsType = asm.GetType("Sussudio.Models.AudioLevelEventArgs", throwOnError: true)!;

        Assert.True(typeof(EventArgs).IsAssignableFrom(argsType));
        var peakProperty = RequireProperty(argsType, "Peak", typeof(double), canWrite: false);
        var rmsProperty = RequireProperty(argsType, "Rms", typeof(double), canWrite: false);
        var clippedProperty = RequireProperty(argsType, "Clipped", typeof(bool), canWrite: false);
        var constructor = argsType.GetConstructor(new[] { typeof(double), typeof(double), typeof(bool) })!;

        var clippedArgs = constructor.Invoke(new object[] { 0.75d, 0.25d, true });
        Assert.Equal(0.75d, peakProperty.GetValue(clippedArgs));
        Assert.Equal(0.25d, rmsProperty.GetValue(clippedArgs));
        Assert.True((bool)clippedProperty.GetValue(clippedArgs)!);

        var unclippedArgs = constructor.Invoke(new object[] { 0.1d, 0.05d, false });
        Assert.Equal(0.1d, peakProperty.GetValue(unclippedArgs));
        Assert.Equal(0.05d, rmsProperty.GetValue(unclippedArgs));
        Assert.False((bool)clippedProperty.GetValue(unclippedArgs)!);
    }

    [Fact]
    public void Sussudio_Models_CaptureDevice_DisplayNameAndDefaultsPreserveDeviceMetadata()
    {
        var asm = SussudioAssembly.Load();
        var deviceType = asm.GetType("Sussudio.Models.CaptureDevice", throwOnError: true)!;
        var mediaFormatType = asm.GetType("Sussudio.Models.MediaFormat", throwOnError: true)!;
        var supportedFormatsType = typeof(ObservableCollection<>).MakeGenericType(mediaFormatType);
        var idProperty = RequireProperty(deviceType, "Id", typeof(string), canWrite: true);
        var nameProperty = RequireProperty(deviceType, "Name", typeof(string), canWrite: true);
        var nativeXuProperty = RequireProperty(deviceType, "NativeXuInterfacePath", typeof(string), canWrite: true);
        var audioDeviceIdProperty = RequireProperty(deviceType, "AudioDeviceId", typeof(string), canWrite: true);
        var audioDeviceNameProperty = RequireProperty(deviceType, "AudioDeviceName", typeof(string), canWrite: true);
        var isHdrCapableProperty = RequireProperty(deviceType, "IsHdrCapable", typeof(bool), canWrite: true);
        var supportedFormatsProperty = RequireProperty(deviceType, "SupportedFormats", supportedFormatsType, canWrite: true);
        var displayNameProperty = RequireProperty(deviceType, "DisplayName", typeof(string), canWrite: false);
        var device = Activator.CreateInstance(deviceType)!;

        Assert.Equal(string.Empty, idProperty.GetValue(device));
        Assert.Equal(string.Empty, nameProperty.GetValue(device));
        Assert.Null(nativeXuProperty.GetValue(device));
        Assert.Null(audioDeviceIdProperty.GetValue(device));
        Assert.Null(audioDeviceNameProperty.GetValue(device));
        Assert.False((bool)isHdrCapableProperty.GetValue(device)!);
        Assert.Equal("Unknown Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Device", device.ToString());

        nameProperty.SetValue(device, "   ");
        Assert.Equal("Unknown Device", displayNameProperty.GetValue(device));
        Assert.Equal("Unknown Device", device.ToString());

        var supportedFormats = supportedFormatsProperty.GetValue(device)!;
        Assert.Equal(supportedFormatsType, supportedFormats.GetType());
        Assert.Empty(((IEnumerable)supportedFormats).Cast<object>());

        var secondDevice = Activator.CreateInstance(deviceType)!;
        var secondSupportedFormats = supportedFormatsProperty.GetValue(secondDevice)!;
        Assert.NotSame(supportedFormats, secondSupportedFormats);

        var format = Activator.CreateInstance(mediaFormatType)!;
        supportedFormatsType.GetMethod("Add", new[] { mediaFormatType })!.Invoke(supportedFormats, new[] { format });
        Assert.Single(((IEnumerable)supportedFormats).Cast<object>());
        Assert.Empty(((IEnumerable)secondSupportedFormats).Cast<object>());

        var replacementFormats = Activator.CreateInstance(supportedFormatsType)!;
        var replacementFormat = Activator.CreateInstance(mediaFormatType)!;
        supportedFormatsType.GetMethod("Add", new[] { mediaFormatType })!.Invoke(replacementFormats, new[] { replacementFormat });
        supportedFormatsProperty.SetValue(device, replacementFormats);
        Assert.Same(replacementFormats, supportedFormatsProperty.GetValue(device));
        Assert.Single(((IEnumerable)replacementFormats).Cast<object>());

        idProperty.SetValue(device, "device-1");
        nameProperty.SetValue(device, "Game Capture 4K X");
        nativeXuProperty.SetValue(device, @"\\?\hid#vid_0fd9");
        audioDeviceIdProperty.SetValue(device, "audio-1");
        audioDeviceNameProperty.SetValue(device, "4K X Audio");
        isHdrCapableProperty.SetValue(device, true);

        Assert.Equal("device-1", idProperty.GetValue(device));
        Assert.Equal("Game Capture 4K X", displayNameProperty.GetValue(device));
        Assert.Equal("Game Capture 4K X", device.ToString());
        Assert.Equal(@"\\?\hid#vid_0fd9", nativeXuProperty.GetValue(device));
        Assert.Equal("audio-1", audioDeviceIdProperty.GetValue(device));
        Assert.Equal("4K X Audio", audioDeviceNameProperty.GetValue(device));
        Assert.True((bool)isHdrCapableProperty.GetValue(device)!);
    }

    [Fact]
    public void Sussudio_Models_AutomationWindowAction_HasExpectedValues()
    {
        var asm = SussudioAssembly.Load();
        var enumType = asm.GetType("Sussudio.Models.AutomationWindowAction", throwOnError: true)!;
        var expectedNames = new[]
        {
            "Minimize", "Maximize", "Restore", "Close",
            "SnapLeft", "SnapRight", "SnapTopLeft", "SnapTopRight",
            "SnapBottomLeft", "SnapBottomRight", "Center", "Move", "Resize"
        };

        Assert.Equal(expectedNames, Enum.GetNames(enumType));
    }

    [Fact]
    public void Sussudio_LoggingJsonContext_ExposesSourceGeneratedTypeInfoForKnownPayloads()
    {
        var asm = SussudioAssembly.Load();
        var contextType = asm.GetType("Sussudio.LoggingJsonContext", throwOnError: true)!;

        var defaultProp = contextType.GetProperty("Default", ReflectionFlags.Static);
        Assert.NotNull(defaultProp);

        var defaultInstance = defaultProp!.GetValue(null);
        Assert.NotNull(defaultInstance);

        Assert.NotNull(contextType.GetProperty("CaptureHealthSnapshot", ReflectionFlags.Instance));
        Assert.NotNull(contextType.GetProperty("CaptureDiagnosticsSnapshot", ReflectionFlags.Instance));
    }

    [Fact]
    public void Sussudio_Services_Automation_DiagnosticThresholds_ComputesPercentSafely()
    {
        var asm = SussudioAssembly.Load();
        var type = asm.GetType("Sussudio.Services.Automation.DiagnosticThresholds", throwOnError: true)!;

        var minSamples = (int)type.GetField("RendererDropWarningMinSamples", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.Equal(120, minSamples);

        var pctConst = (double)type.GetField("RendererDropWarningPercent", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.Equal(0.25, pctConst);

        var calc = type.GetMethod("CalculatePercent", ReflectionFlags.Static, new[] { typeof(long), typeof(long) })!;

        Assert.Equal(0.0, (double)calc.Invoke(null, new object[] { 5L, 0L })!);
        Assert.Equal(25.0, (double)calc.Invoke(null, new object[] { 25L, 100L })!);
        Assert.Equal(0.0, (double)calc.Invoke(null, new object[] { -10L, 100L })!);
    }

    [Fact]
    public void Sussudio_Tools_AutomationPipeSecurityPolicy_DisablesFallbackOnlyWhenWindowsAndUnauthenticated()
    {
        // Referenced from Sussudio.Automation.Contracts so the production
        // policy is tested without linking source into the test project.

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

    private static PropertyInfo RequireProperty(Type type, string name, Type expectedType, bool canWrite)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        Assert.Equal(expectedType, property!.PropertyType);
        Assert.Equal(canWrite, property.CanWrite);
        return property;
    }
}
