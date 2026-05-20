using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FrameRateAutoSelectionPolicy_PreservesSelectionBehavior()
    {
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");

        var sourceNearestOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 30, 30, "30/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 60, 60000d / 1001d, "60000/1001", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true));
        var sourceNearest = InvokeFrameRateAutoSelection(
            sourceNearestOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: true,
            hasUserOverriddenFrameRateForCurrentMode: false,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: false,
            pendingSdrAutoFriendlyFrameRateBucket: null,
            sourceRate: 59.94,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Ntsc1001",
            previousRate: 30);
        AssertEqual(60000d / 1001d, GetDoubleProperty(GetPropertyValue(sourceNearest, "Selected")!, "Value"), "Frame-rate auto source nearest selection");
        AssertEqual(true, GetBoolProperty(sourceNearest, "SelectAutoOption"), "Frame-rate source nearest keeps auto selected");

        var pendingBucketOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 60, 60000d / 1001d, "60000/1001", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true));
        var pendingBucket = InvokeFrameRateAutoSelection(
            pendingBucketOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: true,
            hasUserOverriddenFrameRateForCurrentMode: false,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: true,
            pendingSdrAutoFriendlyFrameRateBucket: 60,
            sourceRate: 120,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Integer",
            previousRate: 120);
        AssertEqual(60d, GetDoubleProperty(GetPropertyValue(pendingBucket, "Selected")!, "FriendlyValue"), "Frame-rate auto pending SDR bucket selection");

        var hdrSkipsPendingBucket = InvokeFrameRateAutoSelection(
            pendingBucketOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: true,
            hasUserOverriddenFrameRateForCurrentMode: false,
            isHdrEnabled: true,
            pendingSdrAutoSelectionForDeviceChange: true,
            pendingSdrAutoFriendlyFrameRateBucket: 60,
            sourceRate: 120,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Integer",
            previousRate: 60);
        AssertEqual(120d, GetDoubleProperty(GetPropertyValue(hdrSkipsPendingBucket, "Selected")!, "Value"), "Frame-rate auto HDR skips pending SDR bucket");

        var manualFallbackOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 30, 30, "30/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 60, 60, "60/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true));
        var manualFallback = InvokeFrameRateAutoSelection(
            manualFallbackOptions,
            autoFrameRateOptionAvailable: true,
            forceAutoSelection: false,
            isAutoFrameRateSelected: false,
            hasUserOverriddenFrameRateForCurrentMode: true,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: false,
            pendingSdrAutoFriendlyFrameRateBucket: null,
            sourceRate: 60,
            sourceTimingFamilyKnown: true,
            sourceTimingFamilyName: "Integer",
            previousRate: 119.88);
        AssertEqual(120d, GetDoubleProperty(GetPropertyValue(manualFallback, "Selected")!, "Value"), "Frame-rate manual previous friendly fallback");
        AssertEqual(false, GetBoolProperty(manualFallback, "SelectAutoOption"), "Frame-rate manual fallback leaves auto deselected");

        var autoFallbackOptions = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 30, 30, "30/1", isEnabled: false),
            CreateFrameRateOption(frameRateType, 60, 60, "60/1", isEnabled: true));
        var autoFallback = InvokeFrameRateAutoSelection(
            autoFallbackOptions,
            autoFrameRateOptionAvailable: false,
            forceAutoSelection: true,
            isAutoFrameRateSelected: false,
            hasUserOverriddenFrameRateForCurrentMode: true,
            isHdrEnabled: false,
            pendingSdrAutoSelectionForDeviceChange: false,
            pendingSdrAutoFriendlyFrameRateBucket: null,
            sourceRate: null,
            sourceTimingFamilyKnown: false,
            sourceTimingFamilyName: "Unknown",
            previousRate: 30);
        AssertEqual(60d, GetDoubleProperty(GetPropertyValue(autoFallback, "Selected")!, "Value"), "Frame-rate forced auto fallback chooses first enabled option");
        AssertEqual(true, GetBoolProperty(autoFallback, "SelectAutoOption"), "Frame-rate forced auto fallback selects auto");

        return Task.CompletedTask;
    }

    internal static Task FrameRateTimingPolicy_PreservesPureTimingBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var policyType = RequireType("Sussudio.ViewModels.FrameRateTimingPolicy");
        var ntscFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", "Ntsc1001");
        var integerFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", "Integer");

        var integer60 = CreateFrameRateTimingFormat(mediaFormatType, 1920, 1080, 60, 60, 1, "NV12", isHdr: false);
        var ntsc60 = CreateFrameRateTimingFormat(mediaFormatType, 1920, 1080, 60000d / 1001d, 60000, 1001, "NV12", isHdr: false);
        var selectPreferred = policyType.GetMethod("SelectPreferredFrameRateFormat", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.SelectPreferredFrameRateFormat missing.");

        var ntscSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, integer60, ntsc60),
                60,
                ntscFamily
            })
            ?? throw new InvalidOperationException("NTSC preferred selection returned null.");
        AssertEqual(60000u, (uint)GetPropertyValue(ntscSelected, "FrameRateNumerator")!, "NTSC timing-family rank numerator");

        var integerSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, ntsc60, integer60),
                60,
                integerFamily
            })
            ?? throw new InvalidOperationException("Integer preferred selection returned null.");
        AssertEqual(1u, (uint)GetPropertyValue(integerSelected, "FrameRateDenominator")!, "Integer timing-family rank denominator");

        var hfrMjpg = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "MJPG", isHdr: false);
        var hfrNv12 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "NV12", isHdr: false);
        var hfrSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, hfrMjpg, hfrNv12),
                120,
                integerFamily
            })
            ?? throw new InvalidOperationException("4K HFR preferred selection returned null.");
        AssertEqual("MJPG", GetStringProperty(hfrSelected, "PixelFormat"), "4K HFR MJPG keeps top pixel-format priority");
        var hfrSourceOrderSelected = selectPreferred.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, hfrNv12, hfrMjpg),
                120,
                integerFamily
            })
            ?? throw new InvalidOperationException("4K HFR source-order selection returned null.");
        AssertEqual("NV12", GetStringProperty(hfrSourceOrderSelected, "PixelFormat"), "4K HFR top priority preserves source order tie");

        var buildTimingVariants = policyType.GetMethod("BuildTimingVariants", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.BuildTimingVariants missing.");
        var variants = ((IEnumerable)buildTimingVariants.Invoke(null, new[]
            {
                CreateMediaFormatList(mediaFormatType, ntsc60, integer60)
            })!)
            .Cast<object>()
            .ToArray();
        AssertEqual(2, variants.Length, "Friendly bucket timing variant count");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(variants[0], "FriendlyBucket")), "NTSC friendly bucket");
        AssertEqual("Ntsc1001", GetPropertyValue(variants[0], "Family")?.ToString(), "NTSC family variant");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(variants[1], "FriendlyBucket")), "Integer friendly bucket");
        AssertEqual("Integer", GetPropertyValue(variants[1], "Family")?.ToString(), "Integer family variant");

        var inferFamily = policyType.GetMethod("TryInferFrameRateTimingFamily", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.TryInferFrameRateTimingFamily missing.");
        var inferArgs = new object?[] { "not/rational", 60000d / 1001d, null };
        AssertEqual(true, (bool)inferFamily.Invoke(null, inferArgs)!, "Timing-family rational parse fallback return");
        AssertEqual("Ntsc1001", inferArgs[2]?.ToString(), "Timing-family rational parse fallback value");

        var friendlyMatch = policyType.GetMethod("IsFriendlyFrameRateMatch", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateTimingPolicy.IsFriendlyFrameRateMatch missing.");
        AssertEqual(true, (bool)friendlyMatch.Invoke(null, new object[] { 60d, 60000d / 1001d })!, "Friendly bucket grouping");

        return Task.CompletedTask;
    }

    private static object CreateFrameRateTimingFormat(
        Type mediaFormatType,
        uint width,
        uint height,
        double frameRate,
        uint numerator,
        uint denominator,
        string pixelFormat,
        bool isHdr)
    {
        var format = CreateTestMediaFormat(mediaFormatType, width, height, frameRate, pixelFormat, isHdr);
        SetPropertyOrBackingField(format, "FrameRateNumerator", numerator);
        SetPropertyOrBackingField(format, "FrameRateDenominator", denominator);
        return format;
    }
}
