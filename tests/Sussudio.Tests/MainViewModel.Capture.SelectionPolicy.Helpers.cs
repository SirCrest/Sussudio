using System;
using System.Collections;
using System.Reflection;

static partial class Program
{
    private static object InvokeDeviceFormatProbeRetargetDecision(
        bool preserveActiveSelection,
        bool allowProbeDrivenRetarget,
        bool isHdrEnabled,
        bool modeChanged,
        string? previousResolution,
        double previousFrameRate,
        string? selectedResolution,
        double selectedFrameRate,
        object? selectedFormat,
        object supportedFormats,
        bool previousResolutionAvailable,
        bool includeSessionMismatchCheck,
        uint? sessionActualWidth,
        uint? sessionActualHeight)
    {
        var requestType = RequireType("Sussudio.ViewModels.DeviceFormatProbeRetargetRequest");
        var policyType = RequireType("Sussudio.ViewModels.DeviceFormatProbeRetargetPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 14);
        var request = constructor.Invoke(new object?[]
        {
            preserveActiveSelection,
            allowProbeDrivenRetarget,
            isHdrEnabled,
            modeChanged,
            previousResolution,
            previousFrameRate,
            selectedResolution,
            selectedFrameRate,
            selectedFormat,
            supportedFormats,
            previousResolutionAvailable,
            includeSessionMismatchCheck,
            sessionActualWidth,
            sessionActualHeight
        });
        var decide = policyType.GetMethod("Decide", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceFormatProbeRetargetPolicy.Decide missing.");
        return decide.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("DeviceFormatProbeRetargetPolicy.Decide returned null.");
    }

    private static string GetEnumName(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName)!.GetValue(instance)?.ToString()
           ?? throw new InvalidOperationException($"{propertyName} returned null.");

    private static object InvokeCaptureResolutionSelection(
        object options,
        object formatsByResolution,
        object telemetry,
        string? preferredSelection,
        double previousFrameRate,
        bool isHdrEnabled,
        bool allowSourceAutoSelect,
        bool pendingSdrAutoSelectionForDeviceChange)
    {
        var requestType = RequireType("Sussudio.ViewModels.CaptureResolutionSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.CaptureResolutionSelectionPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 8);
        var request = constructor.Invoke(new object?[]
        {
            options,
            formatsByResolution,
            telemetry,
            preferredSelection,
            previousFrameRate,
            isHdrEnabled,
            allowSourceAutoSelect,
            pendingSdrAutoSelectionForDeviceChange
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureResolutionSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("CaptureResolutionSelectionPolicy.Select returned null.");
    }

    private static object InvokeAutoCaptureSelection(
        object options,
        object formatsByResolution,
        object telemetry,
        bool isHdrEnabled)
    {
        var requestType = RequireType("Sussudio.ViewModels.AutoCaptureSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.AutoCaptureSelectionPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 4);
        var request = constructor.Invoke(new object?[]
        {
            options,
            formatsByResolution,
            telemetry,
            isHdrEnabled
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutoCaptureSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("AutoCaptureSelectionPolicy.Select returned null.");
    }

    private static object InvokeFrameRateAutoSelection(
        object options,
        bool autoFrameRateOptionAvailable,
        bool forceAutoSelection,
        bool isAutoFrameRateSelected,
        bool hasUserOverriddenFrameRateForCurrentMode,
        bool isHdrEnabled,
        bool pendingSdrAutoSelectionForDeviceChange,
        int? pendingSdrAutoFriendlyFrameRateBucket,
        double? sourceRate,
        bool sourceTimingFamilyKnown,
        string sourceTimingFamilyName,
        double previousRate)
    {
        var sourceType = RequireType("Sussudio.ViewModels.MainViewModel+FrameRateAutoSelectionSource");
        var requestType = RequireType("Sussudio.ViewModels.MainViewModel+FrameRateAutoSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.MainViewModel+FrameRateAutoSelectionPolicy");
        var timingFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", sourceTimingFamilyName);
        var sourceConstructor = FindConstructor(sourceType, parameterCount: 3);
        var source = sourceConstructor.Invoke(new object?[]
        {
            sourceRate,
            sourceTimingFamilyKnown,
            timingFamily
        });
        var requestConstructor = FindConstructor(requestType, parameterCount: 10);
        var request = requestConstructor.Invoke(new object?[]
        {
            options,
            autoFrameRateOptionAvailable,
            forceAutoSelection,
            isAutoFrameRateSelected,
            hasUserOverriddenFrameRateForCurrentMode,
            isHdrEnabled,
            pendingSdrAutoSelectionForDeviceChange,
            pendingSdrAutoFriendlyFrameRateBucket,
            source,
            previousRate
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateAutoSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("FrameRateAutoSelectionPolicy.Select returned null.");
    }

    private static ConstructorInfo FindConstructor(Type type, int parameterCount)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (constructor.GetParameters().Length == parameterCount)
            {
                return constructor;
            }
        }

        throw new InvalidOperationException($"{type.Name} constructor with {parameterCount} parameters was not found.");
    }

    private static object CreateResolutionOptionList(Type resolutionType, params object[] options)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(resolutionType))
                           ?? throw new InvalidOperationException("Failed to create resolution option list."));
        foreach (var option in options)
        {
            list.Add(option);
        }

        return list;
    }

    private static object CreateFrameRateOptionList(Type frameRateType, params object[] options)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(frameRateType))
                           ?? throw new InvalidOperationException("Failed to create frame-rate option list."));
        foreach (var option in options)
        {
            list.Add(option);
        }

        return list;
    }

    private static object CreateResolutionOption(
        Type resolutionType,
        string value,
        uint width,
        uint height,
        bool isEnabled)
    {
        var option = CreateConfigInstance(resolutionType);
        SetPropertyOrBackingField(option, "Value", value);
        SetPropertyOrBackingField(option, "Width", width);
        SetPropertyOrBackingField(option, "Height", height);
        SetPropertyOrBackingField(option, "IsEnabled", isEnabled);
        return option;
    }

    private static object CreateFrameRateOption(
        Type frameRateType,
        double friendlyValue,
        double value,
        string rational,
        bool isEnabled)
    {
        var option = CreateConfigInstance(frameRateType);
        SetPropertyOrBackingField(option, "FriendlyValue", friendlyValue);
        SetPropertyOrBackingField(option, "Value", value);
        SetPropertyOrBackingField(option, "Rational", rational);
        SetPropertyOrBackingField(option, "IsEnabled", isEnabled);
        return option;
    }
}
