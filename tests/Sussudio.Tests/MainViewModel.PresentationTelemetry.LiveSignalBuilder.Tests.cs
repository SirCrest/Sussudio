using System;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task LiveSignalTextProjection_PreservesPixelFormatFallbackOrder()
    {
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureState.cs")
            .Replace("\r\n", "\n");
        var liveSignalText = ReadRepoFile("Sussudio/ViewModels/ViewModelPresentationBuilders.cs")
            .Replace("\r\n", "\n");
        var builderType = RequireType("Sussudio.ViewModels.LiveSignalTextPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var buildMethod = builderType.GetMethod(
            "Build",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build was not found.");

        AssertContains(runtimeLifecycleControllerText, "_context.UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(runtimeLifecycleControllerText, "_context.ResetLiveCaptureInfo();");
        AssertDoesNotContain(runtimeLifecycleControllerText, "IsAudioPreviewActive =");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private void UpdateLiveCaptureInfo(");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private void ResetLiveCaptureInfo()");
        AssertContains(capturePresentationText, "private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)");
        AssertContains(capturePresentationText, "IsAudioPreviewActive = runtime.IsAudioPreviewActive;");
        AssertContains(capturePresentationText, "var liveSignalText = LiveSignalTextPresentationBuilder.Build(");
        AssertContains(capturePresentationText, "_captureService.EncoderCodecName,");
        AssertContains(capturePresentationText, "LiveInfoUnavailable);");
        AssertContains(capturePresentationText, "LiveResolution = liveSignalText.Resolution;");
        AssertContains(capturePresentationText, "LiveFrameRate = liveSignalText.FrameRate;");
        AssertContains(capturePresentationText, "LivePixelFormat = liveSignalText.PixelFormat;");
        AssertContains(capturePresentationText, "private void ResetLiveCaptureInfo()");
        AssertContains(capturePresentationText, "partial void OnIsPreviewingChanged(bool value)");
        AssertContains(capturePresentationText, "if (!value && !IsRecording)");
        AssertContains(capturePresentationText, "IsAudioPreviewActive = false;");
        AssertContains(capturePresentationText, "LiveResolution = LiveInfoUnavailable;");
        AssertContains(capturePresentationText, "LiveFrameRate = LiveInfoUnavailable;");
        AssertContains(capturePresentationText, "LivePixelFormat = LiveInfoUnavailable;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CapturePresentation.cs")),
            "MainViewModel.CapturePresentation.cs folded into capture state");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.LiveSignalPresentation.cs")),
            "old live signal presentation file removed");
        AssertDoesNotContain(runtimeLifecycleControllerText, "runtime.ReaderSourceSubtype ??");
        AssertDoesNotContain(runtimeLifecycleControllerText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "internal static class LiveSignalTextPresentationBuilder");
        AssertContains(liveSignalText, "internal static LiveSignalTextPresentation Build(");
        AssertContains(liveSignalText, "runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth");
        AssertContains(liveSignalText, "runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight");
        AssertContains(liveSignalText, "runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate");
        AssertContains(liveSignalText, "frameRateValue.Value.ToString(\"0.00\")");
        AssertContains(liveSignalText, "runtime.ReaderSourceSubtype ??");
        AssertContains(liveSignalText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "\"hevc_nvenc\" => \" / HEVC\"");
        AssertContains(liveSignalText, "\"h264_nvenc\" => \" / H264\"");
        AssertContains(liveSignalText, "\"av1_nvenc\" => \" / AV1\"");
        AssertContains(liveSignalText, "? unavailableText");

        if (liveSignalText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) >
            liveSignalText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");
        }

        var actualRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(actualRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(actualRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedWidth", (uint?)1920);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedHeight", (uint?)1080);
        SetPropertyOrBackingField(actualRuntime, "ActualWidth", (uint?)3840);
        SetPropertyOrBackingField(actualRuntime, "ActualHeight", (uint?)2160);
        SetPropertyOrBackingField(actualRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedFrameRate", 59.94d);
        SetPropertyOrBackingField(actualRuntime, "ActualFrameRate", 119.88d);
        SetPropertyOrBackingField(actualRuntime, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(actualRuntime, "RequestedReaderSubtype", "YUY2");
        SetPropertyOrBackingField(actualRuntime, "LatestObservedFramePixelFormat", "P010");
        SetPropertyOrBackingField(actualRuntime, "NegotiatedPixelFormat", "NV12");
        SetPropertyOrBackingField(actualRuntime, "VideoNegotiatedSubtype", "I420");
        SetPropertyOrBackingField(actualRuntime, "ReaderSourceSubtype", "RGB32");

        var actualPresentation = buildMethod.Invoke(null, new object?[] { actualRuntime, "av1_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("3840x2160", GetStringProperty(actualPresentation, "Resolution"), "Live signal actual resolution");
        AssertEqual("119.88", GetStringProperty(actualPresentation, "FrameRate"), "Live signal actual frame rate");
        AssertEqual("RGB32 / AV1", GetStringProperty(actualPresentation, "PixelFormat"), "Live signal reader subtype");

        var fallbackRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create fallback CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(fallbackRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(fallbackRuntime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedPixelFormat", "MJPG");
        var fallbackPresentation = buildMethod.Invoke(null, new object?[] { fallbackRuntime, "hevc_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("1280x720", GetStringProperty(fallbackPresentation, "Resolution"), "Live signal requested resolution fallback");
        AssertEqual("30.00", GetStringProperty(fallbackPresentation, "FrameRate"), "Live signal requested frame-rate fallback");
        AssertEqual("MJPG / HEVC", GetStringProperty(fallbackPresentation, "PixelFormat"), "Live signal requested pixel-format fallback");

        var unavailablePresentation = buildMethod.Invoke(
                null,
                new object?[] { CreateLiveSignalUnavailableRuntime(snapshotType), "libx264", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "Resolution"), "Live signal unavailable resolution");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "FrameRate"), "Live signal unavailable frame-rate");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "PixelFormat"), "Live signal unavailable pixel format");

        return Task.CompletedTask;
    }

    private static object CreateLiveSignalUnavailableRuntime(Type snapshotType)
    {
        var runtime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create unavailable CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(runtime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(runtime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(runtime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(runtime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(runtime, "RequestedPixelFormat", null);
        return runtime;
    }
}
