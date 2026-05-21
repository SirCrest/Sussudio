using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureFormatSelectionPolicy_LivesInFocusedHelper()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.Context.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/ViewModels/CaptureFormatSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(captureModeTransactionsText, "private void UpdateSelectedFormat()");
        AssertContains(captureModeTransactionsText, "private void RebuildVideoFormatOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.UpdateSelectedFormat();");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildVideoFormatOptions();");
        AssertContains(captureModeOptionsControllerText, "public void UpdateSelectedFormat()");
        AssertContains(captureModeOptionsControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionsControllerText, "CaptureFormatSelectionPolicy.Select(");
        AssertContains(captureModeOptionsControllerText, ".SelectModeTupleFormats(BuildCaptureFormatSelectionRequest(");
        AssertContains(captureModeOptionsControllerText, "_context.AvailableVideoFormats.Clear();");
        AssertContains(captureModeOptionsControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(captureModeOptionsControllerContextText, "internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertDoesNotContain(captureModeOptionsControllerText, "_viewModel.");
        AssertDoesNotContain(captureModeTransactionsText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool IsHdrModeCandidate(");
        AssertDoesNotContain(captureModeTransactionsText, "ShouldPreserveMjpegHighFrameRateMode(");
        AssertContains(policyText, "internal static class CaptureFormatSelectionPolicy");
        AssertContains(policyText, "internal static MediaFormat? Select(CaptureFormatSelectionRequest request)");
        AssertContains(policyText, "internal static IReadOnlyList<MediaFormat> SelectModeTupleFormats(CaptureFormatSelectionRequest request)");
        AssertContains(policyText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertContains(policyText, "CaptureModeOptionsBuilder.IsHdrModeCandidate(format)");
        AssertContains(policyText, "internal sealed record CaptureFormatSelectionRequest(");
        AssertEqual(
            true,
            policyText.Split('\n').Length >= 100,
            "capture format selection policy is a substantial ownership file");

        return Task.CompletedTask;
    }

    internal static Task CaptureFormatSelectionPolicy_PreservesSelectionBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");

        var sdrNv12 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "NV12", isHdr: false);
        var sdrMjpg = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "MJPG", isHdr: false);
        var hdrP010 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "P010", isHdr: true);
        var ntsc119 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120000d / 1001d, 120000, 1001, "NV12", isHdr: false);
        var otherResolution = CreateFrameRateTimingFormat(mediaFormatType, 1920, 1080, 120, 120, 1, "NV12", isHdr: false);
        var formats = CreateMediaFormatList(mediaFormatType, hdrP010, sdrNv12, sdrMjpg, ntsc119, otherResolution);
        var frameRates = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120000d / 1001d, "120000/1001", isEnabled: true));

        var sdrAuto = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "Auto",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual(false, GetBoolProperty(sdrAuto!, "IsHdr"), "SDR selected format excludes HDR when SDR alternatives exist");
        AssertEqual("NV12", GetStringProperty(sdrAuto!, "PixelFormat"), "4K HFR SDR auto preserves existing source-order tie");

        var hdrAuto = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "Auto",
            isHdrEnabled: true,
            preferredTimingFamilyName: "Integer");
        AssertEqual(true, GetBoolProperty(hdrAuto!, "IsHdr"), "HDR selected format uses HDR candidates");
        AssertEqual("P010", GetStringProperty(hdrAuto!, "PixelFormat"), "HDR selected format keeps P010 candidate");

        var explicitNv12 = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "NV12",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual("NV12", GetStringProperty(explicitNv12!, "PixelFormat"), "explicit selected pixel format narrows candidates");
        AssertEqual(120u, (uint)GetPropertyValue(explicitNv12!, "FrameRateNumerator")!, "integer timing family wins for explicit NV12");

        var ntscPreferred = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120000d / 1001d,
            selectedVideoFormat: "NV12",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Ntsc1001");
        AssertEqual(120000u, (uint)GetPropertyValue(ntscPreferred!, "FrameRateNumerator")!, "friendly bucket selection preserves NTSC timing");

        var unavailablePixelFormat = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "YUY2",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual(null, unavailablePixelFormat, "unavailable explicit pixel format returns no selected format");

        var tupleFormats = InvokeCaptureFormatModeTupleFormats(
                formats,
                frameRates,
                width: 3840,
                height: 2160,
                selectedFrameRate: 120000d / 1001d,
                selectedVideoFormat: "Auto",
                isHdrEnabled: false,
                preferredTimingFamilyName: "Ntsc1001")
            .Cast<object>()
            .ToArray();
        AssertEqual(3, tupleFormats.Length, "friendly 119.88/120 mode tuple includes SDR bucket variants");
        AssertEqual(
            false,
            tupleFormats.Any(format => GetBoolProperty(format, "IsHdr")),
            "mode tuple formats exclude HDR while SDR is selected");

        return Task.CompletedTask;
    }

    private static object? InvokeCaptureFormatSelection(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var request = CreateCaptureFormatSelectionRequest(
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            preferredTimingFamilyName);
        var policyType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionPolicy");
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request });
    }

    private static IEnumerable InvokeCaptureFormatModeTupleFormats(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var request = CreateCaptureFormatSelectionRequest(
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            preferredTimingFamilyName);
        var policyType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionPolicy");
        var selectModeTupleFormats = policyType.GetMethod("SelectModeTupleFormats", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.SelectModeTupleFormats missing.");
        return (IEnumerable)(selectModeTupleFormats.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.SelectModeTupleFormats returned null."));
    }

    private static object CreateCaptureFormatSelectionRequest(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var requestType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionRequest");
        var timingFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", preferredTimingFamilyName);
        var constructor = FindConstructor(requestType, parameterCount: 8);
        return constructor.Invoke(new[]
        {
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            timingFamily
        });
    }
}
