using System;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Controllers;

internal sealed record PreviewRendererStartupPlan(
    bool UseD3DRenderer,
    int RendererWidth,
    int RendererHeight,
    double RendererFps,
    bool IsHdr,
    double PreviewMinPresentationIntervalMs);

internal static class PreviewRendererStartupPlanBuilder
{
    private const int DefaultWidth = 1920;
    private const int DefaultHeight = 1080;
    private const double DefaultFps = 60.0;

    public static double ResolveExpectedIntervalMs(MediaFormat? selectedFormat)
    {
        var sourceFps = selectedFormat?.FrameRateExact ?? 0;
        if (sourceFps <= 0)
        {
            sourceFps = DefaultFps;
        }

        return ResolveFrameIntervalMs(sourceFps);
    }

    public static PreviewRendererStartupPlan Build(
        bool useD3DRenderer,
        MediaFormat? selectedFormat,
        CaptureSettings? settings,
        VideoSourceProbeResult? sourceProbe)
    {
        if (!useD3DRenderer)
        {
            return new PreviewRendererStartupPlan(
                UseD3DRenderer: false,
                RendererWidth: DefaultWidth,
                RendererHeight: DefaultHeight,
                RendererFps: DefaultFps,
                IsHdr: false,
                PreviewMinPresentationIntervalMs: ResolveExpectedIntervalMs(selectedFormat));
        }

        var isHdr = settings != null && HdrOutputPolicy.IsEnabled(settings);
        var settingsWidth = (int)(settings?.Width ?? DefaultWidth);
        var settingsHeight = (int)(settings?.Height ?? DefaultHeight);
        var settingsFps = settings?.FrameRate ?? DefaultFps;

        var negotiatedWidth = sourceProbe?.SessionActive == true ? sourceProbe.CurrentWidth : 0;
        var negotiatedHeight = sourceProbe?.SessionActive == true ? sourceProbe.CurrentHeight : 0;
        var negotiatedFps = sourceProbe?.SessionActive == true ? sourceProbe.CurrentFrameRate : 0.0;

        var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : settingsWidth;
        var rendererHeight = negotiatedHeight > 0 ? negotiatedHeight : settingsHeight;
        var rendererFps = negotiatedFps > 0 ? negotiatedFps : settingsFps;

        return new PreviewRendererStartupPlan(
            UseD3DRenderer: true,
            RendererWidth: rendererWidth,
            RendererHeight: rendererHeight,
            RendererFps: rendererFps,
            IsHdr: isHdr,
            PreviewMinPresentationIntervalMs: ResolveFrameIntervalMs(rendererFps));
    }

    private static double ResolveFrameIntervalMs(double fps)
        => Math.Max(1.0, 1000.0 / fps);
}
