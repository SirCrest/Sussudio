using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Automation;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Configuration;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture;

public sealed partial class MainWindow
{
    private void OnD3DRendererFirstFrameRendered()
    {
        Logger.Log($"PREVIEW_D3D_FIRST_FRAME attempt={_previewStartupAttemptId ?? "none"}");
        ConfirmPreviewFirstVisual("D3D11FirstFrame");
    }
    private void OnPreviewSwapChainPanelSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        // Composition transform only — overlay sizing is driven by the container.
        var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        _d3dRenderer?.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);
    }
    private void OnPreviewContentGridSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        UpdateVideoContentOverlays();
    }
    private void UpdateVideoContentOverlays()
    {
        var srcW = (double)(ViewModel.SourceWidth ?? 0);
        var srcH = (double)(ViewModel.SourceHeight ?? 0);
        // Use the container (PreviewContentGrid) size, not the SwapChainPanel,
        // because the panel is now explicitly sized to fitW x fitH.
        var dstW = PreviewContentGrid.ActualWidth;
        var dstH = PreviewContentGrid.ActualHeight;

        if (dstW <= 0 || dstH <= 0)
        {
            RecordingGlowBorder.Margin = new Thickness(0);
            if (_videoShadowVisual != null) _videoShadowVisual.Size = Vector2.Zero;
            return;
        }

        double fitW, fitH;
        if (srcW <= 0 || srcH <= 0)
        {
            // Source dimensions unknown — fill the container (same as old Stretch behavior).
            fitW = dstW;
            fitH = dstH;
        }
        else
        {
            var srcAspect = srcW / srcH;
            var dstAspect = dstW / dstH;

            if (srcAspect > dstAspect)
            {
                fitW = dstW;
                fitH = dstW / srcAspect;
            }
            else
            {
                fitH = dstH;
                fitW = dstH * srcAspect;
            }
        }

        // Resize SwapChainPanel to exactly the video content area (no letterbox).
        PreviewSwapChainPanel.Width = fitW;
        PreviewSwapChainPanel.Height = fitH;

        var marginH = (dstW - fitW) / 2;
        var marginV = (dstH - fitH) / 2;
        var videoMargin = new Thickness(marginH, marginV, marginH, marginV);
        RecordingGlowBorder.Margin = videoMargin;

        // Update shadow visual to match the video content rect.
        // VideoShadowHost is a sibling of PreviewBorder — shadow casts onto app background.
        if (_videoShadowVisual != null)
        {
            const float borderMarginH = 12f; // PreviewBorder Margin left/right
            const float borderMarginV = 6f;  // PreviewBorder Margin top/bottom
            const float hostMargin = 16f;    // PreviewShadowHost Margin
            _videoShadowVisual.Offset = new Vector3(
                borderMarginH + hostMargin + (float)marginH,
                borderMarginV + hostMargin + (float)marginV, 0);
            _videoShadowVisual.Size = new Vector2(Math.Max(0, (float)fitW), Math.Max(0, (float)fitH));
        }
    }
    private void SetupVideoFrameShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(VideoShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 16;
        shadow.Color = Windows.UI.Color.FromArgb(160, 0, 0, 0);
        shadow.Offset = new Vector3(0, 2, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;

        spriteVisual.Opacity = 0f; // Start invisible — faded in with preview entrance
        _videoShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(VideoShadowHost, spriteVisual);
    }
    private void SetupControlBarShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(ControlBarShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 12;
        shadow.Color = Windows.UI.Color.FromArgb(120, 0, 0, 0);
        shadow.Offset = new Vector3(0, 1, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;
        spriteVisual.Opacity = 0f; // Start invisible — faded in with control bar entrance

        _controlBarShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(ControlBarShadowHost, spriteVisual);

        // Track control bar size changes to keep the shadow aligned.
        ControlBarBorder.SizeChanged += (s, e) =>
        {
            if (_controlBarShadowVisual == null) return;
            var margin = ControlBarBorder.Margin;
            _controlBarShadowVisual.Offset = new Vector3((float)margin.Left, (float)margin.Top, 0);
            _controlBarShadowVisual.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        };
    }
    private void SetGpuPreviewVisibility(Visibility visibility)
    {
        // PreviewLetterboxBackground stays Collapsed — letterbox areas must be
        // transparent so the Composition DropShadow is visible around the video.
        PreviewSwapChainPanel.Visibility = visibility;
    }
    private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()
    {
        var d3d = _d3dRenderer;
        var nowTick = Environment.TickCount64;
        var framesArrived = Interlocked.Read(ref _previewFramesArrived);
        var framesDisplayed = Interlocked.Read(ref _previewFramesDisplayed);
        var framesDropped = Interlocked.Read(ref _previewFramesDropped);
        var lastPresentedTick = Interlocked.Read(ref _previewLastPresentedTick);
        var gpuActive = d3d != null;
        var gpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible;
        var cpuElementVisible = PreviewImage.Visibility == Visibility.Visible;
        var rendererAttached = d3d != null || _previewSource != null;
        var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
        var previewPipelineActive = ViewModel.IsPreviewing && rendererAttached;
        var d3dFramesSubmitted = d3d?.FramesSubmitted ?? 0;
        var d3dFramesRendered = d3d?.FramesRendered ?? 0;
        var d3dFramesDropped = d3d?.FramesDropped ?? 0;
        var d3dRenderCpuTiming = d3d?.GetRenderCpuTimingMetrics();
        var d3dFrameOwnership = d3d?.GetFrameOwnershipMetrics();
        var d3dFrameStats = d3d?.GetDxgiFrameStatisticsMetrics();
        var d3dFrameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();
        var d3dPipelineLatency = d3d?.GetPipelineLatencyMetrics();
        var d3dSlowFrames = d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>();
        if (gpuActive)
        {
            framesArrived = d3dFramesSubmitted;
            framesDisplayed = d3dFramesRendered;
            framesDropped = d3dFramesDropped;
        }

        var rendererMode = d3d?.RendererMode
            ?? (ViewModel.IsPreviewing ? "CpuSoftwareBitmap" : "None");
        var gpuPlaybackState = "None";
        int gpuNaturalVideoWidth = 0, gpuNaturalVideoHeight = 0;
        double gpuPositionMs = 0;
        if (d3d != null)
        {
            gpuPlaybackState = d3d.IsRendering ? "Rendering" : "Idle";
            gpuNaturalVideoWidth = d3d.NaturalWidth;
            gpuNaturalVideoHeight = d3d.NaturalHeight;
            gpuPositionMs = 0;
        }
        var gpuPositionEventCount = Interlocked.Read(ref _previewStartupPositionEventCount);

        var startupElapsedMs = _previewStartupRequestedUtc.HasValue
            ? Math.Max(0, (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds)
            : (double?)null;
        var startupMissingSignals = _previewStartupMissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            _previewStartupState is PreviewStartupState.WaitingForFirstVisual or PreviewStartupState.Failed)
        {
            startupMissingSignals = BuildPreviewStartupMissingSignals();
        }
        var startupTimedOut = ViewModel.IsPreviewing &&
                              _previewStartupState == PreviewStartupState.WaitingForFirstVisual &&
                              startupElapsedMs.GetValueOrDefault() >= PreviewStartupVisualTimeoutMs;
        var blankSuspected = !gpuActive && previewPipelineActive &&
                             framesArrived > 30 &&
                             framesDisplayed == 0;
        if (!blankSuspected && startupTimedOut)
        {
            blankSuspected = true;
        }
        var stallSuspected = !gpuActive && previewPipelineActive &&
                             lastPresentedTick > 0 &&
                             nowTick - lastPresentedTick > 3000;
        var rendererCadence = d3d?.GetPresentCadenceMetrics(_previewMinPresentationIntervalMs);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsPreviewing = ViewModel.IsPreviewing,
            GpuActive = gpuActive,
            PlaceholderVisible = placeholderVisible,
            GpuElementVisible = gpuElementVisible,
            CpuElementVisible = cpuElementVisible,
            RendererAttached = rendererAttached,
            StartupState = _previewStartupState.ToString(),
            StartupAttemptId = _previewStartupAttemptId,
            StartupElapsedMs = startupElapsedMs,
            StartupTimeoutMs = PreviewStartupVisualTimeoutMs,
            StartupGpuSignalMediaOpened = _previewGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = _previewGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = _previewGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = _previewStartupRequiredSignals,
            StartupReceivedSignals = _previewStartupReceivedSignals,
            StartupStrategy = _previewStartupStrategy,
            StartupMissingSignals = startupMissingSignals,
            StartupRecoveryAttemptCount = _previewRecoveryAttemptCount,
            StartupLastFailureReason = _previewLastFailureReason,
            FirstVisualConfirmed = _previewFirstVisualConfirmed,
            FramesArrived = framesArrived,
            FramesDisplayed = framesDisplayed,
            FramesDropped = framesDropped,
            DisplayCadenceSampleCount = rendererCadence?.SampleCount ?? 0,
            DisplayCadenceObservedFps = rendererCadence?.ObservedFps ?? 0,
            DisplayCadenceExpectedIntervalMs = rendererCadence?.ExpectedIntervalMs ?? 0,
            DisplayCadenceAverageIntervalMs = rendererCadence?.AverageIntervalMs ?? 0,
            DisplayCadenceP95IntervalMs = rendererCadence?.P95IntervalMs ?? 0,
            DisplayCadenceP99IntervalMs = rendererCadence?.P99IntervalMs ?? 0,
            DisplayCadenceMaxIntervalMs = rendererCadence?.MaxIntervalMs ?? 0,
            DisplayCadenceOnePercentLowFps = rendererCadence?.OnePercentLowFps ?? 0,
            DisplayCadenceJitterStdDevMs = rendererCadence?.JitterStdDevMs ?? 0,
            DisplayCadenceSlowFrameCount = rendererCadence?.SlowFrameCount ?? 0,
            DisplayCadenceSlowFramePercent = rendererCadence?.SlowFramePercent ?? 0,
            BlankSuspected = blankSuspected,
            StallSuspected = stallSuspected,
            RendererMode = rendererMode,
            D3DPresentSyncInterval = d3d?.PresentSyncInterval ?? 0,
            D3DMaxFrameLatency = d3d?.DxgiMaxFrameLatency ?? 0,
            D3DSwapChainBufferCount = d3d?.SwapChainBufferCount ?? 0,
            D3DSwapChainAddress = d3d?.SwapChainAddress ?? string.Empty,
            D3DFramesSubmitted = d3dFramesSubmitted,
            D3DFramesRendered = d3dFramesRendered,
            D3DFramesDropped = d3dFramesDropped,
            D3DPendingFrameCount = d3d?.PendingFrameCount ?? 0,
            D3DInputColorSpace = _d3dRenderer?.InputColorSpaceLabel ?? "None",
            D3DOutputColorSpace = _d3dRenderer?.OutputColorSpaceLabel ?? "None",
            D3DCpuTimingSampleCount = d3dRenderCpuTiming?.TotalFrame.SampleCount ?? 0,
            D3DInputUploadCpuAvgMs = d3dRenderCpuTiming?.InputUpload.AverageMs ?? 0,
            D3DInputUploadCpuP95Ms = d3dRenderCpuTiming?.InputUpload.P95Ms ?? 0,
            D3DInputUploadCpuP99Ms = d3dRenderCpuTiming?.InputUpload.P99Ms ?? 0,
            D3DInputUploadCpuMaxMs = d3dRenderCpuTiming?.InputUpload.MaxMs ?? 0,
            D3DRenderSubmitCpuAvgMs = d3dRenderCpuTiming?.RenderSubmit.AverageMs ?? 0,
            D3DRenderSubmitCpuP95Ms = d3dRenderCpuTiming?.RenderSubmit.P95Ms ?? 0,
            D3DRenderSubmitCpuP99Ms = d3dRenderCpuTiming?.RenderSubmit.P99Ms ?? 0,
            D3DRenderSubmitCpuMaxMs = d3dRenderCpuTiming?.RenderSubmit.MaxMs ?? 0,
            D3DPresentCallAvgMs = d3dRenderCpuTiming?.PresentCall.AverageMs ?? 0,
            D3DPresentCallP95Ms = d3dRenderCpuTiming?.PresentCall.P95Ms ?? 0,
            D3DPresentCallP99Ms = d3dRenderCpuTiming?.PresentCall.P99Ms ?? 0,
            D3DPresentCallMaxMs = d3dRenderCpuTiming?.PresentCall.MaxMs ?? 0,
            D3DTotalFrameCpuAvgMs = d3dRenderCpuTiming?.TotalFrame.AverageMs ?? 0,
            D3DTotalFrameCpuP95Ms = d3dRenderCpuTiming?.TotalFrame.P95Ms ?? 0,
            D3DTotalFrameCpuP99Ms = d3dRenderCpuTiming?.TotalFrame.P99Ms ?? 0,
            D3DTotalFrameCpuMaxMs = d3dRenderCpuTiming?.TotalFrame.MaxMs ?? 0,
            D3DPipelineLatencySampleCount = d3dPipelineLatency?.SampleCount ?? 0,
            D3DPipelineLatencyAvgMs = d3dPipelineLatency?.AverageMs ?? 0,
            D3DPipelineLatencyP95Ms = d3dPipelineLatency?.P95Ms ?? 0,
            D3DPipelineLatencyP99Ms = d3dPipelineLatency?.P99Ms ?? 0,
            D3DPipelineLatencyMaxMs = d3dPipelineLatency?.MaxMs ?? 0,
            D3DFrameLatencyWaitEnabled = d3dFrameLatencyWait?.Enabled ?? false,
            D3DFrameLatencyWaitHandleActive = d3dFrameLatencyWait?.HandleActive ?? false,
            D3DFrameLatencyWaitCallCount = d3dFrameLatencyWait?.CallCount ?? 0,
            D3DFrameLatencyWaitSignaledCount = d3dFrameLatencyWait?.SignaledCount ?? 0,
            D3DFrameLatencyWaitTimeoutCount = d3dFrameLatencyWait?.TimeoutCount ?? 0,
            D3DFrameLatencyWaitUnexpectedResultCount = d3dFrameLatencyWait?.UnexpectedResultCount ?? 0,
            D3DFrameLatencyWaitLastResult = d3dFrameLatencyWait?.LastResult ?? 0,
            D3DFrameLatencyWaitLastMs = d3dFrameLatencyWait?.LastWaitMs ?? 0,
            D3DFrameLatencyWaitSampleCount = d3dFrameLatencyWait?.Timing.SampleCount ?? 0,
            D3DFrameLatencyWaitAvgMs = d3dFrameLatencyWait?.Timing.AverageMs ?? 0,
            D3DFrameLatencyWaitP95Ms = d3dFrameLatencyWait?.Timing.P95Ms ?? 0,
            D3DFrameLatencyWaitP99Ms = d3dFrameLatencyWait?.Timing.P99Ms ?? 0,
            D3DFrameLatencyWaitMaxMs = d3dFrameLatencyWait?.Timing.MaxMs ?? 0,
            D3DFrameStatsSampleCount = d3dFrameStats?.SampleCount ?? 0,
            D3DFrameStatsSuccessCount = d3dFrameStats?.SuccessCount ?? 0,
            D3DFrameStatsFailureCount = d3dFrameStats?.FailureCount ?? 0,
            D3DFrameStatsLastError = d3dFrameStats?.LastError ?? string.Empty,
            D3DFrameStatsPresentCount = d3dFrameStats?.PresentCount ?? -1,
            D3DFrameStatsPresentRefreshCount = d3dFrameStats?.PresentRefreshCount ?? -1,
            D3DFrameStatsSyncRefreshCount = d3dFrameStats?.SyncRefreshCount ?? -1,
            D3DFrameStatsSyncQpcTime = d3dFrameStats?.SyncQpcTime ?? 0,
            D3DFrameStatsLastPresentDelta = d3dFrameStats?.LastPresentDelta ?? 0,
            D3DFrameStatsLastPresentRefreshDelta = d3dFrameStats?.LastPresentRefreshDelta ?? 0,
            D3DFrameStatsLastSyncRefreshDelta = d3dFrameStats?.LastSyncRefreshDelta ?? 0,
            D3DFrameStatsMissedRefreshCount = d3dFrameStats?.MissedRefreshCount ?? 0,
            D3DLastSubmittedPreviewPresentId = d3dFrameOwnership?.LastSubmittedPreviewPresentId ?? 0,
            D3DLastSubmittedSourceSequenceNumber = d3dFrameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,
            D3DLastSubmittedQpc = d3dFrameOwnership?.LastSubmittedQpc ?? 0,
            D3DLastSubmittedUtcUnixMs = d3dFrameOwnership?.LastSubmittedUtcUnixMs ?? 0,
            D3DLastRenderedPreviewPresentId = d3dFrameOwnership?.LastRenderedPreviewPresentId ?? 0,
            D3DLastRenderedSourceSequenceNumber = d3dFrameOwnership?.LastRenderedSourceSequenceNumber ?? -1,
            D3DLastRenderedQpc = d3dFrameOwnership?.LastRenderedQpc ?? 0,
            D3DLastRenderedUtcUnixMs = d3dFrameOwnership?.LastRenderedUtcUnixMs ?? 0,
            D3DLastRenderedSchedulerToPresentMs = d3dFrameOwnership?.LastRenderedSchedulerToPresentMs ?? 0,
            D3DLastRenderedPipelineLatencyMs = d3dFrameOwnership?.LastRenderedPipelineLatencyMs ?? 0,
            D3DLastDroppedPreviewPresentId = d3dFrameOwnership?.LastDroppedPreviewPresentId ?? 0,
            D3DLastDroppedSourceSequenceNumber = d3dFrameOwnership?.LastDroppedSourceSequenceNumber ?? -1,
            D3DLastDroppedQpc = d3dFrameOwnership?.LastDroppedQpc ?? 0,
            D3DLastDroppedUtcUnixMs = d3dFrameOwnership?.LastDroppedUtcUnixMs ?? 0,
            D3DLastDropReason = d3dFrameOwnership?.LastDropReason ?? string.Empty,
            D3DRecentSlowFrames = d3dSlowFrames,
            EstimatedPipelineLatencyMs = d3dPipelineLatency?.AverageMs ?? 0,
            GpuPlaybackState = gpuPlaybackState,
            GpuNaturalVideoWidth = gpuNaturalVideoWidth,
            GpuNaturalVideoHeight = gpuNaturalVideoHeight,
            GpuPositionMs = gpuPositionMs,
            GpuPositionEventCount = gpuPositionEventCount
        };
    }
    private void CleanupPreviewResources()
    {
        // Clean up composition shadow
        _videoShadowVisual = null;
        ElementCompositionPreview.SetElementChildVisual(VideoShadowHost, null);

        // Clean up D3D11 preview
        PreviewContentGrid.SizeChanged -= OnPreviewContentGridSizeChanged;
        var renderer = _d3dRenderer;
        _d3dRenderer = null;
        if (renderer != null)
        {
            PreviewSwapChainPanel.SizeChanged -= OnPreviewSwapChainPanelSizeChanged;
            renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;
            renderer.Stop();
            renderer.Dispose();
        }
        ViewModel.SetPreviewFrameSink(null);
        SetGpuPreviewVisibility(Visibility.Collapsed);
        ResetPreviewSignalState();

        // Clean up CPU preview
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;
    }
    private void StopPreviewForShutdown()
    {
        _isPreviewReinitAnimating = false;
        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(StopPreviewForShutdown)}");
        StopPreviewFadeInTimer();
        ResetPreviewContentTransform();
        CleanupPreviewResources();
    }
    private Task StartPreviewRendererAsync()
    {
        // Observability: detect the unsafe window where a new renderer could be allocated
        // while the prior instance still has an active swap chain (see reasoning_d3d11_preview.md).
        var prevRenderer = _d3dRenderer;
        var reinitAnimating = _isPreviewReinitAnimating;
        if (prevRenderer != null && !reinitAnimating)
        {
            var prevSwapChainAddress = prevRenderer.SwapChainAddress;
            var prevIsRendering = prevRenderer.IsRendering;
            var timeSinceStopMs = Environment.TickCount64 - Interlocked.Read(ref _lastRendererStopTick);
            Interlocked.Increment(ref _rendererReinitUnsafeWindows);
            Logger.Log($"D3D11_RENDERER_REINIT_UNSAFE_WINDOW prev_present={prevSwapChainAddress} reinit_animating={reinitAnimating} prev_rendering={prevIsRendering} time_since_last_stop_ms={timeSinceStopMs}");
        }

        _previewFramesArrived = 0;
        _previewFramesDisplayed = 0;
        _previewFramesDropped = 0;
        _previewLastResizeLogTick = 0;
        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();

        var useD3dRenderer = ViewModel.IsPreviewing;
        if (useD3dRenderer)
        {
            var settings = ViewModel.BuildCurrentSettings();
            var sourceProbe = ViewModel.ProbeVideoSource();
            var isHdr = settings != null && HdrOutputPolicy.IsEnabled(settings);
            var width = (int)(settings?.Width ?? 1920);
            var height = (int)(settings?.Height ?? 1080);
            var fps = settings?.FrameRate ?? 60.0;
            var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;
            var negotiatedHeight = sourceProbe.SessionActive ? sourceProbe.CurrentHeight : 0;
            var negotiatedFps = sourceProbe.SessionActive ? sourceProbe.CurrentFrameRate : 0.0;
            var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;
            var rendererHeight = negotiatedHeight > 0 ? negotiatedHeight : height;
            var rendererFps = negotiatedFps > 0 ? negotiatedFps : fps;
            _previewMinPresentationIntervalMs = Math.Max(1.0, 1000.0 / rendererFps);

            // Reuse the existing renderer during reinit to avoid recreating the
            // ISwapChainPanelNative COM wrapper. WinUI 3 corrupts the panel's
            // native backing when SetSwapChain is called from a different renderer
            // instance — even with proper unbind/rebind ordering. Reusing the same
            // instance keeps the COM wrapper stable across Stop→Start cycles.
            var reusingRenderer = _d3dRenderer != null && _isPreviewReinitAnimating;
            D3D11PreviewRenderer renderer;
            if (reusingRenderer)
            {
                renderer = _d3dRenderer!;
                Logger.Log("PREVIEW_REINIT_RENDERER_REUSE: reusing existing renderer instance");
            }
            else
            {
                renderer = new D3D11PreviewRenderer(PreviewSwapChainPanel, _dispatcherQueue);
                renderer.FirstFrameRendered += OnD3DRendererFirstFrameRendered;
                _d3dRenderer = renderer;
            }

            renderer.SetExpectedFrameRate(rendererFps);

            if (!reusingRenderer)
            {
                // Wire SizeChanged and make panel visible BEFORE starting the render
                // thread so the renderer has the panel's pixel dimensions from the start.
                SetupVideoFrameShadow();
                PreviewSwapChainPanel.SizeChanged += OnPreviewSwapChainPanelSizeChanged;
                PreviewContentGrid.SizeChanged += OnPreviewContentGridSizeChanged;
            }
            SetGpuPreviewVisibility(Visibility.Visible);
            PreviewImage.Visibility = Visibility.Collapsed;

            // Pre-seed panel size and renderer dimensions.
            // Force layout so the container has ActualWidth/Height, then
            // UpdateVideoContentOverlays sets the panel to fitW x fitH.
            PreviewSwapChainPanel.UpdateLayout();
            UpdateVideoContentOverlays();
            PreviewSwapChainPanel.UpdateLayout();
            var panelW = PreviewSwapChainPanel.ActualWidth;
            var panelH = PreviewSwapChainPanel.ActualHeight;
            if (panelW > 0 && panelH > 0)
            {
                var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
                renderer.OnPanelSizeChanged(panelW, panelH, scale);
            }

            renderer.Start(rendererWidth, rendererHeight, rendererFps, isHdr);
            if (isHdr && ViewModel.IsTrueHdrPreviewEnabled)
            {
                renderer.SetHdrPassthroughEnabled(true);
            }

            ViewModel.SetPreviewFrameSink(_d3dRenderer);
            _previewStartupExpectGpuDualSignals = false; // D3D renderer uses FirstFrameRendered, not MediaPlayer signals
            ConfigurePreviewStartupSignals(
                PreviewStartupStrategy.D3D11VideoProcessor,
                PreviewStartupSignalFlags.FirstVisual);
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;

            Logger.Log("Preview renderer started (mode=D3D11VideoProcessor).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=D3D11VideoProcessor attempt={_previewStartupAttemptId ?? "none"}");
        }
        else
        {
            // Fallback CPU preview path: SoftwareBitmapSource -> Image (unchanged)
            SetupVideoFrameShadow();
            PreviewContentGrid.SizeChanged += OnPreviewContentGridSizeChanged;
            ViewModel.SetPreviewFrameSink(null);
            _previewStartupExpectGpuDualSignals = false;
            ConfigurePreviewStartupSignals(PreviewStartupStrategy.CpuSoftwareBitmap, PreviewStartupSignalFlags.FirstVisual);
            _previewSource = new SoftwareBitmapSource();
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;
            PreviewImage.Source = _previewSource;
            PreviewImage.Visibility = Visibility.Visible;
            SetGpuPreviewVisibility(Visibility.Collapsed);
            Logger.Log($"Preview renderer started (mode=CpuSoftwareBitmap, expectedIntervalMs={_previewMinPresentationIntervalMs:0.###}).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=CpuSoftwareBitmap attempt={_previewStartupAttemptId ?? "none"}");
        }

        return Task.CompletedTask;
    }
    private Task StopPreviewRendererAsync()
    {
        CleanupPreviewResources();
        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = 1000.0 / 60.0;
        Interlocked.Exchange(ref _lastRendererStopTick, Environment.TickCount64);
        Logger.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }
}
