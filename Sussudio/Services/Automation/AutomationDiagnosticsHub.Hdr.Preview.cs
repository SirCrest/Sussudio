using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewHdrState BuildPreviewHdrState(
        CaptureRuntimeSnapshot captureRuntime,
        ViewModelRuntimeSnapshot viewModelSnapshot,
        PreviewRuntimeSnapshot previewRuntime)
    {
        var inputDetected =
            IsHdrSubtype(captureRuntime.NegotiatedPixelFormat) ||
            (captureRuntime.RequestedHdrEnabled ?? false) ||
            viewModelSnapshot.IsHdrEnabled;
        var toneMapMode = !inputDetected
            ? "None"
            : previewRuntime.GpuActive
                ? "Auto"
                : "Unavailable";

        return new PreviewHdrState(inputDetected, toneMapMode);
    }

    private readonly record struct PreviewHdrState(bool InputDetected, string ToneMapMode);
}
